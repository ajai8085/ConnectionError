using System;
using System.Diagnostics;
using StackExchange.Redis;

namespace ThreadTester
{
    public interface ICacheFactory
    {
        IDatabase GetDatabase();
        IDatabaseAsync GetDatabaseAsync();
        TimeSpan? ExpiresAfter { get; }
    }

    
    [DebuggerDisplay("Redis Cache Factory")]
    public class CacheFactory : ICacheFactory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private static readonly object _syncLock = new object();


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Lazy<ConnectionMultiplexer> _lazyConnection =
                new Lazy<ConnectionMultiplexer>(
                    () => ConnectionMultiplexer.Connect("localhost"))
            ;

        [DebuggerHidden]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static ConnectionMultiplexer Connection => _lazyConnection.Value;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public IDatabase GetDatabase()
        {
            return Connection.GetDatabase(2, _syncLock);
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public IDatabaseAsync GetDatabaseAsync()
        {
            return Connection.GetDatabase(2, _syncLock);
        }

        public TimeSpan? ExpiresAfter { get; }

        public CacheFactory(int? expiresAfterSeconds = null)
        {
            if(expiresAfterSeconds.HasValue)
            {
                ExpiresAfter = TimeSpan.FromSeconds(expiresAfterSeconds.Value);
            }

        }
    }
}
