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

namespace window_resolutioner.Controls {
  /// <summary>
  /// Interaktionslogik für WindowEditor.xaml
  /// </summary>
  public partial class WindowEditor : UserControl {

    public event EventHandler<EventArgs> DisplayChanged;

    public Klassen.Position? Position { get; set; } = null;

    public WindowEditor() {
      InitializeComponent();
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
  }
}
