using Ax.Fw.Attributes;
using Ax.Fw.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace Ax.Fw.MetroFramework.Sandbox
{
  internal static class Program
  {
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      using var lifetime = new Lifetime();

      var sw = Stopwatch.StartNew();
      var assembly = Assembly.GetCallingAssembly();
      var containerBuilderFull = DependencyManagerBuilder
        .Create(lifetime, null)
        .Build();

      var elapsed = sw.Elapsed;

      var inst = containerBuilderFull.LocateOrDefault<IDependancyExample>();

      // To customize application configuration such as set high DPI settings or default font,
      // see https://aka.ms/applicationconfiguration.
      ApplicationConfiguration.Initialize();
      StyleManager.Current.SetColors(Color.Red, Color.Green, Color.Linen);
      Application.Run(new Form1());
    }
  }


  [ExportClass(typeof(IDependancyExample))]
  class DependancyExample : IDependancyExample
  {
    public DependancyExample()
    {
      Weight = 10;
    }

    public int Weight { get; }
  }

  interface IDependancyExample
  {
    int Weight { get; }
  }


}