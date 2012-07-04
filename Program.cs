using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidLibrary;
using DirectShowLib;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleApplication3
{
    class Program
    {

        private static HidDevice _device;

        private const int HidReportId = 0x4;

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
            KSPROPERTY_CAMERACONTROL_PRIVACY                  // RW O
                ,
            KSPROPERTY_CAMERACONTROL_PANTILT                  // RW O
                ,
            KSPROPERTY_CAMERACONTROL_PAN_RELATIVE             // RW O
                ,
            KSPROPERTY_CAMERACONTROL_TILT_RELATIVE            // RW O
                ,
            KSPROPERTY_CAMERACONTROL_ROLL_RELATIVE            // RW O
                ,
            KSPROPERTY_CAMERACONTROL_ZOOM_RELATIVE            // RW O
                ,
            KSPROPERTY_CAMERACONTROL_EXPOSURE_RELATIVE        // RW O
                ,
            KSPROPERTY_CAMERACONTROL_IRIS_RELATIVE            // RW O
                ,
            KSPROPERTY_CAMERACONTROL_FOCUS_RELATIVE           // RW O
                ,
            KSPROPERTY_CAMERACONTROL_PANTILT_RELATIVE         // RW O
                ,
            KSPROPERTY_CAMERACONTROL_FOCAL_LENGTH             // R  O    
                , KSPROPERTY_CAMERACONTROL_AUTO_EXPOSURE_PRIORITY   // RW O

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
                KSPROPERTY_CAMERACONTROL_S control =
                    new KSPROPERTY_CAMERACONTROL_S();

                IntPtr controlData = Marshal.AllocCoTaskMem(Marshal.SizeOf(control));
                IntPtr instData = Marshal.AllocCoTaskMem(Marshal.SizeOf(control.Instance));
                int cbBytes = 0;

                //// Convert the data
                //Marshal.StructureToPtr(control, controlData, true);
                //Marshal.StructureToPtr(control.Instance, instData, true);

                ////Get pan data
                //var hr = KSCrazyStuff.Get(PROPSETID_VIDCAP_CAMERACONTROL,
                //   (int)KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE,
                //   instData, Marshal.SizeOf(control.Instance), controlData, Marshal.SizeOf(control), out cbBytes);

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
                    if (info.Key == ConsoleKey.LeftArrow) {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE;
                        moveDir = -1;
                    } else if (info.Key == ConsoleKey.RightArrow) {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE;
                        moveDir = 1;
                    } else if(info.Key == ConsoleKey.UpArrow) {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE;
                        moveDir = 1;
                    } else if(info.Key == ConsoleKey.DownArrow) {
                        axis = KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE;
                        moveDir = -1;
                    }
                    else if (info.Key == ConsoleKey.Home) {
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
                }
            }


            return;





            return;



            //var devices = HidDevices.GetDevice("\\?\hid#vid_046d&pid_0838&mi_03&col01#8&e58617f&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}");
            //foreach (var device in devices)
            _device = HidDevices.Enumerate(0x46D, 0x838).FirstOrDefault();

            Console.WriteLine(_device.Description);
            try
            {
                //_device.OpenDevice();
                //_device.MonitorDeviceEvents = true;
                _device.ReadReport(OnReport);

                var h = _device.CreateReport();
                h.ReportId = 236;
                h.Data[0] = 0x1;

                _device.WriteReport(h);
                Console.ReadKey();
            }
            finally
            {
                _device.CloseDevice();
            }
            Console.WriteLine("closed");

        }

        private static int Zoom(IAMCameraControl CamControl, int oldZoom, int direction)
        {
            int newZoom = 100;
            if(direction > 0)
                newZoom = oldZoom + 10;
            else if (direction < 0)
                newZoom = oldZoom - 10;

            newZoom = Math.Max(100, newZoom);
            newZoom = Math.Min(500, newZoom);
            CamControl.Set(CameraControlProperty.Zoom, newZoom, CameraControlFlags.Manual);
            return newZoom;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }


        private static void OnReport(HidReport report)
        {
            if (!_device.IsConnected) { return; }

            string hex = BitConverter.ToString(report.Data);
            hex = hex.Replace("-", ":");
            Console.WriteLine(hex);
            Console.WriteLine();

            _device.ReadReport(OnReport);
        }
    }
}
