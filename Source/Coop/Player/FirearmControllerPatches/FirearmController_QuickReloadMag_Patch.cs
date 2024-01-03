using Newtonsoft.Json;
using StayInTarkov.Coop.ItemControllerPatches;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using StayInTarkov.Coop.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_QuickReloadMag_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "QuickReloadMag";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void Postfix(EFT.Player.FirearmController __instance, EFT.Player ____player, MagazineClass magazine)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.WeaponPacket.HasQuickReloadMagPacket = true;
                botPlayer.WeaponPacket.QuickReloadMag = new()
                {
                    Reload = true,
                    MagId = magazine.Id
                };
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.WeaponPacket.HasQuickReloadMagPacket = true;
            player.WeaponPacket.QuickReloadMag = new()
            {
                Reload = true,
                MagId = magazine.Id
            };
            player.WeaponPacket.ToggleSend();
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}