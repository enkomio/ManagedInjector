using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ES.ManagedInjector
{
    internal class Client
    {
        private readonly NamedPipeClientStream _client = new NamedPipeClientStream(".", Constants.NamedPipeCode.ToString("X"), PipeDirection.InOut);
        private readonly PipeChanel _pipeChanel = null;
        private readonly List<Byte[]> _dependencies = null;
        private readonly Dictionary<String, Byte[]> _files = null;
        private readonly Byte[] _assemblyContent;
        private readonly String _methodName = null;
        private InjectionResult _lastError = InjectionResult.Success;
        private String _lastErrorMessage = String.Empty;

        public Client(Byte[] assemblyContent, String methodName, List<Byte[]> dependencies, Dictionary<String, Byte[]> files)
        {
            _assemblyContent = assemblyContent;
            _methodName = methodName;
            _dependencies = dependencies;
            _files = files;
            _pipeChanel = new PipeChanel(_client);
        }

        public void ActivateAssembly(Object context)
        {
            try
            {
                _client.Connect(3000);
                if (_client.IsConnected)
                {
                    // send assembly and run it  
                    var invocationResult = 
                        _pipeChanel.SendMessage(Constants.Ping) &&
                        SendDependencies() &&
                        SendFiles() &&
                        SendToken() &&
                        SendAssembly() &&
                        SendContext(context) &&
                        _pipeChanel.SendMessage(Constants.Run);

                    _client.Dispose();
                    SetLastError();
                }
                else
                {
                    _lastError = InjectionResult.UnableToConnectToNamedPipe;
                }
            }
            catch (TimeoutException)
            {
                _lastError = InjectionResult.UnableToConnectToNamedPipe;
            }
        }

        public InjectionResult GetLastError()
        {
            return _lastError;
        }

        public String GetLastErrorMessage()
        {
            return _lastErrorMessage;
        }

        private void SetLastError()
        {
            if (_lastError == InjectionResult.Success)
            {
                _lastError = _pipeChanel.GetLastError();
                _lastErrorMessage = _pipeChanel.GetLastErrorMessage();
            }
        }

        private Int32 GetAssemblyEntryPointToken(Assembly assembly)
        {
            return assembly.EntryPoint.MetadataToken;
        }

        private Int32 GetAssemblyDefaultMethodToken(Assembly assembly)
        {
            var token = 0;
            var methodToInvoke = 
                assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .FirstOrDefault(method => method.IsStatic && method.Name.Equals("Inject", StringComparison.OrdinalIgnoreCase));

            if (methodToInvoke != null)
            {
                token = methodToInvoke.MetadataToken;
            }

            return token;
        }

        private Int32 GetSpecificMethodToken(Assembly assembly, String methodName)
        {
            var methodToken = 0;
            foreach (var type in assembly.GetTypes())
            {
                var typeName = type.FullName;
                foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var fullname = String.Format("{0}.{1}", typeName, method.Name);
                    if (methodName.Equals(fullname, StringComparison.OrdinalIgnoreCase))
                    {
                        methodToken = method.MetadataToken;
                        break;
                    }
                }

                if (methodToken != 0) break;
            }
            return methodToken;
        }

        private Int32 GetMethodToken(Assembly assembly, String methodName)
        {
            var methodToken = 0;
            if (String.IsNullOrWhiteSpace(methodName))
            {
                methodToken = GetAssemblyDefaultMethodToken(assembly);
                if (methodToken == 0)
                {
                    methodToken = GetAssemblyEntryPointToken(assembly);
                }                    
            }
            else
            {
                methodToken = GetSpecificMethodToken(assembly, methodName);
            }
            return methodToken;
        }

        private Boolean SendFiles()
        {
            var result = true;
            foreach (var kv in _files)
            {
                var value = String.Format("{0}|{1}{2}", kv.Key.Length, kv.Key, Convert.ToBase64String(kv.Value));
                result = result && _pipeChanel.SendMessage(Constants.File, value);
            }
            return result;
        }

        private Boolean SendDependencies()
        {
            var result = true;
            foreach(var dependency in _dependencies)
            {
                var stringBuffer = Convert.ToBase64String(dependency);
                result = result && _pipeChanel.SendMessage(Constants.Dependency, stringBuffer);
            }
            return result;
        }

        private Boolean SendContext(Object context)
        {
            var result = false;
            var formatter = new BinaryFormatter();
            try
            {
                using (var memStream = new MemoryStream())
                {
                    formatter.Serialize(memStream, context);
                    var contextString = Convert.ToBase64String(memStream.ToArray());
                    result = _pipeChanel.SendMessage(Constants.Context, contextString);
                }
            }
            catch { }
            return result;
        }

        private Boolean SendAssembly()
        {
            var stringBuffer = Convert.ToBase64String(_assemblyContent);
            return _pipeChanel.SendMessage(Constants.Assembly, stringBuffer);
        }

        private (String, Int32) GetModuleNameAndMethodToken()
        {
            try
            {
                var assembly = Assembly.Load(_assemblyContent);
                var methodToken = GetMethodToken(assembly, _methodName);
                return (assembly.ManifestModule.ScopeName, methodToken);
            }
            catch (ReflectionTypeLoadException e)
            {
                _lastErrorMessage = e.ToString();
                foreach(var loaderEx in e.LoaderExceptions)
                {
                    _lastErrorMessage += Environment.NewLine + loaderEx.ToString();
                }
                return (null, 0);
            }
            catch (Exception e)
            {
                _lastErrorMessage = e.ToString();
                return (null, 0);
            }
        }

        private Boolean SendToken()
        {
            var result = false;
            var (moduleName, methodToken) = GetModuleNameAndMethodToken();

            if (moduleName == null)
            {
                _lastError = InjectionResult.InvalidAssemblyBuffer;                
            }
            else if (methodToken == 0)
            {
                _lastError = InjectionResult.MethodNotFound;
            }
            else
            {
                result = _pipeChanel.SendMessage(Constants.Token, methodToken.ToString());
            }

            return result;
        }
    }
}
