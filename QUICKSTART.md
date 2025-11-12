# Quick Start Guide - DPO Manager

## 🚀 Getting Started in 5 Minutes

### Step 1: Build the Application

**Option A - Using the build script (Easiest):**
```cmd
1. Double-click "build.bat"
2. Choose option 2 (Build Release Version)
3. Wait for compilation to complete
```

**Option B - Using command line:**
```cmd
dotnet build DPOManager.csproj -c Release
```

### Step 2: Prepare Directory Structure

Ensure your directory looks like this:
```
YourProductionFolder/
├── DPOManager.exe          (from bin/Release/net6.0-windows/)
├── DPOManager.dll          (from bin/Release/net6.0-windows/)
├── DPOManager.runtimeconfig.json  (from bin/Release/net6.0-windows/)
├── nginx/
│   ├── nginx.exe
│   ├── conf/
│   └── html/
│       └── (your PHP application files)
└── php/
    ├── php-cgi.exe
    └── php.ini
```

### Step 3: First Launch

1. **Right-click** `DPOManager.exe` and select **"Run as administrator"**
2. The GUI will appear with default settings
3. Verify the **DPO Server** address (default: 10.142.0.204)
4. Click **"Start Servers"**
5. Watch the progress bar and activity log
6. Status indicators will turn green when ready

### Step 4: Open DPO Application

Once servers are running (status shows green ✓):
1. Click **"Open Browser"**
2. Application opens in fullscreen mode automatically
3. Or manually navigate to: `http://[your-dpo-server]/manifesto/`

## 🎯 Common Tasks

### Change HTTP Port
1. Stop servers if running
2. Change **HTTP Port** value (e.g., from 80 to 8080)
3. Click **Start Servers**

### Monitor Server Health
- **Nginx Status**: Shows if web server is running
- **PHP-CGI Status**: Shows if PHP processor is running
- Green ✓ = Running
- Red ✗ = Stopped

### Check Server Connectivity
1. Ensure **DPO Server** address is correct
2. Click **"Start Ping"**
3. Watch **Ping Status** for response time
4. Green = Online, Red = Failed

### View Detailed Logs
- Check the **Activity Log** in the application
- Or open files in the `logs/` directory
- Format: `DPO_YYYYMMDD_HHMMSS.log`

### Enable Auto-Start
1. Check **"Auto-start on launch"** checkbox
2. Servers will automatically start when you open the application
3. Useful for production deployments

## ⚠️ Troubleshooting Quick Fixes

### "Port 80 is in use"
**Solution:** Change HTTP Port to 8080 or another free port

### "nginx.exe not found"
**Solution:** Ensure `nginx/nginx.exe` exists in the same folder as DPOManager.exe

### "Access Denied" or permission errors
**Solution:** Right-click DPOManager.exe → Run as administrator

### Servers start but stop immediately
**Solution:** 
1. Check Activity Log for errors
2. Review log file in `logs/` directory
3. Verify nginx and PHP paths are correct

### Browser doesn't open automatically
**Solution:** 
1. Copy the URL from the error message
2. Open manually in your browser
3. URL format: `http://[dpo-server]/manifesto/`

## 📊 Status Indicators Explained

| Indicator | Color | Meaning |
|-----------|-------|---------|
| Nginx: ✓ Running | Green | Web server is active |
| Nginx: ✗ Stopped | Red | Web server is not running |
| PHP-CGI: ✓ Running | Green | PHP processor is active |
| PHP-CGI: ✗ Stopped | Red | PHP processor is not running |
| Ping: Online - 25ms | Green | Server is reachable |
| Ping: Failed | Red | Server is unreachable |
| Ping: Inactive | Gray | Ping monitoring is off |

## 🔄 Automatic Features

### PHP-CGI Auto-Restart
- Automatically restarts every 240 minutes (default)
- Prevents crashes from request limit
- Adjustable in configuration section
- Monitor restart count in activity log

### Health Monitoring
- Checks process status every 5 seconds
- Automatically attempts recovery if processes crash
- Updates status indicators in real-time
- Logs all issues to file

## 💡 Pro Tips

1. **First Time Setup:** Run as administrator at least once to set up permissions
2. **Production Use:** Enable "Auto-start on launch" for automatic initialization
3. **Monitoring:** Keep the application visible to watch status indicators
4. **Logs:** Review logs regularly for early warning signs
5. **Port Selection:** Use port 80 for standard HTTP, or 8080 for non-admin access

## 📝 Default Settings

- **DPO Server:** 10.142.0.204
- **HTTP Port:** 80
- **PHP Restart Interval:** 240 minutes (4 hours)
- **Health Check Interval:** 5 seconds
- **PHP Max Requests:** 10,000 (increased from default 500)

## 🎓 Next Steps

1. Review the full README.md for detailed features
2. Customize nginx configuration if needed
3. Set up scheduled tasks for automatic startup (optional)
4. Configure monitoring alerts (optional)
5. Review security settings for your environment

## 📞 Need Help?

1. **Check Activity Log** - Most issues are clearly logged
2. **Review Log Files** - Detailed information in `logs/` directory
3. **Verify Paths** - Ensure nginx and PHP directories are correct
4. **Check Permissions** - Run as administrator if needed
5. **Test Components** - Try starting nginx/PHP manually to isolate issues

---

**Remember:** The application must be run as administrator on first launch to set up IUSR permissions. After that, it may work with standard user privileges depending on your Windows configuration.
