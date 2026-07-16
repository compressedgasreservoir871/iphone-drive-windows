using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("iPhone Drive")]
[assembly: System.Reflection.AssemblyDescription("Automatic iPhone app documents drive")]
[assembly: System.Reflection.AssemblyCompany("essa_tareen")]
[assembly: System.Reflection.AssemblyProduct("iPhone Drive")]
[assembly: System.Reflection.AssemblyCopyright("Copyright (c) 2026 essa_tareen")]
[assembly: System.Reflection.AssemblyVersion("0.1.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("0.1.0.0")]

internal sealed class TrayApp : ApplicationContext
{
    readonly NotifyIcon tray = new NotifyIcon();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly string appDir = AppDomain.CurrentDomain.BaseDirectory;
    readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iPhoneDrive");
    readonly List<Process> workers = new List<Process>();
    readonly List<string> mountFolders = new List<string>();
    string udid, drive, baseMount;
    bool busy, trustNoticeShown;

    public TrayApp()
    {
        Directory.CreateDirectory(dataDir);
        tray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        tray.Text = "iPhone Drive - waiting for iPhone";
        tray.Visible = true;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open iPhone Drive", null, (s,e) => OpenDrive());
        menu.Items.Add("Reconnect", null, (s,e) => { Unmount(); Poll(); });
        menu.Items.Add("Unmount", null, (s,e) => Unmount());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (s,e) => Exit());
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (s,e) => OpenDrive();
        timer.Interval = 3000;
        timer.Tick += (s,e) => Poll();
        timer.Start();
        Poll();
    }

    void Poll()
    {
        if (busy) return;
        busy = true;
        try {
            string found = RunCapture("idevice_id.exe", "-l", 5000).Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (String.IsNullOrWhiteSpace(found)) {
                trustNoticeShown = false;
                if (udid != null || drive != null) Unmount();
                tray.Text = "iPhone Drive - waiting for iPhone";
                return;
            }
            if (drive != null && found == udid) return;
            if (drive != null) Unmount();
            udid = found.Trim();
            tray.Text = "iPhone Drive - waiting for trust";
            int pairCode;
            string pair = RunCapture("idevicepair.exe", "validate -u " + Quote(udid), 8000, out pairCode);
            if (pairCode != 0) {
                RunCapture("idevicepair.exe", "pair -u " + Quote(udid), 8000, out pairCode);
                if (pairCode != 0) {
                    if (!trustNoticeShown) Notify("Unlock and trust your iPhone", "Unlock the iPhone and tap Trust This Computer. Mounting will continue automatically.", ToolTipIcon.Info);
                    trustNoticeShown = true;
                    return;
                }
            }
            trustNoticeShown = false;
            Mount(found.Trim());
        } catch(Exception ex) { Log(ex.ToString()); tray.Text = "iPhone Drive - error"; }
        finally { busy = false; }
    }

    void Mount(string id)
    {
        tray.Text = "iPhone Drive - discovering apps";
        int code;
        string output = RunCapture("ifuse-win.exe", "--list-apps --udid " + Quote(id), 20000, out code);
        if (code != 0) { Notify("Could not read iPhone apps", output.Trim(), ToolTipIcon.Error); return; }
        var apps = output.Split(new[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(line => line.Split(new[]{'\t'},2)).Where(p => p.Length==2).ToList();
        if (apps.Count == 0) { Notify("No shared app folders", "No installed apps currently enable Apple File Sharing.", ToolTipIcon.Warning); return; }
        drive = FreeDrive();
        if (drive == null) { Notify("No drive letter available", "Free a drive letter between I: and Z: and reconnect the phone.", ToolTipIcon.Error); return; }
        long total, free;
        if(!ProbeCapacity(id,drive,out total,out free)){Unmount();Notify("Capacity check failed","Could not read the iPhone storage capacity.",ToolTipIcon.Error);return;}
        string root = Path.Combine(dataDir,"Mounts",Safe(id));
        string logs = Path.Combine(dataDir, "Logs");
        Directory.CreateDirectory(logs);Directory.CreateDirectory(root);
        foreach (var app in apps) {
            string name = Safe(String.IsNullOrWhiteSpace(app[1]) ? app[0] : app[1]);
            string folder = Path.Combine(root, name); Directory.CreateDirectory(folder);
            mountFolders.Add(folder);
            var psi = new ProcessStartInfo(Path.Combine(appDir,"ifuse-win.exe"), Quote(folder)+" --documents "+Quote(app[0])+" --udid "+Quote(id));
            psi.UseShellExecute=false; psi.CreateNoWindow=true; psi.RedirectStandardOutput=true; psi.RedirectStandardError=true;
            var p=Process.Start(psi); workers.Add(p);
            Thread.Sleep(750);
        }
        Thread.Sleep(1500);
        var proxyPsi=new ProcessStartInfo(Path.Combine(appDir,"aggregate-proxy.exe"),Quote(root)+" "+drive+" "+total+" "+free);
        proxyPsi.UseShellExecute=false;proxyPsi.CreateNoWindow=true;proxyPsi.RedirectStandardOutput=true;proxyPsi.RedirectStandardError=true;
        var proxy=Process.Start(proxyPsi);workers.Add(proxy);baseMount=drive+"\\";Thread.Sleep(2500);
        if(!Directory.Exists(drive+"\\")){Unmount();Notify("Drive mount failed","The unified iPhone filesystem could not start.",ToolTipIcon.Error);return;}
        tray.Text = "iPhone Drive - " + drive;
        Notify("iPhone mounted", "App Documents are available at "+drive+"\\", ToolTipIcon.Info);
    }

    void Unmount()
    {
        timer.Stop();
        foreach(var folder in mountFolders.ToArray()) try { DokanRemoveMountPoint(folder); } catch {}
        mountFolders.Clear();
        try { if(baseMount!=null)DokanRemoveMountPoint(baseMount); } catch {}
        baseMount=null;
        foreach(var p in workers.ToArray()) try { if(!p.HasExited)p.Kill(); p.Dispose(); } catch {}
        workers.Clear(); drive=null; udid=null; tray.Text="iPhone Drive - waiting for iPhone";
        timer.Start();
    }

    string FreeDrive() { uint mask=GetLogicalDrives(); foreach(char c in "IJKLMNOPQRSTUVWXYZ") if((mask&(1u<<(c-'A')))==0)return c+":"; return null; }
    bool ProbeCapacity(string id,string letter,out long total,out long free){total=free=0;var psi=new ProcessStartInfo(Path.Combine(appDir,"ifuse-win.exe"),letter+" --udid "+Quote(id)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};var p=Process.Start(psi);try{Thread.Sleep(2500);var d=new DriveInfo(letter+"\\");if(!d.IsReady)return false;total=d.TotalSize;free=d.AvailableFreeSpace;return total>0;}finally{try{DokanRemoveMountPoint(letter+"\\");if(!p.HasExited)p.Kill();p.Dispose();Thread.Sleep(2000);}catch{}}}
    void OpenDrive(){if(drive!=null&&Directory.Exists(drive+"\\"))Process.Start("explorer.exe",drive+"\\");}
    void Exit(){timer.Stop();Unmount();tray.Visible=false;tray.Dispose();Application.Exit();}
    void Notify(string title,string text,ToolTipIcon icon){tray.BalloonTipTitle=title;tray.BalloonTipText=text.Length>250?text.Substring(0,250):text;tray.BalloonTipIcon=icon;tray.ShowBalloonTip(6000);}
    void Log(string s){try{File.AppendAllText(Path.Combine(dataDir,"iPhoneDrive.log"),DateTime.Now+" "+s+Environment.NewLine);}catch{}}
    string RunCapture(string exe,string args,int timeout){int c;return RunCapture(exe,args,timeout,out c);}
    string RunCapture(string exe,string args,int timeout,out int code){string local=Path.Combine(appDir,exe);string path=Path.IsPathRooted(exe)||!File.Exists(local)?exe:local;var p=new Process{StartInfo=new ProcessStartInfo(path,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};p.Start();string o=p.StandardOutput.ReadToEnd(),e=p.StandardError.ReadToEnd();if(!p.WaitForExit(timeout)){try{p.Kill();}catch{}code=-1;}else code=p.ExitCode;return o+(String.IsNullOrEmpty(e)?"":Environment.NewLine+e);}
    static string Quote(string s){return "\""+s.Replace("\"","\\\"")+"\"";}
    static string Safe(string s){foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');s=s.Trim(' ','.');return String.IsNullOrEmpty(s)?"App":s;}
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern uint GetLogicalDrives();
    [System.Runtime.InteropServices.DllImport("dokan2.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern bool DokanRemoveMountPoint(string mountPoint);
}

internal static class TrayProgram
{
    [STAThread] static void Main(){bool first;using(var mutex=new Mutex(true,"Local\\iPhoneDriveTray",out first)){if(!first)return;Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new TrayApp());}}
}
