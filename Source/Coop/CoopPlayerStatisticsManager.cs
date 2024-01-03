using EFT;

namespace StayInTarkov.Coop
{
    internal class CoopPlayerStatisticsManager : AStatisticsManagerForPlayer, IStatisticsManager
    {
        public static Profile Profile { get; set; }

        public override void BeginStatisticsSession()
        {
            base.BeginStatisticsSession();

            Profile = Profile_0;
        }

        public override void ShowStatNotification(LocalizationKey localizationKey1, LocalizationKey localizationKey2, int value)
        {
            if (value > 0)
            {
                NotificationManagerClass.DisplayNotification(new AbstractNotification46(localizationKey1, localizationKey2, value));
            }
        }
    }
}
