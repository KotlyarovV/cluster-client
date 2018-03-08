using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterClient
{
    public class RequestReport
    {
        public int Time { get; private set; }
        public string Replica { get; private set; }
        public string Answer { get; private set; }

        public RequestReport(string answer, string replica, int time)
        {
            Time = time;
            Answer = answer;
            Replica = replica;
        }
    }
}
