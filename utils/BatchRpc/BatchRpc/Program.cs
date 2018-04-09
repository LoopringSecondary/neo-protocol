using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace json_rpc
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
            try
            {
                List<string> inputs = readFileToList("input.txt");
                List<string> outputs = new List<string>();
                int total = 0;
                string front = "{'jsonrpc': '2.0', 'method': 'sendmany', 'params': [[{'asset': '0x06fa8be9b6609d963e8fc63977b9f8dc5f10895f','value': ";
                string middle = ",'address': '";
                string last = "'}]],  'id': 1} ";
                foreach (string input in inputs)
                {
                    int amount = Convert.ToInt32(input.Substring(35));
                    string addr = input.Substring(0, 34);
                    string paramter = front + amount + middle + addr + last;
                    Console.WriteLine(amount);
                    Console.WriteLine(input);
                    Console.WriteLine(paramter);
                    total += amount;
                    var r = PostWebRequest("http://10.137.104.96:10332", paramter);
                    Console.WriteLine(ToGB2312(r));
                    var json = batchRpc.MyJson.Parse(ToGB2312(r)).AsDict();
                    var txid = json["result"].AsDict()["txid"].ToString();
                    if (txid.Length != 66)
                    {
                        throw new Exception();
                    }
                    Console.WriteLine(txid.ToString());
                    string output = addr + "  " + amount + "  " + txid;
                    outputs.Add(output);
                }
                writeListToFile(outputs, "outputs.txt");
                Console.WriteLine(total);
                Console.ReadLine();
            }
            catch
            {
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
