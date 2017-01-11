using System;
using CB.Bitcoin.Client.Balances;
using CB.Bitcoin.Client.Histories;
using CB.Bitcoin.Client.Interfaces;

namespace CB.Bitcoin.Client.Monitoring
{
    public abstract class Monitor : IAssertNetwork
    {
        // ReSharper disable once InconsistentNaming
        protected readonly NBitcoin.Network _Network;

        protected Monitor(Network network)
        {
            _Network = network.ToNBitcoinNetwork();
        }

        public Network Network => _Network.ToHiddenBitcoinNetwork();

        public void AssertNetwork(Network network)
        {
            if (network != Network)
                throw new Exception("Wrong network");
        }

        public void AssertNetwork(NBitcoin.Network network)
        {
            if (network != _Network)
                throw new Exception("Wrong network");
        }

        public abstract AddressBalanceInfo GetAddressBalanceInfo(string address);
        public abstract TransactionInfo GetTransactionInfo(string transactionId);
        public abstract AddressHistory GetAddressHistory(string address);
    }
}