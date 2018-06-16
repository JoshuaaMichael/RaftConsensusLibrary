using System;
using System.Collections.Generic;
using System.Text;

namespace TeamDecided.RaftConsensus.Common.Multithreading
{
    internal class DeadlockDetectionHelper<T> where T : IComparable, IFormattable, IConvertible
    {
        private object _classLockObject;
        private object[] _lockObjects;
        private HashSet<int> _lockObjectLookup;

        public DeadlockDetectionHelper()
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("Genertic type must be a type of enum");
            }
        }
    }
}
