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
        {   static int pass;
            public enum SortOrder { Ascending = 1, Descending = -1 };
            public static IList<T> Sort<T>(IList<T> list, SortOrder order = SortOrder.Ascending) where T : IComparable
            {
                pass = 0;
                Sort(list, 0, list.Count - 1, order);
                return list;
            }
            private static IList<T> Sort<T>(IList<T> list, int start, int end, SortOrder order) where T : IComparable
            {
                return QuickSort_Recursive(list, start, end, order);
            }
            public static IList<T> QuickSort_Recursive<T>(IList<T> elements, int start, int end, SortOrder order) where T : IComparable
            {
                int comp = (int)order;
                int left = start;
                int right = end;
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
                Thread th = null;
                if (start < right )                                            //the sequence is now:  start...right,left...end  
                {
                    if (pass++ < 0)
                    {
                        th = new Thread(()=>Sort(elements, start, right, order));
                        th.Start();
                    }
                    else
                        Sort(elements, start, right, order);
                }
                if (left < end)
                {
                    Sort(elements, left, end, order);
                }
                th?.Join();
                return elements;
            }
            public static IList<T> QuickSort_Iterative<T>(IList<T> elements, SortOrder order) where T : IComparable
            {
                var borders = new Stack<Tuple<int,int>>();
                borders.Push(Tuple.Create(0, elements.Count - 1));
                var workers = new List<Thread>();
                for (int i = 0; i < 10; i++)
                    workers.Add(new Thread(DoWork));
                workers.ForEach(w => w.Start());
                while (!workers.TrueForAll(w => w.ThreadState == System.Threading.ThreadState.WaitSleepJoin)) continue;

                return elements;
                void DoWork()
                {
                    Tuple<int,int> area;
                    int comp = (int)order;
                    while (true)
                    {
                        var greater = new List<T>();
                        var lesser = new List<T>();
                        var equel = new List<T>();
                        lock (borders)
                        {
                            while (borders.Count == 0)
                                Monitor.Wait(borders);
                            area = borders.Pop();
                        }
                        T pivot = elements[ area.Item1 / 2 + area.Item2 / 2];
                        for (int i = area.Item1 ; i < area.Item2; i++)
                        {
                            T current = elements[i];
                            if (current.CompareTo(pivot) == comp)
                            {
                                lesser.Add(current);
                            }
                            else if (pivot.CompareTo(current) == comp)
                            {
                                greater.Add(current);
                            }
                            else
                            {
                                equel.Add(current);
                            }
                        }
                        int l = area.Item1;
                        foreach (var item in lesser.Concat(equel).Concat(greater))
                        {
                            elements[l++] = item;
                        }
                        lock (borders)
                        {
                            if (lesser.Count>1)
                                borders.Push(Tuple.Create(area.Item1, area.Item1 + lesser.Count));
                            if (greater.Count > 1)
                                borders.Push(Tuple.Create(area.Item2 - greater.Count, area.Item2 ));
                            Monitor.Pulse(borders);
                        }
                    }
                }
            }
            public static void InsertionSort<T>(IList<T> elements,int start, int end, SortOrder order) where T : IComparable
            {
                int comp = (int)order;
                for (int i = start; i < end-1; i++)
                {
                    int j = i+1;
                    T current = elements[j];
                    while (j > start && current.CompareTo(elements[j - 1]) == comp)
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
            //act.Invoke(); // run once outside of loop to avoid initialization costs
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
            int count = 20000000;
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
/*
 
     
     
     
     
     
     
     static List<Task> tasks = new List<Task>();
            public enum SortOrder { Ascending = 1, Descending = -1 };
            public static IList<T> Sort<T>(IList<T> list, SortOrder order = SortOrder.Ascending) where T : IComparable
            {
                Sort(list, 0, list.Count - 1, order);
                return list;
            }
            private static void Sort<T>(IList<T> list, int start, int end, SortOrder order) where T : IComparable
            {
                if (end - start < 10000)
                {
                    
                }
                else
                {
                    
                }
                //QuickSort_Iterative(list, start, end, order);
                QuickSort_Recursive(list, start, end, order);
            }
            public static void QuickSort_Recursive<T>(IList<T> elements, int start, int end, SortOrder order) where T : IComparable
            {
                Thread t = null;
                int comp = (int)order;
                int left = start;
                int right = end;
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
                    t = new Thread(()=>Sort(elements, start, right, order));
                    t.Start();
                }
                if (left < end)
                {
                    Sort(elements, left, end, order);
                }
                t?.Join();
            }
            public static void QuickSort_Iterative<T>(IList<T> elements, int s, int e, SortOrder order) where T : IComparable
            {
                int comp = (int)order;
                var borders = new Stack<Tuple<int, int>>();
                borders.Push(new Tuple<int, int>(s, e));
                var workers = new List<Thread>();
                for (int i = 0; i < 6; i++)
                    workers.Add(new Thread(DoWork));
                workers.ForEach(w => w.Start());
                while (!workers.TrueForAll(w => w.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                    Thread.Sleep(1);
                void DoWork()
                {
                    Tuple<int, int> indexes;
                    int start, end, left, right;
                    while (true)
                    {
                        lock (borders)
                        {
                            while (borders.Count == 0)
                                Monitor.Wait(borders);
                            indexes = borders.Pop();
                        }
                        left = start = indexes.Item1;
                        right = end = indexes.Item2;
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
                            lock (borders)
                            {
                                borders.Push(new Tuple<int, int>(start, right));
                                Monitor.Pulse(borders);
                            }
                        }
                        if (left < end)
                        {
                            lock (borders)
                            {
                                borders.Push(new Tuple<int, int>(left, end));
                                Monitor.Pulse(borders);
                            }
                        }
                    }
                }
            }
     
     
     
     
     
     
     
     
     
     
     
     
     
     */
