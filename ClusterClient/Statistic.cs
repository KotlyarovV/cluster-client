using System.Threading;

namespace ClusterClient
{
    public class Statistic
    {
        private int _allTime;
        private int _requested;
        public double MiddleTime => _allTime / (double)_requested;

        public void AddTime(int time)
        {
            Interlocked.Add(ref _allTime, time);
            Interlocked.Increment(ref _requested);
        }
    }
}
