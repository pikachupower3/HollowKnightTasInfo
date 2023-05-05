using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Extensions;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using GlobalEnums;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal class RoomTimer : BaseTimer {
        private static float roomTime = 0;
        private static bool didEnter = false;

        public static void OnPreRender(GameManager gameManager, StringBuilder infoBuilder) {
            var transitionState = gameManager.hero_ctrl.transitionState;
            if ((transitionState == HeroTransitionState.ENTERING_SCENE ||
                transitionState == HeroTransitionState.DROPPING_DOWN) 
                && !didEnter) {
                roomTime = 0;
                didEnter = true;
            }

            if (gameManager.hero_ctrl?.transitionState == HeroTransitionState.WAITING_TO_TRANSITION) {
                didEnter = false;
            }

            if (TimeStart && !TimePaused && !TimeEnd) {
                roomTime += Time.unscaledDeltaTime;
            }

            List<string> result = new();

            if (!string.IsNullOrEmpty(gameManager.sceneName) && ConfigManager.ShowSceneName) {
                result.Add(gameManager.sceneName);
            }

            if (roomTime > 0 && ConfigManager.ShowRoomTime) {
                result.Add(FormattedTime(roomTime) + '\n');
            } else {
                result[0] += '\n';
            }

            string resultString = StringUtils.Join("  ", result);
            if (!string.IsNullOrEmpty(resultString)) {
                infoBuilder.Append(resultString);
            }
        }
    }
}
