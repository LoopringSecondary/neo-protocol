using System;
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
            var r = PostWebRequest("http://ip:20332", "{'jsonrpc': '2.0', 'method': 'sendmany', 'params': [[{'asset': '0x9f58b354a93b8bde097b38bddfdbf84d99b213ae','value': 5.5555555,'address': 'AZy6n4jDAN4ssEDucN42Cpyj442K4u16r4'},{'asset': '0x9f58b354a93b8bde097b38bddfdbf84d99b213ae','value': 1,'address': 'AdqLRCBxDRTQLDqQE8GMSGU4j2ydYPLQHv'}]],  'id': 1}");
            Console.WriteLine(ToGB2312(r));
            Console.ReadLine();
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
    }
}
