using log4net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    class RoundRobinClient : CleverClient
    {


        public RoundRobinClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        /*
        RoundRobin(2 балла). Случайным образом выбирать 
        последовательность обхода реплик, делить таймаут 
        из аргумента на кол-во реплик и, используя вычисленный 
        таймаут, последовательно делать запросы к репликам.
        Если реплика не ответила за указанный таймаут, нужно переходить к следующей.
        */                
        public override async Task<Tuple<RequestReport, List<string>>> InspectTasks(
            TimeSpan timeout, 
            string query, 
            Guid id)
        {
            var usedReplicas = new List<string>();
            var exeptions = new List<Exception>();
            foreach (var replicaAddress in SortedReplicas)
            {
                var task = ProcessRequestWithTimeMesureAsync(replicaAddress, query, id);
                await Task.WhenAny(task, Task.Delay(timeout));
                usedReplicas.Add(replicaAddress);
                if (!task.IsCompleted)
                {
                    GreyList[replicaAddress] = Environment.TickCount;
                }
                else if (task.IsFaulted)
                {
                    exeptions.Add(task.Exception);
                }
                else return Tuple.Create(task.Result, usedReplicas);
            }

            throw new AggregateException(exeptions);
        }


        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(RoundRobinClient)); }
        }
    }
}
