using Android.App;
using Android.OS;
using PTZRemoteController.Core;
using Android.Widget;

namespace PTZRemoteControllerAndroid
{
	[Activity (Label = "PTZ Remote Control", MainLauncher = true)]
	public class RemoteActivity : Activity
	{
		private const string RelayServerUrl = "https://hanselmanlyncrelay.azurewebsites.net";
		private const string RemoteGroup = "SHANSELMAN";
		private const string HubName = "RelayHub";
		private PTZRemote _remote;

		protected override async void OnCreate (Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(Resource.Layout.Main);

			_remote = new PTZRemote(RelayServerUrl, RemoteGroup, HubName);
			bool connected = await _remote.Connect();

			Toast.MakeText(this, "Connected: " + connected, ToastLength.Short).Show();
		}
	}
}