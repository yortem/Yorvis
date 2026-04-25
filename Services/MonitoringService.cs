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
        private string _lastProcessName = string.Empty;
        private string _lastWindowTitle = string.Empty;
        private DateTime _lastStartTime;

        public MonitoringService(DatabaseService db, CategoryService categoryService)
        {
            _db = db;
            _categoryService = categoryService;
            _lastProcessName = string.Empty;
            _lastWindowTitle = string.Empty;
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

        private string _pendingProcess = string.Empty;
        private string _pendingTitle = string.Empty;
        private DateTime _pendingStartTime;

        private async Task MonitorLoop(CancellationToken token)
        {
            _lastStartTime = DateTime.UtcNow;
            _lastProcessName = string.Empty;
            _lastWindowTitle = string.Empty;
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var (currentProcess, currentTitle) = Win32Interop.GetActiveWindowInfo();
                    string myProcess = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                    var category = _categoryService.GetCategory(currentProcess, currentTitle);
                    var idleTime = Win32Interop.GetIdleTime();
                    
                    bool isMedia = category.Contains("Media", StringComparison.OrdinalIgnoreCase) || 
                                   category.Contains("Video", StringComparison.OrdinalIgnoreCase) ||
                                   category.Contains("YouTube", StringComparison.OrdinalIgnoreCase);

                    bool isAfk = idleTime.TotalSeconds > 180 && !isMedia;

                    if (isAfk)
                    {
                        // Reset any pending window change on AFK
                        _pendingProcess = string.Empty;
                        
                        if (!string.IsNullOrEmpty(_lastProcessName))
                        {
                            var duration = (DateTime.UtcNow - _lastStartTime).TotalSeconds - idleTime.TotalSeconds;
                            if (duration > 2) await SavePreviousActivity(duration);
                            _lastProcessName = string.Empty;
                            _lastWindowTitle = string.Empty;
                        }
                        await Task.Delay(5000, token);
                        continue;
                    }

                    if (currentProcess == myProcess) 
                    {
                        await Task.Delay(1000, token);
                        continue; 
                    }

                    bool windowMatchesCurrent = currentProcess == _lastProcessName && currentTitle == _lastWindowTitle;

                    if (windowMatchesCurrent)
                    {
                        // User is back to original window or stayed there
                        _pendingProcess = string.Empty; // Cancel any pending change
                        
                        var durationSinceCommit = (DateTime.UtcNow - _lastStartTime).TotalSeconds;
                        if (durationSinceCommit >= 30) // Heartbeat commit
                        {
                            await SavePreviousActivity();
                            _lastStartTime = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Window is different from current committed window
                        bool matchesPending = currentProcess == _pendingProcess && currentTitle == _pendingTitle;
                        
                        if (!matchesPending)
                        {
                            // Start/Update pending buffer
                            _pendingProcess = currentProcess;
                            _pendingTitle = currentTitle;
                            _pendingStartTime = DateTime.UtcNow;
                        }
                        else
                        {
                            // Still on the same pending window - check if we reached 5s threshold
                            var pendingDuration = (DateTime.UtcNow - _pendingStartTime).TotalSeconds;
                            if (pendingDuration >= 5)
                            {
                                // COMMIT CHANGE
                                if (!string.IsNullOrEmpty(_lastProcessName))
                                {
                                    // Save the PREVIOUS window up until the moment we switched to the PENDING one
                                    var previousDuration = (_pendingStartTime - _lastStartTime).TotalSeconds;
                                    await SavePreviousActivity(previousDuration);
                                }
                                
                                _lastProcessName = _pendingProcess;
                                _lastWindowTitle = _pendingTitle;
                                _lastStartTime = _pendingStartTime;
                                _pendingProcess = string.Empty;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitor Error: {ex.Message}");
                }

                await Task.Delay(1000, token);
            }

            await SavePreviousActivity();
        }

        private async Task SavePreviousActivity(double? forcedDuration = null)
        {
            if (string.IsNullOrEmpty(_lastProcessName)) return;

            var duration = forcedDuration ?? (DateTime.UtcNow - _lastStartTime).TotalSeconds;
            if (duration < 2) return; // Ignore very short durations (< 2s)

            var category = _categoryService.GetCategory(_lastProcessName, _lastWindowTitle);

            // Blacklist check
            try 
            {
                var config = await _db.GetGlobalConfig();
                if (!string.IsNullOrEmpty(config.BlacklistKeywords))
                {
                    var patterns = config.BlacklistKeywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in patterns)
                    {
                        var pattern = p.Trim();
                        if (string.IsNullOrEmpty(pattern)) continue;
                        
                        if (_lastProcessName.Contains(pattern, StringComparison.OrdinalIgnoreCase) || 
                            _lastWindowTitle.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            return; // Don't save blacklisted activity
                        }
                    }
                }
            }
            catch { /* Ignore config errors, proceed to save */ }

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
