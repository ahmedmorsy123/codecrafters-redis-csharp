namespace codecrafters_redis.src
{
    public static class ServerInfo
    {
        public static Replication.ReplicaConnectionManager Replicas { get; } = new();

        public static int Port { get; set; } = 6379;
        public static string Role { get; set; } = "master";
        public static bool IsReplica => !Role.Equals("master", StringComparison.OrdinalIgnoreCase);
        public static string ReplId { get; set; } = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
        public static string? ReplicaOfHost { get; set; }
        public static int? ReplicaOfPort { get; set; }
        public static string MasterReplId { get; set; } = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
        public static int MasterReplOffset { get; set; } = 0;

        public static string GetInfoLine(string key)
        {
            string value = key switch
            {
                "role" => Role,
                "port" => Port.ToString(),
                "replicaof" => ReplicaOfHost is null || ReplicaOfPort is null
                    ? string.Empty
                    : $"{ReplicaOfHost} {ReplicaOfPort}",
                "master_replid" => MasterReplId,
                "master_repl_offset" => MasterReplOffset.ToString(),
                _ => string.Empty
            };

            return $"{key}:{value}";
        }
    }
}

