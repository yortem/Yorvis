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

            // Pass 1: Check for WindowTitle matches (Higher Priority/Specificity)
            foreach (var cat in _categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Keywords)) continue;
                try
                {
                    if (Regex.IsMatch(windowTitle ?? "", cat.Keywords, RegexOptions.IgnoreCase))
                    {
                        return cat.Name ?? "Uncategorized";
                    }
                }
                catch { }
            }

            // Pass 2: Check for ProcessName matches (Lower Priority/Generality)
            foreach (var cat in _categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Keywords)) continue;
                try
                {
                    if (Regex.IsMatch(processName, cat.Keywords, RegexOptions.IgnoreCase))
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
    }
}
