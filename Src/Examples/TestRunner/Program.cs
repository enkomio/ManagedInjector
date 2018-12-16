using ES.ManagedInjector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestRunner
{
    public class Program
    {
        public static void TestManagedInjection()
        {
            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);

            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var injector = new Injector(proc.Id, typeof(AssemblyWithDependency.Main).Assembly, "AssemblyWithDependency.Main.Run");
            var injectionResult = injector.Inject();
            proc.Kill();
            Contract.Assert(injectionResult == InjectionResult.Success);
            Console.WriteLine("Injection successful");

        }

        static void Main(string[] args)
        {
            TestManagedInjection();
        }
    }
}
