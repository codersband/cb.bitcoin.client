namespace CodersBand.Bitcoin.States
{
    public enum TransactionBuildState
    {
        NotInProgress,
        GatheringCoinsToSpend,
        BuildingTransaction,
        CheckingTransaction
    }
}