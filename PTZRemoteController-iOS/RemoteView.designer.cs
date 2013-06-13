// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace PTZRemoteControlleriOS
{
	[Register ("RemoteView")]
	partial class RemoteView
	{
		[Outlet]
		MonoTouch.UIKit.UIButton MoveUp { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton MoveLeft { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton MoveRight { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton MoveDown { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIStepper ZoomLevel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (MoveUp != null) {
				MoveUp.Dispose ();
				MoveUp = null;
			}

			if (MoveLeft != null) {
				MoveLeft.Dispose ();
				MoveLeft = null;
			}

			if (MoveRight != null) {
				MoveRight.Dispose ();
				MoveRight = null;
			}

			if (MoveDown != null) {
				MoveDown.Dispose ();
				MoveDown = null;
			}

			if (ZoomLevel != null) {
				ZoomLevel.Dispose ();
				ZoomLevel = null;
			}
		}
	}
}
