using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AOLite
{
    public class UpdateLoop
    {
        public const int UpdateRate = 64;

        private Stopwatch _stopWatch;
        private CancellationTokenSource _cancellationToken;
        private Action<float> _callback;

        public UpdateLoop(Action<float> callback)
        {
            _callback = callback;
        }

        private void Run()
        {
            int desiredDeltaTime = 1000 / UpdateRate;

            while (!_cancellationToken.IsCancellationRequested)
            {
                long deltaTime = _stopWatch.ElapsedMilliseconds;
                _stopWatch.Restart();
                Tick(deltaTime / 1000f);
                Thread.Sleep((int)Math.Max(desiredDeltaTime - _stopWatch.ElapsedMilliseconds, 0));
            }
        }

        private void Tick(float deltaTime)
        {
            _callback.Invoke(deltaTime);
        }

        public void Start()
        {
            _stopWatch = Stopwatch.StartNew();
            _cancellationToken = new CancellationTokenSource();
            Run();
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
        }
    }
}
