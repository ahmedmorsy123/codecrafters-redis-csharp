using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{

    // This class is responsible for decoding RESP (Redis Serialization Protocol) messages.
    // It Should read the string (the string converted using Encoding.UTF8.GetString) received from the client and convert it into a string that can be processed by the server.
    public static class RespDecoder
    {
        public static async Task<List<string>> DecodeAsync(string data)
        {
            List<string> result = new List<string>();
            // RESP messages start with a '*' followed by the number of elements in the array
            if (data.StartsWith("*"))
            {
                string[] lines = data.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                int numElements = int.Parse(lines[0].Substring(1)); // Get the number of elements
                for (int i = 2; i < lines.Length; i += 2) // Skip the length lines
                {
                    result.Add(lines[i]); // Add the actual command/argument to the result
                }
            }
            else
            {
                // If it's not an array, we can just return the raw string as a single element list
                result.Add(data.Trim());
            }
            return result;
        }
    }
}
