using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace LrnAirdropContract
{
    public class LrnAirdropContract : SmartContract
    {
        public static readonly byte[] Owner = "ASXA7rzm9hnrnhbReFEKVHknWpLnGhnN8T".ToScriptHash();
        private const int AIRDROP_START_TIME = 88888888;
        private const string AIR_DROP_SUPPLY = "airdropSupply";
        private const ulong RATE = 730;//two years
        private const int SECONDS_PER_DAY = 86400;
        public delegate byte[] DynamicCall(string method, object[] arr);

        [Appcall("36cef40c189c4291f3595a12a379f84b5d652dbb")]
        static extern object CallLrn(string method, object[] arr);

        /// <summary>
        ///   This smart contract is designed to airdrop and withdraw according to time.
        ///   Parameter List: 0710
        ///   Return List: 05
        /// </summary>
        /// <param name="operation">
        ///   The method being invoked.
        /// </param>
        /// <param name="args">
        ///   Optional input parameters used by the NEP5 methods. 
        /// </param>
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = operation.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy")
                {
                    if (!Runtime.CheckWitness(Owner)) return false;
                    Storage.Put(Storage.CurrentContext, AIR_DROP_SUPPLY, 0);
                    return true;
                }
                if (operation == "deposit")
                {
                    return Deposit(args);
                }
                if (operation == "withdraw")
                {
                    if (args.Length != 1) return false;
                    byte[] to = (byte[])args[0];
                    return Withdraw(to);
                }
                if (operation == "queryAirDropSupply")
                {
                    return Storage.Get(Storage.CurrentContext, AIR_DROP_SUPPLY).AsBigInteger();
                }
            }
            return false;
        }

        /// <summary>
        ///   deposit the amount to an account.
        /// </summary>
        /// <param name="args">
        ///   The contract invoker.
        /// </param>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool Deposit(object[] args)
        {
            if (!Runtime.CheckWitness(Owner)) return false;
            if (args.Length != 2) return false;
            string account = (string)args[0];
            BigInteger depositAmount = (BigInteger)args[1];
            Storage.Put(Storage.CurrentContext, account, depositAmount);
            BigInteger supply = Storage.Get(Storage.CurrentContext, AIR_DROP_SUPPLY).AsBigInteger();
            Storage.Put(Storage.CurrentContext, AIR_DROP_SUPPLY, supply + depositAmount);
            return true;
        }

        /// <summary>
        ///   Withdraw the available amount to an account.
        /// </summary>
        /// <param name="account">
        ///   The contract invoker.
        /// </param>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool Withdraw(byte[] account)
        {
            BigInteger withdrawAmount = CalcAvailableAmount(account);
            if (withdrawAmount < 1) return false;
            byte[] from = Neo.SmartContract.Framework.Services.System.ExecutionEngine.ExecutingScriptHash;
            // call lrn transfer
            byte[] rt = (byte[])CallLrn("transfer", new object[] { from, account, withdrawAmount });
            bool succ = rt.AsBigInteger() == 1;
            if (succ)
            {
                BigInteger balance = Storage.Get(Storage.CurrentContext, account).AsBigInteger();
                Storage.Put(Storage.CurrentContext, account, balance - withdrawAmount);
            }
            return true;
        }

        /// <summary>
        ///   Calculate the available amount for the account.
        /// </summary>
        /// <param name="account">
        /// </param>
        /// <returns>
        ///  available amount to withdraw
        /// </returns>
        public static BigInteger CalcAvailableAmount(byte[] account)
        {
            BigInteger balance = Storage.Get(Storage.CurrentContext, account).AsBigInteger();
            if (balance <= 1) return balance;
            uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            int time = (int)now - AIRDROP_START_TIME;
            if (time < 0) return 0;
            int n = time / SECONDS_PER_DAY + 1;
            BigInteger withdrawAmount = balance * n / RATE;
            return withdrawAmount;
        }

        private static byte[] IntToBytes(BigInteger value)
        {
            byte[] buffer = value.ToByteArray();
            return buffer;
        }

        private static BigInteger BytesToInt(byte[] array)
        {
            var buffer = new BigInteger(array);
            return buffer;
        }
    }
}
