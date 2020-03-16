using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aircraft
{
    public class HUDController : MonoBehaviour
    {
        [Tooltip("The Place in the rac (eg 1st)")]
        public TextMeshProUGUI placeText;

        [Tooltip("Seconds remianing to reach the next checkpoint (eg Time 9.3)")]
        public TextMeshProUGUI timeText;


        [Tooltip("The Current Lap (eg Lap 2)")]
        public TextMeshProUGUI lapText;

        [Tooltip("The Icon Indicating where the next checkpoint is")]
        public Image checkPointIcon;

        [Tooltip("The arrow pointing towards the next checkpoint")]
        public Image checkPointArrow;

        [Tooltip("When to show an arrow toward the checkpoint rather than the icon centered on it")]
        public float indicatorLimit = .7f;

        /// <summary>
        /// The Agent this HUD shows info for
        /// </summary>
        public AircraftAgent FollowAgent { get; internal set; }

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void Update()
        {
            if (FollowAgent != null)
            {
                UpdatePlaceText();
                UpdateTimeText();
                UpdateLapText();
                UpdateArrowText();
            }
        }

        private void UpdatePlaceText()
        {
            string place = raceManager.GetAgentPlace(FollowAgent);
            placeText.text = place;
        }

        private void UpdateTimeText()
        {
            float time = raceManager.GetAgentTime(FollowAgent);
            timeText.text ="Time " + time.ToString("0.0");
        }

        private void UpdateLapText()
        {
            int lap = raceManager.getAgentLap(FollowAgent);
            lapText.text = "Lap " + lap + "/" + raceManager.numLaps;
        }

        private void UpdateArrowText()
        {
            //Find the checkpoint within the viewport
            Transform nextCheckpoint = raceManager.GetAgentNextCheckpoint(FollowAgent);
            Vector3 viewportPoint = raceManager.ActiveCamera.WorldToViewportPoint(nextCheckpoint.transform.position);
            bool behindCamera = viewportPoint.z < 0;
            viewportPoint.z = 0f;

            //Do position calculations
            Vector3 viewportCenter = new Vector3(.5f, .5f, 0f);
            Vector3 fromCenter = viewportPoint - viewportCenter;
            float halfLimit = indicatorLimit / 2f;
            bool showArrow = false;

            if (behindCamera)
            {
                //Limit distance from center 
                //(viewport point is flipped when object is behind camera
                fromCenter = -fromCenter.normalized * halfLimit;
                showArrow = true;
            }
            else
            {
                if(fromCenter.magnitude > halfLimit)
                {
                    //Limit distance from center
                    fromCenter = fromCenter.normalized * halfLimit;
                    showArrow = true;
                }
            }

            //Update the checkpoint icon and arrow
            checkPointArrow.gameObject.SetActive(showArrow);
            checkPointArrow.rectTransform.rotation = Quaternion.FromToRotation(Vector3.up, fromCenter);
            checkPointIcon.rectTransform.position = raceManager.ActiveCamera.ViewportToScreenPoint(fromCenter + viewportCenter);
        }
    }
}
