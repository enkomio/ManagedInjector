using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            File.AppendAllText("log_ConsoleProgram.txt", "\r\nCur dir: " + Directory.GetCurrentDirectory());
        }
    }
}
