using System;
using System.Threading;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            Log("Hello World!");

            SerialPortHelper.Instance.OnDataReceived += (sender, data) =>
            {
                Log(String.Format("OnDataReceived: {0}", data));
                //Console.WriteLine("OnDataReceived: {0}", data);
            };

            SerialPortHelper.Instance.OnStatusChanged += (sender, status) =>
            {
                Log(String.Format("connection status: {0}", status));
            };

            // To open/start serial port on COM4 with 9600 bps
            SerialPortHelper.Instance.Open("COM3", 9600);
            if (SerialPortHelper.Instance.IsOpen)
            {
                Log("COM3 Open");
            }
            else
            {
                SerialPortHelper.Instance.Open("COM4", 9600);
                if (SerialPortHelper.Instance.IsOpen)
                {
                    Log("COM4 Open");
                }
            }

            // Reader
            new Thread(() =>
            {
                string line;
                while ((line = Read()) != null)
                {
                    // line += "\r\n";
                    SerialPortHelper.Instance.SendString(line);
                }

                // To close/stop serial port on COM1
                SerialPortHelper.Instance.Close();
                Environment.Exit(0);
            }).Start();


            // Writer
            //new Thread(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(1000);
            //        Log("----------");
            //    }
            //}).Start();

            // Subscribe to the event
            //SerialPortManager.Instance.OnDataReceived += Handler_OnDataReceived;


        }

        // Event handler
        private static void Handler_OnDataReceived(object sender, string data)
        {
            Console.WriteLine("OnDataReceived: {0}", data);
        }


        static int lastWriteCursorTop = 0;

        static void Log(string message)
        {
            int messageLines = message.Length / Console.BufferWidth + 1;
            int inputBufferLines = Console.CursorTop - lastWriteCursorTop + 1;

            Console.MoveBufferArea(sourceLeft: 0, sourceTop: lastWriteCursorTop,
                                   targetLeft: 0, targetTop: lastWriteCursorTop + messageLines,
                                   sourceWidth: Console.BufferWidth, sourceHeight: inputBufferLines);

            int cursorLeft = Console.CursorLeft;
            Console.CursorLeft = 0;
            Console.CursorTop -= inputBufferLines - 1;
            Console.WriteLine(message);
            lastWriteCursorTop = Console.CursorTop;
            Console.CursorLeft = cursorLeft;
            Console.CursorTop += inputBufferLines - 1;
        }

        static string Read()
        {
            Console.Write(">"); // optional
            string line = Console.ReadLine();
            lastWriteCursorTop = Console.CursorTop;
            return line;
        }
    }
}


