using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Photino.NET;
using Yorvis.Services;

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
                    
                    await db.UpdateGlobalConfig(interval, lang, isRtl, startOfDay, startOfWeek, blacklist, theme, startWithWindows);
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
                    window.SendWebMessage(JsonSerializer.Serialize(new { 
                        action = "cleanupSuccess", 
                        deletedCount = deleted,
                        dbSize = dbInfo.SizeBytes,
                        dbRecords = dbInfo.RecordCount
                    }));
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }
    }
}
