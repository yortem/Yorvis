using SQLite;
using System;

namespace Yorvis.Models
{
    public class ActivityLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ProcessName { get; set; }
        public string? WindowTitle { get; set; }
        public string? Category { get; set; }
        public double DurationSeconds { get; set; }
    }

    public class CategoryConfig
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string? Name { get; set; }
        public string? Keywords { get; set; }
        public string? Color { get; set; }
        public string? ProductivityType { get; set; } // Productive, Neutral, Leisure
        public int IntervalSeconds { get; set; }
        public string? Language { get; set; }
        public bool IsRtl { get; set; }
        public int StartOfDayHour { get; set; }
        public int StartOfWeekDay { get; set; }
        public int DisplayOrder { get; set; } 
        public string? BlacklistKeywords { get; set; }
        public string? Theme { get; set; }
        public bool StartWithWindows { get; set; }
    }
}
