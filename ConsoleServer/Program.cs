using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //anonymousOutPipe();
            //bytePipe();
            //messagePipe();
            asyncMessagePipe();

            Console.WriteLine("Server: Press any key to quit...");
            Console.ReadKey();
        }

        /// <summary>
        /// 父子进程单向通信
        /// 可用于进程间输出重定向
        /// </summary>
        private static void anonymousOutPipe()
        {
            Console.WriteLine("Anonymous Pipe Server"); 

            Process process = new Process();
            process.StartInfo.FileName = "ConsoleAnonymousClient.exe";
            //创建匿名管道流实例
            using (AnonymousPipeServerStream pipeStream =
                new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            {
                //将句柄传递给子进程
                process.StartInfo.Arguments = pipeStream.GetClientHandleAsString();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;
                process.Start();
                //销毁子进程的客户端句柄？
                pipeStream.DisposeLocalCopyOfClientHandle();

                using (StreamWriter sw = new StreamWriter(pipeStream))
                {
                        sw.AutoFlush = true;
                        do
                        {
                            string line = Console.ReadLine();
                            if (line != "stop")
                            {
                                //向匿名管道中写入内容
                                sw.WriteLine(line);
                            }else
                            {
                                break;
                            }
                        } while (true);
                    }
            }

            process.WaitForExit();
            process.Close();
        }

        private static void bytePipe()
        {
            Console.WriteLine("Named Byte Pipe Server");
            byte[] buf = new byte[1024];
            int num =0;

            using (NamedPipeServerStream pipeStream = new NamedPipeServerStream("bytePipe"))
            {
                pipeStream.WaitForConnection();
                pipeStream.ReadMode = PipeTransmissionMode.Byte;//default

                do
                {
                    num = pipeStream.Read(buf, 0, buf.Length);
                    Console.WriteLine(Encoding.UTF8.GetString(buf, 0, num));
                } while (num == 0);

                string temp;
                while (true)
                {
                    temp = Console.ReadLine();
                    byte[] bs = Encoding.UTF8.GetBytes(temp);
                    pipeStream.Write(bs, 0, bs.Length);
                    if(temp == "stop")
                    {
                        break;
                    }
                }
            }
        }

        private static void messagePipe()
        {
            Console.WriteLine("Named Message Pipe Server");

            UTF8Encoding encoding = new UTF8Encoding();
            string message1 = "Named Pipe Message Example.";
            string message2 = "Another Named Pipe Message Example.";
            Byte[] bytes;
            using (NamedPipeServerStream pipeStream = new
                    NamedPipeServerStream("messagePipe", PipeDirection.InOut, 1,
                    PipeTransmissionMode.Message, PipeOptions.None))
            {
                pipeStream.WaitForConnection();

                // Let’s send two messages.
                bytes = encoding.GetBytes(message1);
                pipeStream.Write(bytes, 0, bytes.Length);
                bytes = encoding.GetBytes(message2);
                pipeStream.Write(bytes, 0, bytes.Length);
            }
        }

        static AutoResetEvent quit = new AutoResetEvent(false);
        static string stop = "stop";
        static UTF8Encoding encoder = new UTF8Encoding();
        private static void asyncMessagePipe()
        {
            NamedPipeServerStream pipeStream = new NamedPipeServerStream("messagepipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Console.WriteLine("Message Pipe Server");
            
            pipeStream.WaitForConnection();
            
            byte[] echo = new byte[1024];
            pipeStream.BeginRead(echo, 0, echo.Length, new AsyncCallback(readCallback), new object[] { pipeStream, echo });

            string message = "Hello client"; 
            byte[] bytes = encoder.GetBytes(message);
            pipeStream.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(writeCallback), new object[] { pipeStream, message });

            quit.WaitOne();
        }

        static void writeCallback(IAsyncResult async)
        {
            NamedPipeServerStream pipeStream = ((object[])async.AsyncState)[0] as NamedPipeServerStream;
            string message = ((object[])async.AsyncState)[1] as string;

            pipeStream.EndWrite(async);
            Console.WriteLine("Async Server Write:{0}",message);

            if (message == stop)
            {
                pipeStream.Dispose();
                quit.Set();
            }
            else
            {
                message = Console.ReadLine();
                if (pipeStream.IsConnected)
                {
                    byte[] bytes = encoder.GetBytes(message);
                    pipeStream.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(writeCallback), new object[] { pipeStream, message });
                }
            }
        }

        static void readCallback(IAsyncResult async)
        {
            NamedPipeServerStream pipeStream = ((object[])async.AsyncState)[0] as NamedPipeServerStream;
            byte[] echo = ((object[])async.AsyncState)[1] as byte[];

            int num = pipeStream.EndRead(async);
            string message = encoder.GetString(echo, 0, num);
            Console.WriteLine("Async Server Read:{0}",message);

            if (message == stop)
            {
                pipeStream.Dispose();
                quit.Set();
            }
            else
            {
                if (pipeStream.IsConnected)
                {
                    Array.Clear(echo, 0, echo.Length);
                    pipeStream.BeginRead(echo, 0, echo.Length, new AsyncCallback(readCallback), new object[] { pipeStream, echo });
                }
            }
        }

    }
}
