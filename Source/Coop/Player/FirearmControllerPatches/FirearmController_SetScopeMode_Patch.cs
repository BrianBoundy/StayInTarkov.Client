using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using StayInTarkov.Coop.Web;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    internal class FirearmController_SetScopeMode_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);

        public override string MethodName => "SetScopeMode";

        protected override MethodBase GetTargetMethod() => ReflectionHelpers.GetMethodForType(InstanceType, MethodName);

        [PatchPostfix]
        public static void Postfix(object __instance, ScopeStates[] scopeStates, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.WeaponPacket.ChangeSightMode = true;
                botPlayer.WeaponPacket.ScopeStatesPacket = new()
                {
                    Amount = scopeStates.Length,
                    ScopeStates = scopeStates
                };
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.WeaponPacket.ChangeSightMode = true;
            player.WeaponPacket.ScopeStatesPacket = new()
            {
                Amount = scopeStates.Length,
                ScopeStates = scopeStates
            };
            player.WeaponPacket.ToggleSend();
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}