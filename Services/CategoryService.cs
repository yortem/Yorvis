using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yorvis.Models;

namespace Yorvis.Services
{
    public class CategoryService
    {
        private readonly DatabaseService _db;
        private List<CategoryConfig>? _categories;

        public CategoryService(DatabaseService db)
        {
            _db = db;
            _categories = new List<CategoryConfig>();
            RefreshCategories().Wait();
        }

        public async Task RefreshCategories()
        {
            _categories = await _db.GetCategories();
        }

        public string GetCategory(string processName, string windowTitle)
        {
            if (_categories == null) return "Uncategorized";

            // 0. Blacklist check (Priority)
            var config = _categories.FirstOrDefault(c => c.Name == "_Settings");
            if (config != null && !string.IsNullOrWhiteSpace(config.BlacklistKeywords))
            {
                try
                {
                    var pattern = PreparePattern(config.BlacklistKeywords);
                    if (Regex.IsMatch(windowTitle ?? "", pattern, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(processName ?? "", pattern, RegexOptions.IgnoreCase))
                    {
                        return "Excluded";
                    }
                }
                catch { }
            }

            // Pass 1: Check for WindowTitle matches (Higher Priority/Specificity)
            foreach (var cat in _categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Keywords) || cat.Name == "_Settings") continue;
                try
                {
                    var pattern = PreparePattern(cat.Keywords);
                    if (Regex.IsMatch(windowTitle ?? "", pattern, RegexOptions.IgnoreCase))
                    {
                        return cat.Name ?? "Uncategorized";
                    }
                }
                catch { }
            }

            // Pass 2: Check for ProcessName matches (Lower Priority/Generality)
            foreach (var cat in _categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Keywords) || cat.Name == "_Settings") continue;
                try
                {
                    var pattern = PreparePattern(cat.Keywords);
                    if (Regex.IsMatch(processName, pattern, RegexOptions.IgnoreCase))
                    {
                        return cat.Name ?? "Uncategorized";
                    }
                }
                catch { }
            }

            // Default fallback logic
            bool isExplorer = (processName ?? "").Contains("explorer", StringComparison.OrdinalIgnoreCase);
            bool isEmptyTitle = string.IsNullOrWhiteSpace(windowTitle);

            if (isExplorer || isEmptyTitle) return "Desktop";

            return "Uncategorized";
        }

        private string PreparePattern(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords)) return string.Empty;

            // Split by | to support multiple keywords
            var parts = keywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var escapedParts = parts.Select(p => 
            {
                var trimmed = p.Trim();
                // If it looks like a complex regex (contains *, +, ?, (, [, ^, $), we keep it as is
                // but we specifically escape '.' if it's not followed by a wildcard or similar
                // Actually, the simplest and most robust way to fulfill "ensure keywords are escaped if not intended as regex"
                // is to escape EVERYTHING unless we detect specific intent.
                // But for this project, let's just escape periods and common non-regex-intent chars.
                
                if (trimmed.Contains("*") || trimmed.Contains("+") || trimmed.Contains("?") || 
                    trimmed.Contains("(") || trimmed.Contains("[") || trimmed.Contains("^") || trimmed.Contains("$"))
                {
                    return trimmed; // Keep as regex
                }
                
                return Regex.Escape(trimmed);
            });

            return string.Join("|", escapedParts);
        }
    }
}
