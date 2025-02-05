using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Extensions;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using GlobalEnums;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal class BaseTimer {
        private static readonly FieldInfo TeleportingFieldInfo = typeof(CameraController).GetFieldInfo("teleporting");
        private static readonly FieldInfo TilemapDirtyFieldInfo = typeof(GameManager).GetFieldInfo("tilemapDirty");

        private protected static bool TimeStart { get; private set; } = false;
        private protected static bool TimeEnd { get; private set; } = false;
        private protected static bool TimePaused { get; private set; } = false;
        private static readonly int MinorVersion = int.Parse(Constants.GAME_VERSION.Substring(2, 1));

        private protected static string FormattedTime(double time) {
            if (time == 0) {
                return string.Empty;
            } else if (time < 60) {
                return time.ToString("F2");
            } else if (time < 3600) {
                int minute = (int)(time / 60);
                double second = time - minute * 60;
                return $"{minute}:{second.ToString("F2").PadLeft(5, '0')}";
            } else {
                int hour = (int)(time / 3600);
                int minute = (int)((time - hour * 3600) / 60);
                double second = time - hour * 3600 - minute * 60;
                return $"{hour}:{minute.ToString().PadLeft(2, '0')}:{second.ToString("F2").PadLeft(5, '0')}";
            }
        }

        private static GameState lastGameState;
        private static bool lookForTeleporting;
        private static bool isPaused = false;

        public static void OnPreRender(GameManager gameManager) {
            string currentScene = gameManager.sceneName;
            string nextScene = gameManager.nextSceneName;
            GameState gameState = gameManager.gameState;

            if (Input.GetKeyDown(KeyCode.P)) {
                isPaused = !isPaused;
            }

            double retTime = 0d;

            if (!TimeStart && (!string.IsNullOrEmpty(ConfigManager.TimerStartTransition) && nextScene.Equals(ConfigManager.TimerStartTransition) ||
                               (string.IsNullOrEmpty(ConfigManager.TimerStartTransition) && 
                               (nextScene.Equals("Tutorial_01", StringComparison.OrdinalIgnoreCase) && gameState == GameState.ENTERING_LEVEL ||
                               nextScene is "GG_Vengefly_V" or "GG_Boss_Door_Entrance" or "GG_Entrance_Cutscene" ||
                               HeroController.instance != null)))) {
                TimeStart = true;
                retTime = ConfigManager.StartingGameTime;
            }

            if (TimeStart && !TimeEnd && (nextScene.StartsWith("Cinematic_Ending", StringComparison.OrdinalIgnoreCase) ||
                                          nextScene == "GG_End_Sequence") || AutoSplit.SplitLastSplit) {
                TimeEnd = true;
            }

            TimePaused = false;

            // thanks ShootMe, in game time logic copy from https://github.com/ShootMe/LiveSplit.HollowKnight
            try {
                UIState uiState = gameManager.ui.uiState;
                bool loadingMenu = currentScene != "Menu_Title" && string.IsNullOrEmpty(nextScene) ||
                                   currentScene != "Menu_Title" && nextScene == "Menu_Title";
                if (gameState == GameState.PLAYING && lastGameState == GameState.MAIN_MENU) {
                    lookForTeleporting = true;
                }

                bool teleporting = (bool)TeleportingFieldInfo.GetValue(gameManager.cameraCtrl);
                if (lookForTeleporting && (teleporting || gameState != GameState.PLAYING && gameState != GameState.ENTERING_LEVEL)) {
                    lookForTeleporting = false;
                }

                TimePaused =
                    gameState == GameState.PLAYING && teleporting && gameManager.hero_ctrl?.cState.hazardRespawning == false
                    || lookForTeleporting
                    || gameState is GameState.PLAYING or GameState.ENTERING_LEVEL && uiState != UIState.PLAYING
                    || gameState != GameState.PLAYING && !gameManager.inputHandler.acceptingInput
                    || gameState is GameState.EXITING_LEVEL or GameState.LOADING
                    || gameManager.hero_ctrl?.transitionState == HeroTransitionState.WAITING_TO_ENTER_LEVEL
                    || uiState != UIState.PLAYING &&
                    (loadingMenu || uiState != UIState.PAUSED && (!string.IsNullOrEmpty(nextScene) || currentScene == "_test_charms")) &&
                    nextScene != currentScene
                    || MinorVersion < 3 && (bool)TilemapDirtyFieldInfo.GetValue(gameManager)
                    || ConfigManager.PauseTimer
                    || isPaused;
            } catch {
                // ignore
            }

            lastGameState = gameState;
        }
    }
}