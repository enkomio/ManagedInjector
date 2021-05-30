using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyWithMethodAcceptingAnArgument
{
    public class Program
    {
        // this is the standard signature for activating the Assembly
        private static void Inject(Object context)
        {
            Dictionary<String, String> parameters = (Dictionary<String, String>)context;
            File.WriteAllText(parameters["File"], parameters["Content"]);
        }

        static void Main(string[] args)
        {

        }
    }
}
