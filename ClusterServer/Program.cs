using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace ClusterServer
{
	public static class Program
	{
        private static ConcurrentDictionary<string, CancellationTokenSource> Cancellations { get; } 
            = new ConcurrentDictionary<string, CancellationTokenSource>();

       // private static ConcurrentBag<string> StopedIds { get; }
            

		public static void Main(string[] args)
		{
			XmlConfigurator.Configure();

			try
			{
				ServerArguments parsedArguments;
				if(!ServerArguments.TryGetArguments(args, out parsedArguments))
					return;

				var listener = new HttpListener
				{
					Prefixes =
								{
									string.Format("http://+:{0}/{1}/",
										parsedArguments.Port,
										parsedArguments.MethodName)
								}
				};

				log.InfoFormat("Server is starting listening prefixes: {0}", string.Join(";", listener.Prefixes));

				if(parsedArguments.Async)
				{
					log.InfoFormat("Press ENTER to stop listening");
					listener.StartProcessingRequestsAsync(CreateAsyncCallback(parsedArguments), CreateAsyncCallbackOk(parsedArguments));

					Console.ReadLine();
					log.InfoFormat("Server stopped!");
				}
				else
					listener.StartProcessingRequestsSync(CreateSyncCallback(parsedArguments));
			}
			catch(Exception e)
			{
				Log.Fatal(e);
			}
		}

		private static Func<HttpListenerContext, Task> CreateAsyncCallback(ServerArguments parsedArguments)
		{
			return async (context) =>
			{
			    var timer = Stopwatch.StartNew();
			    //var time = Environment.TickCount;
			    var id = context.Request.QueryString["id"];
			    

                var currentRequestNum = Interlocked.Increment(ref RequestsCount);
				log.InfoFormat("Thread #{0} received request #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestNum, DateTime.Now.TimeOfDay);


                Cancellations[id] = new CancellationTokenSource();
                await Task.Delay(parsedArguments.MethodDuration, Cancellations[id].Token);
                //				Thread.Sleep(parsedArguments.MethodDuration);
       
                var encryptedBytes = GetBase64HashBytes(context.Request.QueryString["query"], Encoding.UTF8);
				await context.Response.OutputStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);

				log.InfoFormat("Thread #{0} sent response #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestNum,
					DateTime.Now.TimeOfDay);

			    Console.WriteLine("End running \"{0}\" in {1}", id, timer.ElapsedMilliseconds);
            };
		}



	    private static Func<HttpListenerContext, Task> CreateAsyncCallbackOk(ServerArguments parsedArguments)
	    {
	        return async (context) =>
	        {
	            var time = Environment.TickCount;
	            var id = context.Request.QueryString["id"];

                Console.WriteLine("Start cancelling \"{0}\" ", id);

	            if (Cancellations.TryGetValue(id, out var ctx))
	            {
	                ctx.Cancel();
	                Cancellations.TryRemove(id, out _);
	                var answer = Encoding.UTF8.GetBytes("STOPED");
	                await context.Response.OutputStream.WriteAsync(answer, 0, answer.Length);
                }
                Console.WriteLine("End cancelling \"{0}\" in {1}", id, Environment.TickCount - time);               
            };
	    }


        private static Action<HttpListenerContext> CreateSyncCallback(ServerArguments parsedArguments)
		{
			return context =>
			{
				var currentRequestId = Interlocked.Increment(ref RequestsCount);
				log.InfoFormat("Thread #{0} received request #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestId, DateTime.Now.TimeOfDay);
                
                Thread.Sleep(parsedArguments.MethodDuration);
    
				var encryptedBytes = GetBase64HashBytes(context.Request.QueryString["query"], Encoding.UTF8);
				context.Response.OutputStream.Write(encryptedBytes, 0, encryptedBytes.Length);

				log.InfoFormat("Thread #{0} sent response #{1} at {2}",
					Thread.CurrentThread.ManagedThreadId, currentRequestId,
					DateTime.Now.TimeOfDay);
			};
		}
	 
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

		private static byte[] GetBase64HashBytes(string query, Encoding encoding)
		{
			using(var hasher = new HMACMD5(Key))
			{
				var hash = Convert.ToBase64String(hasher.ComputeHash(encoding.GetBytes(query ?? "")));
				return encoding.GetBytes(hash);
			}
		}

		private static readonly byte[] Key = Encoding.UTF8.GetBytes("Контур.Шпора");
		private static int RequestsCount;

		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
	}
}
