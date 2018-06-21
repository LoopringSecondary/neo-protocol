using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace LoopringProtocol
{
    public class LoopringProtocol : SmartContract
    {
        private static readonly byte[] SuperAdmin = "AR7W16oCGSyKF4ebGjod9EFFwTUyRPZV9o".ToScriptHash();

        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;

        private static readonly byte[] CUTOFF = "cutoff-".AsByteArray();

        private static readonly byte[] ORDER_BASIC = "orderBasic-".AsByteArray();

        private static readonly byte[] ORDER_STATUS = "orderStatus-".AsByteArray();

        private static readonly byte[] ORDER_TIME = "orderTime-".AsByteArray();

        private static readonly byte[] MIN_ORDER_AMOUNT = "minOrderAmount-".AsByteArray();

        private static readonly byte[] AMOUNT_TO_SELL = "amountS-".AsByteArray();

        private static readonly byte[] AMOUNT_TO_BUY = "amountB-".AsByteArray();

        private static readonly byte[] SOLD_AMOUNT = "soldAmount-".AsByteArray();

        private static readonly byte[] BOUGHT_AMOUNT = "boughtAmount-".AsByteArray();

        private static readonly byte[] LRN_FEE = "lrnFee-".AsByteArray();

        private static readonly byte[] KEY_LRN_ASSET_ID = "lrnAssetId".AsByteArray();

        private static readonly byte[] METHOD_SUBMIT_RING = "submitRing".AsByteArray();

        private static readonly byte[] EQUIVALENT_RANGE = "equivalentRange-".AsByteArray();

        //according to the fee model of NEO, the number of the ring  needn't set too much
        private const int MAX_RING_ORDER_NUM = 3;

        private const int MIN_RING_ORDER_NUM = 2;

        private const int LENGTH_OF_SCRIPTHASH = 20;

        private const int LENGTH_OF_ORDERHASH = 32;

        public delegate object NEP5Contract(string method, object[] args);

        [DisplayName("OrderSubmitted")]
        public static event Action<byte[], byte[]> OrderSubmitted; //(owner, ownerHash)

        [DisplayName("OrderCancelled")]
        public static event Action<byte[], byte[]> OrderCancelled; //(owner, ownerHash)

        [DisplayName("AllOrdersCancelled")]
        public static event Action<byte[], BigInteger> AllOrdersCancelled; //(owner, cutoffTime)

        [DisplayName("RingMined")]
        public static event Action<byte[]> RingMined; //(ringHash)

        // Order status
        private static readonly byte[] Trading = { 0x01 };
        private static readonly byte[] AllFilled = { 0x02 };
        private static readonly byte[] Invalid = { 0x03 };

        private struct TradeOrder
        {
            public byte[] owner;
            public byte[] tokenS;
            public byte[] tokenB;
            public byte[] miner;
            public BigInteger orderTime;
            public BigInteger amountS;
            public BigInteger amountB;
            public BigInteger lrnFee;
            public byte[] orderHash;
            public byte[] status;
        }

        /// <summary>
        ///   This smart contract is loopring protocol
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
                if (Runtime.CheckWitness(SuperAdmin)) return true;
                if (!CheckTx()) return false;

                return true;
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "submitOrder")
                {
                    return SubmitOrder(args);
                }

                if (operation == "cancelOrder")
                {
                    return CancelOrder(args);
                }

                if (operation == "cancelAllOrders")
                {
                    return CancelAllOrders(args);
                }

                if (operation == "submitRing")
                {
                    return SubmitRing(args);
                }

                if (operation == "setMinOrderAmount")
                {
                    return SetMinOrderAmount(args);
                }

                if (operation == "setEquivalentRange")
                {
                    return SetMinOrderAmount(args);
                }

                if (operation == "setLrnAssetId")
                {
                    return SetLrnAssetId(args);
                }

                if (operation == "queryData")
                {
                    byte[] key = (byte[])args[0];
                    return Storage.Get(Storage.CurrentContext, key);
                }
            }
            return false;
        }

        private static bool CheckTx()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;

            var type = tx.Type;
            if (type != INVOCATION_TRANSACTION_TYPE) return false;

            var itx = (InvocationTransaction)tx;
            //2 or 3 nodes in a ring now
            if (itx.Script.Length != 100 || itx.Script.Length != 132)
            {
                Runtime.Log("Script Length illegal!");
                return false;
            }

            if (itx.Script[0] != 65 || itx.Script[0] != 97) return false;
            if (itx.Script[0] == 65 && itx.Script[1] != 2) return false;
            if (itx.Script[0] == 97 && itx.Script[1] != 3) return false;

            if(itx.Script[1] == 2)
            {
                if (itx.Script.Range(65, 34) != (new byte[] { 0x51, 0xc1, 0x0a }).Concat(METHOD_SUBMIT_RING).Concat(new byte[] { 0x67 }).Concat(ExecutionEngine.ExecutingScriptHash))
                {
                    Runtime.Log("Script illegal!");
                    return false;
                }
            } else {
                if (itx.Script.Range(97, 34) != (new byte[] { 0x51, 0xc1, 0x0a }).Concat(METHOD_SUBMIT_RING).Concat(new byte[] { 0x67 }).Concat(ExecutionEngine.ExecutingScriptHash))
                {
                    Runtime.Log("Script illegal!");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///  Create an order.
        /// </summary>
        /// <param name="args">
        ///  include the elements of the order.
        /// </param>
        /// <returns>
        ///  orderHash
        /// </returns>
        private static byte[] SubmitOrder(object[] args)
        {
            if (!CheckOrderParameter(args)) return new byte[] { 0 }; ;

            if (!Runtime.CheckWitness((byte[])args[0])) return new byte[] { 0 };

            TradeOrder order = ConstructTradeOrder(args);
            order.orderHash = CalculateOrderHash(order);

            //Orders should not be covered
            if (Storage.Get(Storage.CurrentContext, ORDER_BASIC.Concat(order.orderHash)).Length != 0) return new byte[] { 0 };

            StorageOrder(order);

            OrderSubmitted(order.owner, order.orderHash);
            return order.orderHash;
        }

        /// <summary>
        ///  Construct a TradeOrder
        /// </summary>
        /// <param name="args">
        ///  elements of the order.
        /// </param>
        /// <returns>
        ///  order
        /// </returns>
        private static TradeOrder ConstructTradeOrder(object[] args)
        {
            BigInteger timestamp = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            return new TradeOrder
            {
                owner = (byte[])args[0],
                tokenS = (byte[])args[1],
                tokenB = (byte[])args[2],
                miner = (byte[])args[3],

                amountS = (BigInteger)args[4],
                amountB = (BigInteger)args[5],
                lrnFee = (BigInteger)args[6],

                orderTime = timestamp,

                status = Trading
            };
        }

        /// <summary>
        ///  Check the order parameters before creating an order
        /// </summary>
        /// <param name="args">
        ///  elements of the order.
        /// </param>
        /// <returns>
        ///  check result
        /// </returns>
        private static bool CheckOrderParameter(object[] args)
        {
            if (args.Length != 7) return false;
            byte[] owner = (byte[])args[0];
            byte[] tokenS = (byte[])args[1];
            byte[] tokenB = (byte[])args[2];
            byte[] miner = (byte[])args[3];
            if (owner.Length != LENGTH_OF_SCRIPTHASH || tokenS.Length != LENGTH_OF_SCRIPTHASH
                || tokenB.Length != LENGTH_OF_SCRIPTHASH || miner.Length != LENGTH_OF_SCRIPTHASH) return false;

            BigInteger amountS = (BigInteger)args[4];
            BigInteger amountB = (BigInteger)args[5];
            BigInteger lrnFee = (BigInteger)args[6];

            //限制灰尘小单
            BigInteger minAmountS = Storage.Get(Storage.CurrentContext, MIN_ORDER_AMOUNT.Concat(tokenS)).AsBigInteger();
            BigInteger minAmountB = Storage.Get(Storage.CurrentContext, MIN_ORDER_AMOUNT.Concat(tokenB)).AsBigInteger();
            if (amountS <= minAmountS || amountB <= minAmountB || lrnFee < 0) return false;
            return true;
        }

        /// <summary>
        ///  Storage the order
        /// </summary>
        /// <param name="order">
        ///  TradeOrder.
        /// </param>
        /// <returns>
        ///  void
        /// </returns>
        private static void StorageOrder(TradeOrder order)
        {
            Storage.Put(Storage.CurrentContext, ORDER_BASIC.Concat(order.orderHash), order.owner.Concat(order.tokenS).Concat(order.tokenB).Concat(order.miner));
            Storage.Put(Storage.CurrentContext, AMOUNT_TO_SELL.Concat(order.orderHash), order.amountS);
            Storage.Put(Storage.CurrentContext, AMOUNT_TO_BUY.Concat(order.orderHash), order.amountB);
            Storage.Put(Storage.CurrentContext, ORDER_STATUS.Concat(order.orderHash), order.status);
            Storage.Put(Storage.CurrentContext, LRN_FEE.Concat(order.orderHash), order.lrnFee);
            Storage.Put(Storage.CurrentContext, ORDER_TIME.Concat(order.orderHash), order.orderTime);
        }



        /// <summary>
        ///  Cancel an order.
        /// </summary>
        /// <param name="args">
        /// owner/orderHash
        /// </param>
        /// <returns>
        ///  cancel result
        /// </returns>
        private static bool CancelOrder(object[] args)
        {
            if (args.Length != 2) return false;
            byte[] owner = (byte[])args[0];
            byte[] orderHash = (byte[])args[1];
            if (owner.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (!Runtime.CheckWitness(owner)) return false;
            if (orderHash.Length != LENGTH_OF_ORDERHASH) return false;
            var status = Storage.Get(Storage.CurrentContext, ORDER_STATUS.Concat(orderHash));
            if (status == Trading || status == Invalid) {
                Storage.Put(Storage.CurrentContext, ORDER_STATUS.Concat(orderHash), Invalid);
            } else {
                return false;
            }
            OrderCancelled(owner, orderHash);
            return true;
        }

        /// <summary>
        ///  Cancel all orders of the order owner.
        /// </summary>
        /// <param name="args">
        /// owner/cutoffTime
        /// </param>
        /// <returns>
        ///  cancel result
        /// </returns>
        private static bool CancelAllOrders(object[] args)
        {
            if (args.Length != 2) return false;
            byte[] owner = (byte[])args[0];
            if (!Runtime.CheckWitness(owner)) return false;
            BigInteger cutoffTime = (BigInteger)args[1];
            BigInteger now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            if (cutoffTime < now && owner.Length == LENGTH_OF_SCRIPTHASH)
            {
                Storage.Put(Storage.CurrentContext, CUTOFF.Concat(owner), cutoffTime);
                AllOrdersCancelled(owner, cutoffTime);
                return true;
            }
            return false;
        }

        /// <summary>
        ///  Check the status of the order
        /// </summary>
        /// <param name="args">
        /// owner/cutoffTime
        /// </param>
        /// <returns>
        /// If the order is valid.
        /// </returns>
        private static bool IsOrderValid(byte[] orderHash)
        {
            byte[] basicInfo = Storage.Get(Storage.CurrentContext, ORDER_BASIC.Concat(orderHash));
            byte[] owner = basicInfo.Range(0, LENGTH_OF_SCRIPTHASH);
            byte[] orderStatus = Storage.Get(Storage.CurrentContext, ORDER_STATUS.Concat(orderHash));

            if (orderStatus == AllFilled || orderStatus == Invalid) return false;

            //if the order expired
            BigInteger orderTime = Storage.Get(Storage.CurrentContext, ORDER_TIME.Concat(orderHash)).AsBigInteger();
            BigInteger cutoffTime = Storage.Get(Storage.CurrentContext, CUTOFF.Concat(owner)).AsBigInteger();
            if (cutoffTime >= orderTime) return false;

            return true;
        }

        /// <summary>
        ///  Submit the ring, the first node should the one which can be dealed completely 
        /// </summary>
        /// <param name="args">
        /// orderNum/orderHashes
        /// </param>
        /// <returns>
        /// the result of the deal
        /// </returns>
        private static bool SubmitRing(object[] args)
        {
            if (args.Length != 1) return false;

            byte[] parameter = (byte[])args[0];
            BigInteger orderNum = parameter.Range(0, 1).AsBigInteger();
            if (parameter.Length != orderNum * LENGTH_OF_ORDERHASH + 1 || orderNum > MAX_RING_ORDER_NUM 
                || orderNum < MIN_RING_ORDER_NUM) return false;

            byte[][] orderHashes = new byte[(int)orderNum][];
            for (int i = 0; i < orderNum; i++)
            {
                orderHashes[i] = parameter.Range(i * LENGTH_OF_ORDERHASH + 1, LENGTH_OF_ORDERHASH);
            }

            byte[][] operateArray = ParseRing((int)orderNum, orderHashes);
            if (operateArray.Length == 0) return false;

            if (!CheckOperateArray((int)orderNum, operateArray)) return false;

            ExcuteOperateArray((int)orderNum, operateArray);
            UpdateOrders((int)orderNum, orderHashes, operateArray);
            RingMined(parameter);
            return true;
        }

        /// <summary>
        ///  Excute the OperateArray 
        /// </summary>
        /// <param name="args">
        /// orderNum/operateArray(assetId--from--to--amount)
        /// </param>
        /// <returns>
        /// the result of the execution
        /// </returns>
        private static bool ExcuteOperateArray(int orderNum, byte[][] operateArray)
        {
            for (int i = 0; i < orderNum; i++)
            {
                byte[] assetId = operateArray[i].Range(0, LENGTH_OF_SCRIPTHASH);
                byte[] owner = operateArray[i].Range(20, LENGTH_OF_SCRIPTHASH);
                byte[] to = operateArray[i].Range(40, LENGTH_OF_SCRIPTHASH);
                BigInteger amount = operateArray[i].Range(60, operateArray[i].Length - 60).AsBigInteger();

                if (!TransferToken(assetId, owner, to, amount)) return false;
            }
            return true;
        }


        /// <summary>
        ///  Excute the OperateArray 
        /// </summary>
        /// <param name="args">
        /// orderNum/operateArray(assetId--from--to--amount)
        /// </param>
        /// <returns>
        /// the result of the execution
        /// </returns>
        private static bool UpdateOrders(int orderNum, byte[][] orderHashes, byte[][] operateArray)
        {
            for (int i = 0; i < orderNum; i++)
            {
                byte[] assetId = operateArray[i].Range(0, LENGTH_OF_SCRIPTHASH);
                BigInteger equivalentRange = Storage.Get(Storage.CurrentContext, EQUIVALENT_RANGE.Concat(assetId)).AsBigInteger();
                BigInteger amountS = operateArray[i].Range(60, operateArray[i].Length - 60).AsBigInteger();
                BigInteger soldAmount = GetSoldAmount(orderHashes[i]);
                BigInteger amountPlanToS = Storage.Get(Storage.CurrentContext, AMOUNT_TO_SELL.Concat(orderHashes[i])).AsBigInteger();
                if (amountPlanToS - soldAmount - amountS <= equivalentRange) {
                    Storage.Put(Storage.CurrentContext, ORDER_STATUS.Concat(orderHashes[i]), AllFilled);
                } else {
                    BigInteger boughtAmount = GetBoughtAmount(orderHashes[i]);
                    Storage.Put(Storage.CurrentContext, SOLD_AMOUNT.Concat(orderHashes[i]), soldAmount + amountS);
                    BigInteger amountB = operateArray[i].Range(60, operateArray[i-1].Length - 60).AsBigInteger();
                    Storage.Put(Storage.CurrentContext, BOUGHT_AMOUNT.Concat(orderHashes[i]), boughtAmount + amountB);
                }
            }
            return true;
        }

        /// <summary>
        ///  Excute the OperateArray 
        /// </summary>
        /// <param name="args">
        /// orderNum/operateArray(assetId--from--to--amount)
        /// </param>
        /// <returns>
        /// the result of the execution
        /// </returns>
        private static bool CheckOperateArray(int orderNum, byte[][] operateArray)
        {
            for (int i = 0; i < orderNum; i++)
            {
                byte[] assetId = operateArray[i].Range(0, LENGTH_OF_SCRIPTHASH);
                byte[] owner = operateArray[i].Range(20, LENGTH_OF_SCRIPTHASH);
                BigInteger amount = operateArray[i].Range(60, operateArray[i].Length - 60).AsBigInteger();

                var allowanceArgs = new object[] { owner, ExecutionEngine.ExecutingScriptHash };
                var allowanceContract = (NEP5Contract)assetId.ToDelegate();
                BigInteger allowanceResult = (BigInteger)allowanceContract("allowance", allowanceArgs);

                var balanceArgs = new object[] { owner };
                var balanceContract = (NEP5Contract)assetId.ToDelegate();
                BigInteger balanceResult = (BigInteger)balanceContract("balanceOf", balanceArgs);

                if (allowanceResult < amount || balanceResult < amount) return false;

            }
            return true;
        }

        /// <summary>
        ///  Set Lrn AssetId 
        /// </summary>
        /// <param name="args">
        /// lrnAssetId
        /// </param>
        /// <returns>
        /// set successful?
        /// </returns>
        private static bool SetLrnAssetId(object[] args)
        {
            if (!Runtime.CheckWitness(SuperAdmin)) return false;

            if (args.Length != 1) return false;
            byte[] lrnAssetId = (byte[])args[0];
            if (lrnAssetId.Length != LENGTH_OF_SCRIPTHASH) return false;

            Storage.Put(Storage.CurrentContext, KEY_LRN_ASSET_ID, lrnAssetId);
            return true;
        }

        /// <summary>
        ///  Set the min amount of the specific asset
        /// </summary>
        /// <param name="args">
        /// assetId/minAmount
        /// </param>
        /// <returns>
        /// set successful?
        /// </returns>
        private static bool SetMinOrderAmount(object[] args)
        {
            if (!Runtime.CheckWitness(SuperAdmin)) return false;

            if (args.Length != 2) return false;

            byte[] assetId = (byte[])args[0];
            BigInteger minAmount = (BigInteger)args[1];
            if (assetId.Length != LENGTH_OF_SCRIPTHASH || minAmount <= 0) return false;
            Storage.Put(Storage.CurrentContext, MIN_ORDER_AMOUNT.Concat(assetId), minAmount);
            return true;
        }

        /// <summary>
        ///  Set the equivalent range of the specific asset
        /// </summary>
        /// <param name="args">
        /// assetId/minAmount
        /// </param>
        /// <returns>
        /// set successful?
        /// </returns>
        private static bool SetEquivalentRange(object[] args)
        {
            if (!Runtime.CheckWitness(SuperAdmin)) return false;

            if (args.Length != 2) return false;

            byte[] assetId = (byte[])args[0];
            BigInteger equivalentRange = (BigInteger)args[1];
            if (assetId.Length != LENGTH_OF_SCRIPTHASH || equivalentRange <= 0) return false;
            Storage.Put(Storage.CurrentContext, EQUIVALENT_RANGE.Concat(assetId), equivalentRange);
            return true;
        }

        /// <summary>
        ///  Get the equivalent range of the asset
        /// </summary>
        /// <param name="args">
        /// assetId
        /// </param>
        /// <returns>
        /// equivalent range of the  asset
        /// </returns>
        private static BigInteger GetEquivalentRange(byte[] assetId)
        {
            return Storage.Get(Storage.CurrentContext, EQUIVALENT_RANGE.Concat(assetId)).AsBigInteger();
        }

        /// <summary>
        ///  Check and parse the ring
        /// </summary>
        /// <param name="args">
        /// orderNum/orderHashes
        /// </param>
        /// <returns>
        /// operateArray
        /// </returns>
        private static byte[][] ParseRing(int orderNum, byte[][] orderHashes)
        {
            //assetId--from--to--amount
            byte[][] operateArray = new byte[orderNum][];

            BigInteger headRemainderSAmount = GetRemainderS(orderHashes[0]);
            BigInteger headRemainderBAmount = GetRemainderB(orderHashes[0]);
            BigInteger currentSamount = headRemainderSAmount;
            byte[] headBasicInfo = Storage.Get(Storage.CurrentContext, ORDER_BASIC.Concat(orderHashes[0]));
            byte[] headOwner = GetOwner(headBasicInfo);

            for (int i = 0; i < orderNum - 1; i++)
            {
                if (!IsOrderValid(orderHashes[i])) return new byte[][] { };
                byte[] currentBasicInfo = Storage.Get(Storage.CurrentContext, ORDER_BASIC.Concat(orderHashes[i]));
                byte[] nextBasicInfo = Storage.Get(Storage.CurrentContext, ORDER_BASIC.Concat(orderHashes[i+1]));
                //the sell token of the pre node should be equal with the buy token of the next node
                if (GetTokenS(currentBasicInfo) != GetTokenB(nextBasicInfo)) return new byte[][] { };
                //the amount could be sold of current node must less than or equal to the amount of the next node to buy
                if (currentSamount > GetRemainderB(orderHashes[i])) return new byte[][] { };

                BigInteger nextPlanToBuy = Storage.Get(Storage.CurrentContext, AMOUNT_TO_BUY.Concat(orderHashes[i])).AsBigInteger();
                BigInteger nextPlanToSell = Storage.Get(Storage.CurrentContext, AMOUNT_TO_SELL.Concat(orderHashes[i])).AsBigInteger();

                //caculate the amount to sell of next node
                BigInteger nextSamount = nextPlanToSell * currentSamount / nextPlanToBuy;

                //get the remainder of the next node to sell in database
                BigInteger nextRemainderSAmount = GetRemainderS(orderHashes[i]);
                if (nextSamount > nextRemainderSAmount) return new byte[][] { };

                operateArray[i] = GetTokenS(currentBasicInfo).Concat(GetOwner(currentBasicInfo)).Concat(GetOwner(nextBasicInfo)).Concat(currentSamount.ToByteArray());
                currentSamount = nextSamount;

                //the last node should transfer to the head node
                if(i == orderNum - 2)
                {
                    if(currentSamount >= headRemainderBAmount - GetEquivalentRange(GetTokenS(headBasicInfo)))
                    {
                        operateArray[orderNum - 1] = GetTokenS(nextBasicInfo).Concat(GetOwner(nextBasicInfo)).Concat(headOwner).Concat(nextSamount.ToByteArray());
                    } else {
                        return new byte[][] { };
                    }
                }
            }
            return operateArray;
        }

        /// <summary>
        ///  Get the remainder amount to sell
        /// </summary>
        /// <param name="args">
        /// orderHash
        /// </param>
        /// <returns>
        /// the remainder amount to sell
        /// </returns>
        private static BigInteger GetRemainderS(byte[] orderHash)
        {
            BigInteger amountS = Storage.Get(Storage.CurrentContext, AMOUNT_TO_SELL.Concat(orderHash)).AsBigInteger();
            if (Storage.Get(Storage.CurrentContext, SOLD_AMOUNT.Concat(orderHash)).AsBigInteger() == 0) {
                return amountS;
            } else {
                return amountS - Storage.Get(Storage.CurrentContext, SOLD_AMOUNT.Concat(orderHash)).AsBigInteger();
            }
        }

        /// <summary>
        ///  Get the remainder amount to buy
        /// </summary>
        /// <param name="args">
        /// orderHash
        /// </param>
        /// <returns>
        /// the remainder amount to buy
        /// </returns>
        private static BigInteger GetRemainderB(byte[] orderHash)
        {
            BigInteger amountB = Storage.Get(Storage.CurrentContext, AMOUNT_TO_BUY.Concat(orderHash)).AsBigInteger();
            if (Storage.Get(Storage.CurrentContext, BOUGHT_AMOUNT.Concat(orderHash)).AsBigInteger() == 0){
                return amountB;
            } else {
                return amountB - Storage.Get(Storage.CurrentContext, BOUGHT_AMOUNT.Concat(orderHash)).AsBigInteger();
            }
        }

        /// <summary>
        ///  Get the sold amount
        /// </summary>
        /// <param name="args">
        /// orderHash
        /// </param>
        /// <returns>
        /// the sold amount
        /// </returns>
        private static BigInteger GetSoldAmount(byte[] orderHash)
        {
            return Storage.Get(Storage.CurrentContext, SOLD_AMOUNT.Concat(orderHash)).AsBigInteger();
        }

        /// <summary>
        ///  Get the bought amount to buy
        /// </summary>
        /// <param name="args">
        /// orderHash
        /// </param>
        /// <returns>
        /// the bought amount to buy
        /// </returns>
        private static BigInteger GetBoughtAmount(byte[] orderHash)
        {
            return Storage.Get(Storage.CurrentContext, BOUGHT_AMOUNT.Concat(orderHash)).AsBigInteger();
        }

        /// <summary>
        ///  Get the owner
        /// </summary>
        /// <param name="basicInfo">
        /// basicInfo
        /// </param>
        /// <returns>
        /// the owner of the order
        /// </returns>
        private static byte[] GetOwner(byte[] basicInfo)
        {
            return basicInfo.Range(0, LENGTH_OF_SCRIPTHASH);
        }

        /// <summary>
        ///  Get the assetId to sell
        /// </summary>
        /// <param name="basicInfo">
        /// basicInfo
        /// </param>
        /// <returns>
        /// the assetId to sell
        /// </returns>
        private static byte[] GetTokenS(byte[] basicInfo)
        {
            return basicInfo.Range(20, LENGTH_OF_SCRIPTHASH);
        }

        /// <summary>
        ///  Get the assetId to buy
        /// </summary>
        /// <param name="basicInfo">
        /// basicInfo
        /// </param>
        /// <returns>
        /// the assetId to buy
        /// </returns>
        private static byte[] GetTokenB(byte[] basicInfo)
        {
            return basicInfo.Range(40, LENGTH_OF_SCRIPTHASH);
        }

        /// <summary>
        ///  Caculate the hash of the order
        /// </summary>
        /// <param name="order">
        /// order
        /// </param>
        /// <returns>
        /// the hash of the order
        /// </returns>
        private static byte[] CalculateOrderHash(TradeOrder order)
        {
            var bytes = order.owner
                .Concat(order.tokenS)
                .Concat(order.tokenB)
                .Concat(order.miner)
                .Concat(order.amountS.AsByteArray())
                .Concat(order.amountB.AsByteArray())
                .Concat(order.orderTime.AsByteArray())
                .Concat(order.lrnFee.AsByteArray());

            return Hash256(bytes);
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
            if (assetId.Length != LENGTH_OF_SCRIPTHASH || owner.Length != LENGTH_OF_SCRIPTHASH
                || to.Length != LENGTH_OF_SCRIPTHASH) return false;

            var args = new object[] { owner, ExecutionEngine.ExecutingScriptHash, to, amount };
            var contract = (NEP5Contract)assetId.ToDelegate();
            bool result = (bool)contract("transferFrom", args);
            if (!result)
            {
                Runtime.Log("Failed to transferFrom NEP-5 tokens!");
                return false;
            }
            return true;
        }
    }
}
