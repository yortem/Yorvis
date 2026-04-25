# Yorvis: High-Performance PC Activity & Productivity Monitor

Yorvis (Yortem + Jarvis) is a premium, lightweight alternative to ActivityWatch, designed specifically for Windows. It operates as a "Ghost Manager"—monitoring your PC activity silently in the background, providing deep insights through a modern dashboard, and staying out of your way when you're in deep work or gaming.

Built with **.NET 8**, **Photino**, and **SQLite**, Yorvis offers privacy-first, local-only tracking with rich visualizations and intelligent noise reduction.

---

## 🚀 Key Features

### 🕒 Advanced Activity Tracking
*   **Precision Monitoring**: Samples foreground window data (Process Name & Window Title) every 1 second via Win32 Interop.
*   **Bounce Protection (New)**: Implements a 5-second buffer for window switches. If you accidentally switch windows or check a notification briefly, Yorvis maintains the continuity of your primary session.
*   **Heartbeat Commits**: Automatically saves activity logs every 30 seconds to ensure data integrity even in case of system crashes.

### 📊 Rich Visualizations
*   **Timeline Map (Horizontal)**: A pixel-perfect, high-density visualization of your day. Uses absolute time-mapping and glassmorphism tooltips for precise activity review.
*   **Activity Stream (Vertical)**: A chronological feed of your actions. Automatically groups similar consecutive activities into expandable "sessions" for a cleaner view.
*   **Interactive Analytics**: Real-time distribution charts and top-category summaries for different time ranges (1h to 30 days).

### 🛡️ Privacy & Exclusions
*   **Global Blacklist**: Define custom keywords or Regex patterns to exclude specific windows or processes from ever being recorded.
*   **Incognito Awareness**: Automatically detects and excludes private browsing sessions by default.
*   **Local-Only Storage**: All data is stored in a local SQLite database (`yorvis.db3`). Your activity never leaves your machine.

### 🧠 Intelligent Categorization
*   **Hierarchical Priority**: Drag-and-drop category management. The system matches rules from top to bottom, allowing you to define specific rules that override general ones.
*   **Regex Support**: Use powerful regular expressions for complex matching logic across titles and processes.
*   **Retroactive Updates**: Changing a category rule or its priority instantly updates the categorization of all historical logs.

### 🛠️ Maintenance & Maintenance
*   **Log Retention**: Keep your database lean by deleting logs older than a specific number of days (default 365).
*   **Health Monitoring**: View real-time database size and record counts directly in the settings.

---

## 📐 Internal Logic & Calculations

### 💤 AFK (Away From Keyboard) Logic
Yorvis uses the `GetLastInputInfo` Win32 API to detect inactivity:
1.  **Threshold**: 180 seconds (3 minutes) of no input triggers AFK status.
2.  **Media Awareness**: The system detects if you are in a "Media" or "Video" category (e.g., YouTube, Netflix, Spotify). If media is playing, AFK detection is disabled so your movie sessions aren't marked as idle.
3.  **Correction**: When AFK is detected, the system saves the previous activity but subtracts the 3-minute threshold to ensure the idle time isn't incorrectly attributed to work.

### 🔄 Bounce Protection Logic
To prevent "fragmented logs" from quick window switching:
- When you switch from Window A to Window B, Yorvis starts a **5-second pending buffer**.
- If you switch back to Window A within those 5 seconds, the switch to B is **discarded**.
- Window A's session continues as if the interruption never happened.

### 🌍 Timezone Handling
- **Database**: All timestamps are stored in **UTC** to ensure consistency across system reboots and travel.
- **Frontend**: The dashboard handles local-to-UTC conversion on-the-fly, ensuring that "Last 1 Hour" always correctly aligns with your current system clock.

### 🌓 RTL & Localization
- **RTL First**: Full native support for Right-to-Left layouts (Hebrew) and Left-to-Right (English).
- **Start of Day**: Configurable reset hour (e.g., 4:00 AM) to ensure night-owls see their "today" correctly.

---

## 🛠️ Technology Stack
*   **Core**: .NET 8 (C#)
*   **UI Engine**: Photino.NET (Native OS Webview - ultra-lightweight)
*   **Database**: SQLite (via sqlite-net-pcl)
*   **Styling**: Vanilla CSS with modern Glassmorphism and Tailwind CSS (Dashboard)
*   **Visuals**: Chart.js for analytics, Material Symbols for icons.

---

## 🏃 Setup & Running
1.  **Requirements**: .NET 8 Runtime.
2.  **Run**: Execute the published `.exe` or run `dotnet run` in the project directory.
3.  **Minimize**: Closing the window hides it in the System Tray. Double-click the tray icon to reopen the dashboard.

---
*Created by Yortem - Your private Ghost Productivity Manager.*
