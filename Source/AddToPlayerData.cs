using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal static class AddToPlayerData {
        public static void OnPreRender(GameManager gameManager) {
            PlayerData playerData = gameManager.playerData;
            if (playerData == null ) { return; }
            if (Input.GetKeyDown(KeyCode.T)) {
                playerData.AddMPCharge(ConfigManager.AddSoul);
                playerData.AddGeo(ConfigManager.AddGeo);
                playerData.AddToMaxHealth(ConfigManager.AddMasks);
                playerData.healthBlue += ConfigManager.AddLifeBlood;
                playerData.dreamOrbs += ConfigManager.AddEssence;
            }
            if (Input.GetKeyDown(KeyCode.Y)) {
                playerData.AddMPCharge(ConfigManager.AddSoul);
            }
            if (Input.GetKeyDown(KeyCode.U)) {
                playerData.AddGeo(ConfigManager.AddGeo);
            }
            if (Input.GetKeyDown(KeyCode.I)) {
                playerData.dreamOrbs += ConfigManager.AddEssence;
            }
            if (Input.GetKeyDown(KeyCode.O)) {
                playerData.AddToMaxHealth(ConfigManager.AddMasks);
            }
            if (Input.GetKeyDown(KeyCode.P)) {
                playerData.healthBlue += ConfigManager.AddLifeBlood;
            }
        }
    }
}
