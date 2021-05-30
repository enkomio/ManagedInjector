# Managed Injector
This project implements a .NET Assembly injection library (it is inspired by the <a href="https://github.com/cplotts/snoopwpf">snoopwpf</a> project). The remote process can be a managed or unmanaged one.

## Download
 - [Source code][1]
 - [Download binary][2]

## Usage
When you want to inject an assembly in a remote process you have to consider the following aspects:

* The ManagedInjector project currently supports only 32 bit process
* The remote process must be a windows application (it must process messages in a message loop)

If the above pre-conditions are satisfied you can inject an assembly and invoke an activation method. There are three possibilities to invoke the activation method:

* You must specify the full method name to invoke (eg. _this.is.my.namespace.class.method_)
* You can inject an executable that defines an _EntryPoint_ method to execute (like a _Console_ project)
* You can define a method with the following signatue: _<public|private> static void Inject()_

This library is also used by <a href="https://github.com/enkomio/shed">Shed</a> to inject a DLL in a remote process. You can see a video <a href="https://raw.githubusercontent.com/enkomio/media/master/Shed/Injection.gif">here</a>.

For practical examples see the <a href="https://github.com/enkomio/ManagedInjector/blob/master/Src/Examples/TestRunner/Program.cs">TestRunner project</a>.

### Adding dependencies
If the injected assembly has any dependencies on not standard .NET assemblies, you can add those dependencies with the ``AddDependency`` method.

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

    var pid = 1234;
    var injector = new Injector(pid, Assembly.LoadFile("AssemblyToInject.dll"));
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
