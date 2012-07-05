using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using DirectShowLib;

namespace PTZ
{
    public enum PTZType
    {
        Relative,
        Absolute
    }

    public class PTZDevice
    {
        private readonly Guid PROPSETID_VIDCAP_CAMERACONTROL = new Guid(0xc6e13370, 0x30ac, 0x11d0, 0xa1, 0x8c, 0x00, 0xa0, 0xc9, 0x11, 0x89, 0x56);
        
        private DsDevice _device;
        private IAMCameraControl _camControl;
        private IKsPropertySet _ksPropertySet;
        private PTZType _type = PTZType.Relative;

        public int ZoomMin { get; set; }
        public int ZoomMax { get; set; }
        public int ZoomStep { get; set; }
        public int ZoomDefault { get; set; }

        private PTZDevice(string name, PTZType type)
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            var device = devices.Where(d => d.Name == name).FirstOrDefault();

            _device = device;
            _type = type;

            if (_device == null) throw new ApplicationException(String.Format("Couldn't find device named {0}!", name));

            IFilterGraph2 graphBuilder = new FilterGraph() as IFilterGraph2;
            IBaseFilter filter = null;
            IMoniker i = _device.Mon as IMoniker;

            graphBuilder.AddSourceFilterForMoniker(i, null, _device.Name, out filter);
            _camControl = filter as IAMCameraControl;
            _ksPropertySet = filter as IKsPropertySet;

            if (_camControl == null) throw new ApplicationException("Couldn't get ICamControl!");
            if (_ksPropertySet == null) throw new ApplicationException("Couldn't get IKsPropertySet!");

            //TODO: Add Absolute
            if (type == PTZType.Relative &&
                !(SupportFor(KSProperties.CameraControlFeature.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE) &&
                SupportFor(KSProperties.CameraControlFeature.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE)))
            {
                throw new NotSupportedException("This camera doesn't appear to support Relative Pan and Tilt");
            }

            //TODO: Do I through NotSupported when methods are called or throw them now?

            //TODO: Do I check for Zoom or ignore if it's not there? 
            InitZoomRanges();
        }

        private bool SupportFor(KSProperties.CameraControlFeature feature)
        {
            KSPropertySupport supported = new KSPropertySupport();
            _ksPropertySet.QuerySupported(PROPSETID_VIDCAP_CAMERACONTROL,(int)feature, out supported);

            return (supported.HasFlag(KSPropertySupport.Set) && supported.HasFlag(KSPropertySupport.Get));
        }

        public void Move(int x, int y) //TODO: Is this the best public API? Should work for Relative AND Absolute, right?
        {
            //TODO: Make it work for Absolute also...using the PTZEnum
            
            //first, tilt
            if (y != 0)
            {
                MoveInternal(KSProperties.CameraControlFeature.KSPROPERTY_CAMERACONTROL_TILT_RELATIVE, y);
            }

            if (x != 0)
            {
                MoveInternal(KSProperties.CameraControlFeature.KSPROPERTY_CAMERACONTROL_PAN_RELATIVE, x);
            }
        }

        private void MoveInternal(KSProperties.CameraControlFeature axis, int value)
        {
            // Create and prepare data structures
            var control = new KSProperties.KSPROPERTY_CAMERACONTROL_S();

            IntPtr controlData = Marshal.AllocCoTaskMem(Marshal.SizeOf(control));
            IntPtr instData = Marshal.AllocCoTaskMem(Marshal.SizeOf(control.Instance));

            control.Instance.Value = value;

            //TODO: Fix for Absolute
            control.Instance.Flags = (int)CameraControlFlags.Relative;

            Marshal.StructureToPtr(control, controlData, true);
            Marshal.StructureToPtr(control.Instance, instData, true);
            var hr2 = _ksPropertySet.Set(PROPSETID_VIDCAP_CAMERACONTROL, (int)axis,
               instData, Marshal.SizeOf(control.Instance), controlData, Marshal.SizeOf(control));

            //TODO: It's a DC motor, no better way?
            Thread.Sleep(20);

            control.Instance.Value = 0; //STOP!
            control.Instance.Flags = (int)CameraControlFlags.Relative;

            Marshal.StructureToPtr(control, controlData, true);
            Marshal.StructureToPtr(control.Instance, instData, true);
            var hr3 = _ksPropertySet.Set(PROPSETID_VIDCAP_CAMERACONTROL, (int)axis,
               instData, Marshal.SizeOf(control.Instance), controlData, Marshal.SizeOf(control));

            if (controlData != IntPtr.Zero) { Marshal.FreeCoTaskMem(controlData); }
            if (instData != IntPtr.Zero) { Marshal.FreeCoTaskMem(instData); }
        }

        private int GetCurrentZoom()
        {
            int oldZoom = 0;
            CameraControlFlags oldFlags = CameraControlFlags.Manual;
            var e = _camControl.Get(CameraControlProperty.Zoom, out oldZoom, out oldFlags);
            return oldZoom;
        }
        
        private void InitZoomRanges()
        {
            int iMin, iMax, iStep, iDefault;
            CameraControlFlags flag;
            _camControl.GetRange(CameraControlProperty.Zoom, out iMin, out iMax, out iStep, out iDefault, out flag);

            //Can't pass properties by refer, so some duplication...
            ZoomMin = iMin;
            ZoomMax = iMax;
            ZoomDefault = iDefault;
            ZoomStep = iStep;
        }

        public int Zoom(int direction)
        {
            int oldZoom = GetCurrentZoom();
            int newZoom = ZoomDefault; 
            if (direction > 0)
                newZoom = oldZoom + 10; //10 is magic...could be anything?
            else if (direction < 0)
                newZoom = oldZoom - 10;

            newZoom = Math.Max(ZoomMin, newZoom);
            newZoom = Math.Min(ZoomMax, newZoom);
            _camControl.Set(CameraControlProperty.Zoom, newZoom, CameraControlFlags.Manual);
            return newZoom;
        }

        public static PTZDevice GetDevice(string name, PTZType type)
        {
            return new PTZDevice(name, type);
        }
    }

    class KSProperties
    {
        public enum CameraControlFeature
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
    }
}