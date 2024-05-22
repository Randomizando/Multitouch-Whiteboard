using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenWhiteboardMode(object sender, RoutedEventArgs e)
        {
            WhiteboardMode whiteboard = new WhiteboardMode();
            whiteboard.Show();
        }
    }
}
