using System.Collections.Generic;
using System.Linq;
using CodersBand.Bitcoin.KeyManagement;

namespace CodersBand.Bitcoin.Histories
{
    public class KeyRingHistory : History
    {
        public KeyRingHistory(KeyRing keyRing, IEnumerable<AddressHistory> addressHistories)
            : base(addressHistories.SelectMany(addressHistory => addressHistory.Records).ToList())
        {
            KeyRing = keyRing;
        }

        public KeyRing KeyRing { get; }
    }
}