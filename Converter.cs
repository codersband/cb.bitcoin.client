using System;
using NBitcoin;

namespace CB.Bitcoin.Client
{
    internal static class Converter
    {
        internal static NBitcoin.Network ToNBitcoinNetwork(this Network hNetwork)
        {
            switch (hNetwork)
            {
                case Network.MainNet:
                    return NBitcoin.Network.Main;
                case Network.TestNet:
                    return NBitcoin.Network.TestNet;
            }
            throw new InvalidOperationException("WrongNetwork");
        }

        internal static Network ToHiddenBitcoinNetwork(this NBitcoin.Network nNetwork)
        {
            if (nNetwork == NBitcoin.Network.Main)
                return Network.MainNet;
            if (nNetwork == NBitcoin.Network.TestNet)
                return Network.TestNet;
            throw new InvalidOperationException("WrongNetwork");
        }

        internal static ISecret ToISecret(string privateKey)
        {
            ISecret secret;

            try
            {
                secret = new BitcoinSecret(privateKey);
            }
            catch
            {
                try
                {
                    secret = new BitcoinExtKey(privateKey);
                }
                catch
                {
                    throw new Exception($"Private key in wrong format: {privateKey}");
                }
            }

            return secret;
        }
    }
}
