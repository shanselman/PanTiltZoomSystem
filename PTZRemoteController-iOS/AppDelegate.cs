using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PTZRemoteControlleriOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		private UIWindow _window;
		private UIViewController _rootViewController;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			_window = new UIWindow (UIScreen.MainScreen.Bounds);
			
            var prefs = NSUserDefaults.StandardUserDefaults;

            if (prefs.StringForKey("RelayServerUrl") == null)
            {
                prefs.SetString("https://hanselmanlyncrelay.azurewebsites.net", "RelayServerUrl");
                prefs.SetString("SHANSELMAN", "RemoteGroup");
                prefs.SetString("RelayHub", "HubName");
            }

            prefs.Init();

            _rootViewController = new UINavigationController(new RemoteView());

			_window.RootViewController = _rootViewController;
			_window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}