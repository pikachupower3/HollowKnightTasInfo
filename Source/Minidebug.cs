using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public static class Minidebug {
        private static bool _cameraFollow;
        public static bool cameraFollow {
            get => _cameraFollow;
            private set => _cameraFollow = value;
        }

        private static int activeLoad;
        public static TransitionPoint[] loadzones;

        private static readonly List<Renderer> _invRenders = new List<Renderer>();

        public static void OnPreCull(GameManager gameManager, StringBuilder infoBuilder) {
            if (Input.GetKeyDown(KeyCode.End)) {
                ToggleInventory();
            }
            if (Input.GetKeyDown(KeyCode.Home)) {
                cameraFollow = !cameraFollow;
            }
            if (Input.GetKeyDown(KeyCode.PageUp)) {
                activeLoad++;
            }
            if (Input.GetKeyDown(KeyCode.PageDown)) {
                activeLoad--;
            }
            if (Input.GetKeyDown(KeyCode.T)) {
                ToggleLoadzones();
            }
            if (Input.GetKeyDown(KeyCode.J)) {
                ClearWhitescreen();
            }

            gameManager.hero_ctrl.vignette.enabled = !ConfigManager.HideVignette;

            infoBuilder.AppendLine($"Loads: {loadzones.Length}");
            infoBuilder.AppendLine($"Active Load: {activeLoad}");
        }

        private static void ToggleInventory() {
            _invRenders.RemoveAll(r => r == null);
            if (_invRenders.Count == 0) {
                foreach (Renderer renderer in GameObject.FindGameObjectWithTag("Inventory Top").GetComponentsInChildren<Renderer>(true)) {
                    if (renderer.enabled) {
                        _invRenders.Add(renderer);
                    }
                }
            }

            foreach (Renderer renderer in _invRenders) {
                renderer.enabled = !renderer.enabled;
            }
        }

        private static void ToggleLoadzones() {
            loadzones = UnityEngine.Object.FindObjectsOfType<TransitionPoint>();
            for (int i = 0; i < loadzones.Length; i++) {
                if (i == activeLoad) {
                    loadzones[i].GetComponent<Collider2D>().enabled = true;
                }
                else {
                    loadzones[i].GetComponent<Collider2D>().enabled = false;
                }
            }
        }

        private static void ClearWhitescreen() {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Blanker White"))) {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}