using System;
using NBitcoin;
using NBitcoin.Stealth;

namespace CB.Bitcoin.Client.KeyManagement
{
    public class KeyRing
    {
        public string KeyRingFilePath { get; }
        public string Seed => _seedPrivateKey.GetWif(_network).ToWif();
        public string SeedPublicKey => _seedPrivateKey.Neuter().GetWif(_network).ToWif();
        public Network Network => _network.ToHiddenBitcoinNetwork();

        private readonly NBitcoin.Network _network;
        private ExtKey _seedPrivateKey;

        protected KeyRing(string keyRingFilePath, Network network)
        {
            _network = network.ToNBitcoinNetwork();
            KeyRingFilePath = keyRingFilePath;
        }

        public KeyRing(KeyRing wallet)
        {
            _network = wallet._network;
            _seedPrivateKey = wallet._seedPrivateKey;
            KeyRingFilePath = wallet.KeyRingFilePath;
        }

        protected KeyRing(string password, string keyRingFilePath, Network network, string mnemonicString)
        {
            _network = network.ToNBitcoinNetwork();
            KeyRingFilePath = keyRingFilePath;
            SetSeed(password, mnemonicString);       
        }

        private Mnemonic SetSeed(string password, string mnemonicString = null)
        {
            var mnemonic =
                string.IsNullOrEmpty(mnemonicString)
                    ? new Mnemonic(Wordlist.English, WordCount.Twelve)
                    : new Mnemonic(mnemonicString);

            _seedPrivateKey = mnemonic.DeriveExtKey(password);

            return mnemonic;
        }

        private void SetSeed(ExtKey seedExtKey)
        {
            _seedPrivateKey = seedExtKey;
        }

        private void Save(string password)
        {
            var privateKey = _seedPrivateKey.PrivateKey;
            var chainCode = Convert.ToBase64String(_seedPrivateKey.ChainCode);
            var encryptedBitcoinPrivateKey = privateKey.GetEncryptedBitcoinSecret(password, _network).ToWif();

            KeyRingStore.Write(KeyRingFilePath, encryptedBitcoinPrivateKey, chainCode, _network.ToHiddenBitcoinNetwork());
        }

        /// <summary>
        ///     Creates a mnemonic, a seed, encrypts it and stores in the specified path.
        /// </summary>
        /// <param name="mnemonic">empty string</param>
        /// <param name="password"></param>
        /// <param name="walletFilePath"></param>
        /// <param name="network"></param>
        /// <returns>KeyRing</returns>
        public static KeyRing Create(out string mnemonic, string password, string walletFilePath, Network network)
        {
            var wallet = new KeyRing(walletFilePath, network);
            mnemonic = wallet.SetSeed(password).ToString();
            wallet.Save(password);
            return wallet;
        }

        public static KeyRing Recover(string mnemonic, string password, string walletFilePath, Network network)
        {
            var wallet = new KeyRing(password, walletFilePath, network, mnemonic);
            wallet.Save(password);
            return wallet;
        }

        public static KeyRing Load(string password, string walletFilePath)
        {
            var walletFileRawContent = KeyRingStore.Read(walletFilePath);

            var encryptedBitcoinPrivateKeyString = walletFileRawContent.EncryptedSeed;
            var chainCodeString = walletFileRawContent.ChainCode;
            var network = walletFileRawContent.Network;
            var chainCode = Convert.FromBase64String(chainCodeString);

            var wallet = new KeyRing(walletFilePath, network);

            var privateKey = Key.Parse(encryptedBitcoinPrivateKeyString, password, wallet._network);
            var seedExtKey = new ExtKey(privateKey, chainCode);

            wallet.SetSeed(seedExtKey);
            return wallet;
        }

        public virtual string GetAddress(int index)
        {
            const string startPath = NormalHdPath;

            var keyPath = new KeyPath(startPath + "/" + index);
            return _seedPrivateKey.Derive(keyPath).ScriptPubKey.GetDestinationAddress(_network).ToWif();
        }

        public virtual string GetPrivateKey(int index)
        {
            const string startPath = NormalHdPath;

            var keyPath = new KeyPath(startPath + "/" + index);
            return _seedPrivateKey.Derive(keyPath).GetWif(_network).ToWif();
        }

        public virtual PrivateKeyAddressPair GetPrivateKeyAddressPair(int index)
        {
            const string startPath = NormalHdPath;

            var keyPath = new KeyPath(startPath + "/" + index);
            var foo = _seedPrivateKey.Derive(keyPath).GetWif(_network);
            return new PrivateKeyAddressPair
            {
                PrivateKey = foo.ToWif(),
                Address = foo.ScriptPubKey.GetDestinationAddress(_network).ToWif()
            };
        }

        public void DeleteWalletFile()
        {
            KeyRingStore.Delete(KeyRingFilePath);
        }

        #region Hierarchy

        private const string StealthPath = "0'";
        private const string NormalHdPath = "1'";

        #endregion

        #region Stealth

        // ReSharper disable InconsistentNaming
        private Key _StealthSpendPrivateKey => _seedPrivateKey.Derive(new KeyPath(StealthPath + "/0'")).PrivateKey;
        public string StealthSpendPrivateKey => _StealthSpendPrivateKey.GetWif(_network).ToWif();
        private Key _StealthScanPrivateKey => _seedPrivateKey.Derive(new KeyPath(StealthPath + "/1'")).PrivateKey;
        public string StealthScanPrivateKey => _seedPrivateKey.Derive(1, true).Derive(1, true).GetWif(_network).ToWif();
        // ReSharper restore InconsistentNaming

        public string StealthAddress => 
            new BitcoinStealthAddress(_StealthScanPrivateKey.PubKey, new[] { _StealthSpendPrivateKey.PubKey }, 1, null, _network).ToWif();

        #endregion
    }
}
