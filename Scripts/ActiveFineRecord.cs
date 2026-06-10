using UnityEngine;

namespace BetterFines
{
    internal sealed class ActiveFineRecord
    {
        internal ViolationType Type;
        internal int IssuedDay;
        internal int IssuedHour;
        internal int IssuedMinute;
        internal int Amount;
        internal int ExpiresDay;

        internal bool IsActive(int currentDay) => currentDay < ExpiresDay;

        internal int DaysRemaining(int currentDay) =>
            Mathf.Max(0, ExpiresDay - currentDay);
    }
}
