using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace ConsoleAnonymousClient
{
    class Program
    {
        static void Main(string[] args)
        {
            anonymousIn(args[0]);

            Console.WriteLine("Client: Press any key to quit...");
            Console.ReadKey();
        }

        static void anonymousIn(string handle)
        {
            Console.WriteLine("Anonymous Pipe In Client");

            AnonymousPipeClientStream pipeClient = new AnonymousPipeClientStream(PipeDirection.In, handle);
            string line = string.Empty;
            using (StreamReader sr = new StreamReader(pipeClient))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine("Echo: {0}", line);
                    if(line=="quit")
                    {
                        break;
                    }
                }
            }
        }
    }
}
