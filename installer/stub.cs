// FloatingDock 安装包自解压桩（.NET Framework 4.x，Windows 原生运行，无需运行时）
// 内嵌 payload.zip（FloatingDock.exe + install.ps1），解压到临时目录后执行安装脚本
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

static class SetupStub
{
    [STAThread]
    static int Main()
    {
        string temp = null;
        try
        {
            temp = Path.Combine(Path.GetTempPath(), "FloatingDockSetup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);

            string zipPath = Path.Combine(temp, "payload.zip");
            using (Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            {
                if (res == null) throw new InvalidOperationException("Payload resource missing");
                using (FileStream fs = File.Create(zipPath))
                    res.CopyTo(fs);
            }

            ZipFile.ExtractToDirectory(zipPath, temp);
            File.Delete(zipPath);

            string script = Path.Combine(temp, "install.ps1");
            if (!File.Exists(script)) throw new FileNotFoundException("install.ps1 not found in payload");

            var psi = new ProcessStartInfo(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"")
            {
                WorkingDirectory = temp,
                UseShellExecute = false
            };
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                return p.ExitCode;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("FloatingDock 安装失败:\n" + ex.Message,
                "FloatingDock Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            // 尽力清理临时目录（安装脚本可能仍占用，忽略失败）
            if (temp != null)
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
