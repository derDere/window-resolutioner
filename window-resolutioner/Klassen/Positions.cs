using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using IO = System.IO;
using MessageBox = System.Windows.MessageBox;

namespace window_resolutioner.Klassen {
  public class Positions {

    public List<Position> PositionList { get; set; } = new List<Position>();
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = false;

    [Newtonsoft.Json.JsonIgnore]
    public int SelectedIndex { get; set; } = -1;

    [Newtonsoft.Json.JsonIgnore]
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
          Positions obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Positions>(json) ?? new Positions();
          PositionList = obj.PositionList;
          StartMinimized = obj.StartMinimized;
          MinimizeToTray = obj.MinimizeToTray;
        }
        catch (Exception ex) {
          MessageBox.Show(ex.Message);
        }
      }
    }

    public void SavePositions() {
      string path = GetFileName();
      try {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
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
    public bool removeBorder { get; set; } = false;
    public bool showAmbientWindows { get; set; } = false;
    public DataTable ambientWindows { get; set; } = new DataTable();

    public Position() {
      ambientWindows.Columns.Add("Active", typeof(bool));
      ambientWindows.Columns.Add("Name", typeof(string));
      ambientWindows.Columns.Add("X", typeof(int));
      ambientWindows.Columns.Add("Y", typeof(int));
      ambientWindows.Columns.Add("Width", typeof(int));
      ambientWindows.Columns.Add("Height", typeof(int));
      ambientWindows.Columns.Add("Color", typeof(System.Windows.Media.Color));
      ambientWindows.Columns.Add("TopMost", typeof(bool));
    }

    public override string ToString() {
      return Name;
    }

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

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
      int cx = width;
      int cy = height;
      SetWindowPos(handle, IntPtr.Zero, x, y, cx, cy, 0);
    }

    internal static List<System.Windows.Forms.Form> ambientWindowList = new List<Form>();
    public void SetAmbientWindows(IntPtr activeWindow, IntPtr handle, ref bool handledAmbientWindows) {
      if (activeWindow == handle) {
        handledAmbientWindows = true;
        string currentWindows = "";
        foreach (System.Windows.Forms.Form f in ambientWindowList) {
          string colorStr = "#";
          colorStr += ((int)Math.Round(f.Opacity * 255)).ToString("X2");
          colorStr += f.BackColor.R.ToString("X2");
          colorStr += f.BackColor.G.ToString("X2");
          colorStr += f.BackColor.B.ToString("X2");
          currentWindows += $"{f.Text},{f.Top},{f.Left},{f.Width},{f.Height},{colorStr},{(f.TopMost ? 1 : 0)};";
        }
        string targetWindows = "";
        if (showAmbientWindows) {
          foreach (DataRow row in ambientWindows.Rows) {
            bool active = (bool)row["Active"];
            if (active) {
              string Name = (string)row["Name"];
              int X = (int)row["X"];
              int Y = (int)row["Y"];
              int Width = (int)row["Width"];
              int Height = (int)row["Height"];
              System.Windows.Media.Color Color = (System.Windows.Media.Color)row["Color"];
              bool TopMost = (bool)row["TopMost"];
              string colorStr = "#";
              colorStr += Color.A.ToString("X2");
              colorStr += Color.R.ToString("X2");
              colorStr += Color.G.ToString("X2");
              colorStr += Color.B.ToString("X2");
              targetWindows += $"{Name},{X},{Y},{Width},{Height},{colorStr},{(TopMost ? 1 : 0)};";
            }
          }
        }
        if (currentWindows == targetWindows) {
          return;
        }
        foreach (System.Windows.Forms.Form f in ambientWindowList) {
          f.Close();
        }
        ambientWindowList.Clear();
        if (showAmbientWindows) {
          foreach (DataRow row in ambientWindows.Rows) {
            bool active = (bool)row["Active"];
            if (active) {
              string Name = (string)row["Name"];
              int X = (int)row["X"];
              int Y = (int)row["Y"];
              int Width = (int)row["Width"];
              int Height = (int)row["Height"];
              System.Windows.Media.Color Color = (System.Windows.Media.Color)row["Color"];
              bool TopMost = (bool)row["TopMost"];
              System.Windows.Forms.Form f = new System.Windows.Forms.Form();
              f.Text = Name;
              f.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
              f.Top = Y;
              f.Left = X;
              f.Width = Width;
              f.Height = Height;
              f.BackColor = System.Drawing.Color.FromArgb(255, Color.R, Color.G, Color.B);
              f.Opacity = (double)Color.A / 255;
              f.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
              f.ShowInTaskbar = false;
              f.TopMost = TopMost;
              f.Show();
              f.Activated += (s, e) => {
                waitAndSelectWindow(activeWindow);
              };
              ambientWindowList.Add(f);
            }
          }
        }
      }
    }

    private void waitAndSelectWindow(IntPtr handle) {
      System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
      timer.Interval = 10;
      timer.Tick += (s, e) => {
        timer.Stop();
        timer.Dispose();
        Klassen.WindowData.forceSetForegroundWindow(handle);
      };
      timer.Start();
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


    //Gets window attributes
    [DllImport("USER32.DLL")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    //Sets window attributes
    [DllImport("USER32.DLL")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public void SetWindowBorder(IntPtr windowHandle, bool borderOn) {
      if (windowHandle == IntPtr.Zero) {
        return;
      }

      int GWL_STYLE = -16;
      int WS_BORDER = 0x00800000;
      int WS_CAPTION = 0x00C00000;
      int WS_SYSMENU = 0x00080000;
      int WS_THICKFRAME = 0x00040000;
      int WS_MINIMIZE = 0x20000000;
      int WS_MAXIMIZEBOX = 0x00010000;

      int lCurStyle = GetWindowLong(windowHandle, GWL_STYLE);
      if (borderOn) {
        lCurStyle |= WS_BORDER;
        lCurStyle |= WS_CAPTION;
        lCurStyle |= WS_SYSMENU;
        lCurStyle |= WS_THICKFRAME;
        lCurStyle |= WS_MINIMIZE;
        lCurStyle |= WS_MAXIMIZEBOX;
      }
      else {
        lCurStyle &= ~WS_BORDER;
        lCurStyle &= ~WS_CAPTION;
        lCurStyle &= ~WS_SYSMENU;
        lCurStyle &= ~WS_THICKFRAME;
        lCurStyle &= ~WS_MINIMIZE;
        lCurStyle &= ~WS_MAXIMIZEBOX;
      }
      SetWindowLong(windowHandle, GWL_STYLE, lCurStyle);
    }
  }
}
