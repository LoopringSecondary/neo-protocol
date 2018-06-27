using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace LrnAirdropContract
{
    public class LrnAirdropContract : SmartContract
    {
        public static readonly byte[] SuperAdmin = "AR7W16oCGSyKF4ebGjod9EFFwTUyRPZV9o".ToScriptHash();
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;
        //private const int FIRST_AIRDROP_START_TIME = 1530633600;//2018-07-04 00:00:00
        //private const int SECOND_AIRDROP_START_TIME = 1535990400;//2018-09-04 00:00:00
        //private const int THIRD_AIRDROP_START_TIME = 1541260800;//2018-11-04 00:00:00
        private const int FIRST_AIRDROP_START_TIME = 1530028800;//2018-06-27 00:00:00
        private const int SECOND_AIRDROP_START_TIME = 1530115200;//2018-06-28 00:00:00
        private const int THIRD_AIRDROP_START_TIME = 1530201600;//2018-06-29 00:00:00

        private const Int64 TOTAL_AMOUNT_PER_PHASE = 2790152100000000;
        private const Int64 TOTAL_AIRDROP_AMOUNT = 8370456300000000;

        private const string AIR_DROP_SUPPLY = "airdropSupply";
        private const string AIR_DROP_ACCOUNT_NUM = "airdropAccountNum";
        private const string LAST_WITHDRAW_TIME = "lastWithdrawTime";
        private const string WITHDRAW_SWITCH = "withdrawSwitch";
        private const string WITHDRAW_NO = "withdrawNo";
        private const string TX = "txInfo";

        private const int SECONDS_PER_DAY = 86400;
        private const int PERIOD = 730;
        private const int COMPENSATE_TIME = 86399;

        public static readonly byte[] FIRST_PHASE_PREFIX = "firstPhase".AsByteArray();
        public static readonly byte[] SECOND_PHASE_PREFIX = "secondPhase".AsByteArray();
        public static readonly byte[] THIRD_PHASE_PREFIX = "thirdPhase".AsByteArray();


        [Appcall("06fa8be9b6609d963e8fc63977b9f8dc5f10895f")]
        static extern object CallLrn(string method, object[] arr);

        [DisplayName("deposited")]
        public static event Action<byte[], BigInteger> Deposited;

        [DisplayName("withdrew")]
        public static event Action<byte[], BigInteger> Withdrew;

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
                if (Runtime.CheckWitness(SuperAdmin)) return true;

                Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
                var type = tx.Type;

                if (type != INVOCATION_TRANSACTION_TYPE) return false;

                var invocationTransaction = (InvocationTransaction)tx;
                if (invocationTransaction.Script.Length != 61)
                {
                    return false;
                }

                if (invocationTransaction.Script[0] != 0x1c) return false;

                if (invocationTransaction.Script.Range(29, 32) != (new byte[] { 0x51, 0xc1, 0x08 }).Concat("withdraw".AsByteArray()).Concat(new byte[] { 0x67 }).Concat(ExecutionEngine.ExecutingScriptHash))
                {
                    return false;
                }

                return true;
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deposit")
                {
                    return Deposit(args);
                }
                if (operation == "withdraw")
                {
                    return Withdraw(args);
                }
                if (operation == "queryAirDropTotalSupply")
                {
                    return Storage.Get(Storage.CurrentContext, AIR_DROP_SUPPLY).AsBigInteger();
                }
                if (operation == "queryAirDropBalance")
                {
                    return QueryAirDropBalance(args);
                }
                if (operation == "queryAvailableBalance")
                {
                    return GueryAvailableAmount(args);
                }
                if (operation == "queryAvailableBalanceWithPhase")
                {
                    return QueryAvailableBalanceWithPhase(args);
                }
                if (operation == "setWithdrawSwitch")
                {
                    return SetWithdrawSwitch(args);
                }
                if (operation == "queryWithdrawSwitch")
                {
                    return Storage.Get(Storage.CurrentContext, WITHDRAW_SWITCH).AsBigInteger();
                }
                if (operation == "queryLastWithdrawTime")
                {
                    return QueryLastWithdrawTime(args);
                }
                if (operation == "queryAirdropAccountNum")
                {
                    return Storage.Get(Storage.CurrentContext, AIR_DROP_ACCOUNT_NUM).AsBigInteger();
                }
                if (operation == "queryAirdropAccount")
                {
                    return QueryAirdropAccount(args); 
                }
                if (operation == "queryData")
                {
                    byte[] key = (byte[])args[0];
                    return Storage.Get(Storage.CurrentContext, key);
                }

            }
            return false;
        }

        /// <summary>
        ///   Deposit the amount to the account.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: account, depositAmount, phase.
        /// </param>
        /// <returns>
        ///   Deposit Successful?
        /// </returns>
        public static bool Deposit(object[] args)
        {
            if (!Runtime.CheckWitness(SuperAdmin)) return false;

            if (args.Length != 3) throw new Exception();

            byte[] account = (byte[])args[0];
            if (account.Length != 20) throw new Exception();

            BigInteger depositAmount = (BigInteger)args[1];
            if (depositAmount <= 0 || depositAmount > TOTAL_AMOUNT_PER_PHASE) throw new Exception();

            BigInteger supply = Storage.Get(Storage.CurrentContext, AIR_DROP_SUPPLY).AsBigInteger();
            BigInteger originAmount = 0;

            if (IsNewAccount(account))
            {
                BigInteger sequenceNumber = Storage.Get(Storage.CurrentContext, AIR_DROP_ACCOUNT_NUM).AsBigInteger() + 1;
                Storage.Put(Storage.CurrentContext, AIR_DROP_ACCOUNT_NUM + sequenceNumber, account);
                Storage.Put(Storage.CurrentContext, AIR_DROP_ACCOUNT_NUM, sequenceNumber);
            }
            string phase = (string)args[2];
            if ("firstPhase" == phase)
            {
                originAmount = Storage.Get(Storage.CurrentContext, FIRST_PHASE_PREFIX.Concat(account)).AsBigInteger();
                Storage.Put(Storage.CurrentContext, FIRST_PHASE_PREFIX.Concat(account), depositAmount);
            }
            else if ("secondPhase" == phase)
            {
                originAmount = Storage.Get(Storage.CurrentContext, SECOND_PHASE_PREFIX.Concat(account)).AsBigInteger();
                Storage.Put(Storage.CurrentContext, SECOND_PHASE_PREFIX.Concat(account), depositAmount);
            }
            else if ("thirdPhase" == phase)
            {
                originAmount = Storage.Get(Storage.CurrentContext, THIRD_PHASE_PREFIX.Concat(account)).AsBigInteger();
                Storage.Put(Storage.CurrentContext, THIRD_PHASE_PREFIX.Concat(account), depositAmount);
            }
            else
            {
                throw new Exception();
            }
            if ((supply - originAmount + depositAmount) > TOTAL_AIRDROP_AMOUNT || (supply - originAmount + depositAmount) < 0) throw new Exception();
            Storage.Put(Storage.CurrentContext, AIR_DROP_SUPPLY, supply - originAmount + depositAmount);
            Deposited(account, depositAmount);
            return true;
        }

        /// <summary>
        ///   Withdraw the available amount to the account.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: account.
        /// </param>
        /// <returns>
        ///   Withdraw Successful?
        /// </returns>
        public static bool Withdraw(object[] args)
        {
            if (args.Length != 1) throw new Exception();
            byte[] withdrawParameter = (byte[])args[0];
            if (withdrawParameter.Length != 28) throw new Exception();
            byte[] account = withdrawParameter.Range(8, 20);

            BigInteger withdrawSwitch = Storage.Get(Storage.CurrentContext, WITHDRAW_SWITCH).AsBigInteger();
            if (withdrawSwitch == 0) throw new Exception();

            BigInteger firstAvailbableAmount = CalcAvailableAmount(account, FIRST_PHASE_PREFIX);
            BigInteger secondAvailbableAmount = CalcAvailableAmount(account, SECOND_PHASE_PREFIX);
            BigInteger thirdAvailbableAmount = CalcAvailableAmount(account, THIRD_PHASE_PREFIX);
            BigInteger withdrawAmount = firstAvailbableAmount + secondAvailbableAmount + thirdAvailbableAmount;

            if (withdrawAmount < 1) throw new Exception();

            byte[] from = Neo.SmartContract.Framework.Services.System.ExecutionEngine.ExecutingScriptHash;
            // call lrn transfer
            byte[] rt = (byte[])CallLrn("transfer", new object[] { from, account, withdrawAmount });
            bool succ = rt.AsBigInteger() == 1;
            if (succ)
            {
                BigInteger now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                Storage.Put(Storage.CurrentContext, account.Concat(LAST_WITHDRAW_TIME.AsByteArray()), now);
                BigInteger withdrawNo = Storage.Get(Storage.CurrentContext, account.Concat(WITHDRAW_NO.AsByteArray())).AsBigInteger();
                withdrawNo = withdrawNo + 1;
                Storage.Put(Storage.CurrentContext, account.Concat(WITHDRAW_NO.AsByteArray()), withdrawNo);
                Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
                Storage.Put(Storage.CurrentContext, account.Concat(TX.AsByteArray()).Concat(withdrawNo.AsByteArray()), tx.Hash);
                Withdrew(account, withdrawAmount);
            }
            else
            {
                return false;
            }
            return true;
        }


        /// <summary>
        ///   Query the available amount to the account for the specific phase.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: account, phase.
        /// </param>
        /// <returns>
        ///   The available amount to the account for the specific phase.
        /// </returns>
        public static BigInteger QueryAvailableBalanceWithPhase(object[] args)
        {
            if (args.Length != 2) return 0;
            byte[] account = (byte[])args[0];
            string phase = (string)args[1];
            return CalcAvailableAmount(account, phase.AsByteArray());
        }


        /// <summary>
        ///   Set the withdraw switch.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: switch.
        /// </param>
        /// <returns>
        ///   Set Successful?
        /// </returns>
        public static bool SetWithdrawSwitch(object[] args)
        {
            if (!Runtime.CheckWitness(SuperAdmin)) return false;
            if (args.Length != 1) return false;
            if ((string)args[0] == "on")
            {
                Storage.Put(Storage.CurrentContext, WITHDRAW_SWITCH, 1);
            }
            else
            {
                Storage.Put(Storage.CurrentContext, WITHDRAW_SWITCH, 0);
            }
            return true;
        }


        /// <summary>
        ///   Query the last withdraw time for the specific account.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: account.
        /// </param>
        /// <returns>
        ///   The last withdraw time.
        /// </returns>
        public static BigInteger QueryLastWithdrawTime(object[] args)
        {
            if (args.Length != 1) return 0;
            byte[] account = (byte[])args[0];
            if (account.Length != 20) return 0;
            return Storage.Get(Storage.CurrentContext, account.Concat(LAST_WITHDRAW_TIME.AsByteArray())).AsBigInteger();
        }

        /// <summary>
        ///   Query the account by sequence number.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: sequence number.
        /// </param>
        /// <returns>
        ///  Account.
        /// </returns>
        public static byte[] QueryAirdropAccount(object[] args)
        {
            if (args.Length != 1) return new byte[] {0};
            BigInteger sequenceNumber = (BigInteger)args[0];
            if(sequenceNumber < 0 || sequenceNumber > 100000000) return new byte[] { 0 };
            return Storage.Get(Storage.CurrentContext, AIR_DROP_ACCOUNT_NUM + sequenceNumber);
        }

        /// <summary>
        ///   Query the total amount of the account for specific phase.
        /// </summary>
        /// <param name="args">
        ///   The contract input parameters: account, phase.
        /// </param>
        /// <returns>
        ///   The total amount of the account for specific phase.
        /// </returns>
        public static BigInteger QueryAirDropBalance(object[] args)
        {
            if (args.Length != 2) return 0;
            byte[] account = (byte[])args[0];

            string phase = (string)args[1];
            if ("firstPhase" == phase)
            {
                return Storage.Get(Storage.CurrentContext, FIRST_PHASE_PREFIX.Concat(account)).AsBigInteger();
            }
            else if ("secondPhase" == phase)
            {
                return Storage.Get(Storage.CurrentContext, SECOND_PHASE_PREFIX.Concat(account)).AsBigInteger();
            }
            else if ("thirdPhase" == phase)
            {
                return Storage.Get(Storage.CurrentContext, THIRD_PHASE_PREFIX.Concat(account)).AsBigInteger();
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        ///  Calculate the available amount to withdraw for the account.
        /// </summary>
        /// <param name="account">
        ///  the account to withdraw
        /// </param>
        /// <param name="phase">
        ///  The phase of the airdrop.
        /// </param>
        /// <returns>
        ///  Available amount to withdraw for the phase.
        /// </returns>
        public static BigInteger CalcAvailableAmount(byte[] account, byte[] phase)
        {
            if (account.Length != 20) return 0;
            BigInteger amount = Storage.Get(Storage.CurrentContext, phase.Concat(account)).AsBigInteger();
            if (amount < 1) return 0;
            BigInteger holdDays = 0;
            BigInteger totalAvailableAmount = 0;
            BigInteger startTime = CalcStartTime(account, phase);
            BigInteger endTime = CalcEndTime(account, phase);

            if (endTime > startTime)
            {
                holdDays = (endTime - startTime) / SECONDS_PER_DAY;
            }

            BigInteger availbableAmount = amount * holdDays / PERIOD;
            return availbableAmount;
        }

        /// <summary>
        ///  Calculate the available amount to withdraw for the account.
        /// </summary>
        /// <param name="account">
        ///  the account to withdraw
        /// </param>
        /// <returns>
        ///  Available amount to withdraw of the account for all phases.
        /// </returns>
        public static BigInteger GueryAvailableAmount(object[] args)
        {
            if (args.Length != 1) return 0;

            byte[] account = (byte[])args[0];
            if (account.Length != 20) return 0;

            BigInteger firstAvailbableAmount = CalcAvailableAmount(account, FIRST_PHASE_PREFIX);
            BigInteger secondAvailbableAmount = CalcAvailableAmount(account, SECOND_PHASE_PREFIX);
            BigInteger thirdAvailbableAmount = CalcAvailableAmount(account, THIRD_PHASE_PREFIX);
            BigInteger availableAmount = firstAvailbableAmount + secondAvailbableAmount + thirdAvailbableAmount;
            return availableAmount;
        }

        /// <summary>
        ///  Calculate the start time for withdraw.
        /// </summary>
        /// <param name="account">
        ///  the account to withdraw
        /// </param>
        /// <param name="phase">
        ///  The phase of the airdrop.
        /// </param>
        /// <returns>
        /// The start time for withdraw.
        /// </returns>
        public static BigInteger CalcStartTime(byte[] account, byte[] phase)
        {
            BigInteger startTime = Storage.Get(Storage.CurrentContext, account.Concat(LAST_WITHDRAW_TIME.AsByteArray())).AsBigInteger();
            if (phase == FIRST_PHASE_PREFIX)
            {
                if (startTime <= FIRST_AIRDROP_START_TIME)
                {
                    startTime = FIRST_AIRDROP_START_TIME;
                }
            }
            else if (phase == SECOND_PHASE_PREFIX)
            {
                if (startTime <= SECOND_AIRDROP_START_TIME)
                {
                    startTime = SECOND_AIRDROP_START_TIME;
                }
            }
            else if (phase == THIRD_PHASE_PREFIX)
            {
                if (startTime <= THIRD_AIRDROP_START_TIME)
                {
                    startTime = THIRD_AIRDROP_START_TIME;
                }
            }
            else
            {
                throw new Exception();
            }
            return startTime;
        }

        /// <summary>
        ///  Calculate the end time for withdraw.
        /// </summary>
        /// <param name="account">
        ///  the account to withdraw
        /// </param>
        /// <param name="phase">
        ///  The phase of the airdrop.
        /// </param>
        /// <returns>
        /// The end time for withdraw.
        /// </returns>
        public static BigInteger CalcEndTime(byte[] account, byte[] phase)
        {
            BigInteger endTime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            if (phase == FIRST_PHASE_PREFIX)
            {
                if (endTime > (FIRST_AIRDROP_START_TIME + PERIOD * SECONDS_PER_DAY))
                {
                    endTime = FIRST_AIRDROP_START_TIME + PERIOD * SECONDS_PER_DAY + COMPENSATE_TIME;
                }
            }
            else if (phase == SECOND_PHASE_PREFIX)
            {
                if (endTime > (SECOND_AIRDROP_START_TIME + PERIOD * SECONDS_PER_DAY))
                {
                    endTime = SECOND_AIRDROP_START_TIME + PERIOD * SECONDS_PER_DAY + COMPENSATE_TIME;
                }
            }
            else if (phase == THIRD_PHASE_PREFIX)
            {
                if (endTime > (THIRD_AIRDROP_START_TIME + PERIOD * SECONDS_PER_DAY))
                {
                    endTime = THIRD_AIRDROP_START_TIME + PERIOD * SECONDS_PER_DAY + COMPENSATE_TIME;
                }
            }
            else
            {
                throw new Exception();
            }
            return endTime;
        }

        /// <summary>
        ///  Judge an account has been deposited before.
        /// </summary>
        /// <param name="account">
        ///  the account to deposit
        /// </param>
        /// <returns>
        ///  false-has been deposited before, true-hasn't been deposited before.
        /// </returns>
        public static bool IsNewAccount(byte[] account)
        {
            BigInteger originFirstAmount = Storage.Get(Storage.CurrentContext, FIRST_PHASE_PREFIX.Concat(account)).AsBigInteger();
            BigInteger originSecondAmount = Storage.Get(Storage.CurrentContext, SECOND_PHASE_PREFIX.Concat(account)).AsBigInteger();
            BigInteger originThirdAmount = Storage.Get(Storage.CurrentContext, THIRD_PHASE_PREFIX.Concat(account)).AsBigInteger();
            if (originFirstAmount == 0 && originSecondAmount == 0 && originThirdAmount == 0)
            {
                return true;
            }
            return false;
        }
    }
}
