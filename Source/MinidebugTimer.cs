using System;
using System.Reflection;
using GlobalEnums;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public class MinidebugTimer : MonoBehaviour {
        private static MinidebugTimer _instance;
        
        private bool timerRunning;
        private bool lookForTeleporting;
        private GameState lastGameState;

        private static readonly FieldInfo cameraControlTeleporting = typeof(CameraController).GetField("teleporting", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo gameManagerDirtyTileMap = typeof(GameManager).GetField("tilemapDirty", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly int minorVersion = int.Parse(Constants.GAME_VERSION.Substring(2, 1));

        private float _timer;

        public static MinidebugTimer Instance {
            get {
                UnityEngine.Debug.Log("Get minidebug timer");
                if (_instance == null) {
                    _instance = new GameObject("Mod Timer").AddComponent<MinidebugTimer>();
                    UnityEngine.Object.DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        public bool StartTimer { get; set; }

        private void Update() {
            if (StartTimer && !timerRunning) {
                StartTimer = false;
                timerRunning = true;
                _timer = ConfigManager.StartingGameTime;
            }
            if (timerRunning) {
                GameManager instance = GameManager.instance;
                if (instance != null && instance.nextSceneName.StartsWith("Cinematic_Ending")) {
                    timerRunning = false;
                } else if (!TimerShouldBePaused()) {
                    _timer += Time.unscaledDeltaTime;
                }
            }
        }

        private bool TimerShouldBePaused() {
            GameManager instance = GameManager.instance;
            if (instance == null) {
                lookForTeleporting = false;
                lastGameState = GameState.INACTIVE;
                return false;
            }

            string nextSceneName = instance.nextSceneName;
            string sceneName = instance.sceneName;
            UIState uiState = instance.ui.uiState;
            GameState gameState = instance.gameState;
            
            bool loadingMenu = sceneName != "Menu_Title" && string.IsNullOrEmpty(nextSceneName) ||
                               sceneName != "Menu_Title" && nextSceneName == "Menu_Title";
            if (gameState == GameState.PLAYING && lastGameState == GameState.MAIN_MENU) {
                lookForTeleporting = true;
            }

            bool teleporting = (bool)cameraControlTeleporting.GetValue(instance.cameraCtrl);
            if (lookForTeleporting && (teleporting || gameState != GameState.PLAYING && gameState != GameState.ENTERING_LEVEL)) {
                lookForTeleporting = false;
            }

            bool timePaused =
                gameState == GameState.PLAYING && teleporting && instance.hero_ctrl?.cState.hazardRespawning == false
                || lookForTeleporting
                || gameState is GameState.PLAYING or GameState.ENTERING_LEVEL && uiState != UIState.PLAYING
                || gameState != GameState.PLAYING && !instance.inputHandler.acceptingInput
                || gameState is GameState.EXITING_LEVEL or GameState.LOADING
                || instance.hero_ctrl?.transitionState == HeroTransitionState.WAITING_TO_ENTER_LEVEL
                || uiState != UIState.PLAYING &&
                (loadingMenu || uiState != UIState.PAUSED && (!string.IsNullOrEmpty(nextSceneName) || sceneName == "_test_charms")) &&
                nextSceneName != sceneName
                || minorVersion < 3 && (bool)gameManagerDirtyTileMap.GetValue(instance);

            lastGameState = gameState;

            return timePaused;
        }

        private void OnGUI() {
            if (Event.current.type != EventType.Repaint) return;
            Color backgroundColor = GUI.backgroundColor;
            Color contentColor = GUI.contentColor;
            Color color = GUI.color;
            Matrix4x4 matrix = GUI.matrix;

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3((float) Screen.width / 1280f,
                (float) Screen.height / 720f, 1f)
            );

            int minutes = (int)_timer / 60;
            float seconds = _timer - (minutes * 60);
            GUI.Label(new Rect(50f, 670f, 200f, 200f), string.Format("{0}:{1}", minutes, seconds.ToString("00.00")));

            GUI.backgroundColor = backgroundColor;
            GUI.contentColor = contentColor;
            GUI.color = color;
            GUI.matrix = matrix;
        }
    }
}