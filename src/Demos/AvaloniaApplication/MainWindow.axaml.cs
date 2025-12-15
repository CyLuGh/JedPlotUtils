using Avalonia.Controls;
using DemoData;

namespace AvaloniaApplication
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Content = new TimeSeriesSampleView { ViewModel = new TimeSeriesSampleViewModel() };
        }
    }
}
