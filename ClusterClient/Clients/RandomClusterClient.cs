using System;
using System.Linq;
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
            /*
            var uri = ReplicaAddresses[random.Next(ReplicaAddresses.Length)];
            while (GreyList[uri].Item1 != 0)
            {
                uri = ReplicaAddresses[random.Next(ReplicaAddresses.Length)];
                if (GreyList[uri].Item1 + GreyList[uri].Item2 < Environment.TickCount)
                {
                    GreyList[uri] = Tuple.Create(0, 0);
                }
            }
              */

            var addresses = ReplicaAddresses;
            var uri = addresses[random.Next(addresses.Length)];
            var webRequest = CreateRequest(uri + "?query=" + query);
            
            Log.InfoFormat("Processing {0}", webRequest.RequestUri);

            var resultTask = ProcessRequestAsync(webRequest);
            await Task.WhenAny(resultTask, Task.Delay(timeout));

            if (!resultTask.IsCompleted)
                throw ThrowTimeout(uri);

            return resultTask.Result;
        }

        private int Sum(int a, int b) => a + b;

        
        private Task<int> Os()
        {
            var t = Task.Run(() =>
            {
                return 5;
            });
            return t;
        }

        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(RandomClusterClient)); }
        }
    }
}