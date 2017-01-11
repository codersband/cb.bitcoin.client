namespace CB.Bitcoin.Client
{
    public struct InOutInfo
    {
        public string Address;
        public decimal Amount;

        public InOutInfo(string address, decimal amount)
        {
            Address = address;
            Amount = amount;
        }
    }
}