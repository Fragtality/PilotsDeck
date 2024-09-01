using System.Collections.Concurrent;
using System.Threading;

namespace PilotsDeck.Tools
{
    public static class Extensions
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static void TryReleaseMutex(this Mutex mutex)
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch { }
        }

        public static void TryWaitOne(this Mutex mutex)
        {
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException) { }
        }

        public static T Dequeue<T>(this ConcurrentQueue<T> queue)
        {
            if (queue.TryDequeue(out T result))
                return result;
            else
                return default;
        }

        public static void Add<K, V>(this ConcurrentDictionary<K, V> dictionary, K key, V value)
        {
            dictionary.TryAdd(key, value);
        }

        public static void Remove<K, V>(this ConcurrentDictionary<K, V> dictionary, K key)
        {
            dictionary.TryRemove(key, out _);
        }

        public static void Add<T>(this ConcurrentDictionary<T, bool> dictionary, T key)
        {
            dictionary.TryAdd(key, true);
        }

        public static void Remove<T>(this ConcurrentDictionary<T, bool> dictionary, T value)
        {
            dictionary.TryRemove(value, out _);
        }
    }
}
