using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SpinLockSynchronization
{
    class Program
    {
        internal class Data
        {
            public string Name { get; set; }
            public double Number { get; set; }
        }

        const int N = 100000;
        static Queue<Data> _queue = new Queue<Data>();

        public delegate void lockProfiling(Data dataUnit, int i);

        //Only used for lock.
        static object _lock = new Object();
        static SpinLock _spinlock = new SpinLock();

        private static Action[] useDiffLockWithNum(lockProfiling lockType,int arrNum)
        {
            List<Action> actionsList = new List<Action>();
            for(int i=0;i<arrNum;i++)
            {
                actionsList.Add(useDiffLock(lockType));
            }
            return actionsList.ToArray();
        }
        private static Action useDiffLock(lockProfiling lockType)
        {
            return
                () =>
                {
                    for (int i = 0; i < N; i++)
                    {
                        lockType(new Data() { Name = i.ToString(), Number = i }, i);
                    }
                };

        }
        static void Main(string[] args)
        {
            //Standard Lock
            UseLock();

            _queue.Clear();
            
            //SpinLock
            UseSpinLock();
        }

        #region standard lock
        private static void UseLock()
        {
            var sw = Stopwatch.StartNew();
            Parallel.Invoke(
                useDiffLockWithNum(UpdateWithLock,2)
                /* Identical
                 * useDiffLock(UpdateWithLock),
                 * useDiffLock(UpdateWithLock)
                 */
                ) ;
            sw.Stop();
            Console.WriteLine($"Standard lock elapsed ms with lock: {sw.ElapsedMilliseconds}");
        }

        private static void UpdateWithLock(Data d,int i)
        {
            //Monitor class implementation
            lock(_lock)
            {
                _queue.Enqueue(d);
            }
        }
        #endregion

        #region spinlock
        private static void UseSpinLock()
        {
            var sw = Stopwatch.StartNew();
            Parallel.Invoke(
                useDiffLockWithNum(UpdateWithSpinLock,2)
                /* Identical
                 * useDiffLock(UpdateWithSpinLock),
                 * useDiffLock(UpdateWithSpinLock)
                 */
                );
            sw.Stop();
            Console.WriteLine($"SpinLock elapsed ms with lock: {sw.ElapsedMilliseconds}");
        }
        private static void UpdateWithSpinLock(Data d,int i)
        {
            bool lockTaken = false;
            try
            {
                _spinlock.Enter(ref lockTaken);
                _queue.Enqueue(d);
            }
            finally
            {
                if(lockTaken)
                {
                    _spinlock.Exit(false);
                }
            }
        }
        #endregion
    }
}
