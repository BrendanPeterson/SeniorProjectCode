using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aircraft
{ 
    public class AircraftArea : MonoBehaviour
    {
        [Tooltip ("The path the race will take")]
        public CinemachineSmoothPath racePath;

        [Tooltip("The Prefab to use for checkpoints")]
        public GameObject checkpointPrefab;

        [Tooltip("The Prefab to use for the start/End checkpoint")]
        public GameObject finishCheckpointPrefab;

        [Tooltip("If true, enable training mode")]
        public bool trainingMode;

        public List<AircraftAgent> AircraftAgents { get; private set; }

        public List<GameObject> Checkpoints { get; private set; }

        public AircraftAcademy AircraftAcademy { get; private set; }

        //<summary>
        //Actions to perform when the script wakes up
        //</summary>

        private void Awake()
        {
            {
                //Find all aircraft agents in the area
                AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();
                Debug.Assert(AircraftAgents.Count > 0, "No AircraftAgents found");

                //Finds aircraftAcademys for us so we dont have to enter the manually
                AircraftAcademy = FindObjectOfType<AircraftAcademy>();
            }
        }

        //<summary>
        //Set Up area
        //</summary>

        private void Start()
        {
            //Create checkpoints along the race path
            Debug.Assert(racePath != null, "Race Path was not set");

            //Finds number of points on cinemachine path
            Checkpoints = new List<GameObject>();
            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
            for (int i = 0; i < numCheckpoints; i++)
            {
                //Instantiate either a checkpoint or finish line checkpoint on cinemachine path
                GameObject checkpoint;
                if (i == numCheckpoints - 1) checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                else checkpoint = Instantiate<GameObject>(checkpointPrefab);

                //Set Parent Position and Rotation
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                //Add the checkpoint to the list
                Checkpoints.Add(checkpoint);
            }
        }
        //<summary>
        //Resets agent posiiton if they crash using its current Next checkpoint Index
        //If randomize is true it resets the plane at  a random checkpoint
        //</summary>
        public void ResetAgentPosition(AircraftAgent agent, bool randomize = false)
        {
            if (randomize)
            {
                //Picka a new next checkpoint at  random
                agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
            }

            //Set start position to the previous checkpoint
            int previousCheckpointIndex = agent.NextCheckpointIndex - 1;
            if (previousCheckpointIndex == -1) previousCheckpointIndex = Checkpoints.Count - 1;

            float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

            //Convert the position on the race path to a position in 3d space;
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);

            //Get orientation at that position oin the race path
            Quaternion orientation = racePath.EvaluateOrientation(startPosition);

            //Calculate a horizontal offset so that agents are spread out
            //Calcualtes based on number of agents and current agent how far away another agent needs to spawn
            //Make it random so that the game isnt exactly the same
            Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f) 
                * UnityEngine.Random.Range(5f, 10f);

            //Set the aircraft position and rotation
            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;
        }

    }
}
