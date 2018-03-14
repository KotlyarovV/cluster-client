using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RandomClusterClient : ClusterClientBase
    {
        private readonly Random random = new Random();

        public RandomClusterClient(string[] replicaAddresses)
            : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var id = Guid.NewGuid();

            var addresses = ReplicaAddresses;
            var uri = addresses[random.Next(addresses.Length)];
            var webRequest = CreateRequest(CreateRequestQuery(uri, query, id));
            
            Log.InfoFormat("Processing {0}", webRequest.RequestUri);

            var task = ProcessRequestAsync(webRequest);
            await Task.WhenAny(task, Task.Delay(timeout));

            if (!task.IsCompleted)
            {
                throw ThrowTimeout(uri);
            }

            return task.Result;
        }
        

        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(RandomClusterClient)); }
        }
    }
}