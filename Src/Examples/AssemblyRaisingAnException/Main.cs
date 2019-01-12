using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyRaisingAnException
{
    public class Main
    {
        private static void Inject()
        {
            var stream = new StreamReader(Stream.Null);
            var exception = 10 / Int32.Parse(stream.ReadLine());
        }
    }
}
