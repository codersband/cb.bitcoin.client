using System.Collections.Generic;
using System.Linq;
using CB.Bitcoin.Client.KeyManagement;

namespace CB.Bitcoin.Client.Sending
{
    public class HttpKeyRingBuilder : HttpBuilder
    {
        public HttpKeyRingBuilder(HttpKeyRing keyRing) : base(keyRing.Network)
        {
            AssertNetwork(keyRing.Network);
            KeyRing = keyRing;
        }

        public HttpKeyRing KeyRing { get; }

        public TransactionInfo BuildTransaction(List<AddressAmountPair> to, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = KeyRing.NotEmptyAddresses.Select(KeyRing.GetPrivateKey).ToList();

            return BuildTransaction(
                notEmptyPrivateKeys,
                to,
                feeType,
                KeyRing.UnusedAddresses.First(),
                message
                );
        }

        public TransactionInfo BuildSpendAllTransaction(string toAddress, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = KeyRing.NotEmptyAddresses.Select(KeyRing.GetPrivateKey).ToList();

            return BuildSpendAllTransaction(
                notEmptyPrivateKeys,
                toAddress,
                feeType,
                message
                );
        }
    }
}