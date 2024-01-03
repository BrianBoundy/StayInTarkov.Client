using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using StayInTarkov.Coop.ItemControllerPatches;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_ReloadGrenadeLauncher_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "ReloadGrenadeLauncher";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player.FirearmController __instance, AmmoPack foundItem, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                var reloadingAmmoIds1 = foundItem.GetReloadingAmmoIds();

                botPlayer.WeaponPacket.HasReloadLauncherPacket = true;
                botPlayer.WeaponPacket.ReloadLauncher = new()
                {
                    Reload = true,
                    AmmoIdsCount = reloadingAmmoIds1.Length,
                    AmmoIds = reloadingAmmoIds1
                };
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            var reloadingAmmoIds = foundItem.GetReloadingAmmoIds();

            player.WeaponPacket.HasReloadLauncherPacket = true;
            player.WeaponPacket.ReloadLauncher = new()
            {
                Reload = true,
                AmmoIdsCount = reloadingAmmoIds.Length,
                AmmoIds = reloadingAmmoIds
            };
            player.WeaponPacket.ToggleSend();
        }



        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }        
    }
}
