using System;
using System.Collections.Generic;
using System.Text;
using codecrafters_redis.src.RedisValues;

namespace codecrafters_redis.src.Resp
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

        public static string EncodeNull()
        {
            return "$-1\r\n";
        }

        public static string EncodeNullArray()
        {
            return "*-1\r\n";
        }

        public static string EncodeError(string errorMessage)
        {
            Console.Error.WriteLine($"Error: {errorMessage}");
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

        public static string EncodeStreamEntries(IReadOnlyList<StreamEntry> entries)
        {
            StringBuilder response = new();
            response.Append('*').Append(entries.Count).Append("\r\n");

            foreach (StreamEntry entry in entries)
            {
                response.Append("*2\r\n");
                response.Append(EncodeBulkString(entry.Id.ToString()));

                response.Append('*').Append(entry.Fields.Count * 2).Append("\r\n");
                foreach (var field in entry.Fields)
                {
                    response.Append(EncodeBulkString(field.Key));
                    response.Append(EncodeBulkString(field.Value));
                }
            }

            return response.ToString();
        }

        public static string EncodeXReadMultipleStreams(IReadOnlyDictionary<string, IReadOnlyList<StreamEntry>> streamEntries)
        {
            StringBuilder response = new();
            response.Append('*').Append(streamEntries.Count).Append("\r\n");
            foreach (var kvp in streamEntries)
            {
                string key = kvp.Key;
                IReadOnlyList<StreamEntry> entries = kvp.Value;
                response.Append("*2\r\n");
                response.Append(EncodeBulkString(key));
                response.Append(EncodeStreamEntries(entries));
            }
            return response.ToString();
        }

        public static byte[] EncodeRDBFile(byte[] rdbData)
        {
            byte[] header = Encoding.ASCII.GetBytes($"${rdbData.Length}\r\n");
            byte[] trailer = Encoding.ASCII.GetBytes("\r\n");

            byte[] result = new byte[header.Length + rdbData.Length + trailer.Length];
            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(rdbData, 0, result, header.Length, rdbData.Length);
            Buffer.BlockCopy(trailer, 0, result, header.Length + rdbData.Length, trailer.Length);

            return result;
        }
    }
}
