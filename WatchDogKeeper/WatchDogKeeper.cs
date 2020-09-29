using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatchDogKeeper
{
    public class DogKeeper
    {
        private const string _PipeName = "WatchDog";

        private Thread _Keeper;
        private bool _IsKeeping;

        public DogKeeper()
        {

        }

        ~DogKeeper()
        {
            StopKeeping();
        }

        public void StartKeeping(string process_name)
        {
            StopKeeping();

            Process.Start("WatchDog", process_name);
            _Keeper = new Thread(Feed);
            _Keeper.IsBackground = true;
            _IsKeeping = true;
            _Keeper.Start();
        }

        private void StopKeeping()
        {
            if (_Keeper != null && _IsKeeping)
            {
                _IsKeeping = false;
                _Keeper.Abort();
                _Keeper = null;
            }
        }

        private void Feed()
        {
            var PipeClient = new NamedPipeClientStream(_PipeName);
            PipeClient.Connect();

            StreamReader Reader = new StreamReader(PipeClient);
            StreamWriter Writer = new StreamWriter(PipeClient);

            long Index = 0;
            while (_IsKeeping)
            {
                try
                {
                    Writer.WriteLine(Index++);
                    Writer.Flush();
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }
    }
}
