using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using StayInTarkov.Coop.Web;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.UsableItemControllerPatches
{
    internal class FireArmController_ToggleAim_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "ToggleAim";

        [PatchPostfix]
        public static void Postfix(EFT.Player.FirearmController __instance, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.WeaponPacket.ToggleAim = true;
                botPlayer.WeaponPacket.AimingIndex = (__instance.IsAiming) ? __instance.Item.AimIndex.Value : -1;
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.WeaponPacket.ToggleAim = true;
            player.WeaponPacket.AimingIndex = (__instance.IsAiming) ? __instance.Item.AimIndex.Value : -1;
            player.WeaponPacket.ToggleSend();
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }
    }
}