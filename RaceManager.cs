﻿using Barracuda;
using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class RaceManager : MonoBehaviour
    {
        [Tooltip("Number of laps for this race")]
        public int numLaps = 2;

        [Tooltip("Bonus seconds to give upon reaching checkpoint")]
        public float checkpointBonusTime = 15f;

        [Serializable]
        public struct DifficultyModel
        {
            public GameDifficulty difficulty;
            public NNModel model;
        }
        public List<DifficultyModel> difficultyModels;

        public AircraftAgent FollowAgent { get; private set; }

        public Camera ActiveCamera { get; private set; }

        private CinemachineVirtualCamera virtualCamera;
        private CountdownUIController countdownUI;
        private PauseMenuController pauseMenu;
        private HUDController hud;
        private GameOverUIController gameoverUI;
        private AircraftArea aircraftArea;
        private AircraftPlayer aircraftPlayer;
        private List<AircraftAgent> sortedAircraftAgents;

        //Pause Tiemrs
        private float lastResumeTime = 0f;
        private float previouslyElapsedTime = 0f;

        //When we last checked positions
        private float lastPlaceUpdate = 0f;
        private Dictionary<AircraftAgent, AircraftStatus> aircraftStatuses;

        private class AircraftStatus
        {
            public int checkpointIndex = 0;
            public int lap = 0;
            public int place = 0;
            public float timeRemaining = 0f;
        }

        /// <summary>
        /// The clock keeping track of race time (considering pausing)
        /// </summary>
        public float RaceTime
        {
            get
            {
                if(GameManager.Instance.GameState == GameState.Playing)
                {
                    return previouslyElapsedTime + Time.time - lastResumeTime;
                }
                else if (GameManager.Instance.GameState == GameState.Paused)
                {
                    return previouslyElapsedTime;
                }
                else
                {
                    return 0f;
                }
            }
        }

        /// <summary>
        /// Get the agents next checkpoints transform
        /// </summary>
        /// <param name="a">The agent</param>
        /// <returns>returns the transform of the next checkpoint the agent should got to</returns>
        public Transform GetAgentNextCheckpoint(AircraftAgent agent)
        {
            return aircraftArea.Checkpoints[aircraftStatuses[agent].checkpointIndex].transform;
        }

        /// <summary>
        /// Get the agents lap
        /// </summary>
        /// <param name="agent">The Agent</param>
        /// <returns>The lap the agent is on</returns>
        public int getAgentLap(AircraftAgent agent)
        {
            return aircraftStatuses[agent].lap;
        }

        /// <summary>
        /// Gets the race place for an agent
        /// </summary>
        /// <param name="agent">The agent</param>
        /// <returns>what place the agent is in (1st,2nd, 3rd...)</returns>
        public string GetAgentPlace(AircraftAgent agent)
        {
            int place = aircraftStatuses[agent].place;
            if(place <= 0)
            {
                return string.Empty;
            }

            if (place >= 11 && place <= 13) return place.ToString() + "th";
            switch(place % 10)
            {
                case 1:
                    return place.ToString() + "st";
                case 2:
                    return place.ToString() + "nd";
                case 3:
                    return place.ToString() + "rd";
                default:
                    return place.ToString() + "th";
            }
        }

        public float GetAgentTime(AircraftAgent agent)
        {
            return aircraftStatuses[agent].timeRemaining;
        }

        private void Awake()
        {
            hud = FindObjectOfType <HUDController>();
            countdownUI = FindObjectOfType<CountdownUIController>();
            pauseMenu = FindObjectOfType<PauseMenuController>();
            gameoverUI = FindObjectOfType<GameOverUIController>();
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            aircraftArea = FindObjectOfType<AircraftArea>();
            ActiveCamera = FindObjectOfType<Camera>();
        }

        /// <summary>
        /// initial setup and start race
        /// </summary>
        private void Start()
        {
            GameManager.Instance.OnStateChange += OnStateChange;

            //Choose a default agent for the camera to follow in case we can't find a player
            FollowAgent = aircraftArea.AircraftAgents[0];
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                agent.FreezeAgent();
                if(agent.GetType() == typeof(AircraftPlayer))
                {
                    //Found the player, follow it
                    FollowAgent = agent;
                    aircraftPlayer = (AircraftPlayer)agent;
                    aircraftPlayer.pauseInput.performed += PauseInputPerformed;
                }
                else
                {
                    //set the difficulty
                    agent.GiveModel(GameManager.Instance.GameDifficulty.ToString(),
                        difficultyModels.Find(x => x.difficulty == GameManager.Instance.GameDifficulty).model);
                }
            }

            //Tell the camera and HUD what to follow
            Debug.Assert(virtualCamera != null, "Virtual camera was not specified");
            virtualCamera.Follow = FollowAgent.transform;
            virtualCamera.LookAt = FollowAgent.transform;
            hud.FollowAgent = FollowAgent;

            //Hide UI
            hud.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive(false);
            countdownUI.gameObject.SetActive(false);
            gameoverUI.gameObject.SetActive(false);

            //Start the Race
            StartCoroutine(StartRace());


        }

        /// <summary>
        /// Starts a countdown at the beginning of the race
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator StartRace()
        {
            //Show Countdown
            countdownUI.gameObject.SetActive(true);
            yield return countdownUI.StartCountdown();

            //Initialize agent status tracking
            aircraftStatuses = new Dictionary<AircraftAgent, AircraftStatus>();
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                AircraftStatus status = new AircraftStatus();
                status.lap = 1;
                status.timeRemaining = checkpointBonusTime;
                aircraftStatuses.Add(agent, status);
            }

            //Begin Playing
            GameManager.Instance.GameState = GameState.Playing;
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        /// <param name="obj">callback context</param>
        private void PauseInputPerformed(InputAction.CallbackContext obj)
        {
            if(GameManager.Instance.GameState == GameState.Playing)
            {
                GameManager.Instance.GameState = GameState.Paused;
                pauseMenu.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// React to State Change
        /// </summary>
        private void OnStateChange()
        {
            if ( GameManager.Instance.GameState == GameState.Playing)
            {
                //Start/Resume game time, show hud, thaw agnets
                lastResumeTime = Time.time;
                hud.gameObject.SetActive(true);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.ThawAgent();
            }
            else if (GameManager.Instance.GameState == GameState.Paused)
            {
                //Pause the game time, freeze the agents
                previouslyElapsedTime += Time.time - lastResumeTime;
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.FreezeAgent();
            }
            else if (GameManager.Instance.GameState == GameState.GameOver)
            {
                //Pause GameTime, hide HUDD, FREEZE Agents
                previouslyElapsedTime += Time.time - lastResumeTime;
                hud.gameObject.SetActive(false);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.FreezeAgent();

                //Show Game Over Screen
                gameoverUI.gameObject.SetActive(true);
            }
            else
            {
                //Reset time
                lastResumeTime = 0f;
                previouslyElapsedTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if ( GameManager.Instance.GameState == GameState.Playing)
            {
                //Update the place list every half second
                if (lastPlaceUpdate + .5f < Time.fixedTime)
                {
                    lastPlaceUpdate = Time.fixedTime;

                    if (sortedAircraftAgents == null)
                    {
                        //Get a copy of the list of agents for sorting
                        sortedAircraftAgents = new List<AircraftAgent>(aircraftArea.AircraftAgents);
                    }

                    //Recalculate race places
                    sortedAircraftAgents.Sort((a,b) => PlaceCompare(a,b));
                    for (int i = 0; i < sortedAircraftAgents.Count; i++)
                    {
                        aircraftStatuses[sortedAircraftAgents[i]].place = i + 1;
                    }
                }

                //Update agent statuses
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
                {
                    AircraftStatus status = aircraftStatuses[agent];

                    //Update Agetn lap
                    if (status.checkpointIndex != agent.NextCheckpointIndex)
                    {
                        status.checkpointIndex = agent.NextCheckpointIndex;
                        status.timeRemaining = checkpointBonusTime;

                        if (status.checkpointIndex == 0)
                        {
                            status.lap++;
                            if(agent == FollowAgent && status.lap > numLaps)
                            {
                                GameManager.Instance.GameState = GameState.GameOver;
                            }
                        }
                    }

                    //Update agent time remaining
                    status.timeRemaining = Mathf.Max(0f, status.timeRemaining - Time.fixedDeltaTime);
                    if (status.timeRemaining == 0f)
                    {
                        aircraftArea.ResetAgentPosition(agent);
                        status.timeRemaining = checkpointBonusTime;
                    }
                }
            }
        }


        /// <summary>
        /// Compares the reace place (1st, 2nd, 3rd ...)
        /// </summary>
        /// <param name="a">An agent</param>
        /// <param name="b">another agent</param>
        /// <returns>returns -1 if a is before b, 0 if equal, 1 if b is before a</returns>
        private int PlaceCompare(AircraftAgent a, AircraftAgent b)
        {
            AircraftStatus statusA = aircraftStatuses[a];
            AircraftStatus statusB = aircraftStatuses[b];
            int checkpointA = statusA.checkpointIndex + (statusA.lap - 1) * aircraftArea.Checkpoints.Count;
            int checkpointB = statusB.checkpointIndex + (statusB.lap - 1) * aircraftArea.Checkpoints.Count;
            if ( checkpointA == checkpointB)
            {
                //If on same checkpoint compare distance to next checkpoint
                Vector3 nextCheckpointPosition = GetAgentNextCheckpoint(a).position;
                int compare = Vector3.Distance(a.transform.position, nextCheckpointPosition)
                    .CompareTo(Vector3.Distance(b.transform.position, nextCheckpointPosition));
                return compare;
            }
            else
            {
                //Compare num of checkpoints hit. The agent with more checkpoints is ahead
                //So we flip the compare
                int compare = -1 * checkpointA.CompareTo(checkpointB);
                return compare;
            }

        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChange -= OnStateChange;
            if (aircraftPlayer != null) aircraftPlayer.pauseInput.performed -= PauseInputPerformed;
        }
    }

   
}
