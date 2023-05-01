using System;
using System.IO;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {

    public static class ScreenShakeModifier {
        public static void EditScreenShake(GameCameras gameCameras) {
            LoadMultiplier();

            var fsm = gameCameras.cameraShakeFSM;

            foreach (var state in fsm.FsmStates) {
                foreach (var action in state.Actions) {
                    if (action is iTweenShakePosition iTweenShakePosition) {
                        iTweenShakePosition.vector = iTweenShakePosition.vector.Value * Multiplier.multiplier;
                    }
                }
            }
        }

        public static Multiplier Multiplier = new Multiplier();
        public static string MultiplierPath => Path.Combine(Application.persistentDataPath, "screenShakeModifier.json");

        public static void LoadMultiplier() {
            try {
                Multiplier = JsonUtility.FromJson<Multiplier>(File.ReadAllText(MultiplierPath));
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        public static void SaveMultiplier() {
            try {
                File.WriteAllText(MultiplierPath, JsonUtility.ToJson(Multiplier, true));
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
}
