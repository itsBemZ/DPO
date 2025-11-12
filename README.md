# DPO Manager - Enhanced Version

A professional Windows GUI application for managing DPO (Production Management) servers with comprehensive monitoring, health checks, and automated recovery features.

## ✨ New Features

### 1. **Process Health Checks**
- Automatic monitoring every 5 seconds
- Real-time status indicators for Nginx and PHP-CGI
- Automatic recovery attempts when processes fail
- Visual status updates with color coding (Green = Running, Red = Stopped)

### 2. **Port Binding Retry Logic**
- Automatically checks if ports are available before starting
- Waits up to 10 attempts (10 seconds) for ports to become available
- Clear error messages if ports remain occupied
- Prevents startup failures due to port conflicts

### 3. **File Logging System**
- All activities logged to timestamped files in `logs/` directory
- Format: `DPO_YYYYMMDD_HHMMSS.log`
- Includes timestamps, process IDs, and detailed error information
- Logs persist across sessions for troubleshooting

### 4. **Path Validation**
- Validates nginx and php directories exist before startup
- Checks for nginx.exe and php-cgi.exe executables
- Clear error messages indicating which paths are missing
- Prevents cryptic startup failures

### 5. **Graceful Browser Launch Degradation**
- Catches browser launch failures
- Shows manual URL if automatic launch fails
- Continues operation even if browser features don't work
- Non-blocking F11 fullscreen attempt

### 6. **Continuous Server Ping**
- Start/Stop ping functionality with dedicated buttons
- Real-time ping statistics (response time, TTL)
- Success/failure counters
- Color-coded status (Green = Online, Red = Failed)
- Runs in background thread without blocking UI

### 7. **Modern GUI Interface**
- Clean, organized layout with grouped controls
- Real-time status indicators
- Progress bar for startup sequence
- Scrollable activity log with console-style font
- Configurable settings directly in the UI
- Color-coded status labels

## 📋 Requirements

- Windows 10/11
- .NET 6.0 or later
- Nginx (in `nginx/` subdirectory)
- PHP with php-cgi.exe (in `php/` subdirectory)

## 🏗️ Building the Application

### Option 1: Using Visual Studio 2022
1. Open `DPOManager.csproj` in Visual Studio
2. Build > Build Solution (or press Ctrl+Shift+B)
3. Executable will be in `bin/Release/net6.0-windows/`

### Option 2: Using Command Line
```bash
# Build Release version
dotnet build DPOManager.csproj -c Release

# Or publish as single file
dotnet publish DPOManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## 📁 Directory Structure

```
DPOManager.exe
├── nginx/
│   ├── nginx.exe
│   ├── conf/
│   └── html/
├── php/
│   ├── php-cgi.exe
│   └── php.ini
└── logs/
    └── DPO_YYYYMMDD_HHMMSS.log
```

## 🚀 Usage

### Starting the Application

**Command Line Arguments (Optional):**
```bash
DPOManager.exe [dpoServer] [httpPort] [phpRestartMinutes]

# Examples:
DPOManager.exe                              # Use defaults
DPOManager.exe 10.142.0.204 8080 180        # Custom settings
```

**Default Values:**
- DPO Server: `10.142.0.204`
- HTTP Port: `80`
- PHP Restart Interval: `240 minutes` (4 hours)

### GUI Controls

#### Configuration Section
- **DPO Server**: IP address or hostname of the DPO server
- **HTTP Port**: Port for Nginx to listen on (default: 80)
- **PHP Restart (min)**: Minutes between automatic PHP-CGI restarts
- **Auto-start on launch**: Checkbox to automatically start servers when app opens

#### Control Buttons
- **Start Servers**: Initializes and starts Nginx and PHP-CGI
- **Stop Servers**: Gracefully stops all server processes
- **Open Browser**: Opens DPO application in default browser (fullscreen)
- **Start Ping**: Begins continuous ping to DPO server
- **Stop Ping**: Stops the ping monitoring

#### Status Section
Shows real-time status of:
- Overall application status
- Nginx process (✓ Running / ✗ Stopped)
- PHP-CGI process (✓ Running / ✗ Stopped)
- Ping monitoring (response time and status)
- Progress bar during startup

#### Activity Log
- Scrollable log showing all activities
- Timestamps for all events
- Error messages highlighted
- Saves to file automatically

## 🔧 Features in Detail

### Automatic PHP-CGI Restart
**Problem Solved:** PHP-CGI has a 500-request limit by default, causing crashes in production.

**Solution:**
- Automatically restarts PHP-CGI at configured intervals
- Sets `PHP_FCGI_MAX_REQUESTS=10000` for extended operation
- Graceful restart with port availability checks
- Automatic recovery if restart fails
- Counter tracking number of restarts

### Health Monitoring
- Checks process status every 5 seconds
- Detects if processes have crashed
- Attempts automatic recovery
- Updates UI status in real-time
- Logs all health check failures

### Port Management
- Verifies port availability before binding
- Waits for ports to be released (up to 10 seconds)
- Clear error messages for port conflicts
- Prevents silent failures

### Robust Error Handling
- Try-catch blocks around all critical operations
- Detailed error logging
- User-friendly error messages
- Automatic recovery attempts
- Graceful degradation

## 📊 Logging

Logs are automatically created in the `logs/` directory with the format:
```
[2025-11-12 14:30:15] Starting Nginx...
[2025-11-12 14:30:16] ✓ Nginx started successfully (PID: 1234)
[2025-11-12 14:30:17] Starting PHP-CGI...
[2025-11-12 14:30:18] ✓ PHP-CGI started successfully (PID: 5678)
[2025-11-12 14:30:19] Health check monitoring started (5s interval)
```

## 🔐 Administrator Privileges

The application requests administrator privileges by default (required for IUSR permissions). 

**To run without admin:**
1. Edit `app.manifest`
2. Change `requireAdministrator` to `asInvoker`
3. Rebuild the application

## 🐛 Troubleshooting

### Port Already in Use
**Solution:** 
- Change the HTTP Port in configuration
- Or stop the application using the port
- Check Task Manager for nginx/php-cgi processes

### Nginx/PHP Not Found
**Solution:**
- Ensure `nginx/` and `php/` directories exist in application folder
- Verify `nginx.exe` exists in `nginx/` folder
- Verify `php-cgi.exe` exists in `php/` folder

### Servers Won't Start
**Solution:**
- Check the Activity Log for specific errors
- Review the log file in `logs/` directory
- Ensure you have administrator privileges
- Verify no other applications are using the configured ports

### Browser Won't Open
**Solution:**
- Manual URL is displayed in error message
- Copy URL and open in your preferred browser
- Check if default browser is configured in Windows

### Ping Fails
**Solution:**
- Verify DPO server address is correct
- Check network connectivity
- Ensure firewall allows ICMP packets
- Try pinging from command prompt first

## ⚙️ Configuration Files

### Nginx Configuration
Automatically generated with:
- HTTP port from settings
- 100MB client body size limit
- CORS headers for cross-origin requests
- 180-second FastCGI timeout
- Enhanced buffers (64 × 16k)
- Status endpoint at `/nginx_status`

### PHP Configuration
- `PHP_FCGI_MAX_REQUESTS=10000` (increased from default 500)
- FastCGI binding to `127.0.0.1:9000`

## 📝 Command Line vs GUI

### Original Console Version
- Manual keyboard commands (Q, O)
- No visual feedback
- Console-based logging only
- No configuration changes without restart
- Manual process monitoring

### New GUI Version
- Click-based controls
- Real-time visual status
- File + UI logging
- Live configuration updates
- Automatic health monitoring
- Ping functionality
- Progress indicators
- Error dialogs

## 🔄 Migration from Original

If you're upgrading from the original console application:

1. **Configuration**: Settings can now be changed in the GUI
2. **Logging**: Check `logs/` directory for detailed logs
3. **Monitoring**: Health checks run automatically
4. **Control**: Use buttons instead of keyboard shortcuts
5. **Status**: Visual indicators replace console messages

## 📈 Performance Improvements

- **Startup Time**: Progress bar shows initialization steps
- **Memory**: Efficient process management and disposal
- **CPU**: Health checks every 5s (not continuous polling)
- **Disk**: Log rotation recommended for long-term use

## 🛡️ Security Considerations

- Runs with administrator privileges by default
- IUSR permissions granted to web root
- HTTP only (no HTTPS) - suitable for local network
- CORS enabled for cross-origin requests
- No authentication on Nginx status endpoint (localhost only)

## 📞 Support

For issues or questions:
1. Check the Activity Log in the GUI
2. Review the log file in `logs/` directory
3. Verify directory structure and executable locations
4. Ensure administrator privileges
5. Check Windows Event Viewer for system-level errors

## 📄 License

This application is provided as-is for DPO system management.

## 🎯 Version History

### Version 2.0 (Current)
- ✅ GUI interface
- ✅ Process health checks
- ✅ Port binding retry logic
- ✅ File logging system
- ✅ Path validation
- ✅ Graceful error handling
- ✅ Continuous ping functionality
- ✅ Auto-start option
- ✅ Progress indicators

### Version 1.0 (Original)
- Console-based interface
- Basic server management
- Manual PHP restart timer
- Console logging only
