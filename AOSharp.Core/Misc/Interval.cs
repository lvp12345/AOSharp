using System;

namespace AOSharp.Core.Misc
{
    public class Interval
    {
        public virtual bool Elapsed => Time.NormalTime >= _nextExecuteTime;

        private double _nextExecuteTime;
        private float _interval;

        public Interval(int ms)
        {
            _interval = ms / 1000f;
            Reset();
        }

        public void ExecuteIfElapsed(Action action)
        {
            if (Elapsed)
            {
                Reset();
                action?.Invoke();
            }
        }

        public void Reset() => _nextExecuteTime = Time.NormalTime + _interval;
    }

    public class AutoResetInterval : Interval
    {
        public override bool Elapsed => GetAndResetIfElapsed();

        public AutoResetInterval(int ms) : base(ms) {}

        private bool GetAndResetIfElapsed()
        {
            if(base.Elapsed)
            {
                Reset();
                return true;
            }

            return false;
        }
    }
}
