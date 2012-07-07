using System;
using System.Collections.Generic;
using System.Configuration;
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
using SignalR.Client;
using SignalR.Client.Hubs;

namespace PTZRemoteLyncOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        HubConnection connection = null;
        IHubProxy proxy = null;
        string remoteGroup;
        string url;

        private void MainWindow_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private async void MoveClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Control;
            Point p = Point.Parse(ui.Tag.ToString());
            await proxy.Invoke("Move", p.X, p.Y, remoteGroup);
        }

        private async void ZoomClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Control;
            int z = int.Parse(ui.Tag.ToString());
            await proxy.Invoke("Zoom", z, remoteGroup);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            url = ConfigurationManager.AppSettings["relayServerUrl"];
            remoteGroup = ConfigurationManager.AppSettings["remoteGroup"];
            connection = new HubConnection(url);
            proxy = connection.CreateProxy("RelayHub");
            await connection.Start();
            await proxy.Invoke("JoinRelay", remoteGroup);
        }

    }
}
