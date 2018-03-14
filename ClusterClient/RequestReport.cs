namespace ClusterClient
{
    public class RequestReport
    {
        public int Time { get; private set; }
        public string Replica { get; private set; }
        public string Answer { get; private set; }

        public RequestReport()
        {
        }

        public RequestReport(string answer, string replica, int time)
        {
            Time = time;
            Answer = answer;
            Replica = replica;
        }
    }
}
