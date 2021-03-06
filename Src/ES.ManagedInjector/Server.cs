﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace ES.ManagedInjector
{
    internal class Server
    {
        private readonly NamedPipeServerStream _server = new NamedPipeServerStream(Constants.NamedPipeCode.ToString("X"), PipeDirection.InOut);
        private readonly PipeChanel _pipeChanell = null;
        private readonly Dictionary<String, Assembly> _dependencies = new Dictionary<String, Assembly>();

        private InjectionResult _lastError = InjectionResult.Success;
        private String _lastErrorMessage = String.Empty;
        private Int32 _metadataToken = 0;
        private Byte[] _assemblyBuffer = null;
        private Object _context = null;

        public Server()
        {
            _pipeChanell = new PipeChanel(_server);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        public void ProcessCommands()
        {
            _server.WaitForConnection();
            var completed = false;
            while(!completed)
            {
                var msg = _pipeChanell.GetMessage();
                completed = ProcessCommand(msg);
                _pipeChanell.SendAck(_lastError, _lastErrorMessage);
            }

            _server.Dispose();
        }

        private Assembly ResolveAssembly(Object sender, ResolveEventArgs e)
        {
            _dependencies.TryGetValue(e.Name, out Assembly res);
            return res;
        }

        private Boolean ProcessCommand(PipeMessage msg)
        {
            var exit = false;
            var msgType = msg.GetType();
            if (msgType.Equals(Constants.Token, StringComparison.OrdinalIgnoreCase))
            {
                _metadataToken = Int32.Parse(msg.GetData());
            }
            else if (msgType.Equals(Constants.Assembly, StringComparison.OrdinalIgnoreCase))
            {
                _assemblyBuffer = Convert.FromBase64String(msg.GetData());
            }
            else if (msgType.Equals(Constants.Dependency, StringComparison.OrdinalIgnoreCase))
            {                
                try
                {
                    var assemblyBuffer = Convert.FromBase64String(msg.GetData());
                    var assembly = Assembly.Load(assemblyBuffer);
                    if (!_dependencies.ContainsKey(assembly.FullName))
                    {
                        _dependencies.Add(assembly.FullName, assembly);
                    }                    
                }
                catch (Exception e)
                {
                    _lastError = InjectionResult.InvalidAssemblyDependencyBuffer;
                    _lastErrorMessage = e.ToString();
                }
            }
            else if (msgType.Equals(Constants.File, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var data = msg.GetData();
                    var indexOfPipe = data.IndexOf("|");
                    var filenameLength = Int32.Parse(data.Substring(0, indexOfPipe));
                    var filename = data.Substring(indexOfPipe + 1, filenameLength);
                    var fileContent = Convert.FromBase64String(data.Substring(indexOfPipe + 1 + filenameLength));
                    File.WriteAllBytes(filename, fileContent);
                }
                catch (Exception e)
                {
                    _lastError = InjectionResult.InvalidFileBuffer;
                    _lastErrorMessage = e.ToString();
                }
            }
            else if (msgType.Equals(Constants.Context, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var dataString = msg.GetData();
                    var data = Convert.FromBase64String(dataString);
                    var formatter = new BinaryFormatter();
                    using(var memStream = new MemoryStream(data))
                    {
                        _context = formatter.Deserialize(memStream);
                    }                    
                }
                catch (SerializationException e)
                {
                    _lastError = InjectionResult.ErrorInContextDeSerialization;
                    _lastErrorMessage = e.ToString();
                }
                catch (Exception e)
                {
                    _lastError = InjectionResult.UnknownError;
                    _lastErrorMessage = e.ToString();                    
                }

            }
            else if (msgType.Equals(Constants.Run, StringComparison.OrdinalIgnoreCase))
            {
                if (_assemblyBuffer == null)
                {
                    _lastError = InjectionResult.InvalidAssemblyBuffer;
                    _lastErrorMessage = "Assembly bytes to inject not received";
                }
                else
                {
                    ActivateDll(_context);
                }
                
                exit = true;
            }
            return exit;
        }

        private Object CreateType(ParameterInfo parameterInfo)
        {
            Object obj = null;
            if (parameterInfo.HasDefaultValue)
            {
                // return the default value
                obj = parameterInfo.DefaultValue;
            }
            else
            {
                var type = parameterInfo.ParameterType;
                if (type == typeof(String))
                {
                    obj = String.Empty;
                }
                else if (type.IsArray)
                {
                    obj = Array.CreateInstance(type.GetElementType(), 0);
                }
                else if (!type.IsAbstract)
                {
                    try
                    {
                        obj = Activator.CreateInstance(type);
                    }
                    catch
                    {
                        // unable to create the given object add an uninitialized object
                        obj = FormatterServices.GetUninitializedObject(type);
                    }
                }
            }
            
            return obj;
        }

        private Object[] CreateArgumentArray(ParameterInfo[] parameters)
        {
           return parameters.Select(CreateType).ToArray();
        }

        private Object[] CreateInjectMethodArguments(ParameterInfo[] parameters, Object context)
        {
            Object[] arguments = null;
            if (context == null)
            {
                arguments = CreateArgumentArray(parameters);
            }
            else if (parameters.Length == 1)
            {
                arguments = new[] { context };
            }

            return arguments;
        }
        
        private void InvokeMethod(MethodBase method, Object context)
        {
            try
            {
                var arguments = CreateInjectMethodArguments(method.GetParameters(), context);
                Object thisObj = null;

                // check if I have to create an instance to invoke the method
                if (!method.IsStatic)
                {
                    var constructor = method.DeclaringType.GetConstructors().FirstOrDefault();
                    if (constructor != null)
                    {
                        var constructorArguments = CreateArgumentArray(constructor.GetParameters());
                        thisObj = Activator.CreateInstance(method.DeclaringType, constructorArguments);
                    }
                }
                // invoke the method                           
                var task = Task.Factory.StartNew(() => method.Invoke(thisObj, arguments), TaskCreationOptions.LongRunning);

                // wait one second to grab early execution exceptions
                Thread.Sleep(1000);

                if (task.Exception != null)
                {
                    _lastError = InjectionResult.ErrorDuringInvocation;
                    _lastErrorMessage = task.Exception.ToString();
                }
            }
            catch
            {
                _lastError = InjectionResult.ErrorDuringInvocation;
            }
        }

        private MethodBase ResolveMethod(Assembly assembly)
        {
            MethodBase methodToInvoke = null;
            foreach (var module in assembly.Modules)
            {
                try
                {
                    methodToInvoke = module.ResolveMethod(_metadataToken);
                    break;
                }
                catch { }
            }
            return methodToInvoke;
        }

        private void ActivateDll(Object context)
        {
            try
            {
                var assembly = Assembly.Load(_assemblyBuffer.ToArray());
                var methodToInvoke = ResolveMethod(assembly);

                if (methodToInvoke != null)
                {
                    InvokeMethod(methodToInvoke, context);
                }
                else
                {
                    _lastError = InjectionResult.MethodNotFound;
                }
            }
            catch (Exception e) {
                _lastError = InjectionResult.InvalidAssemblyBuffer;
                _lastErrorMessage = e.ToString();
            }
        }
    }
}
