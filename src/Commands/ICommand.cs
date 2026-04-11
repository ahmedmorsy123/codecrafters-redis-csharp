using codecrafters_redis.src.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public interface ICommand
    {
        public string Name { get; }
        public Task ExecuteAsync(string[] args, ClientSession session);
    }
}
