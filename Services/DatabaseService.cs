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
            _database.CreateTableAsync<Project>().Wait();
            _database.CreateTableAsync<ChatMessage>().Wait();
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
                    BlacklistKeywords = "incognito|private browsing",
                    SmartWakeDetection = false
                });

                // Top level
                var work = new CategoryConfig { Name = "Work", Keywords = "visual studio|code|terminal|outlook|וורד|אקסל", Color = "#4CAF50", ProductivityType = "Productive" };
                await _database.InsertAsync(work);

                await _database.InsertAsync(new CategoryConfig { ParentId = work.Id, Name = "SEO", Keywords = "ahrefs|semrush|search console|analytics|screaming frog|yoast", Color = "#8BC34A", ProductivityType = "Productive" });

                var media = new CategoryConfig { Name = "Media", Keywords = "vlc|media player", Color = "#FF9800", ProductivityType = "Leisure" };
                await _database.InsertAsync(media);

                var comms = new CategoryConfig { Name = "Communications", Keywords = "slack|teams|zoom", Color = "#2196F3", ProductivityType = "Neutral" };
                await _database.InsertAsync(comms);

                // Media sub-categories
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Games", Keywords = "gta|steam|epic games", Color = "#f44336", ProductivityType = "Leisure" });
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Videos", Keywords = "youtube|netflix|prime video", Color = "#e91e63", ProductivityType = "Leisure" });
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Social Media", Keywords = "facebook|twitter|instagram|reddit|ווטסאפ|טלגרם", Color = "#9c27b0", ProductivityType = "Leisure" });
                await _database.InsertAsync(new CategoryConfig { ParentId = media.Id, Name = "Music", Keywords = "spotify|apple music|itunes", Color = "#673ab7", ProductivityType = "Leisure" });
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
                StartWithWindows = false,
                SmartWakeDetection = false,
                AiProvider = "deepseek",
                AiModel = "deepseek-chat"
            };
        }

        public async Task UpdateGlobalConfig(int interval, string lang, bool isRtl, int startOfDay, int startOfWeek, string blacklistKeywords, string theme, bool startWithWindows, bool smartWakeDetection = false, string? aiApiKey = null, string? aiProvider = null, string? aiModel = null)
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
            config.SmartWakeDetection = smartWakeDetection;
            if (aiApiKey != null) config.AiApiKey = aiApiKey;
            if (aiProvider != null) config.AiProvider = aiProvider;
            if (aiModel != null) config.AiModel = aiModel;

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

        public async Task<string> ExportData()
        {
            var logs = await _database.Table<ActivityLog>().ToListAsync();
            var categories = await _database.Table<CategoryConfig>().ToListAsync();
            var data = new { Logs = logs, Categories = categories };
            return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<int> ImportData(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<DataImportModel>(json);
                if (data == null) return 0;

                int count = 0;
                if (data.Categories != null)
                {
                    foreach (var cat in data.Categories)
                    {
                        if (cat.Name == "_Settings") continue; // Skip settings
                        
                        // Try to find by name first to avoid duplicates
                        var existing = await _database.Table<CategoryConfig>().Where(x => x.Name == cat.Name).FirstOrDefaultAsync();
                        if (existing == null)
                        {
                            cat.Id = 0; // Let SQLite generate new ID
                            await _database.InsertAsync(cat);
                            count++;
                        }
                    }
                }

                if (data.Logs != null)
                {
                    foreach (var log in data.Logs)
                    {
                        log.Id = 0; // New ID
                        await _database.InsertAsync(log);
                        count++;
                    }
                }
                return count;
            }
            catch { return -1; }
        }

        public Task<List<Project>> GetProjects()
        {
            return _database.Table<Project>().ToListAsync();
        }

        public Task<int> SaveProject(Project project)
        {
            if (project.Id != 0)
                return _database.UpdateAsync(project);
            return _database.InsertAsync(project);
        }

        public Task<int> DeleteProject(int id)
        {
            return _database.Table<Project>().Where(x => x.Id == id).DeleteAsync();
        }

        public Task<List<ChatMessage>> LoadChatHistory(string projectId, int beforeId = 0, int limit = 30)
        {
            if (beforeId > 0)
                return _database.Table<ChatMessage>()
                    .Where(x => x.ProjectId == projectId && x.Id < beforeId)
                    .OrderByDescending(x => x.Id)
                    .Take(limit)
                    .ToListAsync();
            else
                return _database.Table<ChatMessage>()
                    .Where(x => x.ProjectId == projectId)
                    .OrderByDescending(x => x.Id)
                    .Take(limit)
                    .ToListAsync();
        }

        public async Task<List<ChatMessage>> SaveChatMessages(List<ChatMessage> messages)
        {
            var saved = new List<ChatMessage>();
            foreach (var msg in messages)
            {
                msg.CreatedAt = DateTime.UtcNow.ToString("o");
                await _database.InsertAsync(msg);
                saved.Add(msg);
            }
            return saved;
        }

        private class DataImportModel
        {
            public List<ActivityLog>? Logs { get; set; }
            public List<CategoryConfig>? Categories { get; set; }
        }
    }
}
