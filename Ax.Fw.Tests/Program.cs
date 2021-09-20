using Ax.Fw.SingletoneExport;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reactive.Linq;
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

            var sc = new ServiceCollection();
            var activator = new SingletoneActivator(new Lifetime(), sc);

            ConsoleWriteLineThreadId($"{nameof(MainAsync)}: Exiting method...");
            Console.ReadLine();
        }

    }
}
