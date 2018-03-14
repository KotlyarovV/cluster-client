using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public abstract class ClusterClientBase
    {
        private readonly int _blockTime = 4000;

        protected ClusterClientBase(string[] replicaAddresses)
        {
            ReplicaAddresses = replicaAddresses;
            foreach (var replica in replicaAddresses)
            {
                GreyList[replica] = 0;  
            }
        }

        protected ConcurrentDictionary<string, int> GreyList { get; } = new ConcurrentDictionary<string, int>();

        protected void RefreshGreyList()
        {
            foreach (var item in GreyList)
                if (item.Value + _blockTime < Environment.TickCount)
                    GreyList[item.Key] = 0;
        }

        private string[] _replicaAddresses;
        protected string[] ReplicaAddresses
        {
            get
            {
                RefreshGreyList();
                return _replicaAddresses
                    .Where(replica => GreyList[replica] == 0)
                    .ToArray();
            }
            private set { _replicaAddresses = value; }
        }
        
        protected TimeoutException ThrowTimeout(string replica)
        {
            GreyList[replica] = Environment.TickCount;
            return new TimeoutException();
        }
        
        public abstract Task<string> ProcessRequestAsync(string query, TimeSpan timeout);

        protected abstract ILog Log { get; }

        protected static HttpWebRequest CreateRequest(string uriStr)
        {
            var request = WebRequest.CreateHttp(Uri.EscapeUriString(uriStr));
            request.Proxy = null;
            request.KeepAlive = true;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = 100500;
            return request;
        }

        public async Task<string> ProcessRequestAsync(WebRequest request)
        {
            var timer = Stopwatch.StartNew();
            using (var response = await request.GetResponseAsync())
            {
                var result = await new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEndAsync();
                Log.InfoFormat("Response from {0} received in {1} ms", request.RequestUri, timer.ElapsedMilliseconds);
                return result;
            }
        }

        protected async Task<string> ProcessRequestAsync(string uriStr)
        {
            var request = CreateRequest(uriStr);
            return await ProcessRequestAsync(request);
        }

        protected async Task ProcessStopRequest(string replicaAddress, Guid id)
        {
            var uri = replicaAddress + "?id=" + id + "&stop=ok";
            await ProcessRequestAsync(uri);
        }

        protected async Task CancelTasks(IEnumerable<string> replicas, Guid id) => 
            await Task.WhenAll(replicas.Select(ra => ProcessStopRequest(ra, id)));

        protected string CreateRequestQuery(string replicaAddress, string query, Guid id) =>
            replicaAddress + "?query=" + query + "&id=" + id;
    }

}