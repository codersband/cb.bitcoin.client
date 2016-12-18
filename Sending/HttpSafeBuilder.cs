using System.Collections.Generic;
using System.Linq;
using CodersBand.Bitcoin.KeyManagement;

namespace CodersBand.Bitcoin.Sending
{
    public class HttpSafeBuilder : HttpBuilder
    {
        public HttpSafeBuilder(HttpKeyRing safe) : base(safe.Network)
        {
            AssertNetwork(safe.Network);
            Safe = safe;
        }

        public HttpKeyRing Safe { get; }

        public TransactionInfo BuildTransaction(List<AddressAmountPair> to, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return BuildTransaction(
                notEmptyPrivateKeys,
                to,
                feeType,
                Safe.UnusedAddresses.First(),
                message
                );
        }

        public TransactionInfo BuildSpendAllTransaction(string toAddress, FeeType feeType = FeeType.Fastest,
            string message = "")
        {
            var notEmptyPrivateKeys = Safe.NotEmptyAddresses.Select(Safe.GetPrivateKey).ToList();

            return BuildSpendAllTransaction(
                notEmptyPrivateKeys,
                toAddress,
                feeType,
                message
                );
        }
    }
}