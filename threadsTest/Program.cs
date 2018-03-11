using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadsTest
{
    
    class Program
    {
        public static class Sorting
        {
            public enum SortOrder { Ascending = 1, Descending = -1 };
            public static IList<T> Sort<T>(IList<T> list, SortOrder order = SortOrder.Ascending) where T : IComparable
            {
                if (list.Count < 16)
                {
                    //InsertionSort(list, order);
                }
                else
                {
                    //QuickSort(list, order);
                    Quicksort(list, 0, list.Count - 1);
                }
                return list;
            }
            public static void Quicksort<T>(IList<T> elements, int left, int right) where T : IComparable
            {
                int i = left, j = right;
                T pivot = elements[(left + right) / 2];

                while (i <= j)
                {
                    while (elements[i].CompareTo(pivot) > 0)
                    {
                        i++;
                    }

                    while (elements[j].CompareTo(pivot) < 0)
                    {
                        j--;
                    }

                    if (i <= j)
                    {
                        // Swap
                        T tmp = elements[i];
                        elements[i] = elements[j];
                        elements[j] = tmp;

                        i++;
                        j--;
                    }
                }

                // Recursive calls
                if (left < j)
                {
                    Quicksort(elements, left, j);
                }

                if (i < right)
                {
                    Quicksort(elements, i, right);
                }
            }


            public static void QuickSort<T>(IList<T> elements, SortOrder order) where T : IComparable
            {
                var SortIndexes = new Stack<Tuple<int, int>>();
                SortIndexes.Push(new Tuple<int, int>(0, elements.Count - 1));
                object _lock = new object();

                MyThreadPool dispatcher = new MyThreadPool(10);
                while (SortIndexes.Count > 0)
                {
                    dispatcher.Add(() =>
                    {
                        int start, end, left, right, comp;
                        Tuple<int, int> indexes = SortIndexes.Pop();
                        left = start = indexes.Item1;
                        right = end = indexes.Item2;
                        comp = (int)order;
                        T pivot = elements[start / 2 + end / 2];
                        while (left < right)
                        {
                            while (elements[left].CompareTo(pivot) == comp)
                            {
                                left++;
                            }
                            while (pivot.CompareTo(elements[right]) == comp)
                            {
                                right--;
                            }
                            if (left <= right)  // '=' needed to push the left and right away
                            {
                                T tmp = elements[left];
                                elements[left] = elements[right];
                                elements[right] = tmp;
                                left++;
                                right--;
                            }
                        }
                        if (start < right)                                            //the sequence is now:  start...right,left...end  
                        {
                            SortIndexes.Push(Tuple.Create(start, right));
                        }
                        if (left < end)
                        {
                            SortIndexes.Push(Tuple.Create(left, end));
                        }
                        lock (_lock)
                        {
                            Monitor
                        }
                    }
                    );
                }
            }

            public static void InsertionSort<T>(IList<T> elements, SortOrder order) where T : IComparable
            {
                int comp = (int)order;
                for (int i = 0; i < elements.Count - 1; i++)
                {
                    int j = i + 1;
                    T current = elements[j];
                    while (j > 0 && current.CompareTo(elements[j - 1]) == comp)
                    {
                        elements[j] = elements[j - 1];
                        j--;
                    }
                    elements[j] = current;
                }
            }
            public static void IsSorted<T>(IList<T> arr, SortOrder order = SortOrder.Ascending) where T : IComparable
            {
                int comp = (int)order;
                for (int i = 0; i < arr.Count - 1; i++)
                {
                    if (arr[i + 1].CompareTo(arr[i]) == comp)
                    {
                        Console.WriteLine($"Unsorted {arr[i + 1]}, {arr[i]}");
                        //return;
                    }
                }
                Console.WriteLine("Sorted");
            }
        }
        class MyManualResetEvent
        {
            readonly object _lock = new object();
            bool signaled;

            public MyManualResetEvent(bool signaled)
            {
                this.signaled = signaled;
            }
            public MyManualResetEvent()
            {
                this.signaled = false;
            }

            void WaitOne()
            {
                lock (_lock)
                {
                    if (!signaled)
                        Monitor.Wait(_lock);
                }
            }

            void Set()
            {
                lock (_lock)
                {
                    signaled = true;
                    Monitor.PulseAll(_lock);
                }
            }
            void Reset()
            {
                lock (_lock)
                    signaled = false;
            }
            
        }
        class MyAutoResetEvent
        {
            readonly object _lock = new object();
            bool signaled;

            public MyAutoResetEvent(bool signaled)
            {
                this.signaled = signaled;
            }
            public MyAutoResetEvent()
            {
                this.signaled = false;
            }

            void WaitOne()
            {
                lock (_lock)
                {
                    if (!signaled)
                        Monitor.Wait(_lock);
                    signaled = false;
                }
            }

            void Set()
            {
                lock (_lock)
                {
                    signaled = true;
                    Monitor.Pulse(_lock);
                }
            }
            void Reset()
            {
                lock (_lock)
                    signaled = false;
            }
        
        }
        class MyBarrier
        {
            readonly object _lock = new object();
            bool signaled;
            int requirement;

            int waiting = 0;
            int Waiting
            {
                get => waiting;
                set
                {
                    waiting = value;
                    if (waiting > requirement)
                        Set();
                }
            }

            public MyBarrier(int count, bool signaled = false)
            {
                this.requirement = count;
                this.signaled = signaled;
            }

            public void Wait()
            {
                lock (_lock)
                {
                    if (!signaled)
                    {
                        Waiting++;
                        Monitor.Wait(_lock);
                    }
                }
            }

            void Set()
            {
                lock (_lock)
                {
                    signaled = true;
                    Monitor.PulseAll(_lock);
                }
            }
            void Reset()
            {
                lock (_lock)
                {
                    waiting = 0;
                    signaled = false;
                }
                    
            }

        }
        class MyTask<TResult>
        {
            public enum TaskState {running, finished};
            readonly object _lock = new object();
            object _notifier = null;
            bool isSet = false;


            TaskState state = TaskState.running;
            public TaskState State => state;

            TResult result;
            public TResult Result
            {
                get
                {
                    lock (_lock)
                    {
                        while(!isSet)
                            Monitor.Wait(_lock);
                        return result;
                    }
                }
                set
                {
                    lock (_lock)
                    {
                        result = value;
                        isSet = true;
                        state = TaskState.finished;
                        Monitor.Pulse(_lock);   
                    }
                }
            }
            public void PulseWhenReady(object _notifier)
            {
                throw new NotImplementedException();
            }
            public static void WaitForAll<T>(IList<MyTask<T>> tasks)
            {
                throw new NotImplementedException();
            }
        }
        class MyThreadPool 
        {
            private Queue<Action> workQueue = new Queue<Action>();
            List<Thread> workers = new List<Thread>();
            readonly Object _lock = new Object();

            public MyThreadPool(int workerCount)
            {
                for (int i = 0; i < workerCount; i++)
                {
                    workers.Add(new Thread(DoWork));
                }
                workers.ForEach(w => w.Start());
            }
            public MyTask<T> Add<T>(Func<T> element)
            {
                MyTask<T> task = new MyTask<T>();
                lock (_lock)
                {
                    workQueue.Enqueue(()=>task.Result = element());
                    Monitor.Pulse(_lock);
                }
                return task;
            }
            public IEnumerable<MyTask<T>> AddMultiple<T>(params Func<T>[] items)
            {
                var tasks = new List<MyTask<T>>();
                foreach (var item in items)
                {
                    var task = new MyTask<T>();
                    tasks.Add(task);
                    lock (_lock)
                    {
                        workQueue.Enqueue(()=> 
                        {
                            task.Result = item();
                        });
                        Monitor.Pulse(_lock);
                    }
                }
                    
                return tasks; 
            }
            private void DoWork()
            {
                Action work;
                while (true)
                {
                    lock (_lock)
                    {
                        while (!workQueue.Any())
                            Monitor.Wait(_lock);
                        work = workQueue.Dequeue();
                    }
                    work?.Invoke();
                }
            }
        }

        public static void ExecuteInPararell(params Action[] items)
        {
            var threads = new List<Thread>(items.Length);
            foreach (var item in items)
            {
                Thread thread = new Thread(new ThreadStart(item));
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(t => t.Join());
        }
        public static List<T> Execute_SingleThreads<T>(params Func<T>[] items)
        {
            var threads = new List<Thread>(items.Length);
            var results = new List<T>();
            Object _lock = new Object();

            foreach (var item in items)
            {
                Thread thread = new Thread(() =>
                {
                    T result = item();
                    lock (_lock)
                        results.Add(result);
                });
                threads.Add(thread);
                thread.Start();
            }
            threads.ForEach(t => t.Join());
            return results.ToList();
        }
        public static List<T> Execute_ThreadsSetAmount<T>(params Func<T>[] items)
        {
            var funcs = new Queue<Func<T>>(items);
            var threads = new List<Thread>(items.Length);
            var results = new List<T>();

            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(() =>
                {
                    Func<T> item;
                    while (true)
                    {
                        lock (funcs)
                        {
                            if (funcs.Any())
                                item = funcs.Dequeue();
                            else
                                break;
                        }
                        T result = item.Invoke();
                        lock (results)
                        {
                            results.Add(result);
                        }
                    }
                });
                /*t.IsBackground = true;
                t.Start();*/
                threads.Add(t);
            }
            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());
            return results;
        }
        public static List<T> Execute_Tasks<T>(params Func<T>[] items)
        {
            var results = new List<T>();
            var tasks = new List<Task<T>>();
            Object _lock = new Object();

            foreach (var item in items)
            {
                Func<T> myFunc = item;
                tasks.Add(Task.Run(myFunc));
            }
            Task.WaitAll(tasks.ToArray());
            tasks.ForEach(t => results.Add(t.Result));
            return results.ToList();
        }

        private static void Benchmark(Action act, int iterations)
        {
            GC.Collect();
            act.Invoke(); // run once outside of loop to avoid initialization costs
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                act();
            }
            sw.Stop();
            Console.WriteLine("Time: " + (sw.ElapsedMilliseconds / iterations).ToString());
        }
        public static int FindNthPrimeNumber(int n)
        {
            int count = 0;
            int num = 2;
            while (count < n)
            {
                int divisor = 2;
                double divisorCap = Math.Sqrt(num);
                bool found = true;
                while (divisor <= divisorCap)
                {
                    if (num % divisor == 0)
                    {
                        found = false;
                        break;
                    }
                    divisor++;
                }
                if (found)
                    count++;
                num++;
            }
            return --num;
        }
        
        static void Main(string[] args)
        {
            Random random = new Random();
            int count = 2000000;
            /*Func<int>[] funcs = new Func<int>[count];
            for (int i = 0; i < count; i++)
            {
                int j = i;
                funcs[i] = (() =>
                {
                    return FindNthPrimeNumber(1000) ;
                });
            }
            
            CustomDispatcher dispatcher = new CustomDispatcher(10);
            int iterationCount = 100;


            
            
            Benchmark(() => Execute_Tasks(funcs), iterationCount);

            Benchmark(() => dispatcher.AddAndWait(funcs), iterationCount);

            Benchmark(() => Execute_ThreadsSetAmount(funcs), iterationCount);

            Benchmark(() => Execute_SingleThreads(funcs), iterationCount);

             */
            var ints = new int[count];
            for (int i = 0; i < count ; i++)
            {
                ints[i] = random.Next(100000);
            }
            //InsertionSort(ints);
            Benchmark(()=>Sorting.Sort(ints, Sorting.SortOrder.Ascending), 1);
            Sorting.IsSorted(ints, Sorting.SortOrder.Ascending);
            Console.ReadKey();
            
        }

    }
}
