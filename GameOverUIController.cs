using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Aircraft
{
    public class GameOverUIController : MonoBehaviour
    {
        [Tooltip("Text to display finish place (e.g 2nd Place")]
        public TextMeshProUGUI placeText;

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void OnEnable()
        {
            if(GameManager.Instance != null &&
                GameManager.Instance.GameState == GameState.GameOver)
            {
                //Gets the place and updates the text
                string place = raceManager.GetAgentPlace(raceManager.FollowAgent);
                this.placeText.text = place + " Place";
            }
        }

        public void MainMenuButtonClicked()
        {
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
        }

    }
}
