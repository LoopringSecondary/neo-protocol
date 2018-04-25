using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace LoopringProtocol
{
    public class LoopringProtocol : SmartContract
    {
        private static readonly byte[] SuperAdmin = "AdqLRCBxDRTQLDqQE8GMSGU4j2ydYPLQHv".ToScriptHash();

        private static readonly byte[] GAS_ASSET_ID = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };

        private static readonly byte[] cutoffPrefix = "cutoff-".AsByteArray();

        private static readonly byte[] cancelledPrefix = "cancelled-".AsByteArray();

        private static readonly byte[] lrnTokenScriptHash = "AGQmqvmBbeGxSSGEhCSuJRLufhDFDr1AfD".ToScriptHash();

        private static uint ringIndex = 0;

        [DisplayName("OrderCancelled")]
        public static event Action<byte[], BigInteger> OrderCancelled; //(ownerHash, name)

        [DisplayName("AllOrdersCancelled")]
        public static event Action<byte[], uint> AllOrdersCancelled; //(ownerHash, uint)

        [DisplayName("RingMined")]
        public static event Action<BigInteger, byte[]> RingMined; //(ringIndex, ownerHash)

        private struct OrderStatus
        {
            public byte[] owner;
            public byte[] tokenS;
            public byte[] tokenB;
            public byte[] wallet;
            public byte[] authAddr;
            public BigInteger validSince;
            public BigInteger validUntil;
            public BigInteger amountS;
            public BigInteger amountB;
            public BigInteger lrcFee;
            public Boolean buyNoMoreThanAmountB;
            public Boolean marginSplitAsFee;
            public byte[] orderHash;
            public BigInteger marginSplitPercentage;
            public BigInteger rateS;
            public BigInteger rateB;
            public BigInteger fillAmountS;
            public BigInteger lrcReward;
            public BigInteger lrcFeeState;
            public BigInteger splitS;
            public BigInteger splitB;
        }


        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(SuperAdmin);
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "cancelOrder")
                {
                    if(args.Length != 3) {
                        return false;
                    }
                    byte[][] addrs = (byte[][])args[0];
                    if (addrs.Length != 5) return false;

                    BigInteger[] values = (BigInteger[])args[1];
                    if (values.Length != 6) return false;

                    BigInteger cancelAmount = values[5];
                    OrderStatus order = ConstructOrder(addrs, values, (Boolean)args[2]);

                    byte[] orderHash = CalculateOrderHash(order);
                    Storage.Put(Storage.CurrentContext, cancelledPrefix.Concat(orderHash), cancelAmount);
                    OrderCancelled(orderHash, cancelAmount);
                    return true;
                }

                if (operation == "cancelAllOrders")
                {
                    uint cutoff = (uint)args[0];
                    uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                    byte[] sender = GetSender();
                    if (cutoff < now && sender.Length == 20)
                    {
                        Storage.Put(Storage.CurrentContext, cutoffPrefix.Concat(sender), cutoff);
                        AllOrdersCancelled(sender, cutoff);
                        return true;
                    }
                    return false;
                }

                if (operation == "submitRing")
                {
                    //RingMined();
                    return true;
                }

                return false;
            }
            return false;
        }

        // get sender script hash
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)Neo.SmartContract.Framework.Services.System.ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();

            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == GAS_ASSET_ID) return output.ScriptHash;
            }
            return new byte[] { };
        }

        private static byte[] CalculateOrderHash(OrderStatus order)
        {
            var bytes = order.owner
                .Concat(order.tokenS)
                .Concat(order.tokenB)
                .Concat(order.wallet)
                .Concat(order.authAddr)
                .Concat(order.amountS.AsByteArray())
                .Concat(order.amountB.AsByteArray())
                .Concat(order.validSince.AsByteArray())
                .Concat(order.validUntil.AsByteArray())
                .Concat(order.lrcFee.AsByteArray());

            return Hash256(bytes);
        }

        private static OrderStatus ConstructOrder(byte[][] addrs, BigInteger[] values, Boolean buyNoMore)
        {
            return new OrderStatus
            {

                owner = addrs[0],
                tokenS = addrs[1],
                tokenB = addrs[2],
                wallet = addrs[3],
                authAddr = addrs[4],

                validSince = values[0],
                validUntil = values[1],
                amountS = values[2],
                amountB = values[3],
                lrcFee = values[4],

                buyNoMoreThanAmountB = buyNoMore,
                marginSplitAsFee = false,
                orderHash = new byte[] { 0 },
                marginSplitPercentage = 0,
                rateS = 0,
                rateB = 0,
                fillAmountS = 0,
                lrcReward = 0,
                lrcFeeState = 0,
                splitS = 0,
                splitB = 0
            };
        }
    }
}
