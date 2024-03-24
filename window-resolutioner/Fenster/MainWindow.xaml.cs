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

namespace window_resolutioner {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private DispatcherTimer ticker = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1), IsEnabled = true };

    public Klassen.Store Store { get; set; } = new Klassen.Store();

    public MainWindow() {
      InitializeComponent();
      ticker.Tick += Ticker_Tick;
      Store.Positions.LoadPositions();
      Store.LoadWindows();
      //this.GetBindingExpression(DataContextProperty)?.UpdateTarget();
    }

    private void Ticker_Tick(object sender, EventArgs e) {
      ticker.Stop();

      Store.Positions.PositionList.ForEach(p => {
        if (p.active) {
          p.FindMatchingWindows().ForEach(h => {
            p.SetPosition(h);
          });
        }
      });

      ticker.Start();
    }

    private void SaveWindowPosition_Click(object sender, RoutedEventArgs e) {
      if (Store.WindowDatas.SelectedWindow == null) {
        return;
      }
      Klassen.Position position = new Klassen.Position {
        Name = Store.WindowDatas.SelectedWindow.Title,
        Pattern = Regex.Escape(Store.WindowDatas.SelectedWindow.Title)
      };
      position.GetPositionFromHandle(Store.WindowDatas.SelectedWindow.Handle);
      Store.Positions.PositionList.Add(position);
      Store.Positions.PositionList = Store.Positions.PositionList.ToArray().ToList();
      SavedWindowPositions.GetBindingExpression(ListBox.ItemsSourceProperty)?.UpdateTarget();
    }

    private void RemoveWindowPosition_Click(object sender, RoutedEventArgs e) {
      if (MessageBox.Show("Wirklich löschen?", "Löschen", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
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
  }
}