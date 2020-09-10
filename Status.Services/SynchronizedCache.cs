using System.Collections.Generic;
using System.Threading;

namespace Handler.Services
{
    /// <summary>
    /// Class to manage access to a resource, allowing multiple threads
    /// for reading or exclusive access for writing
    /// </summary>
    public class SynchronizedCache
    {
        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private readonly Dictionary<int, string> innerCache = new Dictionary<int, string>();

        /// <summary>
        /// SynchronizedCache default constructor
        /// </summary>
        public SynchronizedCache() { }

        /// <summary>
        /// SynchronizeCache destructor
        /// </summary>
        ~SynchronizedCache()
        {
            if (cacheLock != null) cacheLock.Dispose();
        }

        /// <summary>
        /// The cache count
        /// </summary>
        public int Count
        { get { return innerCache.Count; } }

        /// <summary>
        /// Read a key entry
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Read(int key)
        {
            cacheLock.EnterReadLock();
            try
            {
                return innerCache[key];
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Add a key entry
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(int key, string value)
        {
            cacheLock.EnterWriteLock();
            try
            {
                innerCache.Add(key, value);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Add key with timeout
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeout"></param>
        /// <returns>pass/fail</returns>
        public bool AddWithTimeout(int key, string value, int timeout)
        {
            if (cacheLock.TryEnterWriteLock(timeout))
            {
                try
                {
                    innerCache.Add(key, value);
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add or Update the key status
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public AddOrUpdateStatus AddOrUpdate(int key, string value)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (innerCache.TryGetValue(key, out string result))
                {
                    if (result == value)
                    {
                        return AddOrUpdateStatus.Unchanged;
                    }
                    else
                    {
                        cacheLock.EnterWriteLock();
                        try
                        {
                            innerCache[key] = value;
                        }
                        finally
                        {
                            cacheLock.ExitWriteLock();
                        }
                        return AddOrUpdateStatus.Updated;
                    }
                }
                else
                {
                    cacheLock.EnterWriteLock();
                    try
                    {
                        innerCache.Add(key, value);
                    }
                    finally
                    {
                        cacheLock.ExitWriteLock();
                    }
                    return AddOrUpdateStatus.Added;
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Delete key entry
        /// </summary>
        /// <param name="key"></param>
        public void Delete(int key)
        {
            cacheLock.EnterWriteLock();
            try
            {
                innerCache.Remove(key);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Add or Update Status actions
        /// </summary>
        public enum AddOrUpdateStatus
        {
            Added,
            Updated,
            Unchanged
        };
    }
}
