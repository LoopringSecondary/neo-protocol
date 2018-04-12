using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using Neo.SmartContract.Framework.Services.System;
using System.ComponentModel;

namespace TokenTransferDelegate
{
    public class TokenTransferDelegate : SmartContract
    {
        public static readonly byte[] SuperAdmin = "ASXA7rzm9hnrnhbReFEKVHknWpLnGhnN8T".ToScriptHash();
        private static readonly byte[] GAS_ASSET_ID = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;

        public delegate object NEP5Contract(string method, object[] args);

        [DisplayName("transferred")]
        public static event Action<byte[], byte[], byte[], BigInteger> Transferred; // (assetId, owner, to, amount)

        /// <summary>
        ///   This smart contract is designed to delegate to transfer nep5 tokens.
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

                //TODO this length is not 53
                if (invocationTransaction.Script.Length != 53)
                {
                    return false;
                }

                if (invocationTransaction.Script.Range(24, 34) != "transferToken".AsByteArray().Concat(new byte[] { 0x67 }).Concat(ExecutionEngine.ExecutingScriptHash))
                {
                    return false;
                }

                return true;
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "transferToken")
                {
                    if (args.Length != 4) return false;
                    byte[] assetId = (byte[])args[0];
                    byte[] owner = (byte[])args[1];
                    byte[] to = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return TransferToken(assetId, owner, to, amount);
                }
            }
            return false;
        }

        /// <summary>
        ///   Transfer nep5 token by transferFrom.
        /// </summary>
        /// <param name="assetId">
        ///   The nep5 token assetId.
        /// </param>
        /// <param name="owner">
        ///   The account who invoke approve at the begin.
        /// </param>
        /// <param name="to">
        ///   The target account.
        /// </param>
        /// <param name="amount">
        ///   The account to tranfer.
        /// </param>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool TransferToken(byte[] assetId, byte[] owner, byte[] to, BigInteger amount)
        {
            if(assetId.Length !=20 || owner.Length != 20  || to.Length != 20)
            {
                return false;
            }

            var args = new object[] { owner, ExecutionEngine.ExecutingScriptHash, to, amount };
            var contract = (NEP5Contract)assetId.ToDelegate();
            bool result = (bool)contract("transferFrom", args);
            if (!result)
            {
                Runtime.Log("Failed to transferFrom NEP-5 tokens!");
                return false;
            }

            Transferred(assetId, owner, to, amount);
            return true;
        }
    }
}
