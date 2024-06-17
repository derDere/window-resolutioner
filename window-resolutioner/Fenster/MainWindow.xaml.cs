using ShellLink;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using window_resolutioner.Klassen;
using IO = System.IO;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using Window = System.Windows.Window;
using CheckBox = System.Windows.Controls.CheckBox;
using Shortcut = ShellLink.Shortcut;
using System.Data;

namespace window_resolutioner {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private DispatcherTimer ticker = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1), IsEnabled = true };
    private NotifyIcon notifyIcon = new NotifyIcon();

    public Klassen.Store Store { get; set; } = new Klassen.Store();

    private bool closeForReal = false;

    internal static MainWindow Instance { get; private set; }

    public MainWindow() {
      InitializeComponent();
      // Get Icon File from WPF Resources
      using (IO.MemoryStream mem = new IO.MemoryStream(Properties.Resources.icon)) {
        System.Drawing.Icon icon = new System.Drawing.Icon(mem);
        notifyIcon.Icon = icon;
      }
      notifyIcon.MouseClick += NotifyIcon_MouseClick;
      AutostartCB.IsChecked = ShortcutExists();
      Instance = this;
      ticker.Tick += Ticker_Tick;
      Store.Positions.LoadPositions();
      if (Store.Positions.StartMinimized) {
        this.WindowState = WindowState.Minimized;
        if (Store.Positions.MinimizeToTray) {
          this.ShowInTaskbar = false;
          this.notifyIcon.Visible = true;
        }
      }
      notifyIcon.Visible = Store.Positions.StartMinimized;
      Store.LoadWindows();
      Store.Positions.PositionList = Store.Positions.PositionList.ToArray().ToList();
      SavedWindowPositions.GetBindingExpression(ListBox.ItemsSourceProperty)?.UpdateTarget();
      StartMinimizedCB.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateTarget();
      MinimizeToTrayCB.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateTarget();
    }

    private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e) {
      if (e.Button == System.Windows.Forms.MouseButtons.Left) {
        this.WindowState = WindowState.Normal;
      }
    }

    protected override void OnStateChanged(EventArgs e) {
      if (this.WindowState == WindowState.Minimized) {
        if (WindowEditor.previewWindow != null) {
          WindowEditor.previewWindow.Close();
        }
        if (Store.Positions.MinimizeToTray) {
          this.ShowInTaskbar = false;
          this.notifyIcon.Visible = true;
        }
      }
      else {
        this.ShowInTaskbar = true;
        this.notifyIcon.Visible = false;
      }
      base.OnStateChanged(e);
    }

    private void Ticker_Tick(object sender, EventArgs e) {
      ticker.Stop();

      IntPtr activeWindow = Klassen.WindowData.GetActiveWindow();
      bool handledAmbientWindows = false;
      Store.Positions.PositionList.ForEach(p => {
        if (p.active) {
          p.FindMatchingWindows().ForEach(h => {
            p.SetPosition(h);
            if (p.removeBorder) {
              p.SetWindowBorder(h, false);
            }
            p.SetAmbientWindows(activeWindow, h, ref handledAmbientWindows);
          });
        }
      });
      if (!handledAmbientWindows) {
        foreach (System.Windows.Forms.Form f in Position.ambientWindowList.ToArray()) {
          f.Close();
        }
      }
      Klassen.WindowData.forceSetForegroundWindow(activeWindow);

      ticker.Start();
    }

    private void SaveWindowPosition_Click(object sender, RoutedEventArgs e) {
      if (Store.WindowDatas.SelectedWindow == null) {
        return;
      }
      Klassen.Position position = new Klassen.Position {
        Name = Store.WindowDatas.SelectedWindow.Title,
        Pattern = "^(" + Regex.Escape(Store.WindowDatas.SelectedWindow.Title) + ")$"
      };
      position.GetPositionFromHandle(Store.WindowDatas.SelectedWindow.Handle);
      Store.Positions.PositionList.Add(position);
      Store.Positions.PositionList = Store.Positions.PositionList.ToArray().ToList();
      SavedWindowPositions.GetBindingExpression(ListBox.ItemsSourceProperty)?.UpdateTarget();
    }

    private void RemoveWindowPosition_Click(object sender, RoutedEventArgs e) {
      if (MessageBox.Show("Are you shure you want to delete this positioning?", "Delete?", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
        if (Store.Positions.SelectedIndex != -1) {
          Store.Positions.PositionList.RemoveAt(Store.Positions.SelectedIndex);
          Store.Positions.PositionList = Store.Positions.PositionList.ToArray().ToList();
          SavedWindowPositions.GetBindingExpression(ListBox.ItemsSourceProperty)?.UpdateTarget();
        }
      }
    }

    private void SavedWindowPositions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      WindowEditor.Position = Store.Positions.SelectedPosition;
      WindowEditor.GetBindingExpression(DataContextProperty)?.UpdateTarget();
      AmbientWindowsDataGrid.DataContext = Store.Positions.SelectedPosition?.ambientWindows;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e) {
      Store.Positions.SavePositions();
    }

    private void ReloadBtn_Click(object sender, RoutedEventArgs e) {
      Store.Positions.LoadPositions();
    }

    private void ReloadWindowsBtn_Click(object sender, RoutedEventArgs e) {
      Store.LoadWindows();
      Store.WindowDatas.Windows = Store.WindowDatas.Windows.ToArray().ToList();
      OpenWindowsLB.GetBindingExpression(ListBox.ItemsSourceProperty)?.UpdateTarget();
    }

    private void WindowEditor_DisplayChanged(object sender, EventArgs e) {
      Store.Positions.PositionList = Store.Positions.PositionList.ToArray().ToList();
      SavedWindowPositions.GetBindingExpression(ListBox.ItemsSourceProperty)?.UpdateTarget();
    }

    protected override void OnClosing(CancelEventArgs e) {
      if (!this.closeForReal) {
        e.Cancel = true;
        this.WindowState = WindowState.Minimized;
      }
      base.OnClosing(e);
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) {
      if (MessageBox.Show("Are you shure you want to close the application?\n\nThe Resolutioner won't automaticaly update your windows if its closed!\n\n(Closing will also save!)", "Close?", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
        Store.Positions.SavePositions();
        this.closeForReal = true;
        this.Close();
      }
    }

    private static string getExePath() {
      IO.FileInfo dllFile = new IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
      if (dllFile.DirectoryName != null) {
        string dir = dllFile.DirectoryName;
        return IO.Path.Combine(dir, "window-resolutioner.exe");
      }
      else {
        throw new Exception("Could not get the directory of the executing assembly!");
      }
    }

    private static string getAutostartLnkPath() {
      string autostartDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
      return IO.Path.Combine(autostartDir, "window-resolutioner.lnk");
    }

    private static bool ShortcutExists() {
      string shortcutPath = getAutostartLnkPath();
      if (IO.File.Exists(shortcutPath)) {
        return true;
      }
      else {
        return false;
      }
    }

    private void AutostartCB_CheckedChange(object sender, EventArgs e) {
      try {
        string exePath = getExePath();
        string shortcutPath = getAutostartLnkPath();
        if (AutostartCB.IsChecked ?? false) {
          Shortcut.CreateShortcut(exePath).WriteToFile(shortcutPath);
          //MessageBox.Show($"Created a shortcut in the autostart folder!\n\n{shortcutPath}");
        }
        else {
          if (IO.File.Exists(shortcutPath)) {
            IO.File.Delete(shortcutPath);
            //MessageBox.Show($"Deleted the shortcut in the autostart folder!\n\n{shortcutPath}");
          }
          else {
            //MessageBox.Show("Shortcut not found!");
          }
        }
      }
      catch (Exception ex) {
        MessageBox.Show("Error: " + ex.Message);
      }
    }

    private void AmbientWindowsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) {
      if (e.PropertyType == typeof(System.Windows.Media.Color)) {
        DataGridTemplateColumn c = new DataGridTemplateColumn() {
          CellTemplate = (DataTemplate)Resources["ColorTemplate"],
          Header = e.Column.Header,
          HeaderTemplate = e.Column.HeaderTemplate,
          HeaderStringFormat = e.Column.HeaderStringFormat,
          SortMemberPath = e.PropertyName,
        };
        e.Column = c;
      }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e) {
      System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
      if (btn != null) {
        DataRowView row = btn.DataContext as DataRowView;
        if (row != null) {
          System.Windows.Media.Color? color = row["Color"] as System.Windows.Media.Color?;
          if (color == null) {
            color = System.Windows.Media.Colors.Black;
          }
          System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
          colorDialog.Color = System.Drawing.Color.FromArgb(color.Value.A, color.Value.R, color.Value.G, color.Value.B);
          if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
            System.Drawing.Color newColor = colorDialog.Color;
            row["Color"] = System.Windows.Media.Color.FromArgb(newColor.A, newColor.R, newColor.G, newColor.B);
            btn.GetBindingExpression(DataContextProperty)?.UpdateTarget();
          }
        }
      }
    }

    private void AmbientWindowsDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e) {
      DataRow newRow = Store.Positions.SelectedPosition?.ambientWindows.NewRow();
      newRow["Active"] = true;
      newRow["Color"] = System.Windows.Media.Colors.Black;
      e.NewItem = newRow;
    }

    private void AmbientWindowsDataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e) {
      DataRowView row = e.NewItem as DataRowView;
      if (row != null) {
        row["Active"] = true;
        row["Color"] = System.Windows.Media.Colors.Black;
      }
    }
  }
}