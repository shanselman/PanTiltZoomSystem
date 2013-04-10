using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using PTZRemoteController.Core;

namespace PTZRemoteControllerAndroid
{
	[Activity (Label = "PTZ Remote", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation)]
	public class RemoteActivity : Activity
	{
		private PTZRemote _remote;

		protected override async void OnCreate (Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(Resource.Layout.Main);

			PreferenceManager.SetDefaultValues(this, Resource.Xml.Preferences, false);

			await ConnectToRelay();
			AttachHandlers();
		}

		private async Task ConnectToRelay()
		{
			bool connected = false;

			var waitIndicator = new ProgressDialog(this) { Indeterminate = true };
			waitIndicator.SetCancelable(false);
			waitIndicator.SetMessage("Connecting...");
			waitIndicator.Show();

			try
			{
				var prefs = PreferenceManager.GetDefaultSharedPreferences(this);

				_remote = new PTZRemote(
					prefs.GetString("RelayServerUrl", ""), 
					prefs.GetString("RemoteGroup", ""), 
					prefs.GetString("HubName", ""));
				connected = await _remote.Connect();
			}
			catch (Exception)
			{
			}
			finally
			{
				waitIndicator.Hide();
			}

			Toast.MakeText(this, connected ? "Connected!" : "Unable to connect", ToastLength.Short).Show();
		}

		private void AttachHandlers()
		{
			FindViewById<Button>(Resource.Id.MoveUp).Click += delegate { _remote.MoveUp(); };
			FindViewById<Button>(Resource.Id.MoveDown).Click += delegate { _remote.MoveDown(); };
			FindViewById<Button>(Resource.Id.MoveLeft).Click += delegate { _remote.MoveLeft(); };
			FindViewById<Button>(Resource.Id.MoveRight).Click += delegate { _remote.MoveRight(); };
			FindViewById<Button>(Resource.Id.ZoomIn).Click += delegate { _remote.ZoomIn(); };
			FindViewById<Button>(Resource.Id.ZoomOut).Click += delegate { _remote.ZoomOut(); };
		}

		public override bool OnCreateOptionsMenu (IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.RemoteMenu, menu);

			return true;
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			switch (item.ItemId)
			{
				case Resource.Id.Reconnect:
					ConnectToRelay();
					return true;
				case Resource.Id.Settings:
					StartActivity(typeof(SettingsActivity));
					return true;
				default:
					return base.OnOptionsItemSelected(item);
			}
		}
	}
}