using StayInTarkov;
using StayInTarkov.Coop;
using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SIT.Core.Coop.Player.FirearmControllerPatches
{
    internal class FirearmController_ChangeAimingMode_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);

        public override string MethodName => "CheckFireMode";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void Postfix(EFT.Player.FirearmController __instance, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.WeaponPacket.CheckFireMode = true;
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.WeaponPacket.CheckFireMode = true;
            player.WeaponPacket.ToggleSend();

        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
