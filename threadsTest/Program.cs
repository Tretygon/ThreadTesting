using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace threadsTest
{
    class Program
    {
        class CustomDispatcher 
        {
            private Queue<Action> workQueue = new Queue<Action>();
            List<Thread> workers = new List<Thread>();
            Object _lock = new Object();

            public CustomDispatcher(int workerCount)
            {
                for (int i = 0; i < workerCount; i++)
                {
                    workers.Add(new Thread(DoWork));
                }
                workers.ForEach(w => w.Start());
            }
            public void Add(Action item)
            {
                lock (_lock)
                {
                    workQueue.Enqueue(item);
                    Monitor.Pulse(_lock);
                }
            }
            public List<int> AddAndWait(params Func<int>[] items)
            {
                var results = new List<int>();
                foreach (var item in items)
                {
                    lock (_lock)
                    {
                        workQueue.Enqueue(()=> 
                        {
                            int result = item();
                            lock (results)
                                results.Add(result);
                        });
                        Monitor.Pulse(_lock);
                    }
                }
                while (!workers.TrueForAll(w => w.ThreadState == System.Threading.ThreadState.WaitSleepJoin));
                    
                return results; 
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
            var threads = new List<Thread>(items.Count());
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
            var threads = new List<Thread>(items.Count());
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
            var threads = new List<Thread>(items.Count());
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

        private static void Benchmark(Func<List<int>> act, int iterations)
        {
            GC.Collect();
            act.Invoke(); // run once outside of loop to avoid initialization costs
            var results = new List<List<int>>(iterations);
            Stopwatch sw = Stopwatch.StartNew();int a = 0;
            for (int i = 0; i < iterations; i++)
            {
                results.Add(act());
            }
            sw.Stop();
            int average = (int)results.Average(r => ((int)r.Average()));
            Console.WriteLine("Time: "+(sw.ElapsedMilliseconds / iterations).ToString()+" Results: "+ average.ToString());
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
            int count = 100;
            Func<int>[] funcs = new Func<int>[count];
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





            Console.ReadKey();
            
        }


    }
}
