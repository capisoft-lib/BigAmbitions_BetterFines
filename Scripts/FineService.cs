using System;
using System.Collections.Generic;
using BigAmbitions.SaveSystem.Legacy;
using Entities;
using Localizor;
using UI.Smartphone.Apps.Contacts;

namespace BetterFines
{
    internal static class FineService
    {
        private const string GovernmentContactId = "the_city_of_new_york";
        private const string GovernmentContactCategory = "government";
        private const string ParkingFeesCategory = "ba:transactioncategory_parkingfees";

        internal static bool TryChargeFine(ViolationType type, int baseAmount)
        {
            if (baseAmount <= 0)
                return false;

            if (RecidivismService.IsLicenseSuspended)
                return false;

            var activeAfter = FineRecordStore.ActiveCount + 1;
            var amount = RecidivismService.ApplySurcharge(baseAmount, activeAfter);
            if (amount <= 0)
                return false;

            try
            {
                var vehicleInstance = GameManager.Instance?.selectedVehicle?.vehicleInstance;
                if (vehicleInstance == null)
                    return false;

                var vehicleName = vehicleInstance.vehicleTypeName.GetLocalization();
                var transactionData = new Dictionary<string, string> { { "vehicleName", vehicleName } };
                var info = new TransactionInfo(
                    LegacyRef.Transaction.ParkingTicket,
                    ParkingFeesCategory,
                    transactionData);

                if (!GameManager.ChangeMoneySafe(-amount, info, force: true))
                    return false;

                RecidivismService.RegisterIssuedFine(type, amount);
                TrySendGovernmentFineMessage(type, amount, vehicleInstance);
                ModLog.Info(
                    "Fine issued | type=" + type +
                    " | base=" + baseAmount +
                    " | amount=" + amount +
                    " | active=" + activeAfter);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to charge fine: " + ex.Message);
                return false;
            }
        }

        private static void TrySendGovernmentFineMessage(
            ViolationType type,
            int amount,
            VehicleInstance vehicleInstance)
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save == null)
                    return;

                var contact = Contact.GetContact(
                    GovernmentContactId,
                    ContactCategoryName.General,
                    GovernmentContactCategory);

                var messageKey = type switch
                {
                    ViolationType.RedLight => "betterfines:sms_government_red_light_ticket",
                    ViolationType.WrongWay => "betterfines:sms_government_wrong_way_ticket",
                    _ => "betterfines:sms_government_speeding_ticket"
                };

                var messageData = new Dictionary<string, string>
                {
                    { "vehicleTypeName", vehicleInstance.vehicleTypeName },
                    { "hour", $"{save.Hour:00}" },
                    { "minute", $"{save.Minute:00}" },
                    { "day", save.Day.ToString() },
                    { "amount", amount.ToString("0") }
                };

                GameManager.SendTextMessage(contact, messageKey, messageData);
                ContactsHelper.ShowNewMessageNotification(contact);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to send government fine SMS: " + ex.Message);
            }
        }
    }
}
