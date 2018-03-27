using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NameRegistry
{
    public class NameRegistry : SmartContract
    {
        private static readonly byte[] SuperAdmin = "AdqLRCBxDRTQLDqQE8GMSGU4j2ydYPLQHv".ToScriptHash();

        private static readonly byte[] feeRecipientPrefix = "feeRecipient-".AsByteArray();
        private static readonly byte[] signerPrefix = "signer-".AsByteArray();
        private static readonly byte[] namePrefix = "name-".AsByteArray();

        [DisplayName("nameRegister")]
        public static event Action<byte[], byte[]> NameRegistered; //(ownerHash, name)

        [DisplayName("nameUnregister")]
        public static event Action<byte[]> NameUnregistered;// (ownerHash)

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(SuperAdmin);
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "nameRegister")
                {
                    if (args.Length != 3) return false;

                    byte[] owner = (byte[])args[0];
                    byte[] feeRecipient = (byte[])args[1];
                    byte[] signer = (byte[])args[2];
                    byte[] name = (byte[])args[3];
                    if(owner.Length != 20 || feeRecipient.Length != 20 || signer.Length != 20) return false;

                    Storage.Put(Storage.CurrentContext, feeRecipientPrefix.Concat(owner), feeRecipient);
                    Storage.Put(Storage.CurrentContext, signerPrefix.Concat(owner), signer);
                    Storage.Put(Storage.CurrentContext, namePrefix.Concat(owner), name);

                    NameRegistered(owner, name);

                    return true;
                }

                if (operation == "nameUnregister")
                {
                    if (args.Length != 1) return false;

                    byte[] owner = (byte[])args[0];
                    if (owner.Length != 20) return false;

                    Storage.Delete(Storage.CurrentContext, feeRecipientPrefix.Concat(owner));
                    Storage.Delete(Storage.CurrentContext, signerPrefix.Concat(owner));
                    Storage.Delete(Storage.CurrentContext, namePrefix.Concat(owner));

                    NameUnregistered(owner);

                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
