using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    class RoundRobinClient : ClusterClientBase
    {

        //private readonly Random _random = new Random();
        private readonly ConcurrentDictionary<string, Statistic> _replicasStat;


        public RoundRobinClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            _replicasStat = new ConcurrentDictionary<string, Statistic>();
            foreach (var replica in replicaAddresses)
            {
                _replicasStat[replica] = new Statistic();
            }
        }

        /*
        RoundRobin(2 балла). Случайным образом выбирать 
        последовательность обхода реплик, делить таймаут 
        из аргумента на кол-во реплик и, используя вычисленный 
        таймаут, последовательно делать запросы к репликам.
        Если реплика не ответила за указанный таймаут, нужно переходить к следующей.
        */
        /*
        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            timeout = new TimeSpan(timeout.Ticks / ReplicaAddresses.Length);
            Task<string> task = null;
            
            foreach (var replicaAddress in ReplicaAddresses.OrderBy(i => _random.Next()))
            {
                var uri = replicaAddress + "?query=" + query;
                task = ProcessRequestAsync(uri);
                await Task.WhenAny(task, Task.Delay(timeout));
                if (task.IsCompleted)
                    break;
            }

            if (task == null || !task.IsCompleted)
                throw new TimeoutException();
            
            return task.Result;
        }
        */
        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            timeout = new TimeSpan(timeout.Ticks / ReplicaAddresses.Length);
            Task<RequestReport> task = null;

            var sortedReplicas = _replicasStat                
                .OrderBy(replicaStat => replicaStat.Value.MiddleTime)
                .Select(replicaStat => replicaStat.Key);

            foreach (var replicaAddress in sortedReplicas)
            {
                task = ProcessRequestWithTimeMesureAsync(replicaAddress, query);
                await Task.WhenAny(task, Task.Delay(timeout));
                if (task.IsCompleted)
                    break;
            }

            if (task == null || !task.IsCompleted)
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
