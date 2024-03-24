using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace window_resolutioner.Klassen {
  public class WindowDatas {
    public List<WindowData> Windows { get; set; } = new List<WindowData>();
    public int SelectedIndex { get; set; } = -1;
    public WindowData? SelectedWindow => SelectedIndex >= 0 ? Windows[SelectedIndex] : null;
  }

  public class WindowData {
    public string Title { get; set; } = "???";
    public IntPtr Handle { get; set; } = IntPtr.Zero;

    public override string ToString() {
      return Title;
    }


    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    public static IntPtr GetActiveWindow() {
      IntPtr handle = GetForegroundWindow();
      return handle;
    }

    [DllImport("user32.dll")]
    static extern int SetForegroundWindow(IntPtr hWnd);

    public static void forceSetForegroundWindow(IntPtr hWnd) {
      SetForegroundWindow(hWnd);
    }
  }
}
