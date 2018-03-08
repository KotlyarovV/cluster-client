using System;
using System.Collections.Concurrent;
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

        private string[] _replicaAddresses;
        protected string[] ReplicaAddresses
        {
            get
            {
                foreach (var item in GreyList)
                    if (item.Value + _blockTime < Environment.TickCount)
                        GreyList[item.Key] = 0;

                return _replicaAddresses
                    .Where(replica => GreyList[replica] == 0)
                    .ToArray();
            }
            private set { _replicaAddresses = value; }
        }
        
        protected ConcurrentDictionary<string, int> GreyList { get; set; }

        protected TimeoutException ThrowTimeout(string replica)
        {
            GreyList[replica] = Environment.TickCount;
            return new TimeoutException();
        }

        protected ClusterClientBase(string[] replicaAddresses)
        {
            GreyList = new ConcurrentDictionary<string, int>();
            ReplicaAddresses = replicaAddresses;
            foreach (var replica in replicaAddresses)
            {
                GreyList[replica] = 0;
            }            
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

        protected async Task<string> ProcessRequestAsync(WebRequest request)
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

        protected async Task<RequestReport> ProcessRequestWithTimeMesureAsync(string replicaAddress, string query)
        {
            var uriStr = replicaAddress + "?query=" + query;
            var nowTime = Environment.TickCount;

            var request = CreateRequest(uriStr);
            var answer = await ProcessRequestAsync(request);
            var spendedTime = Environment.TickCount - nowTime;

            return new RequestReport(answer, replicaAddress, spendedTime);
        }
    }

}