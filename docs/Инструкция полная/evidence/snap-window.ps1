param([string]$OutFile = "D:/IA/ace/media_tmp/win.png", [int]$TargetPid = 0)
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WinCap {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr lParam);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
  public struct RECT { public int L, T, R, B; }
  public static IntPtr Found = IntPtr.Zero;
  public static RECT Rect;
  public static void FindMain(uint targetPid) {
    EnumWindows((h, l) => {
      uint pid; GetWindowThreadProcessId(h, out pid);
      if (pid == targetPid && IsWindowVisible(h)) {
        var sb = new StringBuilder(512); GetWindowText(h, sb, 512);
        if (sb.Length > 0) {
          RECT r; GetWindowRect(h, out r);
          if ((r.R - r.L) > 100 && (r.B - r.T) > 100) {
            Found = h; Rect = r;
            if (sb.ToString().Contains("Калькулятор") || sb.ToString().Contains("REHAU")) return false;
          }
        }
      }
      return true;
    }, IntPtr.Zero);
  }
}
"@
[WinCap]::SetProcessDPIAware() | Out-Null
$proc = Get-Process SnowMeltingCalculator | Select-Object -First 1
[WinCap]::FindMain([uint32]$proc.Id)
if ([WinCap]::Found -eq [IntPtr]::Zero) { Write-Error "window not found"; exit 1 }
$r = [WinCap]::Rect
$w = $r.R - $r.L; $h = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[WinCap]::PrintWindow([WinCap]::Found, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$g.Dispose()
$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "saved: $OutFile ($w x $h) title='$($proc.MainWindowTitle)'"
