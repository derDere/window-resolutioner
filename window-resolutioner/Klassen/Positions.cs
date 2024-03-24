using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using IO = System.IO;

namespace window_resolutioner.Klassen {
  public class Positions {
    public List<Position> PositionList { get; set; } = new List<Position>();
    public int SelectedIndex { get; set; } = -1;
    public Position? SelectedPosition => SelectedIndex >= 0 ? PositionList[SelectedIndex] : null;

    private string GetFileName() {
      string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      path += "\\WindowResolutioner";
      if (!IO.Directory.Exists(path)) {
        IO.Directory.CreateDirectory(path);
      }
      return path + "\\positions.json";
    }

    public void LoadPositions() {
      string path = GetFileName();
      if (IO.File.Exists(path)) {
        try {
          string json = IO.File.ReadAllText(path);
          PositionList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Position>>(json) ?? new List<Position>();
        }
        catch (Exception ex) {
          MessageBox.Show(ex.Message);
        }
      }
    }

    public void SavePositions() {
      string path = GetFileName();
      try {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(PositionList);
        IO.File.WriteAllText(path, json);
      }
      catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
    }
  }

  public class Position {
    public string Name { get; set; } = "New Position";
    public string Pattern { get; set; } = "EMPTY";
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int width { get; set; } = 50;
    public int height { get; set; } = 50;
    public bool active { get; set; } = false;

    public override string ToString() {
      return Name;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

    public struct Rect {
      public int Left { get; set; }
      public int Top { get; set; }
      public int Right { get; set; }
      public int Bottom { get; set; }
    }

    public void GetPositionFromHandle(IntPtr handle) {
      if (handle == IntPtr.Zero) {
        return;
      }
      Rect rect = new Rect();
      GetWindowRect(handle, ref rect);
      X = rect.Left;
      Y = rect.Top;
      width = rect.Right - rect.Left;
      height = rect.Bottom - rect.Top;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public void SetPosition(IntPtr handle) {
      if (handle == IntPtr.Zero) {
        return;
      }

      Rect rect = new Rect();
      GetWindowRect(handle, ref rect);
      if (rect.Left == X && rect.Top == Y && rect.Right - rect.Left == width && rect.Bottom - rect.Top == height) {
        return;
      }

      int x = X;
      int y = Y;
      int cx = x + width;
      int cy = y + height;
      SetWindowPos(handle, IntPtr.Zero, x, y, cx, cy, 0);
    }

    public List<IntPtr> FindMatchingWindows() {
      Process[] processes = Process.GetProcesses();
      List<IntPtr> handles = new List<IntPtr>();
      foreach (Process process in processes) {
        if (process.MainWindowHandle != IntPtr.Zero) {
          if (Regex.IsMatch(process.MainWindowTitle, Pattern)) {
            handles.Add(process.MainWindowHandle);
          }
        }
      }
      return handles;
    }
  }
}
