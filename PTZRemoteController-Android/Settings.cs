using Android.Preferences;
using Android.App;
using Android.OS;

namespace PTZRemoteControllerAndroid
{
	[Activity (Label = "Settings")]
	public class SettingsActivity : Activity
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			FragmentManager
				.BeginTransaction()
				.Replace(Android.Resource.Id.Content, new SettingsFragment())
				.Commit();
		}
	}

	public class SettingsFragment : PreferenceFragment
	{
		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			AddPreferencesFromResource(Resource.Xml.Preferences);
		}
	}
}