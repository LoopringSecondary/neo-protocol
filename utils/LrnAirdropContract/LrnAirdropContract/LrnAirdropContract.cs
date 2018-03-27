using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace LrnAirdropContract
{
    public class LrnAirdropContract : SmartContract
    {
        public static readonly byte[] SuperAdmin = "ASXA7rzm9hnrnhbReFEKVHknWpLnGhnN8T".ToScriptHash();
        private static readonly byte[] GAS_ASSET_ID = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;
        private const int AIRDROP_START_TIME = 1514736000;//2018-01-01 00:00:00
        private const string AIR_DROP_SUPPLY = "airdropSupply";
        private const ulong PERIOD = 730;//two years
        private const int SECONDS_PER_DAY = 86400;

        [Appcall("06c29b2661be437e9c38485f8797cb4c59ed5999")]
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
        ///   Optional input parameters. 
        /// </param>
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
                var type = tx.Type;
                var inputs = tx.GetInputs();
                var outputs = tx.GetOutputs();
                var attributes = tx.GetAttributes();

                if (type != INVOCATION_TRANSACTION_TYPE) return false;

                if (inputs.Length != 1 || outputs.Length != 1 || attributes.Length != 0)
                {
                    return false;
                }

                if (outputs[0].AssetId != GAS_ASSET_ID && outputs[0].Value != 1)
                {
                    return false;
                }

                var invocationTransaction = (InvocationTransaction)tx;
                if (invocationTransaction.Script.Length != 53)
                {
                    return false;
                }

                if (invocationTransaction.Script.Range(24,29) != "withdraw".AsByteArray().Concat(new byte[]{0x67}).Concat(ExecutionEngine.ExecutingScriptHash))
                {
                    return false;
                }

                return true;
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy")
                {
                    if (!Runtime.CheckWitness(SuperAdmin)) return false;
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
            if (!Runtime.CheckWitness(SuperAdmin)) return false;
            if (args.Length != 2) return false;
            string account = (string)args[0];
            BigInteger depositAmount = (BigInteger)args[1];
            Storage.Put(Storage.CurrentContext, account, depositAmount);
            BigInteger supply = Storage.Get(Storage.CurrentContext, AIR_DROP_SUPPLY).AsBigInteger();
            Storage.Put(Storage.CurrentContext, AIR_DROP_SUPPLY, supply + depositAmount);
            return true;
        }

        /// <summary>
        ///   Withdraw the available amount to the account.
        /// </summary>
        /// <param name="account">
        ///   The account to withdraw.
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
            BigInteger withdrawAmount = balance * n / PERIOD;
            return withdrawAmount;
        }
    }
}
