using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using Microsoft.AspNet.SignalR.Client;
using PTZ;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace PTZRemoteCameraListener
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
        PTZDevice device;
        string remoteGroup;
        string url;
        SynchronizationContext magic;

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var deviceName = ConfigurationManager.AppSettings["DeviceName"];
            device = PTZDevice.GetDevice(deviceName, PTZType.Relative);

            url = ConfigurationManager.AppSettings["relayServerUrl"];
            remoteGroup = Environment.MachineName; //They have to hardcode the group, but for us it's our machine name
            connection = new HubConnection(url);
            proxy = connection.CreateHubProxy("RelayHub");

            connection.TraceLevel = TraceLevels.StateChanges | TraceLevels.Events;
            connection.TraceWriter = new PTZRemoteTraceWriter(Log);

            //Can't do this here because DirectShow has to be on the UI thread!
            // This would cause an obscure COM casting error with no clue what's up. So, um, ya.
            //proxy.On<int, int>("Move",(x,y) => device.Move(x, y));
            //proxy.On<int>("Zoom", (z) => device.Zoom(z));

            magic = SynchronizationContext.Current;

            proxy.On<int, int>("Move", (x, y) =>
            {
                //Toss this over the fence from this background thread to the UI thread
                magic.Post((_) => {
                    Log(String.Format("Move({0},{1})", x,y));
                    device.Move(x, y);
                }, null);
            });

            proxy.On<int>("Zoom", (z) =>
            {
                magic.Post((_) =>
                {
                    Log(String.Format("Zoom({0})", z));
                    device.Zoom(z);
                }, null);
            });


            try
            {
                await connection.Start();
                Log("After connection.Start()");
                await proxy.Invoke("JoinRelay", remoteGroup);
                Log("After JoinRelay");
            }
            catch (Exception pants)
            {
                Log(pants.GetError().ToString());
                throw;
            }
        }

        public void Log(string messsage)
        {
            magic.Post((_) =>
            {
                TextLog.Inlines.Add(new LineBreak());
                TextLog.Inlines.Add(new Run(messsage));

                Scroll.UpdateLayout();
                Scroll.ScrollToVerticalOffset(TextLog.ActualHeight);
            }, null);
        }
    }
}