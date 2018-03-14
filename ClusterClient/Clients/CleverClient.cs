using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    public abstract class CleverClient : ClusterClientBase
    {        
        protected CleverClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            foreach (var replica in replicaAddresses)
            {
                ReplicasStat[replica] = new Statistic();
            }
        }

        protected ConcurrentDictionary<string, Statistic> ReplicasStat { get; }
            = new ConcurrentDictionary<string, Statistic>();

        protected List<string> SortedReplicas
        {
            get
            {
                RefreshGreyList();
                return ReplicasStat
                    .Where(rS => GreyList[rS.Key] == 0)
                    .OrderBy(replicaStat => replicaStat.Value.MiddleTime)
                    .Select(replicaStat => replicaStat.Key)
                    .ToList();
            }
        }

        public abstract Task<Tuple<RequestReport, List<string>>> InspectTasks(TimeSpan timeout, string query, Guid id);

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var id = Guid.NewGuid();
            timeout = new TimeSpan(timeout.Ticks / ReplicaAddresses.Length);
            
            var result = await InspectTasks(timeout, query, id);

            await CancelTasks(result.Item2, id);
            ReplicasStat[result.Item1.Replica].AddTime(result.Item1.Time);
            return result.Item1.Answer;
        }

        protected async Task<RequestReport> ProcessRequestWithTimeMesureAsync(string replicaAddress, string query, Guid id)
        {
            var uriStr = CreateRequestQuery(replicaAddress, query, id);
            var nowTime = Environment.TickCount;

            var request = CreateRequest(uriStr);
            var answer = await ProcessRequestAsync(request);
            var spendedTime = Environment.TickCount - nowTime;

            return new RequestReport(answer, replicaAddress, spendedTime);
        }

    }
}
