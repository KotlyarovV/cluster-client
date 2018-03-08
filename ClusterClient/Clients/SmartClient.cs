using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    

    class SmartClient : ClusterClientBase
    {
        //private readonly Random _random = new Random();
        private readonly ConcurrentDictionary<string, Statistic> _replicasStat;
       
        public SmartClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            _replicasStat = new ConcurrentDictionary<string, Statistic>();
            foreach (var replica in replicaAddresses)
            {
                _replicasStat[replica] = new Statistic();
            }
        }
        /*Smart (3 балла). Так же, как в RoundRobin, 
         * только продолжать ждать ответа от реплик при запросе последующих.
         */
        /*        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
                {
                    timeout = new TimeSpan(timeout.Ticks / ReplicaAddresses.Length);
                    var uris = ReplicaAddresses
                        .Select(uri => uri + "?query=" + query)
                        .OrderBy(i => _random.Next());

                    var tasks = new List<Task<string>>();

                    foreach (var uri in uris)
                    {
                        tasks.Add(ProcessRequestAsync(uri));
                        var tasksWithTimer = new List<Task>(tasks) { Task.Delay(timeout) };

                        await Task.WhenAny(tasksWithTimer);

                        if (tasks.Any(task => task.IsCompleted))
                            break;
                    }

                    return tasks.Single(task => task.IsCompleted).Result;
                }

            */

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            timeout = new TimeSpan(timeout.Ticks / ReplicaAddresses.Length);
            
            var tasks = new List<Task<RequestReport>>();
            Task<RequestReport> task = null;

            var sortedReplicas = _replicasStat
                .OrderBy(replicaStat => replicaStat.Value.MiddleTime)
                .Select(replicaStat => replicaStat.Key);
            
            foreach (var  replica in sortedReplicas)
            {
                tasks.Add(ProcessRequestWithTimeMesureAsync(replica, query));
                var tasksWithTimer = new List<Task>(tasks) { Task.Delay(timeout) };

                await Task.WhenAny(tasksWithTimer);                
                if ((task = tasks.FirstOrDefault(t => t.IsCompleted)) != null)
                    break;
            }

            if (task == null)
                throw ThrowTimeout(ReplicaAddresses.First());

            _replicasStat[task.Result.Replica].AddTime(task.Result.Time);
            return task.Result.Answer;
        }
        


        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(RoundRobinClient)); }
        }
        

    }
}
