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

## Build
TBD

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/enkomio/ManagedInjector/tags). 

## Authors

* **Antonio Parata** - *Core Developer* - [s4tan](https://twitter.com/s4tan)

## License

Managed Injector is licensed under the [Creative Commons](LICENSE.md).

  [1]: https://github.com/enkomio/ManagedInjector/tree/master/Src
  [2]: https://github.com/enkomio/ManagedInjector/releases/latest
