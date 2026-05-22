using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Photino.NET;
using Yorvis.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Yorvis
{
    class Program
    {
        private static NotifyIcon? _trayIcon;
        private static PhotinoWindow? _mainWindow;
        private static bool _isExiting = false;
        private static readonly string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string AppName = "Yorvis";

        private static void SetStartup(bool startWithWindows)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true)!;
                if (startWithWindows)
                {
                    string appPath = $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName}\"";
                    key.SetValue(AppName, appPath);
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry Error: {ex.Message}");
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<CategoryService>();
            services.AddSingleton<MonitoringService>();
            services.AddSingleton<DeepSeekService>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Start Monitoring
            var monitor = serviceProvider.GetRequiredService<MonitoringService>();
            monitor.Start();

            // Setup Icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
            Icon appIcon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;

            // Setup Tray Icon
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = appIcon;
            _trayIcon.Text = "Yorvis - PC Time Monitor";
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApp(monitor));
            _trayIcon.ContextMenuStrip = contextMenu;

            // Window title, the URL to load, and the initial size
            string windowTitle = "Yorvis - PC Time Monitor";
            
            _mainWindow = new PhotinoWindow()
                .SetTitle(windowTitle)
                .SetIconFile(iconPath)
                .SetUseOsDefaultSize(false)
                .SetUseOsDefaultLocation(false) // Required when SetChromeless(true) is used on Windows
                .SetSize(1400, 900)
                .Center()
                .SetChromeless(true) // Frameless window for custom design
                .SetContextMenuEnabled(false) // Disable default right-click menu
                .RegisterWebMessageReceivedHandler((object sender, string message) =>
                {
                    var window = (PhotinoWindow)sender;
                    ProcessMessage(window, message, serviceProvider);
                })
                .RegisterWindowClosingHandler((object sender, EventArgs e) =>
                {
                    if (!_isExiting && _mainWindow != null)
                    {
                        Win32Interop.ShowWindow(_mainWindow.WindowHandle, Win32Interop.SW_HIDE);
                        return true; // Cancel close
                    }
                    return false; // Allow close
                })
                .Load("wwwroot/index.html");

            _mainWindow.WaitForClose();
        }

        static void ShowWindow()
        {
            if (_mainWindow != null)
            {
                Win32Interop.ShowWindow(_mainWindow.WindowHandle, Win32Interop.SW_SHOW);
                _mainWindow.SetMinimized(false);
            }
        }

        static void ExitApp(MonitoringService monitor)
        {
            _isExiting = true;
            monitor.Stop();
            if (_trayIcon != null) _trayIcon.Visible = false;
            _mainWindow?.Close();
            Application.Exit();
        }

        private static bool _isMaximized = false;

        static async void ProcessMessage(PhotinoWindow window, string message, IServiceProvider sp)
        {
            try {
                var doc = JsonDocument.Parse(message);
                var action = doc.RootElement.GetProperty("action").GetString();

                var db = sp.GetRequiredService<DatabaseService>();

                if (action == "getStats")
                {
                    List<Yorvis.Models.ActivityLog> logs;
                    if (doc.RootElement.TryGetProperty("start", out var startEl) && doc.RootElement.TryGetProperty("end", out var endEl))
                    {
                        DateTime start = DateTime.Parse(startEl.GetString()!).ToUniversalTime();
                        DateTime end = DateTime.Parse(endEl.GetString()!).ToUniversalTime();
                        logs = await db.GetStats(start, end);
                    }
                    else
                    {
                        logs = await db.GetRecentActivities(1000);
                    }
                    
                    var categories = await db.GetCategories();
                    
                    // Get current live status
                    string currentProcess = Win32Interop.GetActiveProcessName();
                    string currentTitle = Win32Interop.GetActiveWindowTitle();
                    string currentCategory = sp.GetRequiredService<CategoryService>().GetCategory(currentProcess, currentTitle);

                    var response = new {
                        action = "statsData",
                        logs = logs,
                        categories = categories,
                        current = new {
                            process = currentProcess,
                            title = currentTitle,
                            category = currentCategory
                        }
                    };
                    window.SendWebMessage(JsonSerializer.Serialize(response));
                }
                else if (action == "getSettings")
                {
                    var config = await db.GetGlobalConfig();
                    var dbInfo = await db.GetDatabaseInfo();
                    
                    var response = new {
                        action = "settingsData",
                        interval = config.IntervalSeconds,
                        language = config.Language,
                        isRtl = config.IsRtl,
                        theme = config.Theme ?? "light",
                        startWithWindows = config.StartWithWindows,
                        startOfDay = config.StartOfDayHour,
                        startOfWeek = config.StartOfWeekDay,
                        blacklistKeywords = config.BlacklistKeywords,
                        smartWakeDetection = config.SmartWakeDetection,
                        aiApiKey = config.AiApiKey ?? "",
                        aiProvider = config.AiProvider ?? "deepseek",
                        aiModel = config.AiModel ?? "deepseek-chat",
                        dbSize = dbInfo.SizeBytes,
                        dbRecords = dbInfo.RecordCount
                    };
                    
                    window.SendWebMessage(JsonSerializer.Serialize(response));
                }
                else if (action == "updateSettings")
                {
                    int interval = doc.RootElement.GetProperty("interval").GetInt32();
                    string lang = doc.RootElement.GetProperty("language").GetString() ?? "en";
                    bool isRtl = doc.RootElement.GetProperty("isRtl").GetBoolean();
                    string theme = doc.RootElement.TryGetProperty("theme", out var t) ? t.GetString() ?? "light" : "light";
                    bool startWithWindows = doc.RootElement.TryGetProperty("startWithWindows", out var sww) && sww.GetBoolean();
                    int startOfDay = doc.RootElement.GetProperty("startOfDay").GetInt32();
                    int startOfWeek = doc.RootElement.GetProperty("startOfWeek").GetInt32();
                    string blacklist = doc.RootElement.TryGetProperty("blacklistKeywords", out var bl) ? bl.GetString() ?? "" : "";
                    bool smartWake = doc.RootElement.TryGetProperty("smartWakeDetection", out var sw) && sw.GetBoolean();
                    string aiApiKey = doc.RootElement.TryGetProperty("aiApiKey", out var aak) ? aak.GetString() ?? "" : "";
                    string aiProvider = doc.RootElement.TryGetProperty("aiProvider", out var ap) ? ap.GetString() ?? "deepseek" : "deepseek";
                    string aiModel = doc.RootElement.TryGetProperty("aiModel", out var am) ? am.GetString() ?? "deepseek-chat" : "deepseek-chat";
                    
                    await db.UpdateGlobalConfig(interval, lang, isRtl, startOfDay, startOfWeek, blacklist, theme, startWithWindows, smartWake, aiApiKey, aiProvider, aiModel);
                    SetStartup(startWithWindows);
                }
                else if (action == "saveCategory")
                {
                    var catJson = doc.RootElement.GetProperty("category");
                    var config = new Yorvis.Models.CategoryConfig {
                        Id = catJson.GetProperty("Id").GetInt32(),
                        Name = catJson.GetProperty("Name").GetString(),
                        Keywords = catJson.GetProperty("Keywords").GetString(),
                        Color = catJson.GetProperty("Color").GetString(),
                        ProductivityType = catJson.TryGetProperty("ProductivityType", out var pt) ? pt.GetString() : "Neutral",
                        DisplayOrder = catJson.TryGetProperty("DisplayOrder", out var order) ? order.GetInt32() : 0,
                        ParentId = catJson.TryGetProperty("ParentId", out var pid) && pid.ValueKind != JsonValueKind.Null ? pid.GetInt32() : null
                    };
                    await db.SaveCategory(config);
                    await sp.GetRequiredService<CategoryService>().RefreshCategories();
                }
                else if (action == "deleteCategory")
                {
                    int id = doc.RootElement.GetProperty("id").GetInt32();
                    await db.DeleteCategory(id);
                    await sp.GetRequiredService<CategoryService>().RefreshCategories();
                }
                else if (action == "windowControl")
                {
                    var command = doc.RootElement.GetProperty("command").GetString();
                    if (command == "minimize") window.SetMinimized(true);
                    else if (command == "maximize") 
                    {
                        _isMaximized = !_isMaximized;
                        window.SetMaximized(_isMaximized);
                    }
                    else if (command == "close") window.Close();
                }
                else if (action == "resetData")
                {
                    await db.ClearActivityLogs();
                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "dataResetSuccess" }));
                }
                else if (action == "cleanupLogs")
                {
                    int days = doc.RootElement.GetProperty("days").GetInt32();
                    int deleted = await db.CleanupLogs(days);
                    var dbInfo = await db.GetDatabaseInfo();
                    window.SendWebMessage(JsonSerializer.Serialize(new
                    {
                        action = "cleanupSuccess",
                        deletedCount = deleted,
                        dbSize = dbInfo.SizeBytes,
                        dbRecords = dbInfo.RecordCount
                    }));
                }
                else if (action == "exportData")
                {
                    var json = await db.ExportData();
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "JSON files (*.json)|*.json",
                        FileName = $"yorvis_export_{DateTime.Now:yyyyMMdd_HHmm}.json",
                        Title = "Export Yorvis Data"
                    };

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveDialog.FileName, json);
                        window.SendWebMessage(JsonSerializer.Serialize(new { action = "exportSuccess", path = saveDialog.FileName }));
                    }
                }
                else if (action == "importData")
                {
                    var openDialog = new OpenFileDialog
                    {
                        Filter = "JSON files (*.json)|*.json",
                        Title = "Import Yorvis Data"
                    };

                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        var json = File.ReadAllText(openDialog.FileName);
                        int count = await db.ImportData(json);
                        var dbInfo = await db.GetDatabaseInfo();
                        window.SendWebMessage(JsonSerializer.Serialize(new
                        {
                            action = "importSuccess",
                            importedCount = count,
                            dbSize = dbInfo.SizeBytes,
                            dbRecords = dbInfo.RecordCount
                        }));
                    }
                }
                else if (action == "sendAiMessage")
                {
                    var config = await db.GetGlobalConfig();
                    if (string.IsNullOrWhiteSpace(config.AiApiKey))
                    {
                        window.SendWebMessage(JsonSerializer.Serialize(new { action = "aiError", error = "API key not configured. Set it in Settings > AI." }));
                        return;
                    }

                    string userMsg = doc.RootElement.GetProperty("message").GetString() ?? "";
                    string processName = Win32Interop.GetActiveProcessName();
                    string windowTitle = Win32Interop.GetActiveWindowTitle();
                    string category = sp.GetRequiredService<CategoryService>().GetCategory(processName, windowTitle);
                    var projects = await db.GetProjects();

                    // Filter to single project if specified
                    string projectFilter = doc.RootElement.TryGetProperty("projectId", out var pid) ? pid.GetString() ?? "" : "";
                    if (projectFilter != "" && projectFilter != "general" && int.TryParse(projectFilter, out int projectId))
                    {
                        projects = projects.Where(p => p.Id == projectId).ToList();
                    }

                    string systemPrompt = DeepSeekService.BuildSystemPrompt(projects.ToArray(), processName, windowTitle, category);
                    var ai = sp.GetRequiredService<DeepSeekService>();

                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "aiThinking" }));
                    string reply = await ai.Chat(config.AiApiKey, config.AiModel ?? "deepseek-chat", systemPrompt, userMsg);
                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "aiResponse", content = reply }));
                }
                else if (action == "getProjects")
                {
                    var projects = await db.GetProjects();
                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "projectsData", projects = projects }));
                }
                else if (action == "saveProject")
                {
                    var pJson = doc.RootElement.GetProperty("project");
                    int id = pJson.GetProperty("Id").GetInt32();
                    
                    // Fetch existing to merge (so partial updates don't erase fields)
                    var existing = (await db.GetProjects()).FirstOrDefault(p => p.Id == id);
                    var project = existing ?? new Yorvis.Models.Project { Id = id };
                    
                    if (pJson.TryGetProperty("Name", out var nameJ)) project.Name = nameJ.GetString();
                    if (pJson.TryGetProperty("Url", out var urlJ)) project.Url = urlJ.GetString();
                    if (pJson.TryGetProperty("SitemapUrl", out var sJ)) project.SitemapUrl = sJ.GetString();
                    if (pJson.TryGetProperty("Keywords", out var kJ)) project.Keywords = kJ.GetString();
                    if (pJson.TryGetProperty("Notes", out var nJ)) project.Notes = nJ.GetString();
                    
                    await db.SaveProject(project);
                    var projects = await db.GetProjects();
                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "projectsData", projects = projects }));
                }
                else if (action == "deleteProject")
                {
                    int id = doc.RootElement.GetProperty("id").GetInt32();
                    await db.DeleteProject(id);
                    var projects = await db.GetProjects();
                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "projectsData", projects = projects }));
                }
                else if (action == "fillProjectDetails")
                {
                    var config = await db.GetGlobalConfig();
                    if (string.IsNullOrWhiteSpace(config.AiApiKey))
                    {
                        window.SendWebMessage(JsonSerializer.Serialize(new { action = "fillProjectError", error = "API key not configured." }));
                        return;
                    }

                    int projectId = doc.RootElement.GetProperty("projectId").GetInt32();
                    string url = doc.RootElement.GetProperty("url").GetString() ?? "";
                    string projectName = doc.RootElement.GetProperty("projectName").GetString() ?? "";

                    var ai = sp.GetRequiredService<DeepSeekService>();
                    string prompt = $"You are a web research assistant. Given the website URL \"{url}\" and project name \"{projectName}\", suggest:\n1. Relevant SEO keywords (comma-separated, 5-10 keywords)\n2. A brief 1-sentence description of what this website/project does\n\nRespond in this exact format:\nKEYWORDS: keyword1, keyword2, keyword3\nDESCRIPTION: A brief description here.";
                    
                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "fillProjectThinking", projectId }));
                    string reply = await ai.Chat(config.AiApiKey, config.AiModel ?? "deepseek-chat", "You analyze websites and extract SEO keywords and descriptions.", prompt);

                    string keywords = "";
                    string description = "";

                    foreach (var line in reply.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("KEYWORDS:", StringComparison.OrdinalIgnoreCase))
                            keywords = trimmed.Substring("KEYWORDS:".Length).Trim();
                        else if (trimmed.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                            description = trimmed.Substring("DESCRIPTION:".Length).Trim();
                    }

                    // Update project
                    var projects = await db.GetProjects();
                    var project = projects.FirstOrDefault(p => p.Id == projectId);
                    if (project != null)
                    {
                        if (!string.IsNullOrWhiteSpace(keywords)) project.Keywords = keywords;
                        if (!string.IsNullOrWhiteSpace(description)) project.Notes = description;
                        await db.SaveProject(project);
                        projects = await db.GetProjects();
                    }

                    window.SendWebMessage(JsonSerializer.Serialize(new { action = "fillProjectDone", projectId, keywords, description, projects }));
                }
                else if (action == "saveChatMessages")
                {
                    var msgsJson = doc.RootElement.GetProperty("messages");
                    var messages = new List<Yorvis.Models.ChatMessage>();
                    foreach (var m in msgsJson.EnumerateArray())
                    {
                        messages.Add(new Yorvis.Models.ChatMessage
                        {
                            ProjectId = m.GetProperty("projectId").GetString() ?? "general",
                            Role = m.GetProperty("role").GetString(),
                            Content = m.GetProperty("content").GetString(),
                        });
                    }
                    var saved = await db.SaveChatMessages(messages);
                    window.SendWebMessage(JsonSerializer.Serialize(new
                    {
                        action = "chatMessagesSaved",
                        messages = saved.Select(m => new { m.Id, m.ProjectId, m.Role, m.Content }).ToList()
                    }));
                }
                else if (action == "loadChatHistory")
                {
                    string projectId = doc.RootElement.GetProperty("projectId").GetString() ?? "general";
                    int beforeId = doc.RootElement.TryGetProperty("beforeId", out var bId) ? bId.GetInt32() : 0;
                    int limit = doc.RootElement.TryGetProperty("limit", out var lim) ? lim.GetInt32() : 30;
                    var messages = await db.LoadChatHistory(projectId, beforeId, limit);
                    messages.Reverse(); // Return in chronological order
                    var hasMore = messages.Count == limit;
                    window.SendWebMessage(JsonSerializer.Serialize(new
                    {
                        action = "chatHistory",
                        projectId,
                        messages = messages.Select(m => new { m.Id, m.Role, m.Content }).ToList(),
                        hasMore
                    }));
                }
                else if (action == "getAiContext")
                {
                    string processName = Win32Interop.GetActiveProcessName();
                    string windowTitle = Win32Interop.GetActiveWindowTitle();
                    string category = sp.GetRequiredService<CategoryService>().GetCategory(processName, windowTitle);
                    var projects = await db.GetProjects();
                    window.SendWebMessage(JsonSerializer.Serialize(new
                    {
                        action = "aiContext",
                        process = processName,
                        title = windowTitle,
                        category = category,
                        projects = projects
                    }));
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }
    }
}
