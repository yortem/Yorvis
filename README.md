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
*   **Timeline Map (Horizontal)**: A pixel-perfect, high-density visualization of your day. Uses absolute time-mapping and glassmorphism tooltips for precise activity review.
*   **Timeline Stream (Vertical)**: A chronological feed unified with the "Recent Logs" design.
*   **Smart Search Grouping**: When searching in the Timeline Stream, similar activities are intelligently grouped into time ranges (e.g., `10:00 - 10:45`) allowing for small gaps (up to 5 mins).
*   **Interactive Analytics**: Real-time distribution charts and top-category summaries for different time ranges (Day, Week, Month, or Custom).

### 🎨 Modern UI & Experience
*   **Dark Theme Support**: Full support for Dark and Light themes with an optimized color palette for reduced eye strain.
*   **Open Sans Hebrew**: Native high-quality typography for both English and Hebrew text, ensuring maximum readability.
*   **System Tray Integration**: Runs silently in the background. Close the window to minimize to the tray, and double-click the icon to return to your dashboard.

### 🛡️ Privacy & Exclusions
*   **Global Blacklist**: Define custom keywords or Regex patterns to exclude specific windows or processes from ever being recorded.
*   **Incognito Awareness**: Automatically detects and excludes private browsing sessions by default.
*   **Local-Only Storage**: All data is stored in a local SQLite database (`yorvis.db3`). Your activity never leaves your machine.

### 🧠 Intelligent Categorization
*   **Hierarchical Priority**: Drag-and-drop category management. The system matches rules from top to bottom, allowing specific rules to override general ones.
*   **Quick Categorize**: Right-click any activity in the logs to instantly add it to a category via a context menu.
*   **Regex Support**: Use powerful regular expressions for complex matching logic across titles and processes.
*   **Retroactive Updates**: Changing a category rule or its priority instantly updates the categorization of all historical logs.

---

## 📐 Internal Logic & Calculations

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
