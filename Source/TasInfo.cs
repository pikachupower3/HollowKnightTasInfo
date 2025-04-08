using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    // ReSharper disable once UnusedType.Global
    public static class TasInfo {
        private static bool init;

        // 用于测试
        // ReSharper disable once MemberCanBePrivate.Global
        public static string AdditionalInfo = string.Empty;
        private static StringBuilder infoBuilder;

        // ReSharper disable once UnusedMember.Global
        // CameraController.OnPreCull

        public static void OnPreCull() {
            if (GameManager.instance is not { } gameManager) {
                return;
            }

            AdditionalInfo = string.Empty;

            infoBuilder = new();

            try {
                DesyncChecker.BeforeUpdate();
                if (!init) {
                    init = true;
                    OnInit(gameManager);
                }

                OnPreCull(gameManager, infoBuilder);

                DesyncChecker.AfterUpdate(infoBuilder);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void OnPreRender() {
            if (GameManager.instance is not { } gameManager) {
                return;
            }

            OnPreRender(gameManager, infoBuilder);

            patch_GameManager.TasInfo = infoBuilder.AppendLine(AdditionalInfo).ToString();
        }

        // ReSharper disable once UnusedMember.Global
        // CameraController.OnPostRender
        public static void OnPostRender() {
            if (GameManager.instance is not { } gameManager) {
                return;
            }

            CameraManager.OnPostRender(gameManager);
        }

        // ReSharper disable once UnusedMember.Global
        // PlayMakerUnity2DProxy.start()
        public static void OnColliderCreate(GameObject gameObject) {
            HitboxInfo.TryAddHitbox(gameObject);
            EnemyInfo.TryAddEnemy(gameObject);
        }

        // 重叠房间加载后重新采集数据
#if V1028 || V1028_KRYTHOM
        public static void AfterManualLevelStart() {
            EnemyInfo.RefreshInfo(false);
            HitboxInfo.RefreshInfo(false);
        }
#endif

        private static void OnInit(GameManager gameManager) {
            EnemyInfo.OnInit();
            CustomInfo.OnInit();
            HitboxInfo.OnInit();
            RngInfo.OnInit();
            DiagnosticsLogger.OnInit();
            RandomInjection.Init();
            MultiSync.Init();
        }

        private static void OnPreCull(GameManager gameManager, StringBuilder infoBuilder) {
            // 放第一位，先更新 settings
            ConfigManager.OnPreCull();

            // 放第二位，先处理镜头之后 camera.WorldToScreenPoint 才能获得正确数据
            Minidebug.OnPreCull(gameManager, infoBuilder);
            CameraManager.OnPreCull(gameManager);

            HeroInfo.OnPreCull(gameManager, infoBuilder);
            CustomInfo.OnPreCull(gameManager, infoBuilder);
            TimeInfo.OnPreCull(gameManager, infoBuilder);
            EnemyInfo.OnPreCull(gameManager, infoBuilder);
            HitboxInfo.OnPreCull(gameManager, infoBuilder);
            RngInfo.OnPreCull(infoBuilder);
            DiagnosticsLogger.OnPreCull();
            PlaybackSystem.OnPreCull();
            MultiSync.OnPreCull();

        }

        private static void OnPreRender(GameManager gameManager, StringBuilder infoBuilder) {
            // At this point the TasInfo string should have been constructed - now we have the patch_GameManager write out the addr to the special page for the lua script.
            patch_GameManager.WriteTasInfoAddr();
        }
    }
}