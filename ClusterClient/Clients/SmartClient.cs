using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient.Extensions;
using log4net;

namespace ClusterClient.Clients
{    
    class SmartClient : CleverClient
    {
       
        public SmartClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }
        /*Smart (3 балла). Так же, как в RoundRobin, 
         * только продолжать ждать ответа от
         * реплик при запросе последующих.
         */        
        public override async Task<Tuple<RequestReport, List<string>>> InspectTasks(
            TimeSpan timeout, 
            string query, 
            Guid id)
        {
            var usedReplicas = new List<string>();
            var tasks = new List<Task<RequestReport>>();
            var exeptions = new List<Exception>();

            foreach (var replica in SortedReplicas)
            {
                var delay = Task.Delay(timeout).ContinueWith(_ => new RequestReport("", "", 0));
                tasks.Add(ProcessRequestWithTimeMesureAsync(replica, query, id));
                tasks.Add(delay);
                var task = await Task.WhenAny(tasks);
                usedReplicas.Add(replica);

                if (task == delay)
                {
                    GreyList[replica] = Environment.TickCount;
                    tasks.Remove(delay);
                }
                if (task.IsFaulted)
                {
                    exeptions.Add(task.Exception);
                    tasks.Remove(task);
                    tasks.Remove(delay);
                }

                else if (task.IsCompleted)
                {
                    return Tuple.Create(task.Result, usedReplicas);
                }
            }

            throw new AggregateException(exeptions);
        }



        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(RoundRobinClient)); }
        }
        

    }
}
