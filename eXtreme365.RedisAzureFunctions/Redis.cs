using System;
using StackExchange.Redis;

namespace eXtreme365.RedisAzureFunctions
{
    public class Redis
    {
        private Lazy<ConnectionMultiplexer> _connection;

        public Redis()
        {
            _connection = new Lazy<ConnectionMultiplexer>(() => {
                string cacheConnection = "extreme365.redis.cache.windows.net:6380,password=llTRA+I6ff7XOLZxiX5SAIcVFaidat9diejqzahuJtc=,ssl=True,abortConnect=False";
                return ConnectionMultiplexer.Connect(cacheConnection);
            });
        }

        public IDatabase GetDatabase()
        {
            return _connection.Value.GetDatabase();
        }
    }
}
