using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CB.Bitcoin.Client.KeyManagement
{
    [DataContract]
    internal class KeyRingStore
    {
        [IgnoreDataMember]
        private static readonly object Lock = new object();
        [DataMember]
        public string EncryptedSeed { get; set; }
        [DataMember]
        public string ChainCode { get; set; }
        [DataMember] 
        public Network Network { get; set; }

        public KeyRingStore(string encryptedBitcoinPrivateKey, string chainCode, Network network)
        {
            EncryptedSeed = encryptedBitcoinPrivateKey;
            ChainCode = chainCode;
            Network = network;
        }

        internal static void Write(string walletFilePath, string encryptedBitcoinPrivateKey, string chainCode, Network network)
        {
            if (File.Exists(walletFilePath))
                throw new Exception("WalletFileAlreadyExists");

            var content = new KeyRingStore(encryptedBitcoinPrivateKey, chainCode, network);
            var jsonSerializerconverter = new JsonSerializer();
            lock (Lock)
            {
                var directoryPath = Path.GetDirectoryName(Path.GetFullPath(walletFilePath));
                if (!string.IsNullOrEmpty(directoryPath)) Directory.CreateDirectory(directoryPath);

                using (var fileWriter = File.CreateText(walletFilePath))
                {
                    jsonSerializerconverter.Serialize(fileWriter, content);
                    fileWriter.Flush();
                }

                File.SetAttributes(walletFilePath, FileAttributes.ReadOnly | FileAttributes.Hidden);
            }
        }

        internal static KeyRingStore Read(string walletFilePath)
        {
            if(!File.Exists(walletFilePath))
                throw new Exception("WalletFileDoesNotExists");

            var contentString = File.ReadAllText(walletFilePath);
            return JsonConvert.DeserializeObject<KeyRingStore>(contentString);
        }

        internal static void Delete(string walletFilePath)
        {
            lock (Lock)
            {
                try
                {
                    var attributes = File.GetAttributes(walletFilePath);
                    if((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly 
                        || (attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        File.SetAttributes(walletFilePath, attributes & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden);
                    }
                    File.Delete(walletFilePath);
                }
                catch {}
            }
        }
    }
}
