using System;
using System.Collections.Generic;
using BigAmbitions.SaveSystem.Legacy;
using Entities;
using Localizor;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

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

                var licenseSuspended = RecidivismService.RegisterIssuedFine(type, amount);
                TrySendGovernmentFineMessage(type, amount, vehicleInstance);
                if (licenseSuspended)
                    TrySendLicenseSuspendedMessage(RecidivismService.DaysUntilLicenseRestored());
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

        internal static void TrySendLicenseSuspendedMessage(int daysRemaining)
        {
            if (!BetterFinesConfig.LicenseRevokeEnabled)
                return;

            var save = SaveGameManager.Current;
            if (save == null)
                return;

            TrySendGovernmentMessage(
                "betterfines:sms_government_license_suspended",
                new Dictionary<string, string>
                {
                    { "hour", $"{save.Hour:00}" },
                    { "minute", $"{save.Minute:00}" },
                    { "day", save.Day.ToString() },
                    { "days", LocaleFormat.Integer(Mathf.Max(1, daysRemaining)) }
                },
                isLicenseMessage: true);
        }

        internal static void TrySendLicenseRestoredMessage()
        {
            if (!BetterFinesConfig.LicenseRevokeEnabled)
                return;

            var save = SaveGameManager.Current;
            if (save == null)
                return;

            TrySendGovernmentMessage(
                "betterfines:sms_government_license_restored",
                new Dictionary<string, string>
                {
                    { "hour", $"{save.Hour:00}" },
                    { "minute", $"{save.Minute:00}" },
                    { "day", save.Day.ToString() }
                },
                isLicenseMessage: true);
        }

        private static void TrySendGovernmentFineMessage(
            ViolationType type,
            int amount,
            VehicleInstance vehicleInstance)
        {
            var save = SaveGameManager.Current;
            if (save == null)
                return;

            var messageKey = type switch
            {
                ViolationType.RedLight => "betterfines:sms_government_red_light_ticket",
                ViolationType.WrongWay => "betterfines:sms_government_wrong_way_ticket",
                ViolationType.Pedestrian => "betterfines:sms_government_pedestrian_ticket",
                _ => "betterfines:sms_government_speeding_ticket"
            };

            TrySendGovernmentMessage(
                messageKey,
                new Dictionary<string, string>
                {
                    { "vehicleTypeName", vehicleInstance.vehicleTypeName },
                    { "hour", $"{save.Hour:00}" },
                    { "minute", $"{save.Minute:00}" },
                    { "day", save.Day.ToString() },
                    { "amount", LocaleFormat.Money(amount) }
                },
                playFlash: true);
        }

        private static void TrySendGovernmentMessage(
            string messageKey,
            Dictionary<string, string> messageData,
            bool playFlash = false,
            bool isLicenseMessage = false)
        {
            try
            {
                if (SaveGameManager.Current == null)
                    return;

                messageData["department"] = isLicenseMessage
                    ? ModUiText.SmsDepartmentMotorVehicles
                    : ModUiText.SmsDepartmentTraffic;

                var contact = Contact.GetContact(
                    GovernmentContactId,
                    ContactCategoryName.General,
                    GovernmentContactCategory);

                if (playFlash)
                    RedLightCameraFlash.TryPlay();

                GameManager.SendTextMessage(contact, messageKey, messageData);
                ContactsHelper.ShowNewMessageNotification(contact);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to send government SMS: " + ex.Message);
            }
        }
    }
}
