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
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client;

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

            // Add more selectors as required here.
            //selectors.Add(Lync2010_IMWindow_Selection);
            //selectors.Add(Lync2013_IMWindow_Selection);
            //selectors.Add(Skype5_Selection);

            windowMonitor.Activated += windowMonitor_Activated;
            windowMonitor.Deactivated += windowMonitor_Deactivated;
            windowMonitor.LocationChanged += windowMonitor_LocationChanged;
        }

        HubConnection connection = null;
        IHubProxy proxy = null;
        string remoteGroup;
        string url;
        WindowMonitor windowMonitor = new WindowMonitor();
        List<EventHandler<WindowSelectionEventArgs>> selectors = new List<EventHandler<WindowSelectionEventArgs>>();
        bool isTrackingWindow;
        Point locationBeforeTrack;

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Still allow the user to drag this around if a window isn't being tracked.
            if (e.ChangedButton == MouseButton.Left && !isTrackingWindow)
                this.DragMove();
        }

        private async void MoveClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Control;
            Point p = Point.Parse(ui.Tag.ToString());
            await proxy.Invoke("Move", (int)p.X, (int)p.Y, remoteGroup);
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
            proxy = connection.CreateHubProxy("RelayHub");
            await connection.Start();
            await proxy.Invoke("JoinRelay", remoteGroup);

            if (selectors.Count > 0)
            {
                foreach (var item in selectors)
                    windowMonitor.SelectWindow += item;
                windowMonitor.Start();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try //We really can't do anything except close here
            {
                //base.OnClosing(e); //Prevent a StackOverflow crash
                
                foreach (var item in selectors)
                    windowMonitor.SelectWindow -= item;
                windowMonitor.Stop();
            }
            catch (Exception) { };
        }

        void windowMonitor_LocationChanged(object sender, RectEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                base.Top = e.Rectangle.Bottom - this.Height;
                base.Left = e.Rectangle.Right - this.Width;
            });
        }

        void windowMonitor_Activated(object sender, EventArgs e)
        {
            isTrackingWindow = true;
            Dispatcher.Invoke(() =>
            {
                DragArea.Visibility = System.Windows.Visibility.Hidden;
                locationBeforeTrack = new Point(base.Left, base.Top);
            });
        }

        void windowMonitor_Deactivated(object sender, EventArgs e)
        {
            isTrackingWindow = false;
            Dispatcher.Invoke(() => 
            {
                DragArea.Visibility = System.Windows.Visibility.Visible;
                base.Left = locationBeforeTrack.X;
                base.Top = locationBeforeTrack.Y;
            });
        }

        #region Window Selections
        // MEF is probably overkill for this.
        // If you have another window type or IM client to add you will need to use http://sourceforge.net/projects/windowdetective/
        // to figure out (1) how to determine if a conversation is active and (2) which WindowInformation you should return.
        const string Lync2010_IMWindow_Process = "communicator";
        const string Lync2010_IMWindow_CtrlNotifySink = "CtrlNotifySink";
        const string Lync2010_IMWindow_VideoParent = "LCC_VideoParent";
        const string Lync2010_IMWindow_IMWindow = "IMWindowClass";

        // Specifically for the video in chat windows.
        void Lync2010_IMWindow_Selection(object sender, WindowSelectionEventArgs e)
        {
            if (e.IsSelectionMade) return;

            var info = e.ActiveWindow;
            if (e.Process.ProcessName == Lync2010_IMWindow_Process && info.WindowClass == Lync2010_IMWindow_IMWindow)
            {
                // Get the last CtrlNotifySink that contains a LCC_VideoParent.
                var sink = info.ChildWindows.Last(
                    x => x.WindowClass == Lync2010_IMWindow_CtrlNotifySink &&
                        x.ChildWindows.Any(y => y.WindowClass == Lync2010_IMWindow_VideoParent));

                // Check if we got a result.
                if (sink == null) return;

                // Check that the CtrlNotifySink is visible.
                if (sink.Style.HasFlag(WindowMonitor.WindowStyles.Visible))
                {
                    // Select the video area so that we get the overlay
                    // appearing there.
                    e.Select(sink);
                }
            }
        }



        // MEF is probably overkill for this.
        // If you have another window type or IM client to add you will need to use http://sourceforge.net/projects/windowdetective/
        // to figure out (1) how to determine if a conversation is active and (2) which WindowInformation you should return.
        const string Lync2013_IMWindow_Process = "lync";
        const string Lync2013_IMWindow_CtrlNotifySink = "NetUICtrlNotifySink";
        const string Lync2013_IMWindow_VideoParent = "LyncVdiBorderWindowClass";
        const string Lync2013_IMWindow_IMWindow = "LyncConversationWindowClass";

        // Specifically for the video in chat windows.
        void Lync2013_IMWindow_Selection(object sender, WindowSelectionEventArgs e)
        {
            if (e.IsSelectionMade) return;

            var info = e.ActiveWindow;
            if (e.Process.ProcessName == Lync2013_IMWindow_Process && info.WindowClass == Lync2013_IMWindow_IMWindow)
            {
                // Get the last CtrlNotifySink that contains a LCC_VideoParent.
                //var sink = info.ChildWindows.First(
                //    x => x.WindowClass == Lync2013_IMWindow_CtrlNotifySink &&
                //        x.ChildWindows.Any(y => y.WindowClass == Lync2013_IMWindow_VideoParent));

                //TODO: Hm, looks like Lync 2013 does everything owner-draw so none of the child windows has any size or dimension?
                //TODO: What do we do when Lync is marked as "always on top?"
                var sink = info.ChildWindows.First(x => x.WindowClass == "NetUIHWND");

                // Check if we got a result.
                if (sink == null) return;

                // Check that the CtrlNotifySink is visible.
                if (sink.Style.HasFlag(WindowMonitor.WindowStyles.Visible))
                {
                    // Select the video area so that we get the overlay
                    // appearing there.
                    e.Select(sink);
                }
            }
        }


        #endregion
    }
}
