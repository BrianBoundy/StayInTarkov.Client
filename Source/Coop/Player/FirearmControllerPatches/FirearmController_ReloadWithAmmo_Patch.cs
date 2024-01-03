using EFT.InventoryLogic;
using StayInTarkov.Coop.ItemControllerPatches;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using StayInTarkov.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_ReloadWithAmmo_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "ReloadWithAmmo";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player.FirearmController __instance, AmmoPack ammoPack, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                var ammoIds1 = ammoPack.GetReloadingAmmoIds();

                botPlayer.WeaponPacket.HasReloadWithAmmoPacket = true;
                botPlayer.WeaponPacket.ReloadWithAmmo = new()
                {
                    Reload = true,
                    Status = SITSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
                    AmmoIdsCount = ammoIds1.Length,
                    AmmoIds = ammoIds1
                };
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            var ammoIds = ammoPack.GetReloadingAmmoIds();

            player.WeaponPacket.HasReloadWithAmmoPacket = true;
            player.WeaponPacket.ReloadWithAmmo = new()
            {
                Reload = true,
                Status = SITSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
                AmmoIdsCount = ammoIds.Length,
                AmmoIds = ammoIds
            };
            player.WeaponPacket.ToggleSend();
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
