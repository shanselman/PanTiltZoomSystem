using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidLibrary;
using DirectShowLib;
using System.Runtime.InteropServices.ComTypes;

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

            KSCrazyStuff.Set(PROPSETID_VIDCAP_CAMERACONTROL,
                (int)KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE, 
                IntPtr.Zero, 0, IntPtr.Zero, 1);


            KSCrazyStuff.Set(PROPSETID_VIDCAP_CAMERACONTROL,
                (int)KSPROPERTY_VIDCAP_CAMERACONTROL.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE,
                IntPtr.Zero, 0, IntPtr.Zero, 0);


            int oldZoom = 0;
            CameraControlFlags oldFlags = CameraControlFlags.Manual;
            var e = CamControl.Get(CameraControlProperty.Pan, out oldZoom, out oldFlags);
            CamControl.Set(CameraControlProperty.Zoom, 500, CameraControlFlags.Manual);
            //Console.ReadLine();
            CamControl.Set(CameraControlProperty.Zoom, 100, CameraControlFlags.Manual);

            int iMin, iMax, iStep, iDefault;
            CameraControlFlags flag;
            CamControl.GetRange(CameraControlProperty.Zoom, out iMin, out iMax, out iStep, out iDefault, out flag);

            //This doesn't work and that's deeply lame
            int oldPan = 0;
            int oldTilt = 0;
            var e1 = CamControl.Get(CameraControlProperty.Pan, out oldPan, out oldFlags);
            var e2 = CamControl.Get(CameraControlProperty.Tilt, out oldTilt, out oldFlags);

            var e3 = CamControl.Set(CameraControlProperty.Pan, 1, CameraControlFlags.Manual | CameraControlFlags.Relative);



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
