using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //bytePipe();
            //messagepipe();
            asyncMessagePipe();

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }

        static void bytePipe()
        {
            Console.WriteLine("Named Byte Pipe Client");
            byte[] buf = new byte[1024];
            int num =0;
            string temp = string.Empty;

            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream("bytePipe"))
            {
                pipeStream.Connect();
                pipeStream.ReadMode = PipeTransmissionMode.Byte;

                byte[] bs = Encoding.UTF8.GetBytes("Client bannnana");
                pipeStream.Write(bs, 0, bs.Length);

                while(true)
                {
                    num = pipeStream.Read(buf,0,buf.Length);
                    temp = Encoding.UTF8.GetString(buf, 0, num);
                    Console.WriteLine(temp);
                    if(temp == "stop")
                    {
                        break;
                    }
                }
            }
        }

        static void messagePipe()
        {
            Console.WriteLine("Named Message Pipe Client");

            Decoder decoder = Encoding.UTF8.GetDecoder();
            Byte[] bytes = new Byte[10];
            Char[] chars = new Char[10];
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream("messagePipe"))
            {
                pipeStream.Connect();
                pipeStream.ReadMode = PipeTransmissionMode.Message;
                int numBytes;
                do
                {
                    string message = "";

                    do
                    {
                        numBytes = pipeStream.Read(bytes, 0, bytes.Length);
                        int numChars = decoder.GetChars(bytes, 0, numBytes, chars, 0);
                        message += new String(chars, 0, numChars);
                    } while (!pipeStream.IsMessageComplete);

                    decoder.Reset();
                    Console.WriteLine(message);
                } while (numBytes != 0);
            }
        }


        static AutoResetEvent quit = new AutoResetEvent(false);
        static string stop = "stop";
        static UTF8Encoding encoder = new UTF8Encoding();
        //static NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", "messagepipe", PipeDirection.InOut, PipeOptions.Asynchronous);
        private static void asyncMessagePipe()
        {
            NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", "messagepipe", PipeDirection.InOut, PipeOptions.Asynchronous);
            pipeStream.Connect();
            Console.WriteLine("Async Message Pipe Client");

            byte[] echo = new byte[1024];
            pipeStream.BeginRead(echo, 0, echo.Length, new AsyncCallback(readCallback), new object[] { pipeStream, echo });

            string message = "Hello server"; 
            byte[] bytes = encoder.GetBytes(message);
            pipeStream.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(writeCallback), new object[] { pipeStream, message });

            quit.WaitOne();
        }

        static void writeCallback(IAsyncResult async)
        {
            NamedPipeClientStream pipeStream = ((object[])async.AsyncState)[0] as NamedPipeClientStream;
            string message = ((object[])async.AsyncState)[1] as string;

            pipeStream.EndWrite(async);
            Console.WriteLine("Async Client Write:{0}", message);

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
            NamedPipeClientStream pipeStream = ((object[])async.AsyncState)[0] as NamedPipeClientStream;
            byte[] echo = ((object[])async.AsyncState)[1] as byte[];

            int num = pipeStream.EndRead(async);
            string message = encoder.GetString(echo, 0, num);
            Console.WriteLine("Async Client Read:{0}", message);

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