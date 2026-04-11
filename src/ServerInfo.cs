namespace codecrafters_redis.src
{
    public static class ServerInfo
    {
        public static int Port { get; set; } = 6379;

        public static string Role { get; set; } = "master";

        public static string? ReplicaOfHost { get; set; }

        public static int? ReplicaOfPort { get; set; }

        public static string GetInfoLine(string key)
        {
            string value = key switch
            {
                "role" => Role,
                "port" => Port.ToString(),
                "replicaof" => ReplicaOfHost is null || ReplicaOfPort is null
                    ? string.Empty
                    : $"{ReplicaOfHost} {ReplicaOfPort}",
                _ => string.Empty
            };

            return $"{key}:{value}";
        }
    }
}

