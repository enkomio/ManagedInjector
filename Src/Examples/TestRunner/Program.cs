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
        private static void InjectDllWithDependency()
        {
            // delete the dependency DLL
            if (File.Exists("Newtonsoft.Json.dll"))
            {
                File.Delete("Newtonsoft.Json.dll");
            }

            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);

            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var injectedAssemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "AssemblyWithDependency", "bin", "Debug", "AssemblyWithDependency.dll"));
            var injectedAssembly = Assembly.LoadFile(injectedAssemblyFile);

            var injector = new Injector(proc.Id, injectedAssembly, "AssemblyWithDependency.Main.Run");
            var injectionResult = injector.Inject();
            proc.Kill();
            Contract.Assert(injectionResult == InjectionResult.Success);
            Console.WriteLine("Injection successful");
        }

        private static void InjectConsoleWithEntryPoint()
        {
            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);
            
            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var injector = new Injector(proc.Id, new ConsoleProgram.Program().GetType().Assembly);
            var injectionResult = injector.Inject();
            proc.Kill();

            Contract.Assert(injectionResult == InjectionResult.Success);
            Console.WriteLine("Injection successful");
        }

        private static void InjectAssemblyWithDefaultInjectMethodSignature()
        {
            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);

            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var injector = new Injector(proc.Id, typeof(AssemblyWithDefaultMethodName.Main).Assembly);
            var injectionResult = injector.Inject();
            proc.Kill();

            Contract.Assert(injectionResult == InjectionResult.Success);
            Console.WriteLine("Injection successful");
        }

        private static void InjectAssemblyWithExternalFileNeeded()
        {
            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);

            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var injector = new Injector(proc.Id, typeof(AssemblyWithFileResourceNeeded.Main).Assembly);
            injector.AddFile("my_file.txt", Encoding.ASCII.GetBytes("External file content"));
            var injectionResult = injector.Inject();
            proc.Kill();

            Contract.Assert(injectionResult == InjectionResult.Success);
            Console.WriteLine("Injection successful");
        }

        private static void InjectanAssemblyRaisingAnException()
        {
            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);

            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var injector = new Injector(proc.Id, typeof(AssemblyRaisingAnException.Main).Assembly);
            var injectionResult = injector.Inject();
            proc.Kill();

            Contract.Assert(injectionResult != InjectionResult.Success);
            Console.WriteLine("Injection not execute (as expected), error: " + injector.GetLastErrorMessage());
        }

        private static void InjectAssemblyWithMethodAcceptingAContext()
        {
            var assemblyFile = Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug", "WindowsFormHelloWorld.exe"));
            var assemblyExecute = Assembly.LoadFile(assemblyFile);

            var assemblyDir = Path.GetDirectoryName(assemblyExecute.Location);
            Directory.SetCurrentDirectory(assemblyDir);

            var procInfo = new ProcessStartInfo(assemblyExecute.Location) { WorkingDirectory = assemblyDir };
            var proc = Process.Start(procInfo);
            Thread.Sleep(1000);

            var context = new Dictionary<String, String>();
            context.Add("File", "ResultFile.txt");
            context.Add("Content", "This content must be written to the file");

            var injector = new Injector(proc.Id, typeof(AssemblyWithMethodAcceptingAnArgument.Program).Assembly);
            var injectionResult = injector.Inject(context);
            proc.Kill();

            Contract.Assert(injectionResult == InjectionResult.Success);
            Console.WriteLine("Injection successful");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Log files will be stored in: {0}", Path.GetFullPath(Path.Combine("..", "..", "..", "WindowsFormHelloWorld", "bin", "Debug")));
            InjectAssemblyWithMethodAcceptingAContext();
            InjectDllWithDependency();
            InjectanAssemblyRaisingAnException();
            InjectAssemblyWithExternalFileNeeded();            
            InjectConsoleWithEntryPoint();
            InjectAssemblyWithDefaultInjectMethodSignature();            
        }
    }
}
