using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient.Extensions;
using log4net;

namespace ClusterClient.Clients
{
    class MyClient : CleverClient
    {
        public MyClient(string[] replicas) : base(replicas)
        {
            
        }

        protected override ILog Log =>  LogManager.GetLogger(typeof(RoundRobinClient));

        private IEnumerable<List<T>> SliceByProgression<T>(IEnumerable<T> collection, int chunkSize)
        {
            var pos = 0;
            while (collection.Skip(pos).Any())
            {
                yield return collection
                    .Skip(pos)
                    .Take(chunkSize)
                    .ToList();

                pos += chunkSize;
                chunkSize *= 2;
            }
        }

        public override async Task<Tuple<RequestReport, List<string>>> InspectTasks(
            TimeSpan timeout,
            string query,
            Guid id
            )
        {
            var exceptions = new List<Exception>();

            foreach (var replicaAddresses in SliceByProgression(SortedReplicas, 2))
            {
                var tasks = replicaAddresses
                    .Select(r => ProcessRequestWithTimeMesureAsync(r, query, id))
                    .ToList();

                var delay = Task.Delay(new TimeSpan(timeout.Ticks * replicaAddresses.Count)).ContinueWith(_ => new RequestReport());
                tasks.Add(delay);
                var task = await Task.WhenAny(tasks);
                await CancelTasks(replicaAddresses, id);

                if (task == delay)
                {
                    foreach (var replica in replicaAddresses)
                    {
                        GreyList[replica] = Environment.TickCount;
                    }
                }
                else if (task.IsFaulted)
                {
                    tasks.Remove(delay);
                    tasks.Remove(task);
                    exceptions.Add(task.Exception);
                }
                else return Tuple.Create(task.Result, new List<string>());
            }
            
            throw new AggregateException(exceptions);
        }

    }
}
