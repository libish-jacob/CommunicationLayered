using System.Windows;

namespace WpfApplication1
{
    using SocketLayer;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SocketListener.SendMessage();
        }
    }
}
