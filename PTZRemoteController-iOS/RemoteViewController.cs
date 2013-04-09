using MonoTouch.UIKit;
using PTZRemoteController.Core;

namespace PTZRemoteControlleriOS
{
	public partial class RemoteViewController : UIViewController
	{
		private const string RelayServerUrl = "https://hanselmanlyncrelay.azurewebsites.net";
		private const string RemoteGroup = "SHANSELMAN";
		private const string HubName = "RelayHub";
		private PTZRemote _remote;

		public RemoteViewController() : base ("RemoteViewController", null)
		{
		}

		public override async void ViewDidLoad ()
		{
			base.ViewDidLoad();

			_remote = new PTZRemote(RelayServerUrl, RemoteGroup, HubName);
			bool connected = await _remote.Connect();

			new UIAlertView("connected state", connected.ToString(), null, "Ok", null).Show();
		}
	}
}