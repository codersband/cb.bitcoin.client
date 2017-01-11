﻿using System;
using System.Linq;
using NBitcoin;
using QBitNinja.Client.Models;

namespace CB.Bitcoin.Client.Histories
{
    public class AddressHistoryRecord : IEquatable<AddressHistoryRecord>
    {
        private readonly BalanceOperation _operation;

        public readonly string Address;

        public AddressHistoryRecord(string address, BalanceOperation operation)
        {
            Address = address;
            _operation = operation;
        }

        public decimal Amount
        {
            get
            {
                var amount = (from Coin coin in _operation.ReceivedCoins
                    let address =
                        coin.GetScriptCode().GetDestinationAddress(new BitcoinPubKeyAddress(Address).Network).ToWif()
                    where address == Address
                    select coin.Amount.ToDecimal(MoneyUnit.BTC)).Sum();
                return (from Coin coin in _operation.SpentCoins
                    let address =
                        coin.GetScriptCode().GetDestinationAddress(new BitcoinPubKeyAddress(Address).Network).ToWif()
                    where address == Address
                    select coin).Aggregate(amount, (current, coin) => current - coin.Amount.ToDecimal(MoneyUnit.BTC));
            }
        }

        public DateTimeOffset DateTime => _operation.FirstSeen;
        public bool Confirmed => _operation.Confirmations > 0;
        public int Confirmations => _operation.Confirmations;
        public string TransactionId => _operation.TransactionId.ToString();

        public bool Equals(AddressHistoryRecord other)
        {
            return this.TransactionId == other.TransactionId
                && this.Address == other.Address;
        }
    }
}