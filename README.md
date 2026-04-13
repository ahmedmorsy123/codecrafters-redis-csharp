# Redis Clone — C# (.NET 10)

[![progress-banner](https://backend.codecrafters.io/progress/redis/e493d2f9-4224-41e4-bd8f-96f0bae838b9)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

A fully-featured Redis server clone built in C# as part of the [CodeCrafters "Build Your Own Redis" Challenge](https://codecrafters.io/challenges/redis). It implements the Redis Serialization Protocol (RESP), persistent storage, master/replica replication, Pub/Sub, transactions, geo commands, streams, sorted sets, blocking operations, and access-control authentication — all from scratch.

---

## Table of Contents

- [Requirements](#requirements)
- [Getting Started](#getting-started)
- [CLI Options](#cli-options)
- [Architecture](#architecture)
- [Supported Commands](#supported-commands)
  - [General](#general)
  - [String](#string)
  - [List](#list)
  - [Sorted Set](#sorted-set)
  - [Stream](#stream)
  - [Geo](#geo)
  - [Transactions](#transactions)
  - [Pub/Sub](#pubsub)
  - [Replication](#replication)
  - [Persistence](#persistence)
  - [Security (ACL / AUTH)](#security-acl--auth)
- [Project Structure](#project-structure)

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## Getting Started

```sh
# Run the server on the default port 6379
./your_program.sh

# Connect with the Redis CLI
redis-cli ping
```

---

## CLI Options

| Flag | Description | Default |
|------|-------------|---------|
| `--port <port>` | Port the server listens on | `6379` |
| `--replicaof "<host> <port>"` | Start as a replica of the given master | *(none — master mode)* |
| `--dir <path>` | Directory containing the RDB persistence file | *(none)* |
| `--dbfilename <name>` | RDB file name to load on startup | *(none)* |

**Examples**

```sh
# Start on a custom port
./your_program.sh --port 6380

# Start as a replica
./your_program.sh --port 6380 --replicaof "localhost 6379"

# Load an existing RDB dump
./your_program.sh --dir /data --dbfilename dump.rdb
```

---

## Architecture

```
Program.cs
│
├── ServerOptionsParser   — Parses CLI flags and configures ServerInfo / ServerConfiguration
├── RdbPersistence        — Loads the RDB file from disk on startup
├── ReplicaBootstrapper   — Initiates handshake with master (replica mode only)
│
├── RedisServer           — TCP listener; accepts new client connections
├── ClientConnectionHandler — Creates a ClientSession per connection
├── CommandDispatcher     — Routes incoming RESP commands to the correct handler
└── CommandHandlerRegistry — Discovers all ICommand implementations via reflection
```

### Key Modules

| Module | Location | Responsibility |
|--------|----------|----------------|
| **RESP Codec** | `src/Resp/` | Encode & decode the Redis Serialization Protocol |
| **Storage** | `src/Storage/` | Thread-safe in-memory key-value store with TTL support |
| **Redis Values** | `src/RedisValues/` | Typed value wrappers: `RedisString`, `RedisList`, `RedisSortedSet`, `RedisStream` |
| **Persistence** | `src/Persistence/` | RDB file parsing and loading |
| **Replication** | `src/Replication/` | Master-side propagation + replica handshake |
| **Pub/Sub** | `src/Channels/` | Channel-based message broadcasting |
| **Transactions** | `src/Client/` | Per-session MULTI/EXEC queue and WATCH tracking |
| **Blocking Ops** | `src/Blocking/` | Async blocking pop coordination (`BLPOP`) |
| **Security** | `src/Security/` | User registry, password management, AUTH |
| **Server Config** | `src/ServerConfigration.cs` | Runtime key-value configuration store |
| **Server Info** | `src/ServerInfo.cs` | Role, replication ID, offset, and port |

---

## Supported Commands

### General

| Command | Syntax | Description |
|---------|--------|-------------|
| `PING` | `PING [message]` | Returns `PONG` or echoes the message |
| `ECHO` | `ECHO <message>` | Returns the message back |
| `KEYS` | `KEYS <pattern>` | Lists all keys matching the glob pattern |
| `TYPE` | `TYPE <key>` | Returns the type of the value stored at a key |
| `CONFIG` | `CONFIG GET <param>` | Returns server configuration parameters |
| `INFO` | `INFO [section]` | Returns server and replication information |
| `INCR` | `INCR <key>` | Increments the integer value of a key by 1 |

### String

| Command | Syntax | Description |
|---------|--------|-------------|
| `SET` | `SET <key> <value> [EX seconds \| PX ms]` | Sets a key to a string value with optional TTL |
| `GET` | `GET <key>` | Gets the string value of a key |

### List

| Command | Syntax | Description |
|---------|--------|-------------|
| `LPUSH` | `LPUSH <key> <value> [value ...]` | Prepends values to a list |
| `RPUSH` | `RPUSH <key> <value> [value ...]` | Appends values to a list |
| `LPOP` | `LPOP <key>` | Removes and returns the first element of a list |
| `LRANGE` | `LRANGE <key> <start> <stop>` | Returns a range of elements from a list |
| `LLEN` | `LLEN <key>` | Returns the length of a list |
| `BLPOP` | `BLPOP <key> [key ...] <timeout>` | Blocking left pop; waits up to `timeout` seconds |

### Sorted Set

| Command | Syntax | Description |
|---------|--------|-------------|
| `ZADD` | `ZADD <key> <score> <member>` | Adds or updates a member with a score |
| `ZCARD` | `ZCARD <key>` | Returns the number of members |
| `ZRANGE` | `ZRANGE <key> <start> <stop> [WITHSCORES]` | Returns a range of members by index |
| `ZRANK` | `ZRANK <key> <member>` | Returns the rank of a member (ascending) |
| `ZREM` | `ZREM <key> <member>` | Removes a member |
| `ZSCORE` | `ZSCORE <key> <member>` | Returns the score of a member |

### Stream

| Command | Syntax | Description |
|---------|--------|-------------|
| `XADD` | `XADD <key> <id\|*> <field> <value> [...]` | Appends an entry to a stream |
| `XRANGE` | `XRANGE <key> <start> <end>` | Returns a range of entries from a stream |
| `XREAD` | `XREAD [COUNT n] [BLOCK ms] STREAMS <key> <id>` | Reads entries from one or more streams |

### Geo

| Command | Syntax | Description |
|---------|--------|-------------|
| `GEOADD` | `GEOADD <key> <lng> <lat> <member>` | Adds a geo-indexed member |
| `GEODIST` | `GEODIST <key> <m1> <m2> [unit]` | Returns the distance between two members |
| `GEOPOS` | `GEOPOS <key> <member> [member ...]` | Returns the coordinates of members |
| `GEOSEARCH` | `GEOSEARCH <key> ...` | Searches for members within a radius or box |

### Transactions

| Command | Syntax | Description |
|---------|--------|-------------|
| `MULTI` | `MULTI` | Starts a transaction block |
| `EXEC` | `EXEC` | Executes all queued commands atomically |
| `DISCARD` | `DISCARD` | Discards all queued commands |
| `WATCH` | `WATCH <key> [key ...]` | Marks keys to watch for changes before EXEC |
| `UNWATCH` | `UNWATCH` | Cancels all watched keys |

> **Optimistic locking**: If any watched key is modified before `EXEC`, the transaction is automatically aborted.

### Pub/Sub

| Command | Syntax | Description |
|---------|--------|-------------|
| `SUBSCRIBE` | `SUBSCRIBE <channel>` | Subscribes to a channel |
| `PUBLISH` | `PUBLISH <channel> <message>` | Publishes a message to a channel |
| `UNSUBSCRIBE` | `UNSUBSCRIBE <channel>` | Unsubscribes from a channel |

### Replication

| Command | Syntax | Description |
|---------|--------|-------------|
| `INFO` | `INFO replication` | Reports role, master replication ID, and offset |
| `REPLCONF` | `REPLCONF ...` | Exchanged between master and replica during handshake |
| `PSYNC` | `PSYNC <replid> <offset>` | Initiates partial or full re-synchronisation |
| `WAIT` | `WAIT <numreplicas> <timeout>` | Blocks until replicas acknowledge writes |

**How replication works**

1. Start a master on any port.
2. Start one or more replicas with `--replicaof "master-host master-port"`.
3. The replica performs a handshake (`REPLCONF` + `PSYNC`), receives an RDB snapshot, then streams all subsequent write commands in real time.

### Persistence

| Command | Description |
|---------|-------------|
| `SAVE` | Forces an RDB snapshot to disk immediately |

On startup, if `--dir` and `--dbfilename` are provided, the server automatically loads the RDB file to restore all previously persisted keys.

### Security (ACL / AUTH)

| Command | Syntax | Description |
|---------|--------|-------------|
| `AUTH` | `AUTH <username> <password>` | Authenticates the current connection |
| `ACL WHOAMI` | `ACL WHOAMI` | Returns the username of the current session |
| `ACL SETUSER` | `ACL SETUSER <username> ><password>` | Creates or updates a user with a password |
| `ACL GETUSER` | `ACL GETUSER <username>` | Returns properties of a user |

---

## Project Structure

```
.
├── codecrafters-redis.csproj   # .NET 10 project file
├── codecrafters-redis.sln
├── codecrafters.yml            # CodeCrafters build configuration
├── your_program.sh             # Entry-point script
└── src/
    ├── Program.cs              # Application bootstrap
    ├── ServerInfo.cs           # Role, replication ID/offset, port
    ├── ServerConfigration.cs   # Runtime key-value configuration
    ├── Blocking/
    │   └── KeyBlockingCoordinator.cs   # BLPOP async coordination
    ├── Channels/
    │   └── ChannelsManager.cs          # Pub/Sub channel registry
    ├── Client/
    │   ├── ClientSession.cs            # Per-connection state & transaction queue
    │   └── ClientWatcher.cs            # WATCH key tracking
    ├── Commands/               # One file per Redis command (ICommand)
    │   ├── ACL.cs, AUTH.cs
    │   ├── BLPOP.cs
    │   ├── CONFIG.cs, ECHO.cs, INCR.cs, INFO.cs, KEYS.cs, PING.cs, Type.cs
    │   ├── DISCARD.cs, EXEC.cs, MULTI.cs, WATCH.cs, UNWATCH.cs
    │   ├── GEOADD.cs, GEODIST.cs, GEOPOS.cs, GEOSEARCH.cs
    │   ├── Get.cs, Set.cs
    │   ├── LLEN.cs, LPOP.cs, LPush.cs, LRANGE.cs, RPush.cs
    │   ├── PSYNC.cs, REPLCONF.cs, WAIT.cs
    │   ├── PUBLISH.cs, SUBSCRIBE.cs, UNSUBSCRIBE.cs
    │   ├── SAVE.cs
    │   ├── XADD.cs, XRANGE.cs, XREAD.cs
    │   └── ZADD.cs, ZCARD.cs, ZRANGE.cs, ZRANK.cs, ZREM.cs, ZSCORE.cs
    ├── Persistence/
    │   └── RdbPersistence.cs           # RDB file loading
    ├── RedisValues/
    │   ├── IRedisValue.cs
    │   ├── RedisString.cs
    │   ├── RedisList.cs
    │   ├── RedisSortedSet.cs
    │   └── RedisStream.cs
    ├── Replication/
    │   ├── MasterClient.cs             # Replica-side connection to master
    │   ├── ReplicaBootstrapper.cs      # Replica startup handshake
    │   └── ReplicaConnectionManager.cs # Master-side replica tracking
    ├── Resp/
    │   ├── RespDecoder.cs              # Parse incoming RESP frames
    │   └── RespEncoder.cs              # Serialize RESP responses
    ├── Security/
    │   └── UsersManager.cs             # User registry and authentication
    └── Server/
        ├── ClientConnectionHandler.cs  # Accepts and hands off TCP connections
        ├── CommandDispatcher.cs        # Routes commands to handlers
        ├── CommandHandlerRegistry.cs   # Reflection-based command discovery
        ├── RedisServer.cs              # Async TCP listener
        └── ServerOptionsParser.cs      # CLI argument parsing
```
