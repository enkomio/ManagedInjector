using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyWithDefaultMethodName
{
    public class Main
    {
        // this is the standard signature for activating the Assembly
        private static void Inject()
        {
            File.AppendAllText("log_AssemblyWithDefaultMethodName.txt", "Correctly activated");
            File.AppendAllText("log_AssemblyWithDefaultMethodName.txt", "\r\nCur dir: " + Directory.GetCurrentDirectory());
        }
    }
}
