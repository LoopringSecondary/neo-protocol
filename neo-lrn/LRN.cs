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
        public static string Name() => "Loopring NeoToken";
        public static string Symbol() => "LRN";
        public static readonly byte[] Owner = "AZy6n4jDAN4ssEDucN42Cpyj442K4u16r4".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()

        private const ulong total_amount = 1395076054 * factor; // total token amount

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("approve")]
        public static event Action<byte[], byte[], BigInteger> Approval;

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
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                if (operation == "decimals") return Decimals();
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

        // initialization parameters, only once
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, Owner, IntToBytes(total_amount));
            Storage.Put(Storage.CurrentContext, "totalSupply", total_amount);
            Transferred(null, Owner, total_amount);
            return true;
        }

        // get the total token supply
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;

            var originatorValue = Storage.Get(Storage.CurrentContext, from);
            var targetValue = Storage.Get(Storage.CurrentContext, to);

            BigInteger nOriginatorValue = BytesToInt(originatorValue) - value;
            BigInteger nTargetValue = BytesToInt(targetValue) + value;

            if (nOriginatorValue >= 0 &&
                 value >= 0)
            {
                Storage.Put(Storage.CurrentContext, from, IntToBytes(nOriginatorValue));
                Storage.Put(Storage.CurrentContext, to, IntToBytes(nTargetValue));
                return true;
            }
            return false;
        }

        // get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }


        // return the amount of tokens that the to account can transfer from the from acount
        public static BigInteger Allowance(byte[] from, byte[] to) => BytesToInt(Storage.Get(Storage.CurrentContext, from.Concat(to)));

        // transfer an amount from the from account to the to acount if the originator has been approved to transfer the requested amount
        public static bool TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger amount)
        {
            if (!Runtime.CheckWitness(from)) return false;
            var allValInt = BytesToInt(Storage.Get(Storage.CurrentContext, from.Concat(originator)));
            var fromValInt = BytesToInt(Storage.Get(Storage.CurrentContext, from));
            var toValInt = BytesToInt(Storage.Get(Storage.CurrentContext, to));

            if (fromValInt >= amount &&
                amount >= 0 &&
                allValInt >= 0)
            {
                Storage.Put(Storage.CurrentContext, from.Concat(originator), IntToBytes(allValInt - amount));
                Storage.Put(Storage.CurrentContext, to, IntToBytes(toValInt + amount));
                Storage.Put(Storage.CurrentContext, from, IntToBytes(fromValInt - amount));
                return true;
            }
            return false;

        }

        // approve the to account to transfer amount tokens from the originator acount
        public static bool Approve(byte[] originator, byte[] to, BigInteger amount)
        {
            Storage.Put(Storage.CurrentContext, originator.Concat(to), IntToBytes(amount));
            Approval(originator, to, amount);
            return true;
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
