using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BatchRpc
{
    class Helper
    {
        public static string NumConvert(BigInteger num)
        {
            byte[] bytes = num.ToByteArray();
            byte[] len = BitConverter.GetBytes(bytes.Length);
            string result = ByteToHexStr(len).Substring(0, 2) + ByteToHexStr(bytes);
            return result;
        }

        public static byte[] HexToBytes(string hexString)
        {
            hexString = hexString.Trim().Substring(2);
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return returnBytes;
        }

        //翻转byte数组
        public static byte[] ReverseBytes(byte[] bytes)
        {
            byte tmp;
            int len = bytes.Length;

            for (int i = 0; i < len / 2; i++)
            {
                tmp = bytes[len - 1 - i];
                bytes[len - 1 - i] = bytes[i];
                bytes[i] = tmp;
            }
            return bytes;
        }

        public static string ByteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("x2");
                }
            }
            return returnStr;
        }

        public static string ByteToHexStr(byte[] bytes, int pos)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < pos; i++)
                {
                    returnStr += bytes[i].ToString("x2");
                }
            }
            return returnStr;
        }
    }
}
