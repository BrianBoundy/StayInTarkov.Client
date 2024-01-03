using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_SetTriggerPressed_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "SetTriggerPressed";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player.FirearmController __instance, bool pressed, EFT.Player ____player)
        {

            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                if (__instance.Weapon.MalfState.State != EFT.InventoryLogic.Weapon.EMalfunctionState.None)
                {
                    botPlayer.WeaponPacket.HasMalfunction = true;
                    botPlayer.WeaponPacket.MalfunctionState = __instance.Weapon.MalfState.State;
                }
                botPlayer.WeaponPacket.IsTriggerPressed = pressed;
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            if (__instance.Weapon.MalfState.State != EFT.InventoryLogic.Weapon.EMalfunctionState.None)
            {
                player.WeaponPacket.HasMalfunction = true;
                player.WeaponPacket.MalfunctionState = __instance.Weapon.MalfState.State;
            }
            player.WeaponPacket.IsTriggerPressed = pressed;
            player.WeaponPacket.ToggleSend();
        }


        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
