using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using BatchRpc;


namespace BatchRpc
{
    class Program
    {
        /// <summary>
        /// API 参考：http://docs.neo.org/zh-cn/node/api.html
        /// API Reference: http://docs.neo.org/en-us/node/api.html
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //SendAsset();
            //SendRawTransaction();
            //QueryAvailableBalance();
            Deposit();
            //ProcessNeo();
            //NumConvert();
            //CheckAddr("AXsmH8HNcZ9rc7Jd5ATaUCPD8fjpMUGx6");
            //Bad();
            //Merge();
            //QueryAddrAvailabble();
            //TestToByteArray();
        }

        public static void QueryAddrAvailabble()
        {
            string front = "{ 'jsonrpc': '2.0', 'method': 'invokescript', 'params': ['0a6669727374506861736514";
            string last = "52c113717565727941697244726f7042616c616e6365676de38f3ec36522ec6a8b587a53e94847192769b0'],  'id': 1}";
            List<string> inputs = readFileToList("outLast.txt");
            foreach (string input in inputs)
            {
                string addr = input.Substring(0, 34);
                //CheckAddr(addr, out script);
                string paramter = front + "e952c4dde16eb4d176ad9efcd8ef5896588b746f" + last;
                var r = PostWebRequest("http://10.137.109.3:10332", paramter);
                //var r = PostWebRequest("http://10.137.104.98:10332", paramter);
                //Console.WriteLine(ToGB2312(r));
                var json = BatchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
                var value = json["result"].AsDict()["stack"].GetArrayItem(0).GetDictItem("value");
                Console.WriteLine("addr: " + addr + " value: " + value);
                //if (value.AsString() == "") { 
                //    Console.WriteLine("addr: " +  addr + " value: " + value);
                //}
            }
            Console.ReadLine();
        }

        public static void ProcessNeo()
        {
            List<string> outputs = new List<string>();
            List<string> outDics = new List<string>();
            List<string> rawTransactons = new List<string>();
            Dictionary<string, long> myDictionary = new Dictionary<string, long>();
            int total = 0;
            int totalRight = 0;
            long totalAmount = 0;
            long totalAmountNew = 0;
            List<string> inputs = readFileToList("merges.txt");
            foreach (string input in inputs)
            {
                string ethaddr = input.Substring(0, 42);
                string neoaddr = input.Substring(43, 34);
                long amountOld = Convert.ToInt64(input.Substring(78));
                long amount = amountOld * 2000000;
                string output = neoaddr + "," + amount;
                //Console.WriteLine("eth: " + ethaddr);
                //Console.WriteLine("neo: " + neoaddr);
                //Console.WriteLine("amount: " + amount);
                string script = "";
                if (CheckAddr(neoaddr, out script) && amount >= 100000000)
                {
                    if (myDictionary.ContainsKey(neoaddr))
                    {
                        long value = 0;
                        myDictionary.TryGetValue(neoaddr, out value);
                        value = value + amount;
                        myDictionary.Remove(neoaddr);
                        myDictionary.Add(neoaddr, value);
                        Console.WriteLine("Key:{0},Value:{1}", neoaddr, myDictionary[neoaddr]);
                    }
                    else
                    {
                        myDictionary.Add(neoaddr, amount);
                    }
                    outputs.Add(output);
                    totalAmount = totalAmount + amount;
                    totalRight++;
                }
                else
                {
                    total++;
                    continue;
                }

            }
            Console.WriteLine(myDictionary.Count);
            
            foreach (String key in myDictionary.Keys)
            {
                //long dicAmount = myDictionary[key] * 2000000;
                string outDic = key + "," + myDictionary[key];
                outDics.Add(outDic);
                long finalAmount = myDictionary[key];
                string script = "";
                totalAmountNew = totalAmountNew + myDictionary[key];
                CheckAddr(key, out script);
                string rowtx = "0a66697273745068617365" + Helper.NumConvert((BigInteger)finalAmount) + "14" + script + "53c1076465706f73697467f7c5643ab1896195b8abe8cfd2e3b450441ca45c";
                Console.WriteLine(rowtx);
                rawTransactons.Add(rowtx);
            }

            writeListToFile(outDics, "outLastNew.txt");
            writeListToFile(rawTransactons, "rawTransactonsNew.txt");
            Console.WriteLine(total);
            Console.WriteLine(totalRight);
            Console.WriteLine(totalAmount);
            Console.WriteLine(totalAmountNew);
            Console.ReadLine();
        }

        public static void Bad()
        {
            List<string> outputs = new List<string>();
            List<string> outDics = new List<string>();
            List<string> rawTransactons = new List<string>();
            Dictionary<string, long> myDictionary = new Dictionary<string, long>();
            List<string> inputs = readFileToList("neoaddr.txt");
            foreach (string input in inputs)
            {
                string ethaddr = input.Substring(0, 42);
                string neoaddr = input.Substring(43);
                string script = "";
                Console.WriteLine(ethaddr + "," + neoaddr);
                if (!CheckAddr(neoaddr, out script))
                {
                    rawTransactons.Add(ethaddr + "," + neoaddr);
                }

            }
            writeListToFile(rawTransactons, "bad.csv");

        }

        public static bool CheckAddr(string addr, out string script)
        {

            string paramter = "{'jsonrpc': '2.0', 'method': 'validateaddress', 'params': ['" + addr + "'],  'id': 1}";
            var r = PostWebRequest("http://10.137.105.38:10332", paramter);
            //var r = PostWebRequest("http://10.137.104.98:10332", paramter);
            //Console.WriteLine(ToGB2312(r));
            var json = BatchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
            var value = json["result"].AsDict()["isvalid"];
            //string scriptHash = json["result"].AsDict()["scriptHash"].AsString();
            if (!Convert.ToBoolean(value.ToString()))
            {
                Console.WriteLine("BAD ADDR!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!: " + addr);
                //throw new Exception();
                script = "";
                return false;
            }
            script = json["result"].AsDict()["scriptHash"].AsString();
            //Console.WriteLine(script);
            //Console.WriteLine(ByteToHexStr(ReverseBytes(HexToBytes(script))));
            script = Helper.ByteToHexStr(Helper.ReverseBytes(Helper.HexToBytes(script)));
            return true;
        }

        public static void Deposit()
        {
            List<string> inputs = readFileToList("try385.txt");
            List<string> depositTxs = new List<string>();
            string front = "{'jsonrpc': '2.0', 'method': 'deposit', 'params': ['";
            string last = "'],  'id': 1}";

            foreach (string input in inputs)
            {
                string parameter = front + input + last;
                //string paramter = "{'jsonrpc': '2.0', 'method': 'deposit', 'params': ['0a666972737450686173650500beded5231476c3ff7a1c600e0fd55e422dda77d78e5e4ad5d253c1076465706f736974676de38f3ec36522ec6a8b587a53e94847192769b0'],  'id': 1}";
                var r = PostWebRequest("http://10.137.105.38:10332", parameter);
                //Console.WriteLine(ToGB2312(r));
                var json = BatchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
                var txid = json["result"].AsDict()["txid"].ToString();
                if (txid.Length != 66)
                {
                    Console.WriteLine(parameter);
                    Console.ReadLine();
                    break;
                }
                depositTxs.Add(txid);
                Console.WriteLine("Txid: " + txid);
                //var value = json["result"].AsDict()["stack"].GetArrayItem(0).GetDictItem("value");
                //Console.WriteLine("value: " + value);
            }
            writeListToFile(depositTxs, "try385-txid.txt");
            Console.ReadLine();
        }

        public static void Merge()
        {
            List<string> binds = readFileToList("eth-neo-binding.txt");
            List<string> longs = readFileToList("long-term.txt");
            List<string> merges = new List<string>();
            foreach (string bind in binds)
            {
                string ethaddr = bind.Substring(0, 42);
                string neoaddr = bind.Substring(43, 34);
                long amount = Convert.ToInt64(bind.Substring(78));
                long newAmount = amount;
                foreach (string xlong in longs)
                {
                    string ethaddrlong = xlong.Substring(0, 42);
                    long amountlong = Convert.ToInt64(xlong.Substring(43));
                    if (ethaddr.Equals(ethaddrlong))
                    {
                        Console.WriteLine("addr in long: " + ethaddr);
                        if(amount >0)
                        {
                            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! " + ethaddr);
                        }
                        newAmount = amount + amountlong;
                    }
                }
                merges.Add(ethaddr + "," + neoaddr + "," + newAmount);
            }
            writeListToFile(merges, "merges.txt");
            Console.ReadLine();
        }


        public static void QueryAvailableBalance()
        {

            string paramter = "{'jsonrpc': '2.0', 'method': 'invokescript', 'params': ['14cc82a87f5693c363cd64674024f30a603788134551c1157175657279417661696c61626c6542616c616e63656773031a8b41f8605a281e209249c3bc219522bc7b'],  'id': 1}";
            var r = PostWebRequest("http://localhost:10332", paramter);
            Console.WriteLine(ToGB2312(r));
            var json = BatchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
            var value = json["result"].AsDict()["stack"].GetArrayItem(0).GetDictItem("value");
            Console.WriteLine("value: " + value);
            Console.ReadLine();
        }

        public static void SendRawTransaction()
        {

            string paramter = "{'jsonrpc': '2.0', 'method': 'sendrawtransaction', 'params': ['d1013d1c020203040506070875e0e4e7cfcba82c823d508b114b0a3a321a4cdf51c108776974686472617767bc6f1242290998084d6453653cf64d7eb12095d700000000000000000120bc6f1242290998084d6453653cf64d7eb12095d700000106000430323135fdc9180118c56b6c766b00527ac46c766b51527ac4616168164e656f2e52756e74696d652e47657454726967676572009c6c766b52527ac46c766b52c3645b02616114666f6214f9d89e423d2c40cbdd3a2bf9d949761b6168184e656f2e52756e74696d652e436865636b5769746e6573736c766b56527ac46c766b56c3640e00516c766b57527ac462880561682953797374656d2e457865637574696f6e456e67696e652e476574536372697074436f6e7461696e65726c766b53527ac46c766b53c36168174e656f2e5472616e73616374696f6e2e476574547970656c766b54527ac46c766b54c36102d1009c009c6c766b58527ac46c766b58c3640e00006c766b57527ac46209056c766b53c36c766b55527ac46c766b55c36168234e656f2e496e766f636174696f6e5472616e73616374696f6e2e476574536372697074c0013d9c009c6c766b59527ac46c766b59c36439006116536372697074204c656e67746820696c6c6567616c2161680f4e656f2e52756e74696d652e4c6f6761006c766b57527ac46287046c766b55c36168234e656f2e496e766f636174696f6e5472616e73616374696f6e2e47657453637269707400517f011c9c009c6c766b5a527ac46c766b5ac3640e00006c766b57527ac4623a046c766b55c36168234e656f2e496e766f636174696f6e5472616e73616374696f6e2e476574536372697074011d01207f0351c1080877697468647261777e01677e61682d53797374656d2e457865637574696f6e456e67696e652e476574457865637574696e67536372697074486173687e9c009c6c766b5b527ac46c766b5bc3643200610f53637269707420696c6c6567616c2161680f4e656f2e52756e74696d652e4c6f6761006c766b57527ac4628703516c766b57527ac4627c036168164e656f2e52756e74696d652e47657454726967676572609c6c766b5c527ac46c766b5cc3644703616c766b00c3076465706f736974876c766b5d527ac46c766b5dc3641700616c766b51c3616533036c766b57527ac46220036c766b00c3087769746864726177876c766b5e527ac46c766b5ec3641700616c766b51c3616509086c766b57527ac462ee026c766b00c317717565727941697244726f70546f74616c537570706c79876c766b5f527ac46c766b5fc3644800616168164e656f2e53746f726167652e476574436f6e746578740d61697264726f70537570706c79617c680f4e656f2e53746f726167652e4765746c766b57527ac4627c026c766b00c313717565727941697244726f7042616c616e6365876c766b60527ac46c766b60c3641700616c766b51c36165500d6c766b57527ac4623f026c766b00c3157175657279417661696c61626c6542616c616e6365876c766b0111527ac46c766b0111c3641700616c766b51c36165df0f6c766b57527ac462fe016c766b00c31e7175657279417661696c61626c6542616c616e6365576974685068617365876c766b0112527ac46c766b0112c3641700616c766b51c36165aa096c766b57527ac462b4016c766b00c3117365745769746864726177537769746368876c766b0113527ac46c766b0113c3641700616c766b51c36165da096c766b57527ac46277016c766b00c31371756572795769746864726177537769746368876c766b0114527ac46c766b0114c3644900616168164e656f2e53746f726167652e476574436f6e746578740e5769746864726177537769746368617c680f4e656f2e53746f726167652e4765746c766b57527ac46206016c766b00c31571756572794c617374576974686472617754696d65876c766b0115527ac46c766b0115c3641700616c766b51c36165550a6c766b57527ac462c5006c766b00c316717565727941697264726f704163636f756e744e756d876c766b0116527ac46c766b0116c3644c00616168164e656f2e53746f726167652e476574436f6e746578741161697264726f704163636f756e744e756d617c680f4e656f2e53746f726167652e4765746c766b57527ac4624e006c766b00c313717565727941697264726f704163636f756e74876c766b0117527ac46c766b0117c3641700616c766b51c36165570a6c766b57527ac4620f0061006c766b57527ac46203006c766b57c3616c75660111c56b6c766b00527ac4616114666f6214f9d89e423d2c40cbdd3a2bf9d949761b6168184e656f2e52756e74696d652e436865636b5769746e657373009c6c766b56527ac46c766b56c3640e00006c766b57527ac462a9046c766b00c3c0539c009c6c766b58527ac46c766b58c364050061f06c766b00c300c36c766b51527ac46c766b51c3c001149c009c6c766b59527ac46c766b59c364050061f06c766b00c351c36c766b52527ac46c766b52c300a16314006c766b52c3070031cae8a0e909a0620400516c766b5a527ac46c766b5ac364050061f06168164e656f2e53746f726167652e476574436f6e746578740d61697264726f70537570706c79617c680f4e656f2e53746f726167652e4765746c766b53527ac4006c766b54527ac46c766b51c3616561106c766b5b527ac46c766b5bc364dc00616168164e656f2e53746f726167652e476574436f6e746578741161697264726f704163636f756e744e756d617c680f4e656f2e53746f726167652e47657451936c766b5c527ac46168164e656f2e53746f726167652e476574436f6e746578741161697264726f704163636f756e744e756d6c766b5cc37e6c766b51c3615272680f4e656f2e53746f726167652e507574616168164e656f2e53746f726167652e476574436f6e746578741161697264726f704163636f756e744e756d6c766b5cc3615272680f4e656f2e53746f726167652e50757461616c766b00c352c36c766b55527ac40a666972737450686173656c766b55c3876c766b5d527ac46c766b5dc3649200616168164e656f2e53746f726167652e476574436f6e74657874610a666972737450686173656c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b54527ac46168164e656f2e53746f726167652e476574436f6e74657874610a666972737450686173656c766b51c37e6c766b52c3615272680f4e656f2e53746f726167652e50757461616267010b7365636f6e6450686173656c766b55c3876c766b5e527ac46c766b5ec3649400616168164e656f2e53746f726167652e476574436f6e74657874610b7365636f6e6450686173656c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b54527ac46168164e656f2e53746f726167652e476574436f6e74657874610b7365636f6e6450686173656c766b51c37e6c766b52c3615272680f4e656f2e53746f726167652e507574616162b5000a746869726450686173656c766b55c3876c766b5f527ac46c766b5fc3649200616168164e656f2e53746f726167652e476574436f6e74657874610a746869726450686173656c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b54527ac46168164e656f2e53746f726167652e476574436f6e74657874610a746869726450686173656c766b51c37e6c766b52c3615272680f4e656f2e53746f726167652e50757461616206006161f06c766b53c36c766b54c3946c766b52c3930700935ebae2bc1da06319006c766b53c36c766b54c3946c766b52c393009f620400516c766b60527ac46c766b60c364050061f06168164e656f2e53746f726167652e476574436f6e746578740d61697264726f70537570706c796c766b53c36c766b54c3946c766b52c393615272680f4e656f2e53746f726167652e50757461616c766b51c36c766b52c3617c096465706f736974656453c168124e656f2e52756e74696d652e4e6f7469667961516c766b57527ac46203006c766b57c3616c75660112c56b6c766b00527ac4616c766b00c3c0519c009c6c766b5b527ac46c766b5bc364050061f06c766b00c300c36c766b51527ac46c766b51c3c0011c9c009c6c766b5c527ac46c766b5cc364050061f06c766b51c35801147f6c766b52527ac46168164e656f2e53746f726167652e476574436f6e746578740e5769746864726177537769746368617c680f4e656f2e53746f726167652e4765746c766b53527ac46c766b53c3009c6c766b5d527ac46c766b5dc364050061f06c766b52c3610a66697273745068617365617c65c5066c766b54527ac46c766b52c3610b7365636f6e645068617365617c65a7066c766b55527ac46c766b52c3610a74686972645068617365617c658a066c766b56527ac46c766b54c36c766b55c3936c766b56c3936c766b57527ac46c766b57c3519f6c766b5e527ac46c766b5ec3640e00006c766b5f527ac462890161682d53797374656d2e457865637574696f6e456e67696e652e476574457865637574696e67536372697074486173686c766b58527ac4087472616e7366657253c576006c766b58c3c476516c766b52c3c476526c766b57c3c4617c675f89105fdcf8b97739c68f3e969d60b6e98bfa066c766b59527ac46c766b59c3519c6c766b5a527ac46c766b5ac36c766b60527ac46c766b60c364d800616168184e656f2e426c6f636b636861696e2e4765744865696768746168184e656f2e426c6f636b636861696e2e4765744865616465726168174e656f2e4865616465722e47657454696d657374616d706c766b0111527ac46168164e656f2e53746f726167652e476574436f6e746578746c766b52c3106c617374576974686472617754696d657e6c766b0111c3615272680f4e656f2e53746f726167652e50757461616c766b52c36c766b57c3617c08776974686472657753c168124e656f2e52756e74696d652e4e6f746966796161620f0061006c766b5f527ac4620e00516c766b5f527ac46203006c766b5fc3616c756655c56b6c766b00527ac4616c766b00c3c0529c009c6c766b53527ac46c766b53c3640e00006c766b54527ac46238006c766b00c300c36c766b51527ac46c766b00c351c36c766b52527ac46c766b51c36c766b52c3617c6561046c766b54527ac46203006c766b54c3616c756655c56b6c766b00527ac4616114666f6214f9d89e423d2c40cbdd3a2bf9d949761b6168184e656f2e52756e74696d652e436865636b5769746e657373009c6c766b51527ac46c766b51c3640e00006c766b52527ac462cf006c766b00c3c0519c009c6c766b53527ac46c766b53c3640e00006c766b52527ac462ab006c766b00c300c3026f6e876c766b54527ac46c766b54c3644600616168164e656f2e53746f726167652e476574436f6e746578740e576974686472617753776974636851615272680f4e656f2e53746f726167652e5075746161624300616168164e656f2e53746f726167652e476574436f6e746578740e576974686472617753776974636800615272680f4e656f2e53746f726167652e5075746161516c766b52527ac46203006c766b52c3616c756655c56b6c766b00527ac4616c766b00c3c0519c009c6c766b52527ac46c766b52c3640e00006c766b53527ac46283006c766b00c300c36c766b51527ac46c766b51c3c001149c009c6c766b54527ac46c766b54c3640e00006c766b53527ac46250006168164e656f2e53746f726167652e476574436f6e746578746c766b51c3106c617374576974686472617754696d657e617c680f4e656f2e53746f726167652e4765746c766b53527ac46203006c766b53c3616c756655c56b6c766b00527ac4616c766b00c3c0519c009c6c766b52527ac46c766b52c3640f0001006c766b53527ac46293006c766b00c300c36c766b51527ac46c766b51c3009f6311006c766b51c30400e1f505a0620400516c766b54527ac46c766b54c3640f0001006c766b53527ac46251006168164e656f2e53746f726167652e476574436f6e746578741161697264726f704163636f756e744e756d6c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b53527ac46203006c766b53c3616c756658c56b6c766b00527ac4616c766b00c3c0529c009c6c766b53527ac46c766b53c3640e00006c766b54527ac46268016c766b00c300c36c766b51527ac46c766b00c351c36c766b52527ac40a666972737450686173656c766b52c3876c766b55527ac46c766b55c3644c00616168164e656f2e53746f726167652e476574436f6e74657874610a666972737450686173656c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b54527ac462e3000b7365636f6e6450686173656c766b52c3876c766b56527ac46c766b56c3644d00616168164e656f2e53746f726167652e476574436f6e74657874610b7365636f6e6450686173656c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b54527ac46278000a746869726450686173656c766b52c3876c766b57527ac46c766b57c3644c00616168164e656f2e53746f726167652e476574436f6e74657874610a746869726450686173656c766b51c37e617c680f4e656f2e53746f726167652e4765746c766b54527ac4620f0061006c766b54527ac46203006c766b54c3616c75665cc56b6c766b00527ac46c766b51527ac4616c766b00c3c001149c009c6c766b58527ac46c766b58c3640e00006c766b59527ac462f6006168164e656f2e53746f726167652e476574436f6e746578746c766b51c36c766b00c37e617c680f4e656f2e53746f726167652e4765746c766b52527ac46c766b52c3519f6c766b5a527ac46c766b5ac3640e00006c766b59527ac4629700006c766b53527ac4006c766b54527ac46c766b00c36c766b51c3617c656b016c766b55527ac46c766b00c36c766b51c3617c65b7026c766b56527ac46c766b56c36c766b55c3a06c766b5b527ac46c766b5bc3641c00616c766b56c36c766b55c39403805101966c766b53527ac4616c766b52c36c766b53c39502da02966c766b57527ac46c766b57c36c766b59527ac46203006c766b59c3616c756659c56b6c766b00527ac4616c766b00c3c0519c009c6c766b56527ac46c766b56c3640e00006c766b57527ac462b5006c766b00c300c36c766b51527ac46c766b51c3c001149c009c6c766b58527ac46c766b58c3640e00006c766b57527ac46282006c766b51c3610a66697273745068617365617c6558fe6c766b52527ac46c766b51c3610b7365636f6e645068617365617c653afe6c766b53527ac46c766b51c3610a74686972645068617365617c651dfe6c766b54527ac46c766b52c36c766b53c3936c766b54c3936c766b55527ac46c766b55c36c766b57527ac46203006c766b57c3616c75665ac56b6c766b00527ac46c766b51527ac4616168164e656f2e53746f726167652e476574436f6e746578746c766b00c3106c617374576974686472617754696d657e617c680f4e656f2e53746f726167652e4765746c766b52527ac46c766b51c3610a666972737450686173659c6c766b53527ac46c766b53c3643000616c766b52c30400fe4c5aa16c766b54527ac46c766b54c3641100610400fe4c5a6c766b52527ac4616162a3006c766b51c3610b7365636f6e6450686173659c6c766b55527ac46c766b55c3643000616c766b52c30480c69a5aa16c766b56527ac46c766b56c3641100610480c69a5a6c766b52527ac461616254006c766b51c3610a746869726450686173659c6c766b57527ac46c766b57c3643000616c766b52c3040032eb5aa16c766b58527ac46c766b58c364110061040032eb5a6c766b52527ac461616206006161f06c766b52c36c766b59527ac46203006c766b59c3616c75665ac56b6c766b00527ac46c766b51527ac4616168184e656f2e426c6f636b636861696e2e4765744865696768746168184e656f2e426c6f636b636861696e2e4765744865616465726168174e656f2e4865616465722e47657454696d657374616d706c766b52527ac46c766b51c3610a666972737450686173659c6c766b53527ac46c766b53c3643000616c766b52c30400650f5ea06c766b54527ac46c766b54c3641100610400650f5e6c766b52527ac4616162a3006c766b51c3610b7365636f6e6450686173659c6c766b55527ac46c766b55c3643000616c766b52c304802d5d5ea06c766b56527ac46c766b56c36411006104802d5d5e6c766b52527ac461616254006c766b51c3610a746869726450686173659c6c766b57527ac46c766b57c3643000616c766b52c3040099ad5ea06c766b58527ac46c766b58c364110061040099ad5e6c766b52527ac461616206006161f06c766b52c36c766b59527ac46203006c766b59c3616c756656c56b6c766b00527ac4616168164e656f2e53746f726167652e476574436f6e74657874610a666972737450686173656c766b00c37e617c680f4e656f2e53746f726167652e4765746c766b51527ac46168164e656f2e53746f726167652e476574436f6e74657874610b7365636f6e6450686173656c766b00c37e617c680f4e656f2e53746f726167652e4765746c766b52527ac46168164e656f2e53746f726167652e476574436f6e74657874610a746869726450686173656c766b00c37e617c680f4e656f2e53746f726167652e4765746c766b53527ac46c766b51c3009c6417006c766b52c3009c640d006c766b53c3009c620400006c766b54527ac46c766b54c3640f0061516c766b55527ac4620e00006c766b55527ac46203006c766b55c3616c7566'],  'id': 1}";
            var r = PostWebRequest("http://localhost:10332", paramter);
            Console.WriteLine(ToGB2312(r));
            var json = BatchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
            var txid = json["result"].AsDict()["txid"].ToString();
            if (txid.Length != 66)
            {
                Console.WriteLine(paramter);
                Console.ReadLine();
                throw new Exception();
            }
            Console.WriteLine("Txid: " + txid);
            Console.ReadLine();
        }


        public static void SendAsset()
        {
            List<string> outputs = new List<string>();
            int total = 0;
            try
            {
                List<string> inputs = readFileToList("inputs206.txt");

                string front = "{'jsonrpc': '2.0', 'method': '', 'params': ['0x06fa8be9b6609d963e8fc63977b9f8dc5f10895f', '";
                string middle = "', ";
                string last = "],  'id': 1} ";
                foreach (string input in inputs)
                {
                    int amount = Convert.ToInt32(input.Substring(35));
                    string addr = input.Substring(0, 34);
                    string paramter = front + addr + middle + amount + last;
                    Console.WriteLine(amount);
                    Console.WriteLine(input);
                    Console.WriteLine(paramter);
                    total += amount;
                    var r = PostWebRequest("http://localhost:10332", paramter);
                    var json = BatchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
                    var txid = json["result"].AsDict()["txid"].ToString();
                    if (txid.Length != 66)
                    {
                        writeListToFile(outputs, "outputs206.txt");
                        Console.WriteLine(total);
                        Console.ReadLine();
                        throw new Exception();
                    }
                    Console.WriteLine(txid.ToString());
                    string output = addr + "  " + amount + "  " + txid;
                    outputs.Add(output);
                }
                writeListToFile(outputs, "outputs206.txt");
                Console.WriteLine(total);
                Console.ReadLine();
            }
            catch
            {
                writeListToFile(outputs, "outputs206.txt");
                Console.WriteLine(total);
                Console.WriteLine("Exception!");
                Console.ReadLine();
            }
        }

        public static string PostWebRequest(string postUrl, string paramData)
        {
            try
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(paramData);
                WebRequest webReq = WebRequest.Create(postUrl);
                webReq.Method = "POST";
                using (Stream newStream = webReq.GetRequestStream())
                {
                    newStream.Write(byteArray, 0, byteArray.Length);
                }
                using (WebResponse response = webReq.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";
        }


        public static string ToGB2312(string str)
        {
            MatchCollection mc = Regex.Matches(str, @"\\u([\w]{2})([\w]{2})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            byte[] bts = new byte[2];
            foreach (Match m in mc)
            {
                bts[0] = (byte)int.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
                bts[1] = (byte)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                str = str.Replace(m.ToString(), Encoding.Unicode.GetString(bts));
            }
            return str;
        }


        public static List<string> readFileToList(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            List<string> list = new List<string>();
            StreamReader m_streamReader = new StreamReader(fs);//中文乱码加上System.Text.Encoding.Default,或则 System.Text.Encoding.GetEncoding("GB2312")
            //使用StreamReader类来读取文件
            m_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            // 从数据流中读取每一行，直到文件的最后一行
            string strLine = m_streamReader.ReadLine();
            while (strLine != null)
            {
                list.Add(strLine);
                strLine = m_streamReader.ReadLine();
            }
            //关闭此StreamReader对象
            m_streamReader.Close();
            return list;
        }


        public static void writeListToFile(List<string> pList, string myFileName)
        {
            if (File.Exists(myFileName))
            {
                try
                {
                    File.Delete(myFileName);
                }
                finally { };
            }
            //创建一个文件流，用以写入或者创建一个StreamWriter
            System.IO.FileStream fs = new System.IO.FileStream(myFileName, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter m_streamWriter = new StreamWriter(fs);
            m_streamWriter.Flush();
            // 使用StreamWriter来往文件中写入内容
            m_streamWriter.BaseStream.Seek(0, SeekOrigin.Begin);
            // 写入文件
            for (int i = 0; i < pList.Count; i++)
            {
                m_streamWriter.WriteLine(pList[i]);
            }
            //关闭此文件
            m_streamWriter.Flush();
            m_streamWriter.Close();
        }
    }
}
