using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterClient
{
    class Statistic
    {
        private int _allTime;
        private int _requested;
        public double MiddleTime { get; private set; }

        public void AddTime(int time)
        {
            _allTime = time + _allTime;
            _requested++;
            MiddleTime = _allTime / (double)_requested;
        }
    }
}
