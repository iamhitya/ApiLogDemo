using ApiLogDemo.Models;
using System.Text.Json;

namespace ApiLogDemo.Services
{
    public class ApiLogService
    {
        private readonly string _logFilePath;

        public ApiLogService(IWebHostEnvironment environment)
        {
            _logFilePath = Path.Combine(environment.ContentRootPath, "Data", "apiLogs.json");
        }

        private List<ApiLog> ReadLogs()
        {
            if (!File.Exists(_logFilePath))
                return new List<ApiLog>();

            var json = File.ReadAllText(_logFilePath);
            return JsonSerializer.Deserialize<List<ApiLog>>(json) ?? new List<ApiLog>();
        }

        private void WriteLogs(List<ApiLog> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_logFilePath, json);
        }

        public void AddLog(ApiLog log)
        {
            var logs = ReadLogs();
            logs.Add(log);
            WriteLogs(logs);
        }

        public List<ApiLog> GetLogs(string? method = null)
        {
            var logs = ReadLogs();
            if (!string.IsNullOrEmpty(method))
                logs = logs.Where(l => l.Method.Equals(method, StringComparison.OrdinalIgnoreCase)).ToList();

            AssignVersion(logs);

            return logs;
        }

        #region Private Methods

        private void AssignVersion(List<ApiLog> logs)
        {
            // Group by RecordCount and assign Version based on order of distinct RecordCount
            var recordGroups = logs
                .GroupBy(l => l.RecordCount)
                .OrderBy(g => g.Key) // ensures consistent version ordering
                .ToList();

            int version = 1;
            foreach (var group in recordGroups)
            {
                foreach (var log in group)
                {
                    log.Version = version;
                }
                version++;
            }
        }

        #endregion
    }
}
