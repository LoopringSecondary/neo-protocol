/*

  Copyright 2018 Loopring Foundation.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.

*/
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace neo_lrn
{
    public class LRN : SmartContract
    {
        //Token Settings
        public static string Name() => "Loopring Neo Token";
        public static string Symbol() => "LRN";
        public static readonly byte[] Owner = "AdqLRCBxDRTQLDqQE8GMSGU4j2ydYPLQHv".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong FACTOR = 100000000; //decided by Decimals()
        private const ulong TOTAL_AMOUNT = 139507605 * FACTOR; // total token amount
        private const string TOTAL_SUPPLY = "totalSupply";

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("approve")]
        public static event Action<byte[], byte[], BigInteger> Approval;

        /// <summary>
        ///   This smart contract is designed to implement NEP-5
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
                if (operation == "deploy") return Deploy();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "decimals") return Decimals();
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "allowance")
                {
                    return Allowance((byte[])args[0], (byte[])args[1]);
                }
                if (operation == "approve")
                {
                    return Approve((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                }
                if (operation == "transferFrom")
                {
                    return TransferFrom((byte[])args[0], (byte[])args[1], (byte[])args[2], (BigInteger)args[3]);
                }
            }
            return false;
        }

        /// <summary>
        ///   Deploys the tokens to the admin account，only once
        /// </summary>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, TOTAL_SUPPLY);
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, Owner, IntToBytes(TOTAL_AMOUNT));
            Storage.Put(Storage.CurrentContext, TOTAL_SUPPLY, TOTAL_AMOUNT);
            Transferred(null, Owner, TOTAL_AMOUNT);
            return true;
        }

        // get the total token supply
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, TOTAL_SUPPLY).AsBigInteger();
        }

        // get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        /// <summary>
        ///   Transfer a token balance to another account.
        /// </summary>
        /// <param name="from">
        ///   The contract invoker.
        /// </param>
        /// <param name="to">
        ///   The account to transfer to.
        /// </param>
        /// <param name="amount">
        ///   The amount to transfer.
        /// </param>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool Transfer(byte[] from, byte[] to, BigInteger amount)
        {
            if (from.Length != 20 || to.Length != 20) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (amount <= 0) return false;
            if (from == to) return true;

            BigInteger originatorValue = BytesToInt(Storage.Get(Storage.CurrentContext, from));
            if (originatorValue < amount) return false;
            BigInteger targetValue = BytesToInt(Storage.Get(Storage.CurrentContext, to));

            BigInteger nOriginatorValue = originatorValue - amount;
            BigInteger nTargetValue = targetValue + amount;

            if (nOriginatorValue > 0)
            {
                Storage.Put(Storage.CurrentContext, from, IntToBytes(nOriginatorValue));
            }  else if (nOriginatorValue == 0)
            {
                Storage.Delete(Storage.CurrentContext, from);
            } 
            Storage.Put(Storage.CurrentContext, to, IntToBytes(nTargetValue));
            Transferred(from, to, amount);
            return true;
        }

        /// <summary>
        ///   Approves another account to transfer amount tokens from the originator acount by transferForm
        /// </summary>
        /// <param name="owner">
        ///   The contract invoker.
        /// </param>
        /// <param name="spender">
        ///   The account to grant TransferFrom access to.
        /// </param>
        /// <param name="amount">
        ///   The amount to grant TransferFrom access for.
        /// </param>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool Approve(byte[] owner, byte[] spender, BigInteger amount)
        {
            if (owner.Length != 20 || spender.Length != 20) return false;
            if (!Runtime.CheckWitness(owner)) return false;
            if (owner == spender) return true;
            if (amount < 0) return false;
            if (amount == 0)
            {
                Storage.Delete(Storage.CurrentContext, owner.Concat(spender));
                Approval(owner, spender, amount);
                return true;
            }
            Storage.Put(Storage.CurrentContext, owner.Concat(spender), amount);
            Approval(owner, spender, amount);
            return true;
        }

        /// <summary>
        ///   Return the amount of tokens that the spender account can transfer from the owner acount
        /// </summary>
        /// <param name="owner">
        ///   The account who use invoke the Approve method
        /// </param>
        /// <param name="spender">
        ///   The account to grant TransferFrom access to.
        /// </param>
        /// <returns>
        ///   The amount to grant TransferFrom access for
        /// </returns>
        public static BigInteger Allowance(byte[] owner, byte[] spender)
        {
            return Storage.Get(Storage.CurrentContext, owner.Concat(spender)).AsBigInteger();
        }

        /// <summary>
        ///   Transfers an amount from the from account to the to acount if the originator has been approved to transfer the requested amount
        /// </summary>
        /// <param name="spender">
        ///   The contract invoker.
        /// </param>
        /// <param name="owner">
        ///   The account to transfer a balance from.
        /// </param>
        /// <param name="to">
        ///   The account to transfer a balance to.
        /// </param>
        /// <param name="amount">
        ///   The amount to transfer
        /// </param>
        /// <returns>
        ///   Transaction successful?
        /// </returns>
        public static bool TransferFrom(byte[] owner, byte[] spender, byte[] to, BigInteger amount)
        {
            if (owner.Length != 20 || spender.Length != 20 || to.Length != 20) return false;
            if (!Runtime.CheckWitness(spender)) return false;
            BigInteger allValInt = Storage.Get(Storage.CurrentContext, owner.Concat(spender)).AsBigInteger();
            BigInteger fromValInt = Storage.Get(Storage.CurrentContext, owner).AsBigInteger();
            BigInteger toValInt = Storage.Get(Storage.CurrentContext, to).AsBigInteger();

            if (amount >= 0 &&
                allValInt >= amount &&
                fromValInt >= amount)
            {
                Storage.Put(Storage.CurrentContext, owner.Concat(spender), IntToBytes(allValInt - amount));
                Storage.Put(Storage.CurrentContext, owner, IntToBytes(fromValInt - amount));
                Storage.Put(Storage.CurrentContext, to, IntToBytes(toValInt + amount));
                Transferred(owner, to, amount);
                return true;
            }
            return false;

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
