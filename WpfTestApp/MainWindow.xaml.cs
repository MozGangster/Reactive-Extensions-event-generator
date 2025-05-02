using System.Windows;

using System.Reactive.Linq;
using RxMethodGenerator;

namespace WpfTestApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.RxMouseMove().Subscribe(e=> 
        {
            Label1.Content = e.Item2MouseEventArgs.GetPosition(null);
        });
    }
}