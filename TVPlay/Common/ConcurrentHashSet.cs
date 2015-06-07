using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Serialization;
using System.Collections;

namespace TAS.Server
{
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class ConcurrentHashSet<T> : ICollection<T>, ISet<T>, ISerializable, IDeserializationCallback
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly HashSet<T> _hashSet = new HashSet<T>();

        public ConcurrentHashSet()
        {
        }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _hashSet = new HashSet<T>(comparer);
        }

        public ConcurrentHashSet(IEnumerable<T> collection)
        {
            _hashSet = new HashSet<T>(collection);
        }

        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            _hashSet = new HashSet<T>(collection, comparer);
        }

        protected ConcurrentHashSet(SerializationInfo info, StreamingContext context)
        {
            _hashSet = new HashSet<T>();

            // not sure about this one really...
            var iSerializable = _hashSet as ISerializable;
            iSerializable.GetObjectData(info, context);
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ReaderWriterLockSlim Lock { get { return _lock; } }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (_lock != null)
                    _lock.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _hashSet.GetEnumerator();
        }

        ~ConcurrentHashSet()
        {
            Dispose(false);
        }

        public void OnDeserialization(object sender)
        {
            _hashSet.OnDeserialization(sender);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            _hashSet.GetObjectData(info, context);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _hashSet.Add(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            _lock.EnterReadLock();
            try
            {
                _hashSet.UnionWith(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            _lock.EnterReadLock();
            try
            {
                _hashSet.IntersectWith(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            _lock.EnterReadLock();
            try
            {
                _hashSet.ExceptWith(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _hashSet.SymmetricExceptWith(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.IsSubsetOf(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.IsSupersetOf(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.IsProperSupersetOf(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.IsProperSubsetOf(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.Overlaps(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.SetEquals(other);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        bool ISet<T>.Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.Add(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _hashSet.Clear();
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.Contains(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _lock.EnterWriteLock();
            try
            {
                _hashSet.CopyTo(array, arrayIndex);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.Remove(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterWriteLock();
                try
                {
                    return _hashSet.Count;
                }
                finally
                {
                    if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                }

            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
    }
}
