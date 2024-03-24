using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace window_resolutioner.Klassen {
  public class Store {
    public WindowDatas WindowDatas { get; set; } = new WindowDatas();
    public Positions Positions { get; set; } = new Positions();

    public void LoadWindows() {
      WindowDatas.Windows.Clear();
      Process[] processes = Process.GetProcesses();
      foreach (Process process in processes) {
        if (process.MainWindowHandle != IntPtr.Zero) {
          WindowDatas.Windows.Add(new WindowData {
            Title = process.MainWindowTitle,
            Handle = process.MainWindowHandle
          });
        }
      }
    }
  }
}
