namespace CB.Bitcoin.Client.Interfaces
{
    internal interface IAssertNetwork
    {
        void AssertNetwork(Network network);
        void AssertNetwork(NBitcoin.Network network);
    }
}