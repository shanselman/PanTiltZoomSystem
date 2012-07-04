using System;
using DirectShowLib;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleApplication3
{
    class Program
    {
        enum KSPROPERTY_VIDCAP_CAMERACONTROL
        {
            KSPROPERTY_CAMERACONTROL_PAN,
            KSPROPERTY_CAMERACONTROL_TILT,
            KSPROPERTY_CAMERACONTROL_ROLL,
            KSPROPERTY_CAMERACONTROL_ZOOM,
            KSPROPERTY_CAMERACONTROL_EXPOSURE,
            KSPROPERTY_CAMERACONTROL_IRIS,
            KSPROPERTY_CAMERACONTROL_FOCUS,
            KSPROPERTY_CAMERACONTROL_SCANMODE,
            KSPROPERTY_CAMERACONTROL_PRIVACY,
            KSPROPERTY_CAMERACONTROL_PANTILT,
            KSPROPERTY_CAMERACONTROL_PAN_RELATIVE,
            KSPROPERTY_CAMERACONTROL_TILT_RELATIVE,
            KSPROPERTY_CAMERACONTROL_ROLL_RELATIVE,
            KSPROPERTY_CAMERACONTROL_ZOOM_RELATIVE,
            KSPROPERTY_CAMERACONTROL_EXPOSURE_RELATIVE,
            KSPROPERTY_CAMERACONTROL_IRIS_RELATIVE,
            KSPROPERTY_CAMERACONTROL_FOCUS_RELATIVE,
            KSPROPERTY_CAMERACONTROL_PANTILT_RELATIVE,
            KSPROPERTY_CAMERACONTROL_FOCAL_LENGTH,
            KSPROPERTY_CAMERACONTROL_AUTO_EXPOSURE_PRIORITY
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KSPROPERTY
        {
            // size Guid is long + 2 short + 8 byte = 4 longs
            Guid Set;
            [MarshalAs(UnmanagedType.U4)]
            int Id;
            [MarshalAs(UnmanagedType.U4)]
            int Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KSPROPERTY_CAMERACONTROL_S
        {
            /// <summary> Property Guid </summary>
            public KSPROPERTY Property;
            public KSPROPERTY_CAMERACONTROL Instance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KSPROPERTY_CAMERACONTROL
        {
            [MarshalAs(UnmanagedType.I4)]
            public int Value;

            [MarshalAs(UnmanagedType.U4)]
            public int Flags;

            [MarshalAs(UnmanagedType.U4)]
            public int Capabilities;

            [MarshalAs(UnmanagedType.U4)]
            public int Dummy;
            // Dummy added to get a succesful return of the Get, Set function
        }

        static void Main(string[] args)
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            //TODO: Config? First Logitech?
            var dev = devices[1] as DsDevice;
            IFilterGraph2 graphBuilder = new FilterGraph() as IFilterGraph2;
            DirectShowLib.IBaseFilter filter = null;
            IMoniker i = dev.Mon as IMoniker;
            graphBuilder.AddSourceFilterForMoniker(i, null, dev.Name, out filter);
            var CamControl = filter as IAMCameraControl;
            var KSCrazyStuff = filter as IKsPropertySet;

            Guid PROPSETID_VIDCAP_CAMERACONTROL = new Guid(0xc6e13370, 0x30ac, 0x11d0, 0xa1, 0x8c, 0x00, 0xa0, 0xc9, 0x11, 0x89, 0x56);

            KSPropertySupport supported = new KSPropertySupport();
            KSCrazyStuff.QuerySupported(PROPSETID_VIDCAP_CAMERACONTROL,
                (int)KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE,
                out supported);

            if (supported.HasFlag(KSPropertySupport.Set) && supported.HasFlag(KSPropertySupport.Get))
            {
                // Create and prepare data structures
                KSPROPERTY_CAMERACONTROL_S control = new KSPROPERTY_CAMERACONTROL_S();

                IntPtr controlData = Marshal.AllocCoTaskMem(Marshal.SizeOf(control));
                IntPtr instData = Marshal.AllocCoTaskMem(Marshal.SizeOf(control.Instance));
                int cbBytes = 0;

                //// Convert the data
                //Marshal.StructureToPtr(control, controlData, true);
                //Marshal.StructureToPtr(control.Instance, instData, true);

                int oldZoom = 0;
                CameraControlFlags oldFlags = CameraControlFlags.Manual;
                var e = CamControl.Get(CameraControlProperty.Pan, out oldZoom, out oldFlags);

                int iMin, iMax, iStep, iDefault;
                CameraControlFlags flag;
                CamControl.GetRange(CameraControlProperty.Zoom, out iMin, out iMax, out iStep, out iDefault, out flag);

                while (true)
                {
                    int moveDir = 0;
                    var axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE;
                    ConsoleKeyInfo info = Console.ReadKey();
                    if (info.Key == ConsoleKey.LeftArrow)
                    {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE;
                        moveDir = -1;
                    }
                    else if (info.Key == ConsoleKey.RightArrow)
                    {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE;
                        moveDir = 1;
                    }
                    else if (info.Key == ConsoleKey.UpArrow)
                    {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE;
                        moveDir = 1;
                    }
                    else if (info.Key == ConsoleKey.DownArrow)
                    {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE;
                        moveDir = -1;
                    }
                    else if (info.Key == ConsoleKey.Home)
                    {
                        oldZoom = Zoom(CamControl, oldZoom, 1);

                        continue;
                    }
                    else if (info.Key == ConsoleKey.End)
                    {
                        oldZoom = Zoom(CamControl, oldZoom, -1);
                        continue;
                    }

                    control.Instance.Value = moveDir;
                    control.Instance.Flags = (int)CameraControlFlags.Relative;

                    Marshal.StructureToPtr(control, controlData, true);
                    Marshal.StructureToPtr(control.Instance, instData, true);
                    var hr2 = KSCrazyStuff.Set(PROPSETID_VIDCAP_CAMERACONTROL,
                        (int)axis,
                       instData, Marshal.SizeOf(control.Instance), controlData, Marshal.SizeOf(control));

                    Thread.Sleep(20);

                    control.Instance.Value = 0;
                    control.Instance.Flags = (int)CameraControlFlags.Relative;

                    Marshal.StructureToPtr(control, controlData, true);
                    Marshal.StructureToPtr(control.Instance, instData, true);
                    var hr3 = KSCrazyStuff.Set(PROPSETID_VIDCAP_CAMERACONTROL,
                        (int)axis,
                       instData, Marshal.SizeOf(control.Instance), controlData, Marshal.SizeOf(control));

                    //if (controlData != IntPtr.Zero) { Marshal.FreeCoTaskMem(controlData); }
                    //if (instData != IntPtr.Zero) { Marshal.FreeCoTaskMem(instData); }

                    //TODO: We are leaking like a sieve.
                }
            }
        }

        private static int Zoom(IAMCameraControl CamControl, int oldZoom, int direction)
        {
            int newZoom = 100;
            if (direction > 0)
                newZoom = oldZoom + 10;
            else if (direction < 0)
                newZoom = oldZoom - 10;

            newZoom = Math.Max(100, newZoom);
            newZoom = Math.Min(500, newZoom);
            CamControl.Set(CameraControlProperty.Zoom, newZoom, CameraControlFlags.Manual);
            return newZoom;
        }
    }
}
