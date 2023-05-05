using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Extensions;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using GlobalEnums;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal class TimeInfo : BaseTimer {
        private static float inGameTime = 0f;
        public static void OnPreRender(GameManager gameManager, StringBuilder infoBuilder) {

            if (TimeStart && !TimePaused && !TimeEnd) {
                inGameTime += Time.unscaledDeltaTime;
            }

            List<string> result = new();

            if (inGameTime > 0 && ConfigManager.ShowTime) {
                result.Add(FormattedTime(inGameTime));
            }

            string resultString = StringUtils.Join("  ", result);
            if (!string.IsNullOrEmpty(resultString)) {
                infoBuilder.AppendLine(resultString);
            }

            if (ConfigManager.ShowTimeMinusFixedTime) {
                infoBuilder.AppendLine($"T-FT {1000 * (Time.time - Time.fixedTime):00.0000} ms");
            }
        }
    }
}