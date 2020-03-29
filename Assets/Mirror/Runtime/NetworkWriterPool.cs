using System;
using UnityEngine;

namespace Mirror
{
    /// <summary>
    /// NetworkWriter to be used with <see cref="NetworkWriterPool">NetworkWriterPool</see>
    /// </summary>
    public class PooledNetworkWriter : NetworkWriter, IDisposable
    {
        public void Dispose()
        {
            NetworkWriterPool.Recycle(this);
        }
    }

    /// <summary>
    /// Pool of NetworkWriters
    /// <para>Use this pool instead of <see cref="NetworkWriter">NetworkWriter</see> to reduce memory allocation</para>
    /// <para>Use <see cref="Capacity">Capacity</see> to change size of pool</para>
    /// </summary>
    public static class NetworkWriterPool
    {
        /// <summary>
        /// Mirror usually only uses up to 4 writes in nested usings,
        /// 100 is a good margin for edge cases when
        /// users need a lot writers at the same time.
        ///
        /// <para>keep in mind, most entries of the pool will be null in most cases</para>
        /// </summary>
        const int PoolStartSize = 100;

        /// <summary>
        /// Size of the pool
        /// <para>If pool is too small getting writers will causes memory allocation</para>
        /// <para>Default value: <see cref="PoolStartSize">PoolStartSize</see> </para>
        /// </summary>
        public static int Capacity
        {
            get => pool.Length;
            set => Array.Resize(ref pool, value);
        }

        /// <summary>
        /// Used to reset pool after running tests
        /// </summary>
        internal static void ResetCapacity()
        {
            Array.Resize(ref pool, PoolStartSize);
        }

        static PooledNetworkWriter[] pool = new PooledNetworkWriter[PoolStartSize];

        static int next = -1;

        /// <summary>
        /// Get the next writer in the pool
        /// <para>If pool is empty, creates a new Writer</para>
        /// </summary>
        public static PooledNetworkWriter GetWriter()
        {
            if (next == -1)
            {
                return new PooledNetworkWriter();
            }

            PooledNetworkWriter writer = pool[next];
            pool[next] = null;
            next--;

            // reset cached writer length and position
            writer.SetLength(0);
            return writer;
        }

        /// <summary>
        /// Puts writer back into pool
        /// <para>When pool is full, the extra writer is left for the GC</para>
        /// </summary>
        public static void Recycle(PooledNetworkWriter writer)
        {
            if ((next + 1) < pool.Length)
            {
                next++;
                pool[next] = writer;
            }
            else
            {
                if (LogFilter.Debug) { Debug.LogWarning("NetworkWriterPool.Recycle, Pool was full leaving extra writer for GC"); }
            }
        }
    }
}
