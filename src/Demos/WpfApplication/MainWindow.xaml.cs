using System.Windows;

namespace WpfApplication
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
