namespace ApiLogDemo.Models
{
    public class ApiLog
    {
        public string Method { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double RecordCount { get; set; } = 0;
        public double ExecutionTimeMs { get; set; }
        public int? Version { get; set; }
    }
}
