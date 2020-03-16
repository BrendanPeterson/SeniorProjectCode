using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace Aircraft
{


    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("List of Levels (Scene Names) Names")]
        public List<string> levels;

        [Tooltip("The Dropdown for Selecting the level")]
        public TMP_Dropdown levelDropDown;

        [Tooltip("The Dropdown for Selecting the Difficulty")]
        public TMP_Dropdown difficultyDropDown;

        [Tooltip("The Dopdown for selecting the number of laps")]
        public TMP_Dropdown NumLapsDropdown;

        private string selectedLevel;
        private GameDifficulty selectedDifficulty;


        /// <summary>
        /// Automatically fill dropdown lists
        /// </summary>
        private void Start()
        {
            Debug.Assert(levels.Count > 0, "No levels Available");
            levelDropDown.ClearOptions();
            levelDropDown.AddOptions(levels);
            selectedLevel = levels[0];

            difficultyDropDown.ClearOptions();
            difficultyDropDown.AddOptions(Enum.GetNames(typeof(GameDifficulty)).ToList());
            selectedDifficulty = GameDifficulty.Normal;
        }

        public void SetLevel(int levelIndex)
        {
            selectedLevel = levels[levelIndex];
        }

        public void SetDifficulty(int difficultyIndex)
        {
            selectedDifficulty = (GameDifficulty)difficultyIndex;
        }

        /// <summary>
        /// Start the chosen Level
        /// </summary>
        public void StartButtonClicked()
        {
            //Set game Difficulty
            GameManager.Instance.GameDifficulty = selectedDifficulty;

            //Load the level in preparing mode
            GameManager.Instance.LoadLevel(selectedLevel, GameState.Preparing);
        }

        /// <summary>
        /// Quit the game
        /// </summary>
        public void QuitButtonClicked()
        {
            Application.Quit();
        }
    }
}
