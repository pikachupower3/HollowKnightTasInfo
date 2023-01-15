using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly_CSharp.TasInfo.mm.Source
{
    public class AutoSplit
    {
        public static void OnPreRender(GameManager gameManager, StringBuilder infoBuilder) {
            if (gameManager.gameState == GlobalEnums.GameState.EXITING_LEVEL && gameManager.sceneName.Equals("Tutorial_01") && gameManager.nextSceneName.Equals("Town")) {
                infoBuilder.AppendLine("Exited Kings Pass");
            }
        }
    }
}
