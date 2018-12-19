using System.IO;

namespace AssemblyWithDependency
{
    public class Main
    {
        public void Run()
        {
            var entity = new Entity("TYPE", "SOME VALUE");
            File.AppendAllText("log_AssemblyWithDependency.txt", "\r\nCur dir: " + Directory.GetCurrentDirectory());
            File.AppendAllText("log_AssemblyWithDependency.txt", "\r\nSerialized value: " + entity.Serialize());
        }
    }
}
