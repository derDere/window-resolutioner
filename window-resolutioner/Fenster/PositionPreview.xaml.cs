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
using System.Windows.Shapes;

namespace window_resolutioner.Fenster;
/// <summary>
/// Interaktionslogik für PositionPreview.xaml
/// </summary>
public partial class PositionPreview : Window {

  public MainWindow mainWindow { get; set; }

  public PositionPreview(MainWindow mainWindow) {
    InitializeComponent();
    this.mainWindow = mainWindow;
  }

  protected override void OnMouseDown(MouseButtonEventArgs e) {
    mainWindow.Activate();
    base.OnMouseDown(e);
  }
}
