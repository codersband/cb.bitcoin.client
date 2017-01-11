using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CB.Bitcoin.Client.Balances;
using CB.Bitcoin.Client.Histories;
using CB.Bitcoin.Client.KeyManagement;
using CB.Bitcoin.Client.States;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace CB.Bitcoin.Client.Monitoring
{
    internal class PeriodicUpdate
    {
        private readonly CancellationTokenSource _updateTimeOutSource;
        private volatile bool _isMoniotorEnabled;
        private CancellationToken _cancellationToken;
        private readonly Action _cycleAction;

        public TimeSpan PeriodTimeSpan { set; get; }

        public PeriodicUpdate(Action cycleAction)
        {
            _cycleAction = cycleAction;

            PeriodTimeSpan = TimeSpan.FromSeconds(5);

            _updateTimeOutSource = new CancellationTokenSource();
            _cancellationToken = _updateTimeOutSource.Token;
        }

        public async void StartAsync()
        {
            if (!_isMoniotorEnabled)
            {
                _cancellationToken = _updateTimeOutSource.Token;
                _isMoniotorEnabled = true;

                while (_isMoniotorEnabled)
                {
                    try
                    {
                        await Task.Delay(PeriodTimeSpan, _cancellationToken);
                    }
                    catch (TaskCanceledException) { }
                    _cycleAction();
                }
            } 
        }

        public void Stop()
        {
            _isMoniotorEnabled = false;
            _updateTimeOutSource.Cancel();
        }
    }

    public class BalanceUpdateEventArgs: EventArgs
    {
        public List<AddressHistoryRecord> HistoryRecords { set; get; }
    }

    public class HttpKeyRingMonitor : HttpMonitor, INotifyPropertyChanged
    {
        private readonly WalletClient _qBitNinjaWalletClient;

        private int _initializationProgressPercent;
        private State _initializationState;
        private KeyRingBalanceInfo _safeBalanceInfo;
        private KeyRingHistory _keyRingHistory;

        private int _syncProgressPercent;
        internal KeyRing BaseKeyRing;

        private readonly PeriodicUpdate _periodicUpdate;

        public HttpKeyRingMonitor(KeyRing keyRing, int addressCount) : base(keyRing.Network)
        {
            AssertNetwork(keyRing.Network);
            AddressCount = addressCount;
            BaseKeyRing = keyRing;
            KeyRing = new HttpKeyRing(this);

            _periodicUpdate = new PeriodicUpdate(() =>
            {
                if (_syncProgressPercent == 100)
                    UpdateKeyRingHistoryAndBalanceInfo();
            });

            _qBitNinjaWalletClient = Client.GetWalletClient(QBitNinjaWalletName);
            _qBitNinjaWalletClient.CreateIfNotExists().Wait();

            StartInitializingQBitNinjaWallet();
        }

        public KeyRingHistory KeyRingHistory
        {
            get
            {
                if (_keyRingHistory == null)
                    UpdateKeyRingHistoryAndBalanceInfo();
                return _keyRingHistory;
            }
            private set
            {
                var changeHappened = false;
                var historyRecords = new List<AddressHistoryRecord>();
                if (_keyRingHistory != null)
                {
                    if (_keyRingHistory.Records.Count != value.Records.Count)
                    {
                        historyRecords.AddRange(value.Records.Where(r=>!_keyRingHistory.Records.Contains(r)));
                        changeHappened = true;
                    }
                    else
                    {                      
                        for (var i = 0; i < _keyRingHistory.Records.Count; i++)
                        {
                            if (_keyRingHistory.Records[i].Confirmed != value.Records[i].Confirmed)
                            {
                                AddOrUpdateHistoryRecords(value.Records[i], ref historyRecords);
                                changeHappened = true;
                            }
                            if (_keyRingHistory.Records[i].TransactionId != value.Records[i].TransactionId)
                            {
                                AddOrUpdateHistoryRecords(value.Records[i], ref historyRecords);
                                changeHappened = true; // Malleability check
                            }
                        }
                    }
                }

                _keyRingHistory = value;
                AdjustState(AddressCount);
                if (changeHappened) OnBalanceChanged(historyRecords);
            }
        }

        private static void AddOrUpdateHistoryRecords(AddressHistoryRecord record, ref List<AddressHistoryRecord> historyRecords)
        {
            var foundIndex = historyRecords.IndexOf(record);
            if (foundIndex < 0)
                historyRecords.Add(record);
            else
                historyRecords[foundIndex] = record;
        }

        public State InitializationState
        {
            get { return _initializationState; }
            private set
            {
                if (value == _initializationState) return;
                _initializationState = value;
                OnPropertyChanged();
                OnInitializationStateChanged();
            }
        }

        public int InitializationProgressPercent
        {
            get { return _initializationProgressPercent; }
            private set
            {
                if (value == _initializationProgressPercent) return;

                switch (value)
                {
                    case 0:
                        InitializationState = State.NotStarted;
                        break;
                    case 100:
                        InitializationState = State.Ready;
                        break;
                    default:
                        if (value > 0 && value < 100) InitializationState = State.InProgress;
                        else
                            throw new ArgumentOutOfRangeException(
                                $"InitializationProgressPercent cannot be {value}. It must be >=0 and <=100");
                        break;
                }
                _initializationProgressPercent = value;
                OnPropertyChanged();
                OnInitializationProgressPercentChanged();
            }
        }

        public int AddressCount { get; }

        public HttpKeyRing KeyRing { get; }

        private string QBitNinjaWalletName
        {
            get
            {
                // Let's generate the walletname from seedpublickey
                var bitcoinExtPubKey = new BitcoinExtPubKey(KeyRing.SeedPublicKey);
                // Let's get the pubkey, so the chaincode is lost
                var pubKey = bitcoinExtPubKey.ExtPubKey.PubKey;
                // Let's get the address, you can't directly access it from the keyRing
                // Also nobody would ever use this address for anything
                var address = pubKey.GetAddress(_Network).ToWif();
                // Let's just simply add the addresscount so in case we have the same keyRing, but different
                // sizes it should be in an other wallet
                return address + AddressCount;
            }
        }

        public KeyRingBalanceInfo KeyRingBalanceInfo
        {
            get
            {
                if (_safeBalanceInfo == null)
                    UpdateKeyRingHistoryAndBalanceInfo();
                return _safeBalanceInfo;
            }
            set
            {
                _safeBalanceInfo = value;

                AdjustState(AddressCount);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ResumeMonitoring()
        {
            _periodicUpdate.StartAsync();
        }

        public void StopMonitoring()
        {
            _periodicUpdate.Stop();
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler InitializationStateChanged;
        public event EventHandler InitializationProgressPercentChanged;
        public event EventHandler BalanceChanged;

        protected virtual void OnInitializationStateChanged()
        {
            InitializationStateChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnInitializationProgressPercentChanged()
        {
            InitializationProgressPercentChanged?.Invoke(this, EventArgs.Empty);
        }

        internal async void StartInitializingQBitNinjaWallet()
        {
            await Task.Run(async () =>
            {
                InitializationState = State.NotStarted;
                InitializationProgressPercent = 0;
                List<string> outOfSyncAddresses;
                do
                {
                    outOfSyncAddresses = await GetOutOfSyncAddressesAsync();
                    AdjustState(AddressCount - outOfSyncAddresses.Count);

                    if (outOfSyncAddresses.Count == 0) continue;

                    foreach (var address in outOfSyncAddresses)
                    {
                        AdjustState(outOfSyncAddresses.IndexOf(address));
                        await _qBitNinjaWalletClient.CreateAddressIfNotExists(new BitcoinPubKeyAddress(address));
                    }
                } while (outOfSyncAddresses.Count != 0);
            });

            _periodicUpdate.StartAsync();
        }

        private void AdjustState(int syncedAddressCount)
        {
            if (syncedAddressCount < 0 || syncedAddressCount > AddressCount)
                throw new ArgumentOutOfRangeException(
                    $"syncedAddressCount cannot be {syncedAddressCount}. It must be >=0 and <=AddressCount");

            _syncProgressPercent = (int) Math.Round((double) (100*syncedAddressCount)/AddressCount);
            if (_syncProgressPercent == 100)
            {
                if (_keyRingHistory == null || _safeBalanceInfo == null)
                    InitializationProgressPercent = 99;
                else
                    InitializationProgressPercent = 100;
            }
            else InitializationProgressPercent = _syncProgressPercent;
        }

        private async Task<List<string>> GetOutOfSyncAddressesAsync()
        {
            var qbitResult = await _qBitNinjaWalletClient.GetAddresses();
            var qbitAddresses = qbitResult.Select(x => x.Address.ToWif()).ToList();
            var safeAddresses = new HashSet<string>();
            for (var i = 0; i < AddressCount; i++)
            {
                safeAddresses.Add(KeyRing.GetAddress(i));
            }

            if (qbitAddresses.Any(qbitAddress => !safeAddresses.Contains(qbitAddress)))
            {
                throw new Exception("QBitNinja wallet and HTTPKeyRingMonitor is out of sync.");
            }

            return safeAddresses.Where(safeAddress => !qbitAddresses.Contains(safeAddress)).ToList();
        }

        public KeyRingBalanceInfo GetKeyRingBalanceInfo()
        {
            AssertSynced();

            UpdateKeyRingHistoryAndBalanceInfo();

            return KeyRingBalanceInfo;
        }

        private async void UpdateKeyRingHistoryAndBalanceInfo()
        {
            AssertSynced();

            var balance = await _qBitNinjaWalletClient.GetBalance();            
            var balanceOperations = balance.Operations;

            // Find all the operations concerned to one address
            // address, balanceoperationlist
            var addressOperationPairs = new HashSet<Tuple<string, BalanceOperation>>();
            // address, unconfirmed, confirmed
            var receivedAddressAmountPairs = new HashSet<Tuple<string, decimal, decimal>>();
            var spentAddressAmountPairs = new HashSet<Tuple<string, decimal, decimal>>();
            foreach (var operation in balanceOperations)
            {
                foreach (var coin in operation.ReceivedCoins)
                {
                    string address;
                    if (!KeyRingContainsCoin(out address, coin)) continue;

                    var amount = ((Money) coin.Amount).ToDecimal(MoneyUnit.BTC);

                    receivedAddressAmountPairs.Add(operation.Confirmations == 0
                        ? new Tuple<string, decimal, decimal>(address, amount, 0m)
                        : new Tuple<string, decimal, decimal>(address, 0m, amount));

                    addressOperationPairs.Add(new Tuple<string, BalanceOperation>(address, operation));
                }

                foreach (var coin in operation.SpentCoins)
                {
                    string address;
                    if (!KeyRingContainsCoin(out address, coin)) continue;

                    var amount = ((Money) coin.Amount).ToDecimal(MoneyUnit.BTC);
                    spentAddressAmountPairs.Add(operation.Confirmations == 0
                        ? new Tuple<string, decimal, decimal>(address, amount, 0m)
                        : new Tuple<string, decimal, decimal>(address, 0m, amount));

                    addressOperationPairs.Add(new Tuple<string, BalanceOperation>(address, operation));
                }
            }

            var addressOperationsDict = new Dictionary<string, HashSet<BalanceOperation>>();
            foreach (var pair in addressOperationPairs)
                if (addressOperationsDict.Keys.Contains(pair.Item1))
                    addressOperationsDict[pair.Item1].Add(pair.Item2);
                else
                    addressOperationsDict.Add(pair.Item1, new HashSet<BalanceOperation> {pair.Item2});

            var addressHistories =
                addressOperationsDict.Select(pair => new AddressHistory(pair.Key, pair.Value)).ToList();

            foreach (var address in KeyRing.Addresses)
            {
                if (!addressHistories.Select(x => x.Address).Contains(address))
                    addressHistories.Add(new AddressHistory(address, new List<BalanceOperation>()));
            }

            var addressBalanceInfoList = new List<AddressBalanceInfo>();
            foreach (var addressHistory in addressHistories)
            {
                var unconfirmed = 0m;
                var confirmed = 0m;
                foreach (var record in addressHistory.Records)
                {
                    if (record.Confirmed)
                        confirmed += record.Amount;
                    else unconfirmed += record.Amount;
                }

                var addressBalanceInfo = new AddressBalanceInfo(addressHistory.Address, unconfirmed, confirmed);
                addressBalanceInfoList.Add(addressBalanceInfo);
            }

            KeyRingBalanceInfo = new KeyRingBalanceInfo(KeyRing, addressBalanceInfoList);
            KeyRingHistory = new KeyRingHistory(KeyRing, addressHistories);
        }

        public KeyRingHistory GetKeyRingHistory()
        {
            AssertSynced();

            UpdateKeyRingHistoryAndBalanceInfo();

            return KeyRingHistory;
        }

        private bool KeyRingContainsCoin(out string address, ICoin coin)
        {
            try
            {
                address = coin.GetScriptCode().GetDestinationAddress(_Network).ToWif();
            }
            catch
            {
                // Not concerned, keyRing can't contain something like this
                address = null;
                return false;
            }
            return KeyRing.Addresses.Contains(address);
        }

        private void AssertSynced()
        {
            if (_syncProgressPercent != 100)
                throw new Exception("HttpKeyRingMonitor is not synced with QBitNinja wallet.");
        }

        protected virtual void OnBalanceChanged(List<AddressHistoryRecord> historyRecords)
        {
            BalanceChanged?.Invoke(this, new BalanceUpdateEventArgs{HistoryRecords = historyRecords});
        }
    }
}