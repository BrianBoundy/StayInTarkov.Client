using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using StayInTarkov.Coop.Web;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    internal class FirearmController_SetLightsState_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);

        public override string MethodName => "SetLightsState";

        protected override MethodBase GetTargetMethod() => ReflectionHelpers.GetMethodForType(InstanceType, MethodName);

        [PatchPostfix]
        public static void Postfix(object __instance, LightsStates[] lightsStates, bool force, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.WeaponPacket.ToggleTacticalCombo = true;
                botPlayer.WeaponPacket.LightStatesPacket = new()
                {
                    Amount = lightsStates.Length,
                    LightStates = lightsStates
                };
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.WeaponPacket.ToggleTacticalCombo = true;
            player.WeaponPacket.LightStatesPacket = new()
            {
                Amount = lightsStates.Length,
                LightStates = lightsStates
            };
            player.WeaponPacket.ToggleSend();
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}