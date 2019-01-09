using System;
using System.IO;

namespace ES.ManagedInjector
{
    internal class PipeChanell
    {
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private InjectionResult _lastError = InjectionResult.Success;
        private String _lastErrorMessage = String.Empty;

        public PipeChanell(Stream stream)
        {
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream);
        }

        public Boolean SendMessage(String type, String data)
        {
            return SendMessage(new PipeMessage(type, data));
        }

        public Boolean SendMessage(String type)
        {
            return SendMessage(new PipeMessage(type, String.Empty));
        }        

        public InjectionResult GetLastError()
        {
            return _lastError;
        }

        public String GetLastErrorMessage()
        {
            return _lastErrorMessage;
        }

        public Boolean SendMessage(PipeMessage msg)
        {
            var response = SendData(msg);
            if (!response.IsSuccess())
            {
                var items = response.GetData().Split('|');
                _lastError = (InjectionResult)Int32.Parse(items[0]);
                _lastErrorMessage = items[1];
            }
            return response.IsSuccess();
        }

        public PipeMessage GetMessage()
        {
            return PipeMessage.Create(ReadData());
        }
        
        public void SendAck(InjectionResult code, String message)
        {
            var value = String.Format("{0}|{1}", code, message);
            var type = code == InjectionResult.Success ? Constants.Ok : Constants.Error;
            var ackMsg = new PipeMessage(type, value);
            _writer.WriteLine(ackMsg.Serialize());
            _writer.Flush();
        }

        private String ReadData()
        {
            return _reader.ReadLine();
        }

        private PipeMessage SendData(PipeMessage msg)
        {
            _writer.WriteLine(msg.Serialize());
            _writer.Flush();
            var ack = _reader.ReadLine();
            return PipeMessage.Create(ack);
        }
    }
}
