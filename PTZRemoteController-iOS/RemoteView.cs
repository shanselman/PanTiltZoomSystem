using System;
using System.Threading.Tasks;
using MBProgressHUD;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using PTZRemoteController.Core;

namespace PTZRemoteControlleriOS
{
    public partial class RemoteView : UIViewController
    {
        private PTZRemote _remote;
        private double _currentZoomLevel = 0;

        public RemoteView() : base ("RemoteView", null)
        {
        }
		
        public override async void ViewDidLoad ()
        {
            base.ViewDidLoad();

            Title = "PTZ Remote";
            View.BackgroundColor = UIColor.LightGray;

			_remote = new PTZRemote();

			AttachHandlers();

			var refreshButton = new UIBarButtonItem(UIBarButtonSystemItem.Refresh);
			refreshButton.Clicked += delegate { ConnectToRelay(); };
			NavigationItem.RightBarButtonItem = refreshButton;

			await ConnectToRelay();
        }

        private async Task ConnectToRelay()
        {
            bool connected = false;

            var waitIndicator = new MTMBProgressHUD (View) 
            {
                LabelText = "Connecting...",
                DimBackground = true,
                AnimationType = MBProgressHUDAnimation.MBProgressHUDAnimationZoomIn,
                Mode = MBProgressHUDMode.Indeterminate,
                MinShowTime = 0,
                RemoveFromSuperViewOnHide = true
            };
            View.AddSubview(waitIndicator);
            waitIndicator.Show(animated: true);

            try
            {
                var prefs = NSUserDefaults.StandardUserDefaults;
                
				connected = await _remote.Connect(prefs.StringForKey("RelayServerUrl"), 
				                                  prefs.StringForKey("RemoteGroup"), 
				                                  prefs.StringForKey("HubName"));
            }
            catch (Exception)
            {
            }
            finally
            {
                waitIndicator.Hide(animated: true);
            }

            ShowMessage(connected ? "Connected!" : "Unable to connect");
        }

        private void AttachHandlers()
        {
            MoveUp.TouchUpInside += delegate { _remote.MoveUp(); };
            MoveDown.TouchUpInside += delegate { _remote.MoveDown(); };
            MoveRight.TouchUpInside += delegate { _remote.MoveRight(); };
            MoveLeft.TouchUpInside += delegate { _remote.MoveLeft(); };
            
            ZoomLevel.MinimumValue = double.MinValue;
            ZoomLevel.MaximumValue = double.MaxValue;
            ZoomLevel.Value = _currentZoomLevel;
            ZoomLevel.ValueChanged += delegate 
            {
                if (ZoomLevel.Value > _currentZoomLevel)
                    _remote.ZoomIn();
                else
                    _remote.ZoomOut();
                
                _currentZoomLevel = ZoomLevel.Value;
            };
        }

        private void ShowMessage(string message) 
        {
            var hud = new MTMBProgressHUD (View) 
            {
                DetailsLabelText = message,
                RemoveFromSuperViewOnHide = true,
                DimBackground = false,
                AnimationType = MBProgressHUDAnimation.MBProgressHUDAnimationZoomIn,
                Mode = MBProgressHUDMode.Text
            };
            View.AddSubview (hud);

            hud.Show (animated: true);
            hud.Hide (animated: true, delay: 1.5);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            
            ReleaseDesignerOutlets();
        }
    }
}

