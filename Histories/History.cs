using System;
using System.Collections.Generic;
using System.Linq;

namespace CodersBand.Bitcoin.Histories
{
    public abstract class History
    {
        protected History(IEnumerable<AddressHistoryRecord> records)
        {
            Records = records.ToList()
                .OrderBy(x => x.Confirmed)
                .ThenByDescending(x => x.DateTime).ToList();
        }

        public List<AddressHistoryRecord> Records { get; }
        public decimal TotalReceived => Records.Sum(x => x.Amount > 0 ? Math.Abs(x.Amount) : 0);
        public decimal TotalSpent => Records.Sum(x => x.Amount < 0 ? Math.Abs(x.Amount) : 0);
    }
}