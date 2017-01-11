using System;
using System.Collections.Generic;
using System.Linq;
using CB.Bitcoin.Client.Monitoring;

namespace CB.Bitcoin.Client.KeyManagement
{
    public class HttpKeyRing : LimitedKeyRing
    {
        internal HttpKeyRing(HttpKeyRingMonitor httpKeyRingMonitor)
            : base(httpKeyRingMonitor.BaseKeyRing, httpKeyRingMonitor.AddressCount)
        {
            HttpKeyRingMonitor = httpKeyRingMonitor;
        }

        public HttpKeyRingMonitor HttpKeyRingMonitor { get; }

        public List<string> UnusedAddresses
        {
            get
            {
                var unusedAddresses = Addresses.ToList();
                foreach (var addressHistoryRecord in HttpKeyRingMonitor.KeyRingHistory.Records)
                {
                    unusedAddresses.Remove(addressHistoryRecord.Address);
                }

                if (unusedAddresses.Count == 0)
                    throw new ArgumentException("Every address of HttpKeyRing has been used.");

                return unusedAddresses;
            }
        }

        public List<string> NotEmptyAddresses
            => HttpKeyRingMonitor.KeyRingBalanceInfo.AddressBalances.Where(
                addressBalanceInfo => addressBalanceInfo.Balance > 0)
                .Select(addressBalanceInfo => addressBalanceInfo.Address).ToList();
    }
}