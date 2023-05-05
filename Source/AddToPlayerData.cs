using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal static class AddToPlayerData {
        public static void OnPreRender(GameManager gameManager) {
            var playerData = gameManager.playerData;
            if (playerData == null) {
                return;
            }
            var heroCtrl = gameManager.hero_ctrl;
            if (heroCtrl == null) {
                return;
            }
            if (Input.GetKeyDown(KeyCode.R)) {
                int toAddMasks = ConfigManager.AddMasks;
                int toAddVessels = ConfigManager.AddSoulVessel * 33;
                heroCtrl.AddMPCharge(ConfigManager.AddSoul);
                heroCtrl.AddGeo(ConfigManager.AddGeo);
                while ((playerData.maxHealthBase + toAddMasks) > playerData.maxHealthCap) {
                    toAddMasks--;
                }
                if (toAddMasks > 0) {
                    heroCtrl.AddToMaxHealth(toAddMasks);
                    PlayMakerFSM.BroadcastEvent("MAX HP UP");
                }
                playerData.dreamOrbs += ConfigManager.AddEssence;
                while ((playerData.MPReserveMax + toAddVessels) > playerData.MPReserveCap) {
                    toAddVessels -= 33;
                }
                if (toAddVessels > 0) {
                    heroCtrl.AddToMaxMPReserve(toAddVessels);
                    PlayMakerFSM.BroadcastEvent("NEW SOUL ORB");
                }
                playerData.joniHealthBlue += ConfigManager.AddLifeBlood;
                PlayMakerFSM.BroadcastEvent("UPDATE BLUE HEALTH");
            }
            if (Input.GetKeyDown(KeyCode.T)) {
                heroCtrl.AddMPCharge(ConfigManager.AddSoul);

            }
            if (Input.GetKeyDown(KeyCode.Y)) {
                heroCtrl.AddGeo(ConfigManager.AddGeo);
            }
            if (Input.GetKeyDown(KeyCode.U)) {
                playerData.dreamOrbs += ConfigManager.AddEssence;
            }
            if (Input.GetKeyDown(KeyCode.G)) {
                int toAddMasks = ConfigManager.AddMasks;
                while ((playerData.maxHealthBase + toAddMasks) > playerData.maxHealthCap) {
                    toAddMasks--;
                }
                if (toAddMasks > 0) {
                    heroCtrl.AddToMaxHealth(toAddMasks);
                    PlayMakerFSM.BroadcastEvent("MAX HP UP");
                    PlayMakerFSM.BroadcastEvent("UPDATE BLUE HEALTH");
                }
            }
            if (Input.GetKeyDown(KeyCode.H)) {
                playerData.joniHealthBlue += ConfigManager.AddLifeBlood;
                PlayMakerFSM.BroadcastEvent("UPDATE BLUE HEALTH");
            }
            if (Input.GetKeyDown(KeyCode.J)) {
                int toAddVessels = ConfigManager.AddSoulVessel * 33;
                while ((playerData.MPReserveMax + toAddVessels) > playerData.MPReserveCap) {
                    toAddVessels -= 33;
                }
                if (toAddVessels > 0) {
                    heroCtrl.AddToMaxMPReserve(toAddVessels);
                    PlayMakerFSM.BroadcastEvent("NEW SOUL ORB");
                }
            }
        }
    }
}
