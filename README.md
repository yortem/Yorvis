# Yorvis: Lightweight PC Productivity Monitor

Yorvis is a high-performance, low-resource alternative to ActivityWatch, built for Windows using .NET 8, Photino, and SQLite. It provides automatic activity tracking and categorization with a minimal footprint.

## Core Features
*   **Background Monitoring**: Samplings foreground window data (process & title) every 1 second via Win32 interop.
*   **Intelligent Categorization**: Uses a hierarchical, regex-based rules engine to classify activities. Matches can be prioritized by window title (more specific) or process name (general).
*   **Retroactive Analysis**: Updates your historical logs automatically when you adjust category rules or priority.
*   **Visual Dashboard**: Modern, responsive UI built with Tailwind CSS and Chart.js, featuring real-time pie charts, activity logs, and hierarchical category management.
*   **Customizable**: Configure start-of-day/week preferences and local language/layout (RTL/LTR) support.
*   **Resource Efficient**: Runs as a tray application with a frameless, native-feeling UI.

## Technology Stack
*   **Backend**: .NET 8 (C#)
*   **UI Engine**: Photino.NET (Native OS webview)
*   **Frontend**: HTML5, Tailwind CSS, JavaScript, Chart.js
*   **Database**: SQLite (sqlite-net-pcl)
*   **Windowing**: Win32 API (user32.dll)

## Setup & Running
1. **Requirements**: .NET 8 SDK installed.
2. **Launch**: Navigate to the `Yorvis` directory and run:
   ```bash
   dotnet run
   ```
3. **Usage**:
   - The app minimizes to the system tray on close.
   - Access the dashboard or settings via the tray icon or by running the app again.
   - Use the **Categorize** tab to define your hierarchy via drag-and-drop. Drag more specific categories (e.g., "Games") above generic ones (e.g., "Browsing") to set priority.

## Categorization Logic
Yorvis uses a two-pass classification system:
1. **Title Match**: Rules are tested against the Window Title first.
2. **Process Match**: If no title rule matches, rules are tested against the Process Name.
3. **Priority**: The first matching category in your hierarchical list takes precedence. Use the Categorize page to reorder your categories as needed.

## Development
- **Project Structure**:
    - `Yorvis/Models`: SQLite entities.
    - `Yorvis/Services`: Background monitoring and category logic.
    - `Yorvis/wwwroot`: Frontend assets and localization files.
