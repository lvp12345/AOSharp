# AOSharp Crash Logger

## Overview
The AOSharp Crash Logger is a comprehensive crash reporting system that automatically captures and logs unhandled exceptions throughout the application. It provides detailed crash reports, user-friendly error dialogs, and helps developers diagnose issues.

## Features

### Automatic Exception Handling
- **UI Thread Exceptions**: Catches unhandled exceptions in the main UI thread
- **Background Thread Exceptions**: Captures exceptions in background threads
- **Task Exceptions**: Handles unobserved task exceptions
- **Manual Logging**: Allows manual exception logging in try-catch blocks

### Detailed Crash Reports
Each crash report includes:
- Unique crash ID for easy reference
- Timestamp of the crash
- Exception type and message
- Complete stack trace
- Inner exception details (if any)
- Application information (version, location, etc.)
- System information (OS, CLR version, memory usage, etc.)
- Exception data dictionary contents

### File Locations
- **Crash Reports**: Saved to `CrashLogs/` folder in the application directory
- **Filename Format**: `crash_YYYYMMDD_HHMMSS_[8-char-ID].txt`
- **Emergency Logs**: `emergency_crash_YYYYMMDD_HHMMSS.txt` (if normal logging fails)

## Usage

### Automatic Initialization
The crash logger is automatically initialized when the application starts via `App.xaml.cs`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    CrashLogger.Initialize();
    base.OnStartup(e);
}
```

### Manual Exception Logging
You can manually log exceptions in your try-catch blocks:

```csharp
try
{
    // Your code here
}
catch (Exception ex)
{
    Log.Error(ex, "Error in MyMethod");
    CrashLogger.LogException(ex, "MyMethod Context");
    // Handle the exception appropriately
}
```

### Exception Handling Behavior
- **UI Thread Exceptions**: Shows user-friendly dialog and marks exception as handled (prevents app termination)
- **Background Thread Exceptions**: Logs the exception but cannot prevent termination if it's a terminating exception
- **Task Exceptions**: Marks as observed to prevent app termination

## Integration Points

The crash logger has been integrated into critical areas of AOSharp:

### MainWindow Constructor
- Wraps the entire constructor in try-catch
- Logs initialization steps and any failures

### Plugin Management
- `AddPluginButton_Click`: Logs plugin addition attempts and failures
- Validates file paths and provides detailed error information

### Injection System
- `InjectButton_Clicked`: Logs injection attempts, plugin counts, and results
- `EjectButton_Clicked`: Logs ejection attempts and results

### Event Handlers
- `Plugins.CollectionChanged`: Protects against exceptions during config saves

## Crash Report Example

```
================================================================================
AOSharp Crash Report
================================================================================
Crash ID: a1b2c3d4
Timestamp: 2024-01-15 14:30:25
Crash Type: UI Thread Exception

APPLICATION INFORMATION:
----------------------------------------
Application: AOSharp
Version: 1.0.0.0
Location: C:\Users\User\AOSharp\AOSharp.exe
Working Directory: C:\Users\User\AOSharp

SYSTEM INFORMATION:
----------------------------------------
OS: Microsoft Windows NT 10.0.19045.0
CLR Version: 4.0.30319.42000
Machine Name: DESKTOP-ABC123
User: UserName
Processor Count: 8
Working Set: 45,678,912 bytes

EXCEPTION DETAILS:
----------------------------------------
Exception Type: System.NullReferenceException
Message: Object reference not set to an instance of an object.
Source: AOSharp
Stack Trace:
  at AOSharp.MainWindow.SomeMethod() in MainWindow.xaml.cs:line 123
  at AOSharp.MainWindow.ButtonClick(Object sender, RoutedEventArgs e) in MainWindow.xaml.cs:line 456
```

## Troubleshooting

### If Crash Logging Fails
- Emergency logs are created in the same directory
- Check Windows Event Viewer for additional information
- Ensure the application has write permissions to its directory

### Common Issues
1. **Permissions**: Ensure the app can write to the CrashLogs directory
2. **Disk Space**: Verify sufficient disk space for crash reports
3. **Antivirus**: Some antivirus software may interfere with file creation

## Best Practices

1. **Don't Suppress All Exceptions**: The crash logger is designed to capture unexpected exceptions, not replace proper error handling
2. **Use Manual Logging**: In critical sections, use `CrashLogger.LogException()` for additional context
3. **Review Crash Reports**: Regularly check the CrashLogs folder for patterns in crashes
4. **Include Context**: When manually logging, provide meaningful context strings

## Configuration

The crash logger uses minimal configuration:
- Crash log directory is automatically created relative to the executable
- No external configuration files required
- Uses existing Serilog configuration for structured logging

## Dependencies

- **Serilog**: Used for structured logging alongside crash reports
- **System.IO**: For file operations
- **System.Diagnostics**: For system information gathering
- **System.Windows**: For UI thread exception handling
