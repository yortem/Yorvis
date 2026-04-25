using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Yorvis.Models;

namespace Yorvis.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yorvis.db3");

        public DatabaseService()
        {
            _database = new SQLiteAsyncConnection(DbPath);
            _database.CreateTableAsync<ActivityLog>().Wait();
            _database.CreateTableAsync<CategoryConfig>().Wait();
            SeedDefaults().Wait();
        }

        private async Task SeedDefaults()
        {
            var count = await _database.Table<CategoryConfig>().CountAsync();
            if (count == 0)
            {
                // Global Settings stored in a dummy or first record
                await _database.InsertAsync(new CategoryConfig { 
                    Name = "_Settings", 
                    IntervalSeconds = 1, 
                    Language = "en", 
                    IsRtl = false,
                    StartOfDayHour = 4,
                    StartOfWeekDay = 1, // Monday
                    BlacklistKeywords = "incognito|private browsing"
                });

                // Top level
                var work = new CategoryConfig { Name = "Work", Keywords = "visual studio|code|terminal|outlook|וורד|אקסל", Color = "#4CAF50" };
                await _database.InsertAsync(work);

                var media = new CategoryConfig { Name = "Media", Keywords = "vlc|media player", Color = "#FF9800" };
                await _database.InsertAsync(media);

                var comms = new CategoryConfig { Name = "Communications", Keywords = "slack|teams|zoom", Color = "#2196F3" };
                await _database.InsertAsync(comms);

                // Media sub-categories
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Games", Keywords = "gta|steam|epic games", Color = "#f44336" });
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Videos", Keywords = "youtube|netflix|prime video", Color = "#e91e63" });
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Social Media", Keywords = "facebook|twitter|instagram|reddit|ווטסאפ|טלגרם", Color = "#9c27b0" });
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Music", Keywords = "spotify|apple music|itunes", Color = "#673ab7" });
            }
        }

        public async Task<CategoryConfig> GetGlobalConfig()
        {
            var config = await _database.Table<CategoryConfig>().Where(x => x.Name == "_Settings").FirstOrDefaultAsync();
            return config ?? new CategoryConfig { 
                Name = "_Settings",
                IntervalSeconds = 1, 
                Language = "en", 
                IsRtl = false, 
                StartOfDayHour = 4, 
                StartOfWeekDay = 1,
                BlacklistKeywords = "incognito|private browsing",
                Theme = "light",
                StartWithWindows = false
            };
        }

        public async Task UpdateGlobalConfig(int interval, string lang, bool isRtl, int startOfDay, int startOfWeek, string blacklistKeywords, string theme, bool startWithWindows)
        {
            var config = await _database.Table<CategoryConfig>().Where(x => x.Name == "_Settings").FirstOrDefaultAsync();
            bool isNew = false;
            if (config == null)
            {
                config = new CategoryConfig { Name = "_Settings" };
                isNew = true;
            }

            config.IntervalSeconds = interval;
            config.Language = lang;
            config.IsRtl = isRtl;
            config.StartOfDayHour = startOfDay;
            config.StartOfWeekDay = startOfWeek;
            config.BlacklistKeywords = blacklistKeywords;
            config.Theme = theme;
            config.StartWithWindows = startWithWindows;

            if (isNew)
                await _database.InsertAsync(config);
            else
                await _database.UpdateAsync(config);
        }

        public async Task<int> GetInterval()
        {
            var config = await GetGlobalConfig();
            return config.IntervalSeconds;
        }

        public Task<List<ActivityLog>> GetRecentActivities(int count = 100)
        {
            return _database.Table<ActivityLog>().OrderByDescending(x => x.Timestamp).Take(count).ToListAsync();
        }

        public async Task<int> SaveActivity(ActivityLog log)
        {
            // Check if the last log is the same and continuous (within 5 seconds gap)
            var lastLog = await _database.Table<ActivityLog>().OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            
            if (lastLog != null && 
                lastLog.ProcessName == log.ProcessName && 
                lastLog.WindowTitle == log.WindowTitle &&
                (log.Timestamp - lastLog.Timestamp).TotalSeconds <= lastLog.DurationSeconds + 5)
            {
                // Update duration: new end time - original start time
                var newEndTime = log.Timestamp.AddSeconds(log.DurationSeconds);
                lastLog.DurationSeconds = (newEndTime - lastLog.Timestamp).TotalSeconds;
                return await _database.UpdateAsync(lastLog);
            }

            return await _database.InsertAsync(log);
        }

        public Task<List<CategoryConfig>> GetCategories()
        {
            return _database.Table<CategoryConfig>().OrderBy(c => c.DisplayOrder).ToListAsync();
        }

        public Task<int> SaveCategory(CategoryConfig config)
        {
            if (config.Id != 0)
                return _database.UpdateAsync(config);
            return _database.InsertAsync(config);
        }

        public Task<int> DeleteCategory(int id)
        {
            return _database.Table<CategoryConfig>().Where(x => x.Id == id).DeleteAsync();
        }
        
        public async Task<List<ActivityLog>> GetStats(DateTime start, DateTime end)
        {
            // Ensure we use UTC for comparison
            var startUtc = start.ToUniversalTime();
            var endUtc = end.ToUniversalTime();

            return await _database.Table<ActivityLog>()
                .Where(x => x.Timestamp >= startUtc && x.Timestamp <= endUtc)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();
        }
        public async Task<int> ClearActivityLogs()
        {
            return await _database.DeleteAllAsync<ActivityLog>();
        }

        public async Task<int> CleanupLogs(int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            return await _database.Table<ActivityLog>().Where(x => x.Timestamp < cutoff).DeleteAsync();
        }

        public async Task<(long SizeBytes, int RecordCount)> GetDatabaseInfo()
        {
            try
            {
                var fileInfo = new FileInfo(DbPath);
                var size = fileInfo.Exists ? fileInfo.Length : 0;
                var count = await _database.Table<ActivityLog>().CountAsync();
                return (size, count);
            }
            catch { return (0, 0); }
        }
    }
}
