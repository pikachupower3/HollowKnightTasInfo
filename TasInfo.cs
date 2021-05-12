﻿using System;
using System.Reflection;
using System.Text;
using GlobalEnums;
using UnityEngine;

// ReSharper disable Unity.NoNullPropagation

namespace HollowKnightTasInfo {
    public static class TasInfo {
        private static readonly FieldInfo TeleportingFieldInfo =
            typeof(CameraController).GetField("teleporting", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo TilemapDirtyFieldInfo =
            typeof(GameManager).GetField("tilemapDirty", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool timeStart = false;
        private static bool timeEnd = false;
        private static float inGameTime = 0f;

        private static string FormattedTime {
            get {
                if (inGameTime == 0) {
                    return string.Empty;
                } else if (inGameTime < 60) {
                    return inGameTime.ToString("F2");
                } else if (inGameTime < 3600) {
                    int minute = (int) (inGameTime / 60);
                    float second = inGameTime - minute * 60;
                    return $"{minute}:{second.ToString("F2").PadLeft(5, '0')}";
                } else {
                    int hour = (int) (inGameTime / 3600);
                    int minute = (int) ((inGameTime - hour * 3600) / 60);
                    float second = inGameTime - hour * 3600 - minute * 60;
                    return $"{hour}:{minute.ToString().PadLeft(2, '0')}:{second.ToString("F2").PadLeft(5, '0')}";
                }
            }
        }

        private static GameState lastGameState;
        private static bool lookForTeleporting;

        public static void OnGameManagerLateUpdate() {
            if (GameManager._instance is { } gameManager) {
                ShowHitboxes.Instance.Initialize();

                StringBuilder infoBuilder = new();
                if (gameManager.hero_ctrl is { } heroController) {
                    Vector3 position = heroController.transform.position;
                    infoBuilder.AppendLine($"pos: {position.ToSimpleString(5)}");
                    infoBuilder.AppendLine($"vel: {heroController.current_velocity.ToSimpleString(3)}");
                    infoBuilder.AppendLine(heroController.hero_state.ToString());
                }

                string currentScene = gameManager.sceneName;
                string nextScene = gameManager.nextSceneName;
                GameState gameState = gameManager.gameState;

                if (!timeStart && (nextScene.Equals("Tutorial_01", StringComparison.OrdinalIgnoreCase) && gameState == GameState.ENTERING_LEVEL ||
                                   nextScene is "GG_Vengefly_V" or "GG_Boss_Door_Entrance" or "GG_Entrance_Cutscene")) {
                    timeStart = true;
                }

                if (timeStart && !timeEnd && nextScene.StartsWith("Cinematic_Ending", StringComparison.OrdinalIgnoreCase) ||
                    nextScene == "GG_End_Sequence") {
                    timeEnd = true;
                }

                bool timePaused = false;

                // thanks ShootMe, in game time logic copy from https://github.com/ShootMe/LiveSplit.HollowKnight
                try {
                    UIState uiState = gameManager.ui.uiState;
                    bool loadingMenu = currentScene != "Menu_Title" && string.IsNullOrEmpty(nextScene) ||
                                       currentScene != "Menu_Title" && nextScene == "Menu_Title";
                    if (gameState == GameState.PLAYING && lastGameState == GameState.MAIN_MENU) {
                        lookForTeleporting = true;
                    }

                    bool teleporting = (bool) TeleportingFieldInfo.GetValue(gameManager.cameraCtrl);
                    if (lookForTeleporting && (teleporting || (gameState != GameState.PLAYING && gameState != GameState.ENTERING_LEVEL))) {
                        lookForTeleporting = false;
                    }

                    timePaused =
                        gameState == GameState.PLAYING && teleporting && gameManager.hero_ctrl?.cState.hazardRespawning == false
                        || lookForTeleporting
                        || gameState is GameState.PLAYING or GameState.ENTERING_LEVEL && uiState != UIState.PLAYING
                        || gameState != GameState.PLAYING && !gameManager.inputHandler.acceptingInput
                        || gameState is GameState.EXITING_LEVEL or GameState.LOADING
                        || gameManager.hero_ctrl?.transitionState == HeroTransitionState.WAITING_TO_ENTER_LEVEL
                        || uiState != UIState.PLAYING &&
                        (loadingMenu || uiState != UIState.PAUSED && (!string.IsNullOrEmpty(nextScene) || currentScene == "_test_charms")) &&
                        nextScene != currentScene
                        || (bool) TilemapDirtyFieldInfo.GetValue(gameManager);
                } catch (Exception) {
                    // ignore
                }

                lastGameState = gameState;

                if (timeStart && !timePaused && !timeEnd) {
                    inGameTime += Time.unscaledDeltaTime;
                }

                infoBuilder.AppendLine(FormattedTime);
                GameManager.Info = infoBuilder.ToString();
            }
        }
    }

    internal static class Vector2Extension {
        public static string ToSimpleString(this Vector2 vector2, int precision) {
            return $"{vector2.x.ToString($"F{precision}")}, {vector2.y.ToString($"F{precision}")}";
        }

        public static string ToSimpleString(this Vector3 vector3, int precision) {
            return $"{vector3.x.ToString($"F{precision}")}, {vector3.y.ToString($"F{precision}")}";
        }

        public static Vector3 Add(this Vector3 vector, Vector3 otherVector) {
            return new Vector3(vector.x + otherVector.x, vector.y + otherVector.y, vector.z + otherVector.z);
        }

        public static Vector3 Sub(this Vector3 vector, Vector3 otherVector) {
            return new Vector3(vector.x - otherVector.x, vector.y - otherVector.y, vector.z - otherVector.z);
        }

        public static Vector3 Mul(this Vector3 vector, float scaleFactor) {
            return new Vector3(vector.x * scaleFactor, vector.y * scaleFactor, vector.z * scaleFactor);
        }

        public static Vector3 Div(this Vector3 vector, float scaleFactor) {
            return new Vector3(vector.x / scaleFactor, vector.y / scaleFactor, vector.z / scaleFactor);
        }
    }
}