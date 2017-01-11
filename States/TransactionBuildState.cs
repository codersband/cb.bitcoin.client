namespace CB.Bitcoin.Client.States
{
    public enum TransactionBuildState
    {
        NotInProgress,
        GatheringCoinsToSpend,
        BuildingTransaction,
        CheckingTransaction
    }
}