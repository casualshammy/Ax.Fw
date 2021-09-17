using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Tests
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        private static void ConsoleWriteLineThreadId(string _msg)
        {
            Console.WriteLine($" | {Thread.CurrentThread.ManagedThreadId:00} | {_msg}");
        }

        public async Task MainAsync(string[] args)
        {
            ConsoleWriteLineThreadId($"{nameof(MainAsync)}: Entering method...");

            Task p = null;
            ConsoleWriteLineThreadId(p.GetType().ToString());

            var subject = new Subject<int>();
            subject.ObserveOn(new EventLoopScheduler()).Subscribe(x =>
            {
                //Thread.Sleep(1000);
                ConsoleWriteLineThreadId($"from subscribe: {x}");
            });
            subject.OnNext(5);

            ConsoleWriteLineThreadId($"{nameof(MainAsync)}: Exiting method...");
            Console.ReadLine();
        }

        private void Method_01()
        {
            for (int i = 0; i < 10; i++)
            {
                Task.Run(() =>
                {
                    ConsoleWriteLineThreadId($"{nameof(Method_01)}/{i}: Executing task...");
                    Thread.Sleep(100);
                });
            }
        }

        private void Method_02()
        {
            var task =
                new Task(() => {
                    ConsoleWriteLineThreadId($"{nameof(Method_01)}: Main task is executing...");
                    Thread.Sleep(100);
                });

            var continuations = task
                .ContinueWith(x => 
                {
                    ConsoleWriteLineThreadId($"{nameof(Method_01)}: Continuation 0 is executing...");
                    Thread.Sleep(100);
                })
                .ContinueWith(x =>
                {
                    ConsoleWriteLineThreadId($"{nameof(Method_01)}: Continuation 1 is executing...");
                    Thread.Sleep(100);
                })
                .ContinueWith(x =>
                {
                    ConsoleWriteLineThreadId($"{nameof(Method_01)}: Continuation 2 is executing...");
                    Thread.Sleep(100);
                });

            task.Start();
        }

        private void Method_03()
        {
            static void action()
            {
                ConsoleWriteLineThreadId("Parallel.Invoke");
                Thread.Sleep(100);
            }

            var actions = Enumerable.Repeat<Action>(action, 4).ToArray();
            Parallel.Invoke(actions);
        }

        private async Task Method_04()
        {
            ConsoleWriteLineThreadId($"{nameof(Method_04)}: Entering method...");
            await Task.Delay(TimeSpan.FromSeconds(1));
            ConsoleWriteLineThreadId($"{nameof(Method_04)}: After first Task.Delay...");
            await Task.Delay(TimeSpan.FromSeconds(1));
            ConsoleWriteLineThreadId($"{nameof(Method_04)}: After second Task.Delay...");
        }

        private async Task<IEnumerable<int>> GetValuesFromRemoteSlowStorageAsync_0(CancellationToken _token)
        {
            var list = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                ConsoleWriteLineThreadId($"{nameof(GetValuesFromRemoteSlowStorageAsync_0)}: Getting element {i}...");
                await Task.Delay(250, _token);
                list.Add(i);
            }
            return list;
        }

        private async IAsyncEnumerable<int> GetValuesFromRemoteSlowStorageAsync_1([EnumeratorCancellation] CancellationToken _token)
        {
            for (int i = 0; i < 10; i++)
            {
                ConsoleWriteLineThreadId($"{nameof(GetValuesFromRemoteSlowStorageAsync_1)}: Getting element {i}...");
                await Task.Delay(250, _token);
                yield return i;
            }
        }

    }
}
