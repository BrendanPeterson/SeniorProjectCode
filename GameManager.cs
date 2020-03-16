using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aircraft
{
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Paused,
        GameOver
    }

    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard,
        Harder
    }
    //Called Anytime Game State Changes
    public delegate void OnStateChangeHandler();

    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// event called when game state changes
        /// </summary>
        public event OnStateChangeHandler OnStateChange;

        private GameState gameState;

        /// <summary>
        /// Curent GameState
        /// </summary>
        public GameState GameState
        {
            get
            {
                return gameState;
            }

            set
            {
                gameState = value;
                if (OnStateChange != null) OnStateChange();
            }
        }

        public GameDifficulty GameDifficulty { get; set; }
        
        /// <summary>
        /// The singleton game manager Instance
        /// </summary>
        public static GameManager Instance
        {
            get; private set;
        }

        /// <summary>
        ///Manager the singleton and set fullscreeen res
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnApplicationQuit()
        {
            Instance = null;
        }

        /// <summary>
        /// Loads a new level and sets the game state
        /// </summary>
        /// <param name="levelName">Level to load</param>
        /// <param name="newState">The new State</param>
        public void LoadLevel(string levelName, GameState newState)
        {
            StartCoroutine(LoadLevelAsync(levelName, newState));
        }

        private IEnumerator LoadLevelAsync(string levelName, GameState newState)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(levelName);
            while(operation.isDone == false)
            {
                yield return null;
            }

            //Set resolution
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);

            //Update game state
            GameState = newState;
        }
    }
}
