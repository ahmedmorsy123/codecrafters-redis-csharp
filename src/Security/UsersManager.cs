using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace codecrafters_redis.src.Security
{
    public static class UsersManager
    {
        private static readonly List<User> users = new()
        {
            new User(
                "default",
                new List<UserProperties>
                {
                    new UserProperties("flags", new[] { "nopass" }),
                    new UserProperties("passwords", Array.Empty<string>())
                })
        };

        public static void AddUser(string username, string plainPassword)
        {
            lock (users)
            {
                if (users.Exists(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"User '{username}' already exists.");
                }
                string passwordHash = HashPassword(plainPassword);
                users.Add(new User(username, new List<UserProperties>()));
            }
        }

        public static void AddPassword(string username, string plainPassword)
        {
            lock (users)
            {
                var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    throw new InvalidOperationException($"User '{username}' not found.");
                }
                string passwordHash = HashPassword(plainPassword);
                var passwordsProp = user.Properties.Find(p => p.property.Equals("passwords", StringComparison.OrdinalIgnoreCase));
                if (passwordsProp == null)
                {
                    user.Properties.Add(new UserProperties("passwords", new[] { passwordHash }));
                }
                else
                {
                    var newPasswords = passwordsProp.values.Append(passwordHash).ToArray();
                    int passwordsIndex = user.Properties.FindIndex(p => p.property.Equals("passwords", StringComparison.OrdinalIgnoreCase));
                    user.Properties[passwordsIndex] = new UserProperties("passwords", newPasswords);
                }
                NormalizeNoPassFlag(user);
            }
        }


        public static bool Authenticate(string username, string password)
        {
            lock (users)
            {
                var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    return false;
                }
                
                string[] passwordHashes = GetPasswordsHash(username);
                return passwordHashes.Any(hash => VerifyPassword(password, hash)) || passwordHashes.Length == 0;
            }
        }

        public static List<object> GetUserProperties(string username)
        {
            lock (users)
            {
                var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    throw new InvalidOperationException($"User '{username}' not found.");
                }
                List<object> properties = new();
                foreach (var prop in user.Properties)
                {
                    properties.Add(prop.property);
                    properties.Add(prop.values);
                }
                return properties;
            }
        }

        public static void SetUserProperties(string username, List<UserProperties> properties)
        {
            lock (users)
            {
                var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    throw new InvalidOperationException($"User '{username}' not found.");
                }
                user.Properties.Clear();
                user.Properties.AddRange(properties);
            }
        }

        public static void UpdateUserProperties(string username, List<UserProperties> properties)
        {
            lock (users)
            {
                var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    throw new InvalidOperationException($"User '{username}' not found.");
                }
                foreach (var prop in properties)
                {
                    var existingProp = user.Properties.Find(p => p.property.Equals(prop.property, StringComparison.OrdinalIgnoreCase));
                    if (existingProp != null)
                    {
                        user.Properties.Remove(existingProp);
                    }

                    user.Properties.Add(prop);
                }

                NormalizeNoPassFlag(user);
            }
        }

        private static string[] GetPasswordsHash(string username)
        {
            var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                throw new InvalidOperationException($"User '{username}' not found.");
            }
            var passwordsProp = user.Properties.Find(p => p.property.Equals("passwords", StringComparison.OrdinalIgnoreCase));
            if (passwordsProp == null || passwordsProp.values.Length == 0)
            {
                return Array.Empty<string>();
            }
            return passwordsProp.values;
        }

        private static void NormalizeNoPassFlag(User user)
        {
            var passwords = user.Properties.Find(p => p.property.Equals("passwords", StringComparison.OrdinalIgnoreCase));
            if (passwords == null || passwords.values.Length == 0)
            {
                return;
            }

            var flags = user.Properties.Find(p => p.property.Equals("flags", StringComparison.OrdinalIgnoreCase));
            if (flags == null)
            {
                return;
            }

            string[] newFlags = flags.values
                .Where(v => !v.Equals("nopass", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            int flagsIndex = user.Properties.FindIndex(p => p.property.Equals("flags", StringComparison.OrdinalIgnoreCase));
            user.Properties[flagsIndex] = new UserProperties(flags.property, newFlags);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            string hashOfInput = HashPassword(password);
            return hashOfInput.Equals(passwordHash);
        }
    }

    public class User
    {
        public string Username { get; }
        public List<UserProperties> Properties { get; } = new List<UserProperties>();
        public User(string username, List<UserProperties> properties)
        {
            Username = username;
            Properties = properties;
        }

        
    }

    public sealed record UserProperties(string property, string[] values);
}
