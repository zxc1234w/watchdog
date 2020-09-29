using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatchDog
{
    class Program
    {
        private static bool _Created = false;
        private static Mutex _Lock = new Mutex(false, "WatchDog", out _Created);
        private static string _Last = "";
        private static string _Current = "";
        private static ulong _TimeoutCounter = 0;
        private static ulong _RebootCounter = 0;

        static void Main(string[] args)
        {
            if (!_Created || args == null || args.Length == 0)
            {
                return;
            }

            string ProcessName = args[0];

            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine($"本程式將監控 {ProcessName} 是否持續運作");
            Console.WriteLine($"倘若偵測到 {ProcessName} 關閉時，將自動重新啟動");
            Console.WriteLine($"若需關閉 {ProcessName} 時，請先將本程式關閉");
            Console.WriteLine("-----------------------------------------------\n");

            var PipeServer = new NamedPipeServerStream("WatchDog");
            StreamReader Reader = new StreamReader(PipeServer);
            StreamWriter Writer = new StreamWriter(PipeServer);

            Console.WriteLine($"等待 {ProcessName} 上線");
            PipeServer.WaitForConnection();
            Console.WriteLine($"連線成功");

            DateTime StartTime = DateTime.Now;
            while (true)
            {
                if (_TimeoutCounter > 3)
                {
                    Console.CursorTop += 3;
                    Console.WriteLine();
                    Console.WriteLine($"{ProcessName} 無回應，嘗試重新啟動");

                    Process[] ProcessList = null;
                    if (args != null && args.Length > 0)
                    {
                        ProcessList = Process.GetProcessesByName(ProcessName);
                    }
                    if (ProcessList != null && ProcessList.Length == 1)
                    {
                        Console.WriteLine($"{ProcessName} 異常，強制關閉程式");
                        ProcessList[0].Kill();
                    }
                    Console.WriteLine("重新啟動中...");
                    Process.Start(ProcessName);

                    _TimeoutCounter = 0;

                    PipeServer.Disconnect();
                    PipeServer.Dispose();

                    PipeServer = new NamedPipeServerStream("WatchDog");
                    Reader = new StreamReader(PipeServer);
                    Writer = new StreamWriter(PipeServer);
                    PipeServer.WaitForConnection();
                    Console.WriteLine("重啟成功");
                    _RebootCounter++;
                    StartTime = DateTime.Now;
                    Console.WriteLine($"累計重啟次數： {_RebootCounter}");
                }

                _Current = Reader.ReadLine();
                if (_Current != null)
                {
                    TimeSpan Duration = DateTime.Now - StartTime;

                    Console.WriteLine("-----------------------------------------------");
                    Console.WriteLine($"程式已運行： {Duration.Days.ToString("0000")}天 {Duration.Hours.ToString("00")}時 {Duration.Minutes.ToString("00")}分 {Duration.Seconds.ToString("00")}秒");
                    Console.WriteLine("-----------------------------------------------");
                    Console.SetCursorPosition(0, Console.CursorTop - 3);
                }

                Thread.Sleep(1000);
                if (_Last == _Current)
                {
                    _TimeoutCounter++;
                }
                else
                {
                    _TimeoutCounter = 0;
                    _Last = _Current;
                }
            }
        }

    }
}
