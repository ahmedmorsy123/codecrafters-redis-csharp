using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Storage;
using System.Globalization;
using System.Text;

namespace codecrafters_redis.src.Persistence
{
    public static class RdbPersistence
    {
        private const byte OpCodeAux = 0xFA;
        private const byte OpCodeResizeDb = 0xFB;
        private const byte OpCodeExpireTimeMs = 0xFC;
        private const byte OpCodeExpireTimeSeconds = 0xFD;
        private const byte OpCodeSelectDb = 0xFE;
        private const byte OpCodeEof = 0xFF;
        private const byte ValueTypeString = 0x00;

        public static string GetConfiguredRdbPath()
        {
            string dir = ServerConfigration.TryGet("dir", out string? configuredDir)
                ? configuredDir!
                : Directory.GetCurrentDirectory();

            string dbFileName = ServerConfigration.TryGet("dbfilename", out string? configuredName)
                ? configuredName!
                : "dump.rdb";

            return Path.Combine(dir, dbFileName);
        }

        public static void LoadFromConfiguredFile(InMemoryStorage store)
        {
            string path = GetConfiguredRdbPath();
            if (!File.Exists(path))
            {
                return;
            }

            using FileStream fs = File.OpenRead(path);
            using BinaryReader reader = new(fs, Encoding.UTF8, leaveOpen: false);

            List<ParsedEntry> entries = Parse(reader);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            foreach (ParsedEntry entry in entries)
            {
                if (entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value <= now)
                {
                    continue;
                }

                if (entry.ExpiresAtUtc.HasValue)
                {
                    TimeSpan ttl = entry.ExpiresAtUtc.Value - now;
                    if (ttl > TimeSpan.Zero)
                    {
                        store.SetEntry(entry.Key, new RedisString(entry.Value), ttl);
                    }
                }
                else
                {
                    store.SetEntry(entry.Key, new RedisString(entry.Value));
                }
            }
        }

        public static int SaveToConfiguredFile(InMemoryStorage store)
        {
            string path = GetConfiguredRdbPath();
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var snapshot = store.SnapshotEntries();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var stringEntries = snapshot
                .Where(kvp => kvp.Value.Value is RedisString)
                .Select(kvp => new
                {
                    Key = kvp.Key,
                    Value = ((RedisString)kvp.Value.Value).Value,
                    ExpiresAtUtc = kvp.Value.ExpiresAtUtc
                })
                .Where(e => !e.ExpiresAtUtc.HasValue || e.ExpiresAtUtc.Value > now)
                .ToList();

            int expiresCount = stringEntries.Count(e => e.ExpiresAtUtc.HasValue);

            using FileStream fs = File.Create(path);
            using BinaryWriter writer = new(fs, Encoding.UTF8, leaveOpen: false);

            writer.Write(Encoding.ASCII.GetBytes("REDIS0011"));

            writer.Write(OpCodeSelectDb);
            WriteLength(writer, 0);

            writer.Write(OpCodeResizeDb);
            WriteLength(writer, (ulong)stringEntries.Count);
            WriteLength(writer, (ulong)expiresCount);

            foreach (var entry in stringEntries)
            {
                if (entry.ExpiresAtUtc.HasValue)
                {
                    writer.Write(OpCodeExpireTimeMs);
                    ulong unixTimeMs = (ulong)entry.ExpiresAtUtc.Value.ToUnixTimeMilliseconds();
                    writer.Write(unixTimeMs);
                }

                writer.Write(ValueTypeString);
                WriteString(writer, entry.Key);
                WriteString(writer, entry.Value);
            }

            writer.Write(OpCodeEof);
            writer.Write(0UL);

            return stringEntries.Count;
        }

        private static List<ParsedEntry> Parse(BinaryReader reader)
        {
            byte[] header = reader.ReadBytes(9);
            if (header.Length != 9 || Encoding.ASCII.GetString(header) != "REDIS0011")
            {
                throw new InvalidDataException("Invalid or unsupported RDB header.");
            }

            List<ParsedEntry> entries = new();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte opCode = reader.ReadByte();

                if (opCode == OpCodeAux)
                {
                    _ = ReadString(reader);
                    _ = ReadString(reader);
                    continue;
                }

                if (opCode == OpCodeSelectDb)
                {
                    _ = ReadLength(reader);
                    continue;
                }

                if (opCode == OpCodeResizeDb)
                {
                    _ = ReadLength(reader);
                    _ = ReadLength(reader);
                    continue;
                }

                if (opCode == OpCodeEof)
                {
                    _ = reader.ReadBytes(8);
                    break;
                }

                DateTimeOffset? expiresAtUtc = null;
                byte valueType;

                if (opCode == OpCodeExpireTimeMs)
                {
                    ulong expireMs = reader.ReadUInt64();
                    expiresAtUtc = DateTimeOffset.FromUnixTimeMilliseconds((long)expireMs);
                    valueType = reader.ReadByte();
                }
                else if (opCode == OpCodeExpireTimeSeconds)
                {
                    uint expireSeconds = reader.ReadUInt32();
                    expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expireSeconds);
                    valueType = reader.ReadByte();
                }
                else
                {
                    valueType = opCode;
                }

                if (valueType != ValueTypeString)
                {
                    throw new NotSupportedException($"Unsupported RDB value type '{valueType}'.");
                }

                string key = ReadString(reader);
                string value = ReadString(reader);

                entries.Add(new ParsedEntry(key, value, expiresAtUtc));
            }

            return entries;
        }

        private static string ReadString(BinaryReader reader)
        {
            EncodedLength encodedLength = ReadEncodedLength(reader);

            if (!encodedLength.IsSpecialEncoding)
            {
                byte[] bytes = reader.ReadBytes((int)encodedLength.Value);
                if (bytes.Length != (int)encodedLength.Value)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading RDB string.");
                }

                return Encoding.UTF8.GetString(bytes);
            }

            return encodedLength.Value switch
            {
                0 => reader.ReadSByte().ToString(CultureInfo.InvariantCulture),
                1 => reader.ReadInt16().ToString(CultureInfo.InvariantCulture),
                2 => reader.ReadInt32().ToString(CultureInfo.InvariantCulture),
                3 => throw new NotSupportedException("LZF-compressed strings are not supported."),
                _ => throw new InvalidDataException("Invalid string encoding type in RDB data.")
            };
        }

        private static ulong ReadLength(BinaryReader reader)
        {
            EncodedLength encodedLength = ReadEncodedLength(reader);
            if (encodedLength.IsSpecialEncoding)
            {
                throw new InvalidDataException("Expected plain length encoding but found special encoding.");
            }

            return encodedLength.Value;
        }

        private static EncodedLength ReadEncodedLength(BinaryReader reader)
        {
            byte firstByte = reader.ReadByte();
            byte topTwoBits = (byte)(firstByte >> 6);

            return topTwoBits switch
            {
                0b00 => new EncodedLength(false, (ulong)(firstByte & 0b0011_1111)),
                0b01 => new EncodedLength(false, (ulong)(((firstByte & 0b0011_1111) << 8) | reader.ReadByte())),
                0b10 => new EncodedLength(false, ReadUInt32BigEndian(reader)),
                0b11 => new EncodedLength(true, (ulong)(firstByte & 0b0011_1111)),
                _ => throw new InvalidDataException("Invalid RDB length encoding.")
            };
        }

        private static uint ReadUInt32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (bytes.Length != 4)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading 32-bit value.");
            }

            return ((uint)bytes[0] << 24)
                 | ((uint)bytes[1] << 16)
                 | ((uint)bytes[2] << 8)
                 | bytes[3];
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteLength(writer, (ulong)bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteLength(BinaryWriter writer, ulong value)
        {
            if (value <= 63)
            {
                writer.Write((byte)value);
                return;
            }

            if (value <= 16383)
            {
                byte first = (byte)(0b0100_0000 | ((value >> 8) & 0b0011_1111));
                byte second = (byte)(value & 0xFF);
                writer.Write(first);
                writer.Write(second);
                return;
            }

            if (value <= uint.MaxValue)
            {
                writer.Write((byte)0b1000_0000);
                writer.Write((byte)((value >> 24) & 0xFF));
                writer.Write((byte)((value >> 16) & 0xFF));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)(value & 0xFF));
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(value), "Length too large for RDB size encoding.");
        }

        private readonly record struct EncodedLength(bool IsSpecialEncoding, ulong Value);
        private readonly record struct ParsedEntry(string Key, string Value, DateTimeOffset? ExpiresAtUtc);
    }
}
