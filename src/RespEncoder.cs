using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public static class RespEncoder
    {
        // RESP messages are encoded as follows:
        // - Simple Strings: Start with '+' followed by the string and end with '\r\n'
        // - Errors: Start with '-' followed by the error message and end with '\r\n'
        // - Integers: Start with ':' followed by the integer and end with '\r\n'
        // - Bulk Strings: Start with '$' followed by the length of the string, then '\r\n', then the string itself, and end with '\r\n'
        // - Arrays: Start with '*' followed by the number of elements, then '\r\n', then each element encoded as above

        public static string EncodeSimpleString(string data)
        {
             return "+" + data + "\r\n";
        }

        public static string EncodeBulkString(string data)
        {
            return "$" + data.Length + "\r\n" + data + "\r\n";
        }

        public static string EncodeNullBulkString()
        {
            return "$-1\r\n";
        }

        public static string EncodeError(string errorMessage)
        {
            return "-" + errorMessage + "\r\n";
        }   

        public static string EncodeInteger(int number)
        {
            return ":" + number + "\r\n";
        }

        public static string EncodeArray(IReadOnlyList<string> elements)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("*" + elements.Count + "\r\n");
            foreach (var element in elements)
            {
                sb.Append(EncodeBulkString(element));
            }
            return sb.ToString();
        }
    }
}
