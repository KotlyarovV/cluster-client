using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient.Extensions;

namespace ClusterClient.Clients
{
    class AllTogetherClusterClient : ClusterClientBase
    {
        public AllTogetherClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            
        }


        private List<Task<string>> GetTasks(IEnumerable<string> addresses, string query, Guid id) =>
            addresses
                .Select(address => address + "?query=" + query + "&id=" + id)
                .Select(ProcessRequestAsync)
                .ToList();


        /*
         * Ходить ко всем репликам одновременно, дожидаться ответа
         * хотя бы от одной из них и сразу же возвращать этот ответ (1 балл)
         */
        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var id = Guid.NewGuid();
            var requestAddresses = ReplicaAddresses;           
            var tasks = GetTasks(requestAddresses, query, id);
            
            var exceptions = new List<Exception>();
            var delay = Task.Delay(timeout).ContinueWith(_ => "");
            
            
            while (tasks.Count > 0)
            {
                tasks.Add(delay);
                var task = await Task.WhenAny(tasks);

                if (task.IsFaulted)
                {
                    exceptions.Add(task.Exception);
                    tasks.Remove(task);
                    continue;
                }
                if (task == delay)
                    throw new TimeoutException();
                
                return task.Result;
            }

            await CancelTasks(requestAddresses, id);
            throw new AggregateException(exceptions);
            
        }

        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(AllTogetherClusterClient)); }
        }
    }
}
