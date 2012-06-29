using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidLibrary;

namespace ConsoleApplication3
{
    class Program
    {

        private static HidDevice _device;

        static void Main(string[] args)
        {
            //var devices = HidDevices.GetDevice("\\?\hid#vid_046d&pid_0838&mi_03&col01#8&e58617f&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}");
            //foreach (var device in devices)
            _device = HidDevices.Enumerate(0x46D, 0x838).FirstOrDefault();

            Console.WriteLine(_device.Description);
            try
            {
                //_device.OpenDevice();
                //_device.MonitorDeviceEvents = true;
                _device.ReadReport(OnReport);
                Console.ReadKey();
            }
            finally
            {
                _device.CloseDevice();
            }
            Console.WriteLine("closed");
            
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
