namespace StayInTarkov.Coop
{
    public class NetworkFirearmController : AIFirearmController
    {
        private class NetworkFirearmActioneer : AbstractFirearmActioner
        {
            NetworkFirearmController NetworkFirearmController { get; set; }

            private NetworkFirearmActioneer(AIFirearmController controller) : base(controller)
            {
                NetworkFirearmController = controller as NetworkFirearmController;
            }
        }

    }
}
