using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player
{
    internal class Player_Gesture_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player);
        public override string MethodName => "Gesture";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, "vmethod_3");
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player __instance, EGesture gesture)
        {
            var botPlayer = __instance as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.WeaponPacket.Gesture = gesture;
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = __instance as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.WeaponPacket.Gesture = gesture;
            player.WeaponPacket.ToggleSend();
        }


        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
