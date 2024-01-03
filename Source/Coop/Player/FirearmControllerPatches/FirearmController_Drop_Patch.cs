//using EFT.InventoryLogic;
//using StayInTarkov.Coop.Players;
//using System;
//using System.Collections.Generic;
//using System.Reflection;

//namespace StayInTarkov.Coop.Player.FirearmControllerPatches
//{
//    public class FirearmController_Drop_Patch : ModuleReplicationPatch
//    {
//        public override Type InstanceType => typeof(EFT.Player.FirearmController);
//        public override string MethodName => "ChangeFireMode";
//        //public override bool DisablePatch => true;

//        protected override MethodBase GetTargetMethod()
//        {
//            var method = ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
//            return method;
//        }

//        [PatchPostfix]
//        public static void PostPatch(EFT.Player.FirearmController __instance, Weapon.EFireMode fireMode, EFT.Player ____player)
//        {
//            var botPlayer = ____player as CoopBot;
//            if (botPlayer != null)
//            {
//                botPlayer.WeaponPacket.ChangeFireMode = true;
//                botPlayer.WeaponPacket.FireMode = fireMode;
//                botPlayer.WeaponPacket.ToggleSend();
//                return;
//            }

//            var player = ____player as CoopPlayer;
//            if (player == null || !player.IsYourPlayer)
//                return;

//            player.WeaponPacket.ChangeFireMode = true;
//            player.WeaponPacket.FireMode = fireMode;
//            player.WeaponPacket.ToggleSend();
//        }

//        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
//        {

//        }
//    }
//}
