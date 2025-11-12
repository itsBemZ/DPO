using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace DPOManager
{
    public class DPOManagerApp : Form
    {
        #region Windows API
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const byte VK_F11 = 0x7A;
        private const int KEYEVENTF_KEYDOWN = 0x0;
        private const int KEYEVENTF_KEYUP = 0x2;
        #endregion

        #region Fields
        private Process nginxProcess;
        private Process phpCgiProcess;
        private Thread pingThread;
        private CancellationTokenSource pingCancellationToken;
        
        private string nginxPath;
        private string phpCgiPath;
        private string dpoServer;
        private int httpPort;
        private int requestDelay;
        private int phpRestartInterval;

        private System.Windows.Forms.Timer healthCheckTimer;
        private System.Windows.Forms.Timer phpRestartTimer;
        private int phpRefreshCounter = 0;
        
        private StreamWriter logWriter;
        private string logFilePath;

        // GUI Controls
        private TextBox txtLog;
        private Button btnStart;
        private Button btnStop;
        private Button btnOpenBrowser;
        private Button btnStartPing;
        private Button btnStopPing;
        private Label lblStatus;
        private Label lblNginxStatus;
        private Label lblPhpStatus;
        private Label lblPingStatus;
        private TextBox txtDpoServer;
        private NumericUpDown numHttpPort;
        private NumericUpDown numRestartInterval;
        private ProgressBar progressBar;
        private CheckBox chkAutoStart;
        #endregion

        public DPOManagerApp(string[] args)
        {
            InitializeComponent();
            InitializeConfiguration(args);
            InitializeLogging();
            
            if (chkAutoStart.Checked)
            {
                Task.Run(async () => 
                {
                    await Task.Delay(1000);
                    this.Invoke(new Action(() => StartServers()));
                });
            }
        }

        #region Initialization
        private void InitializeComponent()
        {
            this.Text = "DPO Server Manager";
            this.Size = new Size(900, 700);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += OnFormClosing;

            // Configuration Panel
            GroupBox grpConfig = new GroupBox
            {
                Text = "Configuration",
                Location = new Point(10, 10),
                Size = new Size(860, 120)
            };

            Label lblServer = new Label { Text = "DPO Server:", Location = new Point(10, 25), AutoSize = true };
            txtDpoServer = new TextBox { Location = new Point(120, 22), Width = 200, Text = "10.142.0.204" };

            Label lblPort = new Label { Text = "HTTP Port:", Location = new Point(10, 55), AutoSize = true };
            numHttpPort = new NumericUpDown 
            { 
                Location = new Point(120, 52), 
                Width = 100, 
                Minimum = 1, 
                Maximum = 65535, 
                Value = 80 
            };

            Label lblInterval = new Label { Text = "PHP Restart (min):", Location = new Point(10, 85), AutoSize = true };
            numRestartInterval = new NumericUpDown 
            { 
                Location = new Point(120, 82), 
                Width = 100, 
                Minimum = 1, 
                Maximum = 1440, 
                Value = 240 
            };

            chkAutoStart = new CheckBox 
            { 
                Text = "Auto-start on launch", 
                Location = new Point(350, 25), 
                AutoSize = true 
            };

            grpConfig.Controls.AddRange(new Control[] { 
                lblServer, txtDpoServer, lblPort, numHttpPort, 
                lblInterval, numRestartInterval, chkAutoStart 
            });

            // Control Panel
            GroupBox grpControls = new GroupBox
            {
                Text = "Controls",
                Location = new Point(10, 140),
                Size = new Size(860, 80)
            };

            btnStart = new Button 
            { 
                Text = "Start Servers", 
                Location = new Point(10, 25), 
                Size = new Size(120, 40),
                BackColor = Color.LightGreen
            };
            btnStart.Click += (s, e) => StartServers();

            btnStop = new Button 
            { 
                Text = "Stop Servers", 
                Location = new Point(140, 25), 
                Size = new Size(120, 40),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnStop.Click += (s, e) => StopServers();

            btnOpenBrowser = new Button 
            { 
                Text = "Open Browser", 
                Location = new Point(270, 25), 
                Size = new Size(120, 40),
                Enabled = false
            };
            btnOpenBrowser.Click += (s, e) => OpenBrowser();

            btnStartPing = new Button 
            { 
                Text = "Start Ping", 
                Location = new Point(400, 25), 
                Size = new Size(120, 40),
                BackColor = Color.LightBlue
            };
            btnStartPing.Click += (s, e) => StartPing();

            btnStopPing = new Button 
            { 
                Text = "Stop Ping", 
                Location = new Point(530, 25), 
                Size = new Size(120, 40),
                Enabled = false
            };
            btnStopPing.Click += (s, e) => StopPing();

            grpControls.Controls.AddRange(new Control[] { 
                btnStart, btnStop, btnOpenBrowser, btnStartPing, btnStopPing 
            });

            // Status Panel
            GroupBox grpStatus = new GroupBox
            {
                Text = "Status",
                Location = new Point(10, 230),
                Size = new Size(860, 100)
            };

            lblStatus = new Label 
            { 
                Text = "Status: Ready", 
                Location = new Point(10, 25), 
                Size = new Size(400, 20),
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };

            lblNginxStatus = new Label 
            { 
                Text = "Nginx: Stopped", 
                Location = new Point(10, 50), 
                Size = new Size(250, 20),
                ForeColor = Color.Red
            };

            lblPhpStatus = new Label 
            { 
                Text = "PHP-CGI: Stopped", 
                Location = new Point(270, 50), 
                Size = new Size(250, 20),
                ForeColor = Color.Red
            };

            lblPingStatus = new Label 
            { 
                Text = "Ping: Inactive", 
                Location = new Point(530, 50), 
                Size = new Size(300, 20),
                ForeColor = Color.Gray
            };

            progressBar = new ProgressBar 
            { 
                Location = new Point(10, 75), 
                Size = new Size(835, 15),
                Style = ProgressBarStyle.Continuous
            };

            grpStatus.Controls.AddRange(new Control[] { 
                lblStatus, lblNginxStatus, lblPhpStatus, lblPingStatus, progressBar 
            });

            // Log Panel
            GroupBox grpLog = new GroupBox
            {
                Text = "Activity Log",
                Location = new Point(10, 340),
                Size = new Size(860, 310)
            };

            txtLog = new TextBox
            {
                Location = new Point(10, 20),
                Size = new Size(835, 275),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };

            grpLog.Controls.Add(txtLog);

            this.Controls.AddRange(new Control[] { grpConfig, grpControls, grpStatus, grpLog });
        }

        private void InitializeConfiguration(string[] args)
        {
            try { dpoServer = args.Length > 0 ? args[0] : txtDpoServer.Text; }
            catch { dpoServer = txtDpoServer.Text; }

            try { httpPort = args.Length > 1 ? int.Parse(args[1]) : (int)numHttpPort.Value; }
            catch { httpPort = (int)numHttpPort.Value; }

            try { requestDelay = args.Length > 2 ? int.Parse(args[2]) : (int)numRestartInterval.Value; }
            catch { requestDelay = (int)numRestartInterval.Value; }

            phpRestartInterval = requestDelay * 60; // Convert to seconds

            txtDpoServer.Text = dpoServer;
            numHttpPort.Value = httpPort;
            numRestartInterval.Value = requestDelay;
        }

        private void InitializeLogging()
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                logFilePath = Path.Combine(logDirectory, $"DPO_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                logWriter = new StreamWriter(logFilePath, true) { AutoFlush = true };
                
                Log("=== DPO Manager Started ===");
                Log($"Configuration - Server: {dpoServer}, Port: {httpPort}, PHP Restart: {requestDelay} min");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize logging: {ex.Message}", "Warning", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region Logging
        private void Log(string message, bool isError = false)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";

            try
            {
                logWriter?.WriteLine(logMessage);
            }
            catch { }

            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(logMessage, isError)));
            }
            else
            {
                AppendLog(logMessage, isError);
            }
        }

        private void AppendLog(string message, bool isError)
        {
            txtLog.AppendText(message + Environment.NewLine);
            if (isError)
            {
                // Optionally highlight errors
            }
        }
        #endregion

        #region Path Validation
        private bool ValidateServerPaths()
        {
            Log("Validating server paths...");
            
            nginxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nginx");
            phpCgiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "php");

            if (!Directory.Exists(nginxPath))
            {
                Log($"ERROR: Nginx directory not found at: {nginxPath}", true);
                MessageBox.Show($"Nginx directory not found at:\n{nginxPath}\n\nPlease ensure nginx is installed in the application directory.", 
                    "Path Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string nginxExe = Path.Combine(nginxPath, "nginx.exe");
            if (!File.Exists(nginxExe))
            {
                Log($"ERROR: nginx.exe not found at: {nginxExe}", true);
                MessageBox.Show($"nginx.exe not found at:\n{nginxExe}", 
                    "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!Directory.Exists(phpCgiPath))
            {
                Log($"ERROR: PHP directory not found at: {phpCgiPath}", true);
                MessageBox.Show($"PHP directory not found at:\n{phpCgiPath}\n\nPlease ensure PHP is installed in the application directory.", 
                    "Path Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string phpCgiExe = Path.Combine(phpCgiPath, "php-cgi.exe");
            if (!File.Exists(phpCgiExe))
            {
                Log($"ERROR: php-cgi.exe not found at: {phpCgiExe}", true);
                MessageBox.Show($"php-cgi.exe not found at:\n{phpCgiExe}", 
                    "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            Log("✓ All server paths validated successfully");
            return true;
        }
        #endregion

        #region Port Management
        private bool IsPortAvailable(int port)
        {
            try
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        return false;
                    }
                }

                IPEndPoint[] listeners = ipGlobalProperties.GetActiveTcpListeners();
                foreach (IPEndPoint endpoint in listeners)
                {
                    if (endpoint.Port == port)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log($"Error checking port availability: {ex.Message}", true);
                return false;
            }
        }

        private async Task<bool> WaitForPortAvailable(int port, int maxRetries = 10, int delayMs = 1000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                if (IsPortAvailable(port))
                {
                    Log($"✓ Port {port} is available");
                    return true;
                }

                Log($"Port {port} is in use, waiting... (Attempt {i + 1}/{maxRetries})");
                await Task.Delay(delayMs);
            }

            return false;
        }
        #endregion

        #region Server Management
        private async void StartServers()
        {
            btnStart.Enabled = false;
            UpdateStatus("Starting servers...");
            progressBar.Value = 10;

            try
            {
                // Read current configuration
                dpoServer = txtDpoServer.Text;
                httpPort = (int)numHttpPort.Value;
                requestDelay = (int)numRestartInterval.Value;
                phpRestartInterval = requestDelay * 60;

                // Validate paths
                progressBar.Value = 20;
                if (!ValidateServerPaths())
                {
                    UpdateStatus("Failed: Invalid paths");
                    btnStart.Enabled = true;
                    progressBar.Value = 0;
                    return;
                }

                // Kill existing processes
                progressBar.Value = 30;
                Log("Terminating existing nginx and php-cgi processes...");
                KillExistingProcesses();
                await Task.Delay(1000);

                // Check port availability
                progressBar.Value = 40;
                Log($"Checking port {httpPort} availability...");
                if (!await WaitForPortAvailable(httpPort, 5, 1000))
                {
                    Log($"ERROR: Port {httpPort} is still in use after cleanup", true);
                    MessageBox.Show($"Port {httpPort} is in use by another application.\nPlease choose a different port or close the application using this port.", 
                        "Port Conflict", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Failed: Port in use");
                    btnStart.Enabled = true;
                    progressBar.Value = 0;
                    return;
                }

                // Setup web root
                progressBar.Value = 50;
                SetupWebRoot();

                // Configure Nginx
                progressBar.Value = 60;
                ConfigureNginx();

                // Start Nginx
                progressBar.Value = 70;
                if (!StartNginx())
                {
                    UpdateStatus("Failed: Nginx startup failed");
                    btnStart.Enabled = true;
                    progressBar.Value = 0;
                    return;
                }

                await Task.Delay(1000);

                // Start PHP-CGI
                progressBar.Value = 80;
                if (!StartPhpCgi())
                {
                    UpdateStatus("Failed: PHP-CGI startup failed");
                    StopNginx();
                    btnStart.Enabled = true;
                    progressBar.Value = 0;
                    return;
                }

                // Start health checks
                progressBar.Value = 90;
                StartHealthChecks();

                // Start PHP restart timer
                StartPhpRestartTimer();

                progressBar.Value = 100;
                UpdateStatus("Servers running");
                btnStop.Enabled = true;
                btnOpenBrowser.Enabled = true;
                
                Log("=== All servers started successfully ===");
                
                await Task.Delay(500);
                progressBar.Value = 0;
            }
            catch (Exception ex)
            {
                Log($"CRITICAL ERROR during startup: {ex.Message}", true);
                Log($"Stack trace: {ex.StackTrace}", true);
                MessageBox.Show($"Failed to start servers:\n{ex.Message}", "Startup Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Failed: Exception");
                btnStart.Enabled = true;
                progressBar.Value = 0;
            }
        }

        private void KillExistingProcesses()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("nginx"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                        Log($"Killed nginx process (PID: {process.Id})");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to kill nginx process: {ex.Message}", true);
                    }
                }

                foreach (var process in Process.GetProcessesByName("php-cgi"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                        Log($"Killed php-cgi process (PID: {process.Id})");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to kill php-cgi process: {ex.Message}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error during process cleanup: {ex.Message}", true);
            }
        }

        private void SetupWebRoot()
        {
            try
            {
                string webRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nginx\\html");
                if (!Directory.Exists(webRootPath))
                {
                    Directory.CreateDirectory(webRootPath);
                    Log($"Created web root directory: {webRootPath}");
                }

                GrantPermissionsToIUSR(webRootPath);
            }
            catch (Exception ex)
            {
                Log($"Error setting up web root: {ex.Message}", true);
                throw;
            }
        }

        private void GrantPermissionsToIUSR(string path)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                DirectorySecurity directorySecurity = directory.GetAccessControl();
                FileSystemAccessRule rule = new FileSystemAccessRule("IUSR", 
                    FileSystemRights.ReadAndExecute, AccessControlType.Allow);
                directorySecurity.AddAccessRule(rule);
                directory.SetAccessControl(directorySecurity);
                Log("✓ Permissions granted to IUSR");
            }
            catch (Exception ex)
            {
                Log($"Warning: Failed to grant IUSR permissions: {ex.Message}");
            }
        }

        private void ConfigureNginx()
        {
            try
            {
                string nginxConfigPath = Path.Combine(nginxPath, "conf");
                if (!Directory.Exists(nginxConfigPath))
                {
                    Directory.CreateDirectory(nginxConfigPath);
                }

                string nginxConfigContent = GenerateNginxConfig(httpPort.ToString(), "9000");
                string configFile = Path.Combine(nginxConfigPath, "nginx.conf");
                
                File.WriteAllText(configFile, nginxConfigContent);
                Log($"✓ Nginx configuration written to: {configFile}");
            }
            catch (Exception ex)
            {
                Log($"Error configuring Nginx: {ex.Message}", true);
                throw;
            }
        }

        private bool StartNginx()
        {
            try
            {
                Log("Starting Nginx...");
                nginxProcess = StartProcess("nginx", nginxPath, "nginx.exe", "");
                
                if (nginxProcess != null && !nginxProcess.HasExited)
                {
                    Log($"✓ Nginx started successfully (PID: {nginxProcess.Id})");
                    UpdateNginxStatus(true);
                    return true;
                }
                else
                {
                    Log("ERROR: Nginx process exited immediately", true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR starting Nginx: {ex.Message}", true);
                return false;
            }
        }

        private bool StartPhpCgi()
        {
            try
            {
                Log("Starting PHP-CGI...");
                phpCgiProcess = StartProcess("php-cgi", phpCgiPath, "php-cgi.exe", "-b 127.0.0.1:9000");
                
                if (phpCgiProcess != null && !phpCgiProcess.HasExited)
                {
                    Log($"✓ PHP-CGI started successfully (PID: {phpCgiProcess.Id})");
                    UpdatePhpStatus(true);
                    return true;
                }
                else
                {
                    Log("ERROR: PHP-CGI process exited immediately", true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR starting PHP-CGI: {ex.Message}", true);
                return false;
            }
        }

        private Process StartProcess(string type, string path, string fileName, string arguments)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                
                Process process = new Process();
                process.StartInfo.WorkingDirectory = path;
                process.StartInfo.FileName = fullPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                
                if (type == "php-cgi")
                {
                    process.StartInfo.EnvironmentVariables["PHP_FCGI_MAX_REQUESTS"] = "10000";
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log($"[{type}] {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log($"[{type} ERROR] {e.Data}", true);
                    }
                };

                bool started = process.Start();
                
                if (started)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                return started ? process : null;
            }
            catch (Exception ex)
            {
                Log($"Exception starting {type}: {ex.Message}", true);
                return null;
            }
        }

        private void StopServers()
        {
            UpdateStatus("Stopping servers...");
            Log("=== Shutdown initiated ===");

            StopHealthChecks();
            StopPhpRestartTimer();

            try
            {
                StopPhpCgi();
                StopNginx();

                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnOpenBrowser.Enabled = false;
                UpdateStatus("Servers stopped");
                Log("=== Shutdown completed ===");
            }
            catch (Exception ex)
            {
                Log($"Error during shutdown: {ex.Message}", true);
                MessageBox.Show($"Error during shutdown:\n{ex.Message}", "Shutdown Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void StopPhpCgi()
        {
            try
            {
                if (phpCgiProcess != null && !phpCgiProcess.HasExited)
                {
                    phpCgiProcess.Kill();
                    phpCgiProcess.WaitForExit(3000);
                    Log("✓ PHP-CGI stopped");
                }

                foreach (var process in Process.GetProcessesByName("php-cgi"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                    catch { }
                }

                UpdatePhpStatus(false);
            }
            catch (Exception ex)
            {
                Log($"Error stopping PHP-CGI: {ex.Message}", true);
            }
        }

        private void StopNginx()
        {
            try
            {
                // Try graceful shutdown first
                Process stopProcess = StartProcess("nginx-stop", nginxPath, "nginx.exe", "-s quit");
                stopProcess?.WaitForExit(5000);

                // Force kill if still running
                foreach (var process in Process.GetProcessesByName("nginx"))
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                    }
                    catch { }
                }

                Log("✓ Nginx stopped");
                UpdateNginxStatus(false);
            }
            catch (Exception ex)
            {
                Log($"Error stopping Nginx: {ex.Message}", true);
            }
        }
        #endregion

        #region Health Checks
        private void StartHealthChecks()
        {
            healthCheckTimer = new System.Windows.Forms.Timer();
            healthCheckTimer.Interval = 5000; // Check every 5 seconds
            healthCheckTimer.Tick += HealthCheckTimer_Tick;
            healthCheckTimer.Start();
            Log("Health check monitoring started (5s interval)");
        }

        private void StopHealthChecks()
        {
            if (healthCheckTimer != null)
            {
                healthCheckTimer.Stop();
                healthCheckTimer.Dispose();
                healthCheckTimer = null;
                Log("Health check monitoring stopped");
            }
        }

        private void HealthCheckTimer_Tick(object sender, EventArgs e)
        {
            bool nginxHealthy = CheckNginxHealth();
            bool phpHealthy = CheckPhpHealth();

            if (!nginxHealthy || !phpHealthy)
            {
                Log($"Health check failed - Nginx: {nginxHealthy}, PHP: {phpHealthy}", true);
                
                // Attempt recovery
                if (!phpHealthy && nginxHealthy)
                {
                    Log("Attempting PHP-CGI recovery...");
                    Task.Run(() => RestartPhpCgi());
                }
            }
        }

        private bool CheckNginxHealth()
        {
            try
            {
                bool isRunning = Process.GetProcessesByName("nginx").Any();
                UpdateNginxStatus(isRunning);
                return isRunning;
            }
            catch
            {
                UpdateNginxStatus(false);
                return false;
            }
        }

        private bool CheckPhpHealth()
        {
            try
            {
                bool isRunning = phpCgiProcess != null && !phpCgiProcess.HasExited;
                
                if (!isRunning)
                {
                    isRunning = Process.GetProcessesByName("php-cgi").Any();
                }

                UpdatePhpStatus(isRunning);
                return isRunning;
            }
            catch
            {
                UpdatePhpStatus(false);
                return false;
            }
        }
        #endregion

        #region PHP Auto-Restart
        private void StartPhpRestartTimer()
        {
            phpRestartTimer = new System.Windows.Forms.Timer();
            phpRestartTimer.Interval = phpRestartInterval * 1000;
            phpRestartTimer.Tick += PhpRestartTimer_Tick;
            phpRestartTimer.Start();
            Log($"PHP-CGI auto-restart scheduled every {requestDelay} minutes");
        }

        private void StopPhpRestartTimer()
        {
            if (phpRestartTimer != null)
            {
                phpRestartTimer.Stop();
                phpRestartTimer.Dispose();
                phpRestartTimer = null;
            }
        }

        private void PhpRestartTimer_Tick(object sender, EventArgs e)
        {
            Task.Run(() => RestartPhpCgi());
        }

        private async Task RestartPhpCgi()
        {
            phpRefreshCounter++;
            Log($"=== PHP-CGI Auto-Restart #{phpRefreshCounter} ===");
            Log("Restarting PHP-CGI (preventing 500-request limit)...");

            try
            {
                // Kill PHP-CGI
                if (phpCgiProcess != null && !phpCgiProcess.HasExited)
                {
                    phpCgiProcess.Kill();
                    phpCgiProcess.WaitForExit(3000);
                }

                foreach (var process in Process.GetProcessesByName("php-cgi"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                    catch { }
                }

                UpdatePhpStatus(false);

                // Wait for port release
                await Task.Delay(500);

                // Wait for port 9000 to be available
                bool portAvailable = await WaitForPortAvailable(9000, 5, 500);
                if (!portAvailable)
                {
                    Log("WARNING: Port 9000 may still be in use", true);
                }

                // Restart
                if (StartPhpCgi())
                {
                    Log($"✓ PHP-CGI restart #{phpRefreshCounter} completed successfully");
                }
                else
                {
                    Log($"ERROR: PHP-CGI restart #{phpRefreshCounter} failed", true);
                    
                    // Retry once
                    await Task.Delay(2000);
                    if (StartPhpCgi())
                    {
                        Log("✓ PHP-CGI recovery successful on retry");
                    }
                    else
                    {
                        Log("CRITICAL: PHP-CGI recovery failed", true);
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show("PHP-CGI failed to restart.\nPlease restart the servers manually.", 
                                "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION during PHP-CGI restart: {ex.Message}", true);
            }
        }
        #endregion

        #region Ping Management
        private void StartPing()
        {
            if (pingThread != null && pingThread.IsAlive)
            {
                Log("Ping already running");
                return;
            }

            pingCancellationToken = new CancellationTokenSource();
            pingThread = new Thread(() => PingWorker(pingCancellationToken.Token));
            pingThread.IsBackground = true;
            pingThread.Start();

            btnStartPing.Enabled = false;
            btnStopPing.Enabled = true;
            Log($"Started continuous ping to {dpoServer}");
        }

        private void StopPing()
        {
            if (pingCancellationToken != null)
            {
                pingCancellationToken.Cancel();
            }

            if (pingThread != null && pingThread.IsAlive)
            {
                pingThread.Join(2000);
            }

            btnStartPing.Enabled = true;
            btnStopPing.Enabled = false;
            UpdatePingStatus("Inactive", Color.Gray);
            Log("Stopped ping");
        }

        private void PingWorker(CancellationToken cancellationToken)
        {
            using (Ping pinger = new Ping())
            {
                int successCount = 0;
                int failureCount = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        PingReply reply = pinger.Send(dpoServer, 1000);
                        
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            failureCount = 0;
                            string message = $"Reply from {dpoServer}: time={reply.RoundtripTime}ms TTL={reply.Options.Ttl}";
                            Log($"[PING] {message}");
                            UpdatePingStatus($"Online - {reply.RoundtripTime}ms (Success: {successCount})", Color.Green);
                        }
                        else
                        {
                            failureCount++;
                            Log($"[PING] Failed: {reply.Status}", true);
                            UpdatePingStatus($"Failed: {reply.Status} (Failures: {failureCount})", Color.Red);
                        }

                        Thread.Sleep(1000);
                    }
                    catch (PingException ex)
                    {
                        failureCount++;
                        Log($"[PING] Error: {ex.Message}", true);
                        UpdatePingStatus($"Error (Failures: {failureCount})", Color.Red);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Log($"[PING] Unexpected error: {ex.Message}", true);
                        Thread.Sleep(1000);
                    }
                }
            }

            Log("[PING] Thread terminated");
        }
        #endregion

        #region Browser Management
        private void OpenBrowser()
        {
            try
            {
                string url = $"http://{dpoServer}/manifesto/";
                Log($"Opening browser: {url}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                    Verb = "open"
                };

                Process browserProcess = Process.Start(startInfo);
                
                if (browserProcess != null)
                {
                    Log("✓ Browser opened successfully");
                    
                    // Wait and send F11 for fullscreen
                    Task.Run(async () =>
                    {
                        await Task.Delay(4000);
                        try
                        {
                            keybd_event(VK_F11, 0, KEYEVENTF_KEYDOWN, 0);
                            keybd_event(VK_F11, 0, KEYEVENTF_KEYUP, 0);
                            Log("✓ Sent F11 for fullscreen mode");
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to send F11: {ex.Message}");
                        }
                    });
                }
                else
                {
                    throw new Exception("Failed to start browser process");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to open browser: {ex.Message}", true);
                MessageBox.Show($"Failed to open browser:\n{ex.Message}\n\nYou can manually navigate to:\nhttp://{dpoServer}/manifesto/", 
                    "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region UI Updates
        private void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => lblStatus.Text = $"Status: {status}"));
            }
            else
            {
                lblStatus.Text = $"Status: {status}";
            }
        }

        private void UpdateNginxStatus(bool isRunning)
        {
            if (lblNginxStatus.InvokeRequired)
            {
                lblNginxStatus.Invoke(new Action(() => UpdateNginxStatusDirect(isRunning)));
            }
            else
            {
                UpdateNginxStatusDirect(isRunning);
            }
        }

        private void UpdateNginxStatusDirect(bool isRunning)
        {
            lblNginxStatus.Text = isRunning ? "Nginx: ✓ Running" : "Nginx: ✗ Stopped";
            lblNginxStatus.ForeColor = isRunning ? Color.Green : Color.Red;
        }

        private void UpdatePhpStatus(bool isRunning)
        {
            if (lblPhpStatus.InvokeRequired)
            {
                lblPhpStatus.Invoke(new Action(() => UpdatePhpStatusDirect(isRunning)));
            }
            else
            {
                UpdatePhpStatusDirect(isRunning);
            }
        }

        private void UpdatePhpStatusDirect(bool isRunning)
        {
            lblPhpStatus.Text = isRunning ? "PHP-CGI: ✓ Running" : "PHP-CGI: ✗ Stopped";
            lblPhpStatus.ForeColor = isRunning ? Color.Green : Color.Red;
        }

        private void UpdatePingStatus(string status, Color color)
        {
            if (lblPingStatus.InvokeRequired)
            {
                lblPingStatus.Invoke(new Action(() =>
                {
                    lblPingStatus.Text = $"Ping: {status}";
                    lblPingStatus.ForeColor = color;
                }));
            }
            else
            {
                lblPingStatus.Text = $"Ping: {status}";
                lblPingStatus.ForeColor = color;
            }
        }
        #endregion

        #region Nginx Configuration
        private string GenerateNginxConfig(string httpPort, string phpPort)
        {
            return $@"
worker_processes  1;

events {{
    worker_connections  1024;
}}

http {{
    include       mime.types;
    default_type  application/octet-stream;
    client_max_body_size 100M;

    sendfile        on;
    keepalive_timeout  65;

    server {{
        listen {httpPort};
        server_name localhost;
        root   html;

        location / {{
            index index.html index.htm index.php;

            if ($request_method = 'OPTIONS') {{
                add_header 'Access-Control-Allow-Origin' '*';
                add_header 'Access-Control-Allow-Methods' 'GET, POST, OPTIONS';
                add_header 'Access-Control-Max-Age' 1728000;
                add_header 'Content-Type' 'text/plain; charset=utf-8';
                add_header 'Content-Length' 0;
                return 204;
            }}
            if ($request_method = 'POST') {{
                add_header 'Access-Control-Allow-Origin' '*' always;
                add_header 'Access-Control-Allow-Methods' 'GET, POST, OPTIONS' always;
                add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range' always;
                add_header 'Access-Control-Expose-Headers' 'Content-Length,Content-Range' always;
            }}
            if ($request_method = 'GET') {{
                add_header 'Access-Control-Allow-Origin' '*' always;
                add_header 'Access-Control-Allow-Methods' 'GET, POST, OPTIONS' always;
                add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range' always;
                add_header 'Access-Control-Expose-Headers' 'Content-Length,Content-Range' always;
            }}
        }}
        
        location ~ \.php$ {{
            fastcgi_pass 127.0.0.1:{phpPort};
            fastcgi_index index.php;
            fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
            fastcgi_read_timeout 180;
            fastcgi_buffers 64 16k;
            fastcgi_buffer_size 32k;
            include fastcgi_params;
        }}

        location /nginx_status {{
            stub_status on;
            access_log   off;
            allow 127.0.0.1;
            deny all;
        }}
    }}
}}";
        }
        #endregion

        #region Form Events
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnStop.Enabled)
            {
                var result = MessageBox.Show(
                    "Servers are still running. Do you want to stop them before exiting?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    StopServers();
                }
            }

            StopPing();

            try
            {
                Log("=== Application closing ===");
                logWriter?.Close();
                logWriter?.Dispose();
            }
            catch { }
        }
        #endregion

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DPOManagerApp(args));
        }
    }
}
