/*

  Copyright 2018 Loopring Project Ltd (Loopring Foundation).

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

namespace TokenRegistry
{
    public class TokenRegistry : SmartContract
    {
        public static readonly byte[] SuperAdmin = "AdqLRCBxDRTQLDqQE8GMSGU4j2ydYPLQHv".ToScriptHash();

        [DisplayName("register")]
        public static event Action<byte[], byte[]> TokenRegistered;

        [DisplayName("unregister")]
        public static event Action<byte[], byte[]> TokenUnregistered;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(SuperAdmin);
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if(operation == "register")
                {
                    if (args.Length != 2) return false;
                    byte[] tokenHash = (byte[])args[0];
                    byte[] symbol = (byte[])args[1];
                    if (tokenHash.Length != 20 || symbol.Length == 0) return false;
                    Storage.Put(Storage.CurrentContext, tokenHash, symbol);
                    TokenRegistered(tokenHash, symbol);
                    return true;
                }

                if (operation == "unregister")
                {
                    if (args.Length != 1) return false;
                    byte[] tokenHash = (byte[])args[0];
                    if (tokenHash.Length != 20) return false;
                    byte[] symbol = Storage.Get(Storage.CurrentContext, tokenHash);
                    if(symbol.Length != 0)
                    {
                        Storage.Delete(Storage.CurrentContext, tokenHash);
                        TokenUnregistered(tokenHash, symbol);
                        return true;
                    }
                    return false;
                }

                if (operation == "queryRegisterState")
                {
                    if (args.Length != 1) return false;
                    byte[] tokenHash = (byte[])args[0];
                    if (tokenHash.Length != 20) return false;
                    byte[] symbol = Storage.Get(Storage.CurrentContext, tokenHash);
                    if(symbol.Length != 0)
                    {
                        Runtime.Log(symbol.AsString());
                        return true;
                    }
                    return false;
                }

            }

            return false;
        }
    }
}
