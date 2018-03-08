using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    class AllTogetherClusterClient : ClusterClientBase
    {
        public AllTogetherClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            
        }

        /*
         * Ходить ко всем репликам одновременно, дожидаться ответа
         * хотя бы от одной из них и сразу же возвращать этот ответ (1 балл)
         */
        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var requestAddresses = ReplicaAddresses.Select(address => address + "?query=" + query);
            var tasks = requestAddresses
                .Select(ProcessRequestAsync)
                .ToList();

            var tasksWithTimer = new List<Task>(tasks)
            {
                Task.Delay(timeout)
            };
            Task<string> task = null;
            await Task.WhenAny(tasksWithTimer);

            if ((task = tasks.FirstOrDefault(t => t.IsCompleted)) == null)
                throw ThrowTimeout(ReplicaAddresses.First());

            return task.Result;
        }

        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(AllTogetherClusterClient)); }
        }
    }
}
