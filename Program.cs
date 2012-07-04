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

        static void Main(string[] args)
        {
            
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            var dev = devices[1] as DsDevice;
            IFilterGraph2 graphBuilder = new FilterGraph() as IFilterGraph2;
            DirectShowLib.IBaseFilter filter = null;
            IMoniker i = dev.Mon as IMoniker;
            graphBuilder.AddSourceFilterForMoniker(i, null, dev.Name, out filter);
            var CamControl = filter as IAMCameraControl;
            int oldZoom = 0;
            CameraControlFlags oldFlags;
            CamControl.Get(CameraControlProperty.Zoom, out oldZoom, out oldFlags);
            CamControl.Set(CameraControlProperty.Zoom, 150, CameraControlFlags.Manual );
            //Console.ReadLine();
            CamControl.Set(CameraControlProperty.Zoom, 100, CameraControlFlags.Manual);

            //This doesn't work and that's deeply lame
            int oldPan = 0;
            int oldTilt = 0;
            var e1 = CamControl.Get(CameraControlProperty.Pan, out oldPan, out oldFlags);
            var e2 = CamControl.Get(CameraControlProperty.Tilt, out oldTilt, out oldFlags);



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
            hex = hex.Replace("-",":");
            Console.WriteLine(hex);
            Console.WriteLine();

            _device.ReadReport(OnReport);
        }
    }
}
