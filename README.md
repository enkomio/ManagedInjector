# Managed Injector
This project implements a .NET Assembly injection library. The method used to inject the Assembly is by using the _SetWindowsHookEx_ method.

## Download
 - [Source code][1]
 - [Download binary][2]

## Usage
When you want to inject an assembly in a remote project you have to consider the following aspects:

* The project currently supports only 32 bit process
* The remote process must be a windows application (it must process messages in a message loop)
* If the CLR version is different, you will not be able to use reflection to inspect the loaded assemblies

If the above pre-conditions are satisfiedm you can inject an assembly and invoke an activation method. There are three possibilities to invoke the activation method:

* You have to specify the full method name to invoke (eg. _this.is.my.namespace.class.method_)
* You can inject an executable that defines an _EntryPoint_ method to execute
* You can define a method with the following signatue: _<public|private> static void Inject()_

For practical examples see the <a href="https://github.com/enkomio/ManagedInjector/blob/master/Src/Examples/TestRunner/Program.cs">TestRunner project</a>.

### Adding dependencies
If the injected assembly has any dependencies on not standard .NET Assembly, you can add those dependencies with the ``AddDependency`` method.

### Adding external files
If the injected assembly needs to load some external file in order to work correctly (like a configuration file) you can specify them with the ``AddFile`` method. This method will copy the specified file in the working directory of the injected process.

### Example

Let's consider the following code:
    
    using System;
    
    namespace InjectedAssembly
    {
        public class Main
        {
            // we use a default injection method name in order to execute our code in the remote process
            private static void Inject()
            {
                Console.WriteLine("Hello world from the injected process!");
            }
        }
    }
    
in order to inject the Assembly generated from the above code it is enough to use the following code:

    var process = Process.GetProcessById(1234);
    var injector = new Injector(process.Id, Assembly.LoadFile("AssemblyToInject.dll"));
    var injectionResult = injector.Inject();


## Build
_ManagedInjector_ is currently developed by using VisualStudio 2017 Community Edition (be sure to have the latest version installed). To build the source code be sure you have to:
* install <a href="https://www.microsoft.com/net/download">.NET Core SDK</a>
* clone the repository
* run ``build.bat``

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/enkomio/ManagedInjector/tags). 

## Authors

* **Antonio Parata** - *Core Developer* - [s4tan](https://twitter.com/s4tan)

## License

Managed Injector is licensed under the [Creative Commons](LICENSE.md).

  [1]: https://github.com/enkomio/ManagedInjector/tree/master/Src
  [2]: https://github.com/enkomio/ManagedInjector/releases/latest
