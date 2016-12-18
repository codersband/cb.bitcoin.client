using System.Collections.Generic;
using System.Linq;
using CodersBand.Bitcoin.KeyManagement;

namespace CodersBand.Bitcoin.Balances
{
    public class KeyRingBalanceInfo : BalanceInfo
    {
        public List<AddressBalanceInfo> AddressBalances;

        public KeyRingBalanceInfo(KeyRing keyRing, List<AddressBalanceInfo> addressBalances) :
            base(addressBalances.Sum(x => x.Unconfirmed), addressBalances.Sum(x => x.Confirmed))
        {
            KeyRing = keyRing;
            AddressBalances = addressBalances;
        }

        public KeyRing KeyRing { get; }
        public int MonitoredAddressCount => AddressBalances.Count;
    }
}