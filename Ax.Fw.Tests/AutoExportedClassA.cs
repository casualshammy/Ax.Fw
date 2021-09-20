using Ax.Fw.SingletoneExport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw.Tests
{
    [SingletoneActivatorAutoExport(typeof(IAutoExportedClassA), true, true)]
    public class AutoExportedClassA : IAutoExportedClassA
    {
        public AutoExportedClassA(IAutoExportedClassB autoExportedClassB)
        {
            TestString = "Hi!";
            Console.WriteLine("AutoExportedClassA..ctor");
        }

        public string TestString { get; }
    }

    public interface IAutoExportedClassA
    {
        string TestString { get; }
    }

    [SingletoneActivatorAutoExport(typeof(IAutoExportedClassB), true, false)]
    public class AutoExportedClassB : IAutoExportedClassB
    {
        public AutoExportedClassB()
        {
            TestString = "Hi!";
            Console.WriteLine("AutoExportedClassB..ctor");
        }

        public string TestString { get; }
    }

    public interface IAutoExportedClassB
    {
        string TestString { get; }
    }

}
