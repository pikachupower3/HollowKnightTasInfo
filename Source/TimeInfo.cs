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
        public new static void OnPreRender(GameManager gameManager, StringBuilder infoBuilder) {
            BaseTimer.OnPreRender(gameManager, infoBuilder);

            List<string> result = new();
            if (!string.IsNullOrEmpty(gameManager.sceneName) && ConfigManager.ShowSceneName) {
                result.Add(gameManager.sceneName);
            }

            if (InGameTime > 0 && ConfigManager.ShowTime) {
                result.Add(FormattedTime(InGameTime));
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