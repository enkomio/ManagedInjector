using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyWithFileResourceNeeded
{
    public class Main
    {
        // this is the standard signature for activating the Assembly
        private static void Inject()
        {
            File.AppendAllText("log_AssemblyWithFileResourceNeeded.txt", "Correctly activated");
            var fileContent = File.ReadAllText("my_file.txt");
            File.AppendAllText("log_AssemblyWithFileResourceNeeded.txt", "\r\nFile content: " + fileContent);
        }
    }
}
