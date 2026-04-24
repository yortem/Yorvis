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
                    StartOfWeekDay = 1 // Monday
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
            return config ?? new CategoryConfig { IntervalSeconds = 1, Language = "en", IsRtl = false, StartOfDayHour = 4, StartOfWeekDay = 1 };
        }

        public async Task UpdateGlobalConfig(int interval, string lang, bool isRtl, int startOfDay, int startOfWeek)
        {
            var config = await _database.Table<CategoryConfig>().Where(x => x.Name == "_Settings").FirstOrDefaultAsync();
            if (config != null)
            {
                config.IntervalSeconds = interval;
                config.Language = lang;
                config.IsRtl = isRtl;
                config.StartOfDayHour = startOfDay;
                config.StartOfWeekDay = startOfWeek;
                await _database.UpdateAsync(config);
            }
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

        public Task<int> SaveActivity(ActivityLog log)
        {
            return _database.InsertAsync(log);
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
    }
}
