using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using UserControl = System.Windows.Controls.UserControl;
using TextBox = System.Windows.Controls.TextBox;

namespace window_resolutioner.Controls {
  /// <summary>
  /// Interaktionslogik für WindowEditor.xaml
  /// </summary>
  public partial class WindowEditor : UserControl {

    public event EventHandler<EventArgs> DisplayChanged;

    public Klassen.Position? Position { get; set; } = null;

    private DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMicroseconds(100), IsEnabled = true };

    public WindowEditor() {
      InitializeComponent();
      timer.Tick += Timer_Tick;
    }

    private void SetPosBtn_Click(object sender, RoutedEventArgs e) {
      if (Position != null) {
        IntPtr activeWindow = Klassen.WindowData.GetActiveWindow();
        Position.FindMatchingWindows().ForEach(
          h => {
            Position.SetPosition(h);
            if (Position.removeBorder) {
              Position.SetWindowBorder(h, false);
            } else {
              Position.SetWindowBorder(h, true);
            }
          }
        );
        Klassen.WindowData.forceSetForegroundWindow(activeWindow);
      }
    }

    private void NameTxb_TextChanged(object sender, TextChangedEventArgs e) {
      DisplayChanged?.Invoke(this, new EventArgs());
    }

    private void ActiveCB_CheckedChange(object sender, RoutedEventArgs e) {
      DisplayChanged?.Invoke(this, new EventArgs());
    }

    public Fenster.PositionPreview? previewWindow = null;
    private void ShowPreviewTBtn_CheckedChange(object sender, RoutedEventArgs e) {
      if (ShowPreviewTBtn.IsChecked == true) {
        ShowPreviewTBtn.Content = "Hide Preview";
        if (Position != null) {
          if (previewWindow == null) {
            previewWindow = new Fenster.PositionPreview(MainWindow.Instance);
            previewWindow.Closed += (s, e) => {
              ShowPreviewTBtn.IsChecked = false;
              previewWindow = null;
            };
          }
          previewWindow.Show();
          previewWindow.BringIntoView();
        }
      } else {
        ShowPreviewTBtn.Content = "Show Preview";
        if (previewWindow != null) {
          previewWindow.Close();
        }
      }
    }

    private void Timer_Tick(object sender, EventArgs e) {
      if (previewWindow != null) {
        if (Position != null) {
          previewWindow.Top = Position.Y;
          previewWindow.Left = Position.X;
          previewWindow.Width = Position.width;
          previewWindow.Height = Position.height;
        } else {
          ShowPreviewTBtn.IsChecked = false;
        }
      }
    }

    private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e) {
      TextBox? tb = sender as TextBox;
      if (tb != null) {
        int value;
        if (int.TryParse(tb.Text, out value)) {
          int jump = 1;
          if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) {
            jump = 5;
          }
          else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
            jump = 10;
          }
          else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
            jump = 100;
          }
          if (e.Delta > 0) {
            value += jump;
          } else {
            value -= jump;
          }
          tb.Text = value.ToString();
          tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
          Timer_Tick(timer, new EventArgs());
        }
      }
      e.Handled = true;
    }
  }
}
