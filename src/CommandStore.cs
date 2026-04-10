using codecrafters_redis.src.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class CommandStore
    {
        public static bool isMULTI = false;

        public static Dictionary<ICommand, string[]> Commands = new();

        public static void Store(ICommand command, string[] args)
        {
            Commands[command] = args;
        }

        public static void Clear()
        {
            Commands.Clear();
        }

        public static async Task<string> ExecuteAllAsync()
        {
            StringBuilder result = new();
            result.Append("*").Append(Commands.Count).Append("\r\n");
            foreach (var entry in Commands)
            {
                string commandResult = await entry.Key.ExecuteAsync(entry.Value);
                result.Append(commandResult);
            }
            return result.ToString();
        }
    }
}
