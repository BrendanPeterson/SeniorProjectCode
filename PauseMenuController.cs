using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class PauseMenuController : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance.OnStateChange += OnStateChange;

        }

        private void OnStateChange()
        {
            if(GameManager.Instance.GameState == GameState.Playing)
            {
                gameObject.SetActive(false);
            }
        }

        public void ResumeButtonClicked()
        {
            GameManager.Instance.GameState = GameState.Playing;
            print("Game Resumed");
        }

        public void MainMenuButtonClicked()
        {
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
            print("Main Menu Loaded");
        }

        public void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChange -= OnStateChange;
        }
    }
}
