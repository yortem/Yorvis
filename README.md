# Yorvis: High-Performance PC Activity & Productivity Monitor

Yorvis (Yortem + Jarvis) is a lightweight alternative to ActivityWatch, designed specifically for Windows. It operates as a "Ghost Manager"—monitoring your PC activity silently in the background, providing deep insights through a modern dashboard, and staying out of your way when you're in deep work or gaming.

Built with **.NET 8**, **Photino**, and **SQLite**, Yorvis offers privacy-first, local-only tracking with rich visualizations and intelligent noise reduction.

---

## 🚀 Key Features

### 🕒 Advanced Activity Tracking
*   **Precision Monitoring**: Samples foreground window data (Process Name & Window Title) every 1 second via Win32 Interop.
*   **Bounce Protection**: Implements a 5-second buffer for window switches. If you accidentally switch windows or check a notification briefly, Yorvis maintains the continuity of your primary session.
*   **AFK Detection**: Intelligent idle detection with "Media Awareness"—your movie or music sessions won't be marked as idle.
*   **Launch on Startup**: Option to automatically start Yorvis when Windows boots, managed directly from the Settings page.

### 📊 Rich Visualizations
*   **Tabbed Timeline**: A unified interface with three specialized views:
    *   **Map**: A pixel-perfect, high-density visualization of your day using absolute time-mapping.
    *   **Stream**: A chronological feed of all activities with smart grouping and search.
    *   **Daily Graph**: A high-level stacked bar chart showing daily activity totals divided by category.
*   **Productivity Score**: Real-time 0-100 scoring based on the ratio of productive work vs. leisure time.
*   **Interactive Analytics**: Real-time distribution charts and top-category summaries for different time ranges (Day, Week, Month, or Custom).

### 🎨 Modern UI & Experience
*   **Productivity States**: Define categories as **Productive**, **Neutral**, or **Leisure** to drive intelligent scoring.
*   **Dark Theme Support**: Full support for Dark and Light themes with an optimized color palette for reduced eye strain.
*   **Open Sans Hebrew**: Native high-quality typography for both English and Hebrew text.
*   **System Tray Integration**: Runs silently in the background. Close the window to minimize to the tray.

### 🛡️ Privacy & Exclusions
*   **Smart Blacklist**: Define keywords or Regex patterns to exclude specific windows. Excluded items are saved under a dedicated "Excluded" category to preserve timeline continuity without compromising privacy.
*   **Local-Only Storage**: All data is stored in a local SQLite database (`yorvis.db3`).

### 🧠 Intelligent Categorization
*   **Hierarchical Priority**: Drag-and-drop category management with top-to-bottom rule matching.
*   **Auto-Escaping Keywords**: Intelligent logic that automatically escapes special characters (like `.`) unless a complex Regex pattern is detected, preventing common matching errors.
*   **Quick Categorize**: Right-click any activity in the logs to instantly add it to a category via a context menu.
*   **Retroactive Updates**: Changing a rule instantly updates all historical log categorizations.

---

## 📐 Internal Logic & Calculations

### 📈 Productivity Scoring
The score is calculated based on clear signals, excluding "Neutral" or "Uncategorized" time:
`Score = (Productive Time / (Productive Time + Leisure Time)) * 100`

### 💤 AFK (Away From Keyboard) Logic
Yorvis uses the `GetLastInputInfo` Win32 API to detect inactivity:
1.  **Threshold**: 180 seconds (3 minutes) of no input triggers AFK status.
2.  **Correction**: When AFK is detected, the system saves the previous activity but subtracts the 3-minute threshold to ensure the idle time isn't incorrectly attributed to work.

### 🔄 Bounce Protection Logic
To prevent "fragmented logs" from quick window switching:
- When you switch from Window A to Window B, Yorvis starts a **5-second pending buffer**.
- If you switch back to Window A within those 5 seconds, the switch to B is **discarded**.
- Window A's session continues as if the interruption never happened.

### 🌓 RTL & Localization
- **RTL First**: Full native support for Right-to-Left layouts (Hebrew) and Left-to-Right (English).
- **Full Translation**: Every UI element, description, and dynamic message is fully localized in both Hebrew and English.
- **Configurable Periods**: Customizable "Start of Day" hour and "Start of Week" day to align with your personal work schedule.

---

## 🛠️ Technology Stack
*   **Core**: .NET 8 (C#)
*   **UI Engine**: Photino.NET (Native OS Webview - ultra-lightweight)
*   **Database**: SQLite (via sqlite-net-pcl)
*   **Styling**: Tailwind CSS & Vanilla CSS with modern Glassmorphism.
*   **Fonts**: Open Sans (with Hebrew support).

---

## 🏃 Setup & Running
1.  **Requirements**: .NET 8 Runtime.
2.  **Run**: Execute the published `.exe` or run `dotnet run` in the project directory.

---

## 👤 Support & Documentation
Created by **Yortem**.
For support, feature requests, or to view the source code, visit the [Yorvis GitHub Repository](https://github.com/yortem/Yorvis).
