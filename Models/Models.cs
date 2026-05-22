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
        public bool SmartWakeDetection { get; set; }
        public string? AiApiKey { get; set; }
        public string? AiProvider { get; set; }
        public string? AiModel { get; set; }
    }

    public class Project
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? SitemapUrl { get; set; }
        public string? Keywords { get; set; }
        public string? Notes { get; set; }
    }

    public class ChatMessage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? ProjectId { get; set; }
        public string? Role { get; set; }
        public string? Content { get; set; }
        public string? CreatedAt { get; set; }
    }
}
