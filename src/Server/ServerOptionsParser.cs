using System;

namespace codecrafters_redis.src.Server
{
    public static class ServerOptionsParser
    {
        public static void Apply(string[] args)
        {
            if (args is null || args.Length == 0)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.Equals(arg, "--port", StringComparison.Ordinal))
                {
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out int port))
                        throw new ArgumentException("Missing or invalid value for --port.");

                    ServerInfo.Port = port;
                    i++;
                    continue;
                }

                if (string.Equals(arg, "--replicaof", StringComparison.Ordinal))
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("Missing value for --replicaof.");

                    string value = args[i + 1];
                    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length != 2 || !int.TryParse(parts[1], out int masterPort))
                        throw new ArgumentException("Invalid value for --replicaof. Expected: \"host port\".");

                    ServerInfo.ReplicaOfHost = parts[0];
                    ServerInfo.ReplicaOfPort = masterPort;
                    ServerInfo.Role = "slave";
                    i++;
                    continue;
                }
            }
        }
    }
}
