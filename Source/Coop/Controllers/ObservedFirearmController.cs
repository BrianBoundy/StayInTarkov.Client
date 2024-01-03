//using EFT.InventoryLogic;
//using StayInTarkov.Coop.Players;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace StayInTarkov.Coop.Controllers
//{
//    internal class ObservedFirearmController : FirearmController
//    {
//        // Replace with own struct
//        private Queue<FiredShotInfo> ShotQueue = new(20);
//        // These 2 will be assigned from the queue
//        private Vector3 ShotPosition;
//        private Vector3 ShotDirection;

//        private ObservedFirearmController AssignController(ObservedCoopPlayer player, Weapon weapon)
//        {
//            return smethod_5<ObservedFirearmController>(player, weapon);
//        }

//        private void HandleTrigger()
//        {
//            if (Item.MalfState.State != Weapon.EMalfunctionState.None)
//            {
//                Item.MalfState.AddPlayerWhoKnowMalfunction(_player.Profile.Id, false);
//            }
//            if (Item.ChamberAmmoCount == 0)
//            {
//                SetTriggerPressed(true);
//            }
//        }

//        public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
//        {
//            // Look at ObservedFirearmController to return new classes
//            return base.GetOperationFactoryDelegates();
//        }

//        public override void InitiateShot(IWeapon weapon, BulletClass ammo, Vector3 shotPosition, Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
//        {
//            // Will be used to shoot fake shots from packets
//            base.InitiateShot(weapon, ammo, ShotPosition, ShotDirection, fireportPosition, chamberIndex, overheat);
//        }
//    }
//}
