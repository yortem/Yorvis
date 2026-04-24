using System;
using System.Threading;
using System.Threading.Tasks;
using Yorvis.Models;

namespace Yorvis.Services
{
    public class MonitoringService
    {
        private readonly DatabaseService _db;
        private readonly CategoryService _categoryService;
        private CancellationTokenSource? _cts;
        private string? _lastProcessName;
        private string? _lastWindowTitle;
        private DateTime _lastStartTime;

        public MonitoringService(DatabaseService db, CategoryService categoryService)
        {
            _db = db;
            _categoryService = categoryService;
            _lastProcessName = "";
            _lastWindowTitle = "";
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            _lastStartTime = DateTime.UtcNow;
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    int intervalSeconds = 1; // Force 1 second for high precision as requested
                    string currentProcess = Win32Interop.GetActiveProcessName();
                    string currentTitle = Win32Interop.GetActiveWindowTitle();

                    bool windowChanged = currentProcess != _lastProcessName || currentTitle != _lastWindowTitle;
                    var durationSinceStart = (DateTime.UtcNow - _lastStartTime).TotalSeconds;

                    // Save if window changed OR if activity has been ongoing for > 60 seconds (Heartbeat)
                    if (windowChanged || durationSinceStart >= 60)
                    {
                        await SavePreviousActivity();

                        _lastProcessName = currentProcess;
                        _lastWindowTitle = currentTitle;
                        _lastStartTime = DateTime.UtcNow;
                    }
                    
                    await Task.Delay(intervalSeconds * 1000, token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitor Error: {ex.Message}");
                    await Task.Delay(5000, token);
                }
            }

            // Final save on stop
            await SavePreviousActivity();
        }

        private async Task SavePreviousActivity()
        {
            if (string.IsNullOrEmpty(_lastProcessName)) return;

            var duration = (DateTime.UtcNow - _lastStartTime).TotalSeconds;
            if (duration < 1) return; // Ignore very short durations

            var category = _categoryService.GetCategory(_lastProcessName, _lastWindowTitle);

            var log = new ActivityLog
            {
                Timestamp = _lastStartTime,
                ProcessName = _lastProcessName,
                WindowTitle = _lastWindowTitle,
                Category = category,
                DurationSeconds = duration
            };

            await _db.SaveActivity(log);
        }
    }
}
