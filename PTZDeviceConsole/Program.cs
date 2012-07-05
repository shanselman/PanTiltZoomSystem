using System;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Threading;
using PTZ;

namespace PTZDeviceConsole
{
    class Program
    {

        static void Main(string[] args)
        {
            var p = PTZDevice.GetDevice("BCC950 ConferenceCam", PTZType.Relative);

            while (true)
            {
                ConsoleKeyInfo info = Console.ReadKey();
                if (info.Key == ConsoleKey.LeftArrow)
                {
                    p.Move(-1, 0);
                }
                else if (info.Key == ConsoleKey.RightArrow)
                {
                    p.Move(1, 0);
                }
                else if (info.Key == ConsoleKey.UpArrow)
                {
                    p.Move(0, 1);
                }
                else if (info.Key == ConsoleKey.DownArrow)
                {
                    p.Move(0, -1);
                }
                else if (info.Key == ConsoleKey.Home)
                {
                    p.Zoom(1);
                }
                else if (info.Key == ConsoleKey.End)
                {
                    p.Zoom(-1);
                }
            }
        }
    }
}
