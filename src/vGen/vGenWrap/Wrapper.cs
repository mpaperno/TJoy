using System;
using System.Runtime.InteropServices;

//////////////////////////////////////////////////////////////////////////////////////
///
///  vJoy interface fuctions (Native vJoy)
///  If you wish to write GENERIC code (Used for vJoy AND vXbox)
///  then you can use this set of interface functions
///
///  vJoy device ID range: 1-16
///  vXbox device ID range: 1001-1004
///
///  Axis & Button Mapping:
///  According to https://msdn.microsoft.com/en-us/library/windows/desktop/hh405052(v=vs.85).aspx
///  (Assuming XUSB subtype  1 - Gamepad)
///
///  vJoy      | Xbox
///  -----------------------
///  X         | X
///  Y         | Y
///  Z         | Trigger (R)
///  Rx        | Rx
///  Ry        | Ry
///  Rz        | Trigger (L)
///  Button 1  | A
///  Button 2  | B
///  Button 3  | X
///  Button 4  | Y
///  Button 5  | Left Bumper (LB) A.K.A Left Shoulder
///  Button 6  | Right Bumper (RB) A.K.A Right Shoulder
///  Button 7  | Back
///  Button 8  | Start
///  Button 9  | Guide
///  Button 10 | Left Thumb (LT)
///  Button 11 | Right Thumb (RT)
///
///  Axis Serial number is:
///    | vJoy    |vXbox
///  --------------------------
///  1 | X       | X
///  2 | Y       | Y
///  3 | Z       | Trigger (R)
///  4 | RX      | Rx
///  5 | RY      | Ry
///  6 | RZ      | Trigger (L)
///  7 | Slider0 | -
///  8 | Slider1 | -
///

public enum HID_USAGES : ushort
{
    HID_USAGE_X   = 0x30,
    HID_USAGE_LX  = HID_USAGE_X,
    HID_USAGE_Y   = 0x31,
    HID_USAGE_LY  = HID_USAGE_Y,
    HID_USAGE_Z   = 0x32,
    HID_USAGE_LT  = HID_USAGE_Z,
    HID_USAGE_RX  = 0x33,
    HID_USAGE_RY  = 0x34,
    HID_USAGE_RZ  = 0x35,
    HID_USAGE_RT  = HID_USAGE_RZ,
    HID_USAGE_SL0 = 0x36,
    HID_USAGE_SL1 = 0x37,
    HID_USAGE_WHL = 0x38,
    HID_USAGE_POV = 0x39,
}

[Flags]
public enum XINPUT_BUTTONS : ushort
{
    NONE            = 0,
    DPAD_UP         = 0x0001,
    DPAD_DOWN       = 0x0002,
    DPAD_LEFT       = 0x0004,
    DPAD_RIGHT      = 0x0008,
    START           = 0x0010,
    BACK            = 0x0020,
    LEFT_THUMB      = 0x0040,
    RIGHT_THUMB     = 0x0080,
    LEFT_SHOULDER   = 0x0100,
    RIGHT_SHOULDER  = 0x0200,
    GUIDE           = 0x0400,
    A               = 0x1000,
    B               = 0x2000,
    X               = 0x4000,
    Y               = 0x8000,
    DPAD_UP_RIGHT   = DPAD_UP | DPAD_RIGHT,
    DPAD_UP_LEFT    = DPAD_UP | DPAD_LEFT,
    DPAD_DOWN_RIGHT = DPAD_DOWN | DPAD_RIGHT,
    DPAD_DOWN_LEFT  = DPAD_DOWN | DPAD_LEFT,
    DPAD_MASK       = 0x000F,
}

public enum VJDSTATUS : short
{
    VJD_STAT_OWN,	// The  vJoy Device is owned by this application.
    VJD_STAT_FREE,	// The  vJoy Device is NOT owned by any application (including this one).
    VJD_STAT_BUSY,	// The  vJoy Device is owned by another application. It cannot be acquired by this application.
    VJD_STAT_MISS,	// The  vJoy Device is missing. It either does not exist or the driver is down.
    VJD_STAT_UNKN	// Unknown
};

// all possible result status codes for xoutput native and common APIs
public enum VJRESULT : UInt32
{
    SUCCESS                 = 0x00000000,
    UNSUCCESSFUL            = 0xC0000001,
    INVALID_HANDLE          = 0xC0000008,
    INVALID_PARAMETER       = 0xC000000D,
    NO_SUCH_DEVICE          = 0xC000000E,
    DEVICE_ALREADY_ATTACHED = 0xC0000038,
    MEMORY_NOT_ALLOCATED    = 0xC00000A0,
    DEVICE_NOT_READY        = 0xC00000A3,
    DEVICE_DOES_NOT_EXIST   = 0xC00000C0,
    INVALID_PARAMETER_1     = 0xC00000EF,
    INVALID_PARAMETER_2     = 0xC00000F0,
    INVALID_PARAMETER_3     = 0xC00000F1,
    TIMEOUT                 = 0x00000102,
    IO_DEVICE_ERROR         = 0xC0000185,
    RESOURCE_NOT_OWNED      = 0xC0000264,
    DEVICE_REMOVED          = 0xC00002B6,
    DEVICE_NOT_CONNECTED    = 0x0000048F,  // winerror for XInput
    DEVICE_NOT_AVAILABLE    = 0x000010DF,  // winerror
}

// FFB Declarations

// HID Descriptor definitions - FFB Report IDs

public enum FFBPType // FFB Packet Type
{
	// Write
	PT_EFFREP	=  0x01,	// Usage Set Effect Report
	PT_ENVREP	=  0x02,	// Usage Set Envelope Report
	PT_CONDREP	=  0x03,	// Usage Set Condition Report
	PT_PRIDREP	=  0x04,	// Usage Set Periodic Report
	PT_CONSTREP	=  0x05,	// Usage Set Constant Force Report
	PT_RAMPREP	=  0x06,	// Usage Set Ramp Force Report
	PT_CSTMREP	=  0x07,	// Usage Custom Force Data Report
	PT_SMPLREP	=  0x08,	// Usage Download Force Sample
	PT_EFOPREP	=  0x0A,	// Usage Effect Operation Report
	PT_BLKFRREP	=  0x0B,	// Usage PID Block Free Report
	PT_CTRLREP	=  0x0C,	// Usage PID Device Control
	PT_GAINREP	=  0x0D,	// Usage Device Gain Report
	PT_SETCREP	=  0x0E,	// Usage Set Custom Force Report

	// Feature
	PT_NEWEFREP	=  0x01+0x10,	// Usage Create New Effect Report
	PT_BLKLDREP	=  0x02+0x10,	// Usage Block Load Report
	PT_POOLREP	=  0x03+0x10,	// Usage PID Pool Report
};

public enum FFBEType // FFB Effect Type
{

	// Effect Type
	ET_NONE		=	0,	  //    No Force
	ET_CONST	=	1,    //    Constant Force
	ET_RAMP		=	2,    //    Ramp
	ET_SQR		=	3,    //    Square
	ET_SINE		=	4,    //    Sine
	ET_TRNGL	=	5,    //    Triangle
	ET_STUP		=	6,    //    Sawtooth Up
	ET_STDN		=	7,    //    Sawtooth Down
	ET_SPRNG	=	8,    //    Spring
	ET_DMPR		=	9,    //    Damper
	ET_INRT		=	10,   //    Inertia
	ET_FRCTN	=	11,   //    Friction
	ET_CSTM		=	12,   //    Custom Force Data
};

public enum FFB_CTRL
{
    CTRL_ENACT = 1,	// Enable all device actuators.
    CTRL_DISACT = 2,	// Disable all the device actuators.
    CTRL_STOPALL = 3,	// Stop All Effects­ Issues a stop on every running effect.
    CTRL_DEVRST = 4,	// Device Reset– Clears any device paused condition, enables all actuators and clears all effects from memory.
    CTRL_DEVPAUSE = 5,	// Device Pause– The all effects on the device are paused at the current time step.
    CTRL_DEVCONT = 6,	// Device Continue– The all effects that running when the device was paused are restarted from their last time step.
};

public enum FFBOP
{
    EFF_START = 1, // EFFECT START
    EFF_SOLO = 2, // EFFECT SOLO START
    EFF_STOP = 3, // EFFECT STOP
};

public enum VGEN_DEV_TYPE
{
    vJoy,
    vXbox
};


namespace vGenInterfaceWrap
{
    public class vGen
    {

        /***************************************************/
        /*********** Various declarations ******************/
        /***************************************************/
        private static RemovalCbFunc UserRemCB;
        private static WrapRemovalCbFunc wrf;
        private static GCHandle hRemUserData;


        private static FfbCbFunc UserFfbCB;
        private static WrapFfbCbFunc wf;
        private static GCHandle hFfbUserData;

        [StructLayout(LayoutKind.Sequential)] public struct JoystickState
        {
            public byte bDevice;
            public Int32 Throttle;
            public Int32 Rudder;
            public Int32 Aileron;
            public Int32 AxisX;
            public Int32 AxisY;
            public Int32 AxisZ;
            public Int32 AxisXRot;
            public Int32 AxisYRot;
            public Int32 AxisZRot;
            public Int32 Slider;
            public Int32 Dial;
            public Int32 Wheel;
            public Int32 AxisVX;
            public Int32 AxisVY;
            public Int32 AxisVZ;
            public Int32 AxisVBRX;
            public Int32 AxisVBRY;
            public Int32 AxisVBRZ;
            public UInt32 Buttons;
            public UInt32 bHats;	// Lower 4 bits: HAT switch or 16-bit of continuous HAT switch
            public UInt32 bHatsEx1;	// Lower 4 bits: HAT switch or 16-bit of continuous HAT switch
            public UInt32 bHatsEx2;	// Lower 4 bits: HAT switch or 16-bit of continuous HAT switch
            public UInt32 bHatsEx3;	// Lower 4 bits: HAT switch or 16-bit of continuous HAT switch
            public UInt32 ButtonsEx1;
            public UInt32 ButtonsEx2;
            public UInt32 ButtonsEx3;
        };

        [StructLayout(LayoutKind.Sequential)] public struct GamepadState
        {
            public XINPUT_BUTTONS Buttons;
            public byte LeftTrigger;
            public byte RightTrigger;
            public short ThumbLX;
            public short ThumbLY;
            public short ThumbRX;
            public short ThumbRY;
        };

        [StructLayout(LayoutKind.Sequential)] public struct XInputState
        {
            public UInt32 PacketNumber;
            public GamepadState Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FFB_DATA
        {
            private UInt32 size;
            private UInt32 cmd;
            private IntPtr data;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_CONSTANT
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public Int16 Magnitude;
        }

        [System.Obsolete("use FFB_EFF_REPORT")]
        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_CONST
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public FFBEType EffectType;
            [FieldOffset(8)]
            public UInt16 Duration;// Value in milliseconds. 0xFFFF means infinite
            [FieldOffset(10)]
            public UInt16 TrigerRpt;
            [FieldOffset(12)]
            public UInt16 SamplePrd;
            [FieldOffset(14)]
            public Byte Gain;
            [FieldOffset(15)]
            public Byte TrigerBtn;
            [FieldOffset(16)]
            public bool Polar; // How to interpret force direction Polar (0-360°) or Cartesian (X,Y)
            [FieldOffset(20)]
            public Byte Direction; // Polar direction: (0x00-0xFF correspond to 0-360°)
            [FieldOffset(20)]
            public Byte DirX; // X direction: Positive values are To the right of the center (X); Negative are Two's complement
            [FieldOffset(21)]
            public Byte DirY; // Y direction: Positive values are below the center (Y); Negative are Two's complement
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_REPORT
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public FFBEType EffectType;
            [FieldOffset(8)]
            public UInt16 Duration;// Value in milliseconds. 0xFFFF means infinite
            [FieldOffset(10)]
            public UInt16 TrigerRpt;
            [FieldOffset(12)]
            public UInt16 SamplePrd;
            [FieldOffset(14)]
            public Byte Gain;
            [FieldOffset(15)]
            public Byte TrigerBtn;
            [FieldOffset(16)]
            public bool Polar; // How to interpret force direction Polar (0-360°) or Cartesian (X,Y)
            [FieldOffset(20)]
            public Byte Direction; // Polar direction: (0x00-0xFF correspond to 0-360°)
            [FieldOffset(20)]
            public Byte DirX; // X direction: Positive values are To the right of the center (X); Negative are Two's complement
            [FieldOffset(21)]
            public Byte DirY; // Y direction: Positive values are below the center (Y); Negative are Two's complement
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_OP
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public FFBOP EffectOp;
            [FieldOffset(8)]
            public Byte LoopCount;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_COND
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public bool isY;
            [FieldOffset(8)]
            public Int16 CenterPointOffset; // CP Offset: Range 0x80­0x7F (­10000 ­ 10000)
            [FieldOffset(12)]
            public Int16 PosCoeff; // Positive Coefficient: Range 0x80­0x7F (­10000 ­ 10000)
            [FieldOffset(16)]
            public Int16 NegCoeff; // Negative Coefficient: Range 0x80­0x7F (­10000 ­ 10000)
            [FieldOffset(20)]
            public UInt32 PosSatur; // Positive Saturation: Range 0x00­0xFF (0 – 10000)
            [FieldOffset(24)]
            public UInt32 NegSatur; // Negative Saturation: Range 0x00­0xFF (0 – 10000)
            [FieldOffset(28)]
            public Int32 DeadBand; // Dead Band: : Range 0x00­0xFF (0 – 10000)
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_ENVLP
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public UInt16 AttackLevel;
            [FieldOffset(8)]
            public UInt16 FadeLevel;
            [FieldOffset(12)]
            public UInt32 AttackTime;
            [FieldOffset(16)]
            public UInt32 FadeTime;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_PERIOD
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public UInt32 Magnitude;
            [FieldOffset(8)]
            public Int16 Offset;
            [FieldOffset(12)]
            public UInt32 Phase;
            [FieldOffset(16)]
            public UInt32 Period;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FFB_EFF_RAMP
        {
            [FieldOffset(0)]
            public Byte EffectBlockIndex;
            [FieldOffset(4)]
            public Int16 Start;             // The Normalized magnitude at the start of the effect
            [FieldOffset(8)]
            public Int16 End;               // The Normalized magnitude at the end of the effect
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_VIBRATION
        {
            public UInt16 wLeftMotorSpeed;
            public UInt16 wRightMotorSpeed;
        }

        public static byte XINPUT_GAMEPAD_DPAD_UP = 1;
        public static byte XINPUT_GAMEPAD_DPAD_DOWN = 2;
        public static byte XINPUT_GAMEPAD_DPAD_LRFT = 4;
        public static byte XINPUT_GAMEPAD_DPAD_RIGHT = 8;
        /***************************************************/
        /***** Import from file vGenInterface.dll (C) ******/
        /***************************************************/
        #region Backward compatibility API (vJoy)


        /////	General driver data
        [DllImport("vGenInterface.dll", EntryPoint = "GetvJoyVersion")]
        private static extern short _GetvJoyVersion();

        [DllImport("vGenInterface.dll", EntryPoint = "vJoyEnabled")]
        private static extern bool _vJoyEnabled();

        [DllImport("vGenInterface.dll", EntryPoint = "GetvJoyProductString")]
        private static extern IntPtr _GetvJoyProductString();

        [DllImport("vGenInterface.dll", EntryPoint = "GetvJoyManufacturerString")]
        private static extern IntPtr _GetvJoyManufacturerString();

        [DllImport("vGenInterface.dll", EntryPoint = "GetvJoySerialNumberString")]
        private static extern IntPtr _GetvJoySerialNumberString();

        [DllImport("vGenInterface.dll", EntryPoint = "DriverMatch")]
        private static extern bool _DriverMatch(ref UInt32 DllVer, ref UInt32 DrvVer);

        /////	vJoy Device properties
        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDButtonNumber")]
        private static extern int _GetVJDButtonNumber(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDDiscPovNumber")]
        private static extern int _GetVJDDiscPovNumber(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDContPovNumber")]
        private static extern int _GetVJDContPovNumber(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDAxisExist")]
        private static extern UInt32 _GetVJDAxisExist(UInt32 rID, HID_USAGES Axis);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDAxisMax")]
        private static extern bool _GetVJDAxisMax(UInt32 rID, HID_USAGES Axis, ref long Max);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDAxisMin")]
        private static extern bool _GetVJDAxisMin(UInt32 rID, HID_USAGES Axis, ref long Min);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDAxisRange")]
        private static extern bool _GetVJDAxisRange(UInt32 rID, HID_USAGES Axis, ref Int32 Min, ref Int32 Max);

        [DllImport("vGenInterface.dll", EntryPoint = "isVJDExists")]
        private static extern bool _isVJDExists(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "GetOwnerPid")]
        private static extern int _GetOwnerPid(UInt32 rID);

        /////	Write access to vJoy Device - Basic
        [DllImport("vGenInterface.dll", EntryPoint = "AcquireVJD")]
        private static extern bool _AcquireVJD(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "RelinquishVJD")]
        private static extern void _RelinquishVJD(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "UpdateVJD")]
        private static extern bool _UpdateVJD(UInt32 rID, ref JoystickState pData);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVJDStatus")]
        private static extern int _GetVJDStatus(UInt32 rID);

        //// Reset functions
        [DllImport("vGenInterface.dll", EntryPoint = "ResetVJD")]
        private static extern bool _ResetVJD(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "ResetAll")]
        private static extern bool _ResetAll();

        [DllImport("vGenInterface.dll", EntryPoint = "ResetButtons")]
        private static extern bool _ResetButtons(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "ResetPovs")]
        private static extern bool _ResetPovs(UInt32 rID);

        ////// Write data
        [DllImport("vGenInterface.dll", EntryPoint = "SetAxis")]
        private static extern bool _SetAxis(Int32 Value, UInt32 rID, HID_USAGES Axis);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtn")]
        private static extern bool _SetBtn(bool Value, UInt32 rID, Byte nBtn);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDiscPov")]
        private static extern bool _SetDiscPov(Int32 Value, UInt32 rID, uint nPov);

        [DllImport("vGenInterface.dll", EntryPoint = "SetContPov")]
        private static extern bool _SetContPov(Int32 Value, UInt32 rID, uint nPov);

        [DllImport("vGenInterface.dll", EntryPoint = "RegisterRemovalCB", CallingConvention = CallingConvention.Cdecl)]
        private extern static void _RegisterRemovalCB(WrapRemovalCbFunc cb, IntPtr data);

        public delegate void RemovalCbFunc(bool complete, bool First, object userData);
        public delegate void WrapRemovalCbFunc(bool complete, bool First, IntPtr userData);

        public static void WrapperRemCB(bool complete, bool First, IntPtr userData)
        {

            object obj = null;

            if (userData != IntPtr.Zero)
            {
                // Convert userData from pointer to object
                GCHandle handle2 = (GCHandle)userData;
                obj = handle2.Target as object;
            }

            // Call user-defined CB function
            UserRemCB(complete,  First, obj);
        }

        // Force Feedback (FFB)
        [DllImport("vGenInterface.dll", EntryPoint = "FfbRegisterGenCB", CallingConvention = CallingConvention.Cdecl)]
        private extern static void _FfbRegisterGenCB(WrapFfbCbFunc cb, IntPtr data);

        public delegate void FfbCbFunc(IntPtr data,  object userData);
        public delegate  void WrapFfbCbFunc(IntPtr data, IntPtr userData);

        public static void  WrapperFfbCB(IntPtr data, IntPtr userData)
        {

            object obj = null;

            if (userData != IntPtr.Zero)
            {
                // Convert userData from pointer to object
                GCHandle handle2 = (GCHandle)userData;
                obj = handle2.Target as object;
            }

            // Call user-defined CB function
            UserFfbCB(data, obj);
        }

        [DllImport("vGenInterface.dll", EntryPoint = "FfbStart")]
        private static extern bool _FfbStart(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "FfbStop")]
        private static extern bool _FfbStop(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "IsDeviceFfb")]
        private static extern bool _IsDeviceFfb(UInt32 rID);

        [DllImport("vGenInterface.dll", EntryPoint = "IsDeviceFfbEffect")]
        private static extern bool _IsDeviceFfbEffect(UInt32 rID, UInt32 Effect);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_DeviceID")]
        private static extern UInt32 _Ffb_h_DeviceID(IntPtr Packet, ref int DeviceID);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Type")]
        private static extern UInt32 _Ffb_h_Type(IntPtr Packet, ref FFBPType Type);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Packet")]
        private static extern UInt32 _Ffb_h_Packet(IntPtr Packet, ref UInt32 Type, ref Int32 DataSize, ref IntPtr Data);


        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_EBI")]
        private static extern UInt32 _Ffb_h_EBI(IntPtr Packet, ref Int32 Index);

#pragma warning disable 618
        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Const")]
        private static extern UInt32 _Ffb_h_Eff_Const(IntPtr Packet, ref FFB_EFF_CONST Effect);
#pragma warning restore 618

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Report")]
        private static extern UInt32 _Ffb_h_Eff_Report(IntPtr Packet, ref FFB_EFF_REPORT Effect);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_DevCtrl")]
        private static extern UInt32 _Ffb_h_DevCtrl(IntPtr Packet, ref FFB_CTRL Control);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_EffOp")]
        private static extern UInt32 _Ffb_h_EffOp(IntPtr Packet, ref FFB_EFF_OP Operation);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_DevGain")]
        private static extern UInt32 _Ffb_h_DevGain(IntPtr Packet, ref Byte Gain);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Cond")]
        private static extern UInt32 _Ffb_h_Eff_Cond(IntPtr Packet, ref FFB_EFF_COND Condition);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Envlp")]
        private static extern UInt32 _Ffb_h_Eff_Envlp(IntPtr Packet, ref FFB_EFF_ENVLP Envelope);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Period")]
        private static extern UInt32 _Ffb_h_Eff_Period(IntPtr Packet, ref FFB_EFF_PERIOD Effect);

        [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_EffNew")]
        private static extern UInt32 _Ffb_h_EffNew(IntPtr Packet, ref FFBEType Effect);

         [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Ramp")]
        private static extern UInt32 _Ffb_h_Eff_Ramp(IntPtr Packet, ref FFB_EFF_RAMP RampEffect);

         [DllImport("vGenInterface.dll", EntryPoint = "Ffb_h_Eff_Constant")]
         private static extern UInt32 _Ffb_h_Eff_Constant(IntPtr Packet, ref FFB_EFF_CONSTANT ConstantEffect);
        #endregion Backward compatibility API (vJoy)


        #region vXbox API

        //////////////////////////////////////////////////////////////////////////////////////
        ///
        ///  vXbox interface fuctions
        ///
        ///  Device range: 1-4 (Not necessarily related to Led number)
        ///
        //////////////////////////////////////////////////////////////////////////////////////

        // Virtual vXbox bus information
        [DllImport("vGenInterface.dll", EntryPoint = "isVBusExist")]
        private static extern VJRESULT _isVBusExist();

        [DllImport("vGenInterface.dll", EntryPoint = "GetVBusVersion")]
        private static extern UInt32 _GetVBusVersion();

        [DllImport("vGenInterface.dll", EntryPoint = "GetNumEmptyBusSlots")]
        private static extern VJRESULT _GetNumEmptyBusSlots(ref Byte nSlots);

        // Device Status (Plugin/Unplug and check ownership)
        [DllImport("vGenInterface.dll", EntryPoint = "isControllerPluggedIn")]
        private static extern VJRESULT _isControllerPluggedIn(UInt32 UserIndex, ref bool Exist);

        [DllImport("vGenInterface.dll", EntryPoint = "isControllerOwned")]
        private static extern VJRESULT _isControllerOwned(UInt32 UserIndex, ref Boolean Exist);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVXAxisRange")]
        private static extern VJRESULT _GetVXAxisRange(UInt32 UserIndex, HID_USAGES Axis, ref Int32 Min, ref Int32 Max);

        [DllImport("vGenInterface.dll", EntryPoint = "PlugIn")]
        private static extern VJRESULT _PlugIn(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "PlugInNext")]
        private static extern VJRESULT _PlugInNext(ref UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "UnPlug")]
        private static extern VJRESULT _UnPlug(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "UnPlugForce")]
        private static extern VJRESULT _UnPlugForce(UInt32 UserIndex);

        // Reset Devices
        [DllImport("vGenInterface.dll", EntryPoint = "ResetController")]
        private static extern VJRESULT _ResetController(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "ResetAllControllers")]
        private static extern VJRESULT _ResetAllControllers();

        // Button functions: Per-button Press/Release
        [DllImport("vGenInterface.dll", EntryPoint = "SetButton")]
        private static extern VJRESULT _SetButton(UInt32 UserIndex, UInt16 Button, Boolean Press);

#if SPECIFICRESET
        [DllImport("vGenInterface.dll", EntryPoint = "ResetControllerBtns")]
        private static extern VJRESULT _ResetControllerBtns(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "ResetControllerDPad")]
        private static extern VJRESULT _ResetControllerDPad(UInt32 UserIndex);
#endif // SPECIFICRESET

#if SPECIFICBUTTONS
        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnA")]
        private static extern VJRESULT _SetBtnA(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnB")]
        private static extern VJRESULT _SetBtnB(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnX")]
        private static extern VJRESULT _SetBtnX(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnY")]
        private static extern VJRESULT _SetBtnY(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnLT")]
        private static extern VJRESULT _SetBtnLT(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnRT")]
        private static extern VJRESULT _SetBtnRT(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnLB")]
        private static extern VJRESULT _SetBtnLB(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnRB")]
        private static extern VJRESULT _SetBtnRB(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnStart")]
        private static extern VJRESULT _SetBtnStart(UInt32 UserIndex, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetBtnBack")]
        private static extern VJRESULT _SetBtnBack(UInt32 UserIndex, Boolean Press);
#endif // SPECIFICBUTTONS


        // Trigger/Axis functions: Set value in the range
        [DllImport("vGenInterface.dll", EntryPoint = "SetGamepadAxis")]
        private static extern VJRESULT _SetGamepadAxis(UInt32 UserIndex, HID_USAGES Axis, Int16 Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetTriggerL")]
        private static extern VJRESULT _SetTriggerL(UInt32 UserIndex, Byte Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetTriggerR")]
        private static extern VJRESULT _SetTriggerR(UInt32 UserIndex, Byte Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetAxisLx")]
        private static extern VJRESULT _SetAxisLx(UInt32 UserIndex, Int16 Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetAxisLy")]
        private static extern VJRESULT _SetAxisLy(UInt32 UserIndex, Int16 Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetAxisRx")]
        private static extern VJRESULT _SetAxisRx(UInt32 UserIndex, Int16 Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetAxisRy")]
        private static extern VJRESULT _SetAxisRy(UInt32 UserIndex, Int16 Value);


        // DPAD Functions
        [DllImport("vGenInterface.dll", EntryPoint = "SetDpad")]
        private static extern VJRESULT _SetDpad(UInt32 UserIndex, Byte Value);

#if SPECIFICBUTTONS
        [DllImport("vGenInterface.dll", EntryPoint = "SetDpadUp")]
        private static extern VJRESULT _SetDpadUp(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDpadRight")]
        private static extern VJRESULT _SetDpadRight(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDpadDown")]
        private static extern VJRESULT _SetDpadDown(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDpadLeft")]
        private static extern VJRESULT _SetDpadLeft(UInt32 UserIndex);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDpadOff")]
        private static extern VJRESULT _SetDpadOff(UInt32 UserIndex);

#endif // SPECIFICBUTTONS


        // Feedback Polling: Assigned Led number / Vibration values
        [DllImport("vGenInterface.dll", EntryPoint = "GetLedNumber")]
        private static extern VJRESULT _GetLedNumber(UInt32 UserIndex, ref Byte pLed);

        [DllImport("vGenInterface.dll", EntryPoint = "GetVibration")]
        private static extern VJRESULT _GetVibration(UInt32 UserIndex, ref XINPUT_VIBRATION pVib);

        #endregion vXbox API

        #region Common API

        [DllImport("vGenInterface.dll", EntryPoint = "AcquireDev")]
        private static extern VJRESULT _AcquireDev(UInt32 DevId, VGEN_DEV_TYPE dType, ref Int32 hDev);

        [DllImport("vGenInterface.dll", EntryPoint = "RelinquishDev")]
        private static extern VJRESULT _RelinquishDev(Int32 hDev);

        [DllImport("vGenInterface.dll", EntryPoint = "GetDevType")]
        private static extern VJRESULT _GetDevType(Int32 hDev, ref VGEN_DEV_TYPE dType);

        [DllImport("vGenInterface.dll", EntryPoint = "GetDevNumber")]
        private static extern VJRESULT _GetDevNumber(Int32 hDev, ref UInt32 dNumber);

        [DllImport("vGenInterface.dll", EntryPoint = "GetDevId")]
        private static extern VJRESULT _GetDevId(Int32 hDev, ref UInt32 dID);

        [DllImport("vGenInterface.dll", EntryPoint = "isDevOwned")]
        private static extern VJRESULT _isDevOwned(UInt32 DevId, VGEN_DEV_TYPE dType, ref Boolean Owned);

        [DllImport("vGenInterface.dll", EntryPoint = "isDevExist")]
        private static extern VJRESULT _isDevExist(UInt32 DevId, VGEN_DEV_TYPE dType, ref Boolean Exist);

        [DllImport("vGenInterface.dll", EntryPoint = "isDevFree")]
        private static extern VJRESULT _isDevFree(UInt32 DevId, VGEN_DEV_TYPE dType, ref Boolean Free);

        [DllImport("vGenInterface.dll", EntryPoint = "GetDevHandle")]
        private static extern VJRESULT _GetDevHandle(UInt32 DevId, VGEN_DEV_TYPE dType, ref Int32 hDev);

        [DllImport("vGenInterface.dll", EntryPoint = "isAxisExist")]
        private static extern VJRESULT _isAxisExist(Int32 hDev, HID_USAGES Axis, ref Boolean Exist);

        [DllImport("vGenInterface.dll", EntryPoint = "GetDevButtonN")]
        private static extern VJRESULT _GetDevButtonN(Int32 hDev, ref UInt32 nBtn);

        [DllImport("vGenInterface.dll", EntryPoint = "GetDevHatN")]
        private static extern VJRESULT _GetDevHatN(Int32 hDev, ref UInt32 nHat);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDevButton")]
        private static extern VJRESULT _SetDevButton(Int32 hDev, UInt32 Button, Boolean Press);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDevAxis")]
        private static extern VJRESULT _SetDevAxis(Int32 hDev, HID_USAGES Axis, float Value);

        [DllImport("vGenInterface.dll", EntryPoint = "SetDevPov")]
        private static extern VJRESULT _SetDevPov(Int32 hDev, UInt32 nPov, float Value);

        [DllImport("vGenInterface.dll", EntryPoint = "GetPosition")]
        private static extern VJRESULT _GetPosition(UInt32 rID, ref JoystickState pPosition);

        [DllImport("vGenInterface.dll", EntryPoint = "GetPosition")]
        private static extern VJRESULT _GetPosition(UInt32 rID, ref GamepadState pPosition);
        #endregion Common API

        #region XInput helpers
        [DllImport("vGenInterface.dll", EntryPoint = "GetXInputState")]
        private static extern VJRESULT _GetXInputState(UInt32 rID, ref XInputState pState);
        #endregion XInput helpers

        /***************************************************/
        /********** Export functions (C#) ******************/
        /***************************************************/

        #region Backward compatibility API (vJoy)
        /////	General driver data
        public short GetvJoyVersion() { return _GetvJoyVersion(); }
        public bool vJoyEnabled() { return _vJoyEnabled(); }
        public string GetvJoyProductString() { return Marshal.PtrToStringAuto(_GetvJoyProductString()); }
        public string GetvJoyManufacturerString() { return Marshal.PtrToStringAuto(_GetvJoyManufacturerString()); }
        public string GetvJoySerialNumberString() { return Marshal.PtrToStringAuto(_GetvJoySerialNumberString()); }
        public bool DriverMatch(ref UInt32 DllVer, ref UInt32 DrvVer) { return _DriverMatch(ref DllVer, ref DrvVer); }

        /////	vJoy Device properties
        public int GetVJDButtonNumber(uint rID) { return _GetVJDButtonNumber(rID); }
        public int GetVJDDiscPovNumber(uint rID) { return _GetVJDDiscPovNumber(rID); }
        public int GetVJDContPovNumber(uint rID) { return _GetVJDContPovNumber(rID); }
        public bool GetVJDAxisExist(UInt32 rID, HID_USAGES Axis)
        {
            UInt32 res = _GetVJDAxisExist(rID, Axis);
            if (res == 1)
                return true;
            else
                return false;
        }
        public bool GetVJDAxisMax(UInt32 rID, HID_USAGES Axis, ref long Max) { return _GetVJDAxisMax(rID, Axis, ref Max); }
        public bool GetVJDAxisMin(UInt32 rID, HID_USAGES Axis, ref long Min) { return _GetVJDAxisMin(rID, Axis, ref Min); }
        public bool GetVJDAxisRange(UInt32 rID, HID_USAGES Axis, ref Int32 Min, ref Int32 Max) { return _GetVJDAxisRange(rID, Axis, ref Min, ref Max); }
        public bool isVJDExists(UInt32 rID) { return _isVJDExists(rID); }
        public int  GetOwnerPid(UInt32 rID) { return _GetOwnerPid(rID); }

        /////	Write access to vJoy Device - Basic
        public bool AcquireVJD(UInt32 rID) { return _AcquireVJD(rID); }
        public void RelinquishVJD(uint rID) {  _RelinquishVJD(rID); }
        public bool UpdateVJD(UInt32 rID, ref JoystickState pData) {return _UpdateVJD( rID, ref pData);}
        public VJDSTATUS GetVJDStatus(UInt32 rID) { return (VJDSTATUS)_GetVJDStatus(rID); }

        //// Reset functions
        public bool ResetVJD(UInt32 rID){return _ResetVJD(rID);}
        public bool ResetAll(){return _ResetAll();}
        public bool ResetButtons(UInt32 rID){return _ResetButtons(rID);}
        public bool ResetPovs(UInt32 rID) { return _ResetPovs(rID); }

        ////// Write data
        public bool SetAxis(Int32 Value, UInt32 rID, HID_USAGES Axis) { return _SetAxis(Value, rID, Axis); }
        public bool SetBtn(bool Value, UInt32 rID, uint nBtn) { return _SetBtn( Value, rID, (Byte)nBtn); }
        public bool SetDiscPov(Int32 Value, UInt32 rID, uint nPov) { return _SetDiscPov(Value, rID, nPov); }
        public bool SetContPov(Int32 Value, UInt32 rID, uint nPov) { return _SetContPov(Value, rID, nPov); }

        // Register CB function that takes a C# object as userdata
        public void RegisterRemovalCB(RemovalCbFunc cb, object data)
        {
            // Free existing GCHandle (if exists)
            if (hRemUserData.IsAllocated && hRemUserData.Target != null)
                hRemUserData.Free();

            // Convert object to pointer
            hRemUserData = GCHandle.Alloc(data);

            // Apply the user-defined CB function
            UserRemCB = new RemovalCbFunc(cb);
            wrf = new WrapRemovalCbFunc(WrapperRemCB);

            _RegisterRemovalCB(wrf, (IntPtr)hRemUserData);
        }

        // Register CB function that takes a pointer as userdata
        public void RegisterRemovalCB(WrapRemovalCbFunc cb, IntPtr data)
        {
            wrf = new WrapRemovalCbFunc(cb);
            _RegisterRemovalCB(wrf, data);
        }


        /////////////////////////////////////////////////////////////////////////////////////////////
        //// Force Feedback (FFB)

        // Register CB function that takes a C# object as userdata
        public void FfbRegisterGenCB(FfbCbFunc cb, object data)
        {
            // Free existing GCHandle (if exists)
            if (hFfbUserData.IsAllocated && hFfbUserData.Target != null)
                hFfbUserData.Free();

            // Convert object to pointer
            hFfbUserData = GCHandle.Alloc(data);

            // Apply the user-defined CB function
            UserFfbCB = new FfbCbFunc(cb);
            wf = new WrapFfbCbFunc(WrapperFfbCB);

            _FfbRegisterGenCB(wf, (IntPtr)hFfbUserData);
        }

        // Register CB function that takes a pointer as userdata
        public void FfbRegisterGenCB(WrapFfbCbFunc cb, IntPtr data)
        {
            wf = new WrapFfbCbFunc(cb);
            _FfbRegisterGenCB(wf, data);
        }

        [Obsolete("you can remove the function from your code")]
        public bool FfbStart(UInt32 rID) { return _FfbStart(rID); }
        [Obsolete("you can remove the function from your code")]
        public bool FfbStop(UInt32 rID) { return _FfbStop(rID); }
        public bool IsDeviceFfb(UInt32 rID) { return _IsDeviceFfb(rID); }
        public bool IsDeviceFfbEffect(UInt32 rID, UInt32 Effect) { return _IsDeviceFfbEffect(rID, Effect); }
        public UInt32 Ffb_h_DeviceID(IntPtr  Packet, ref int DeviceID) {return _Ffb_h_DeviceID(Packet, ref DeviceID);}
        public UInt32 Ffb_h_Type(IntPtr Packet, ref FFBPType Type) { return _Ffb_h_Type(Packet, ref  Type); }
        public UInt32 Ffb_h_Packet(IntPtr Packet, ref UInt32 Type, ref Int32 DataSize, ref Byte[] Data)
        {
            IntPtr buf = IntPtr.Zero;
            UInt32 res = _Ffb_h_Packet(Packet, ref  Type, ref  DataSize, ref buf);
            if (res != 0)
                return res;

            DataSize -= 8;
            Data = new byte[DataSize];
            Marshal.Copy(buf, Data, 0, DataSize);
            return res;
        }
        public UInt32 Ffb_h_EBI(IntPtr Packet, ref Int32 Index) { return _Ffb_h_EBI( Packet, ref  Index);}
        [Obsolete("use Ffb_h_Eff_Report instead")]
        public UInt32 Ffb_h_Eff_Const(IntPtr Packet, ref FFB_EFF_CONST Effect) { return _Ffb_h_Eff_Const(Packet, ref  Effect); }
        public UInt32 Ffb_h_Eff_Report(IntPtr Packet, ref FFB_EFF_REPORT Effect) { return _Ffb_h_Eff_Report(Packet, ref  Effect); }
        public UInt32 Ffb_h_DevCtrl(IntPtr Packet, ref FFB_CTRL Control) { return _Ffb_h_DevCtrl(Packet, ref  Control); }
        public UInt32 Ffb_h_EffOp(IntPtr Packet, ref FFB_EFF_OP Operation) { return _Ffb_h_EffOp( Packet, ref  Operation);}
        public UInt32 Ffb_h_DevGain(IntPtr Packet, ref Byte Gain) { return _Ffb_h_DevGain( Packet, ref  Gain);}
        public UInt32 Ffb_h_Eff_Cond(IntPtr Packet, ref FFB_EFF_COND Condition) { return _Ffb_h_Eff_Cond( Packet, ref  Condition); }
        public UInt32 Ffb_h_Eff_Envlp(IntPtr Packet, ref FFB_EFF_ENVLP Envelope) { return _Ffb_h_Eff_Envlp( Packet, ref  Envelope); }
        public UInt32 Ffb_h_Eff_Period(IntPtr Packet, ref FFB_EFF_PERIOD Effect) { return _Ffb_h_Eff_Period( Packet, ref  Effect); }
        public UInt32 Ffb_h_EffNew(IntPtr Packet, ref FFBEType Effect) { return _Ffb_h_EffNew( Packet, ref  Effect); }
        public UInt32 Ffb_h_Eff_Ramp(IntPtr Packet, ref FFB_EFF_RAMP RampEffect) { return _Ffb_h_Eff_Ramp( Packet, ref  RampEffect);}
        public UInt32 Ffb_h_Eff_Constant(IntPtr Packet, ref FFB_EFF_CONSTANT ConstantEffect) { return _Ffb_h_Eff_Constant(Packet, ref  ConstantEffect); }
        #endregion Backward compatibility API (vJoy)

        #region vXbox API
        public VJRESULT isVBusExist() { return _isVBusExist(); }
        public UInt32   GetVBusVersion() { return _GetVBusVersion(); }
        public VJRESULT GetNumEmptyBusSlots(ref Byte nSlots) { return _GetNumEmptyBusSlots(ref nSlots); }
        public VJRESULT isControllerPluggedIn(UInt32 UserIndex, ref bool Exist) { return _isControllerPluggedIn(UserIndex, ref  Exist); }
        public VJRESULT isControllerOwned(UInt32 UserIndex, ref Boolean Exist) { return _isControllerOwned(UserIndex, ref Exist); }
        public VJRESULT GetVBAxisRange(UInt32 UserIndex, HID_USAGES Axis, ref Int32 Min, ref Int32 Max) { return _GetVXAxisRange(UserIndex, Axis, ref Min, ref Max); }
        public VJRESULT PlugIn(UInt32 UserIndex) { return _PlugIn(UserIndex); }
        public VJRESULT PlugInNext(ref UInt32 UserIndex) { return _PlugInNext(ref UserIndex); }
        public VJRESULT UnPlug(UInt32 UserIndex) { return _UnPlug(UserIndex); }
        public VJRESULT UnPlugForce(UInt32 UserIndex) { return _UnPlugForce(UserIndex); }
        public VJRESULT ResetController(UInt32 UserIndex) { return _ResetController(UserIndex); }
        public VJRESULT ResetAllControllers() { return _ResetAllControllers(); }
        public VJRESULT SetButton(UInt32 UserIndex, UInt16 Button, Boolean Press) { return _SetButton(UserIndex, Button, Press); }

#if SPECIFICRESET
        public UInt32 ResetControllerBtns(UInt32 UserIndex) { return _ResetControllerBtns(UserIndex); }

        public UInt32 ResetControllerDPad(UInt32 UserIndex) { return _ResetControllerDPad(UserIndex); }
#endif // SPECIFICRESET

#if SPECIFICBUTTONS
        public VJRESULT SetBtnA(UInt32 UserIndex, Boolean Press) { return _SetBtnA(UserIndex, Press); }
        public VJRESULT SetBtnB(UInt32 UserIndex, Boolean Press) { return _SetBtnB(UserIndex, Press); }
        public VJRESULT SetBtnX(UInt32 UserIndex, Boolean Press) { return _SetBtnX(UserIndex, Press); }
        public VJRESULT SetBtnY(UInt32 UserIndex, Boolean Press) { return _SetBtnY(UserIndex, Press); }
        public VJRESULT SetBtnLT(UInt32 UserIndex, Boolean Press) { return _SetBtnLT(UserIndex, Press); }
        public VJRESULT SetBtnRT(UInt32 UserIndex, Boolean Press) { return _SetBtnRT(UserIndex, Press); }
        public VJRESULT SetBtnLB(UInt32 UserIndex, Boolean Press) { return _SetBtnLB(UserIndex, Press); }
        public VJRESULT SetBtnRB(UInt32 UserIndex, Boolean Press) { return _SetBtnRB(UserIndex, Press); }
        public VJRESULT SetBtnStart(UInt32 UserIndex, Boolean Press) { return _SetBtnStart(UserIndex, Press); }
        public VJRESULT SetBtnBack(UInt32 UserIndex, Boolean Press) { return _SetBtnBack(UserIndex, Press); }
#endif
        // Trigger/Axis functions: Set value in the range
        public VJRESULT SetGamepadAxis(UInt32 UserIndex, HID_USAGES Axis, short Value) { return _SetGamepadAxis(UserIndex, Axis, Value); }
        public VJRESULT SetTriggerL(UInt32 UserIndex, Byte Value) { return _SetTriggerL(UserIndex, Value); }
        public VJRESULT SetTriggerR(UInt32 UserIndex, Byte Value) { return _SetTriggerR(UserIndex, Value); }
        public VJRESULT SetAxisLx(UInt32 UserIndex, Int16 Value) { return _SetAxisLx(UserIndex, Value); }
        public VJRESULT SetAxisLy(UInt32 UserIndex, Int16 Value) { return _SetAxisLy(UserIndex, Value); }
        public VJRESULT SetAxisRx(UInt32 UserIndex, Int16 Value) { return _SetAxisRx(UserIndex, Value); }
        public VJRESULT SetAxisRy(UInt32 UserIndex, Int16 Value) { return _SetAxisRy(UserIndex, Value); }
        public VJRESULT SetDpad(UInt32 UserIndex, Byte Value) { return _SetDpad(UserIndex, Value); }

#if SPECIFICBUTTONS
        public VJRESULT SetDpadUp(UInt32 UserIndex) { return _SetDpadUp(UserIndex); }
        public VJRESULT SetDpadRight(UInt32 UserIndex) { return _SetDpadRight(UserIndex); }
        public VJRESULT SetDpadDown(UInt32 UserIndex) { return _SetDpadDown(UserIndex); }
        public VJRESULT SetDpadLeft(UInt32 UserIndex) { return _SetDpadLeft(UserIndex); }
        public VJRESULT SetDpadOff(UInt32 UserIndex) { return _SetDpadOff(UserIndex); }
#endif // SPECIFICBUTTONS

        // Feedback Polling: Assigned Led number / Vibration values
        public VJRESULT GetLedNumber(UInt32 UserIndex, ref Byte pLed) { return _GetLedNumber(UserIndex, ref pLed); }
        public VJRESULT GetVibration(UInt32 UserIndex, ref XINPUT_VIBRATION pVib) { return _GetVibration( UserIndex, ref  pVib); }

        #endregion vXbox API

        #region Common API

        public VJRESULT AcquireDev(UInt32 DevId, VGEN_DEV_TYPE dType, ref Int32 hDev) { return _AcquireDev(DevId, dType, ref hDev); }
        public VJRESULT RelinquishDev(Int32 hDev) { return _RelinquishDev( hDev); }
        public VJRESULT GetDevType(Int32 hDev, ref VGEN_DEV_TYPE dType) { return _GetDevType( hDev, ref  dType); }
        public VJRESULT GetDevNumber(Int32 hDev, ref UInt32 dNumber) { return _GetDevNumber( hDev, ref  dNumber); }
        public VJRESULT GetDevId(Int32 hDev, ref UInt32 dID) { return _GetDevId(hDev, ref dID); }
        public VJRESULT isDevOwned(UInt32 DevId, VGEN_DEV_TYPE dType, ref Boolean Owned) { return _isDevOwned( DevId,  dType, ref  Owned); }
        public VJRESULT isDevExist(UInt32 DevId, VGEN_DEV_TYPE dType, ref Boolean Exist) { return _isDevExist(DevId, dType, ref Exist); }
        public VJRESULT isDevFree(UInt32 DevId, VGEN_DEV_TYPE dType, ref Boolean Free) { return _isDevFree(DevId, dType, ref Free); }
        public VJRESULT GetDevHandle(UInt32 DevId, VGEN_DEV_TYPE dType, ref Int32 hDev) { return _GetDevHandle( DevId,  dType, ref  hDev); }
        public VJRESULT isAxisExist(Int32 hDev, HID_USAGES Axis, ref Boolean Exist) { return _isAxisExist(hDev, Axis, ref Exist); }
        public VJRESULT GetDevButtonN(Int32 hDev, ref UInt32 nBtn) { return _GetDevButtonN(hDev, ref nBtn); }
        public VJRESULT GetDevHatN(Int32 hDev, ref UInt32 nHat) { return _GetDevHatN(hDev, ref nHat); }
        public VJRESULT SetDevButton(Int32 hDev, UInt32 Button, Boolean Press) { return _SetDevButton(hDev, Button, Press); }
        public VJRESULT SetDevAxis(Int32 hDev, HID_USAGES Axis, float Value) { return _SetDevAxis(hDev, Axis, Value); }
        public VJRESULT SetDevPov(Int32 hDev, UInt32 nPov, float Value) { return _SetDevPov(hDev, nPov, Value); }
        public VJRESULT GetPosition(UInt32 rID, ref JoystickState pPosition) { return _GetPosition(rID, ref pPosition); }
        public VJRESULT GetPosition(UInt32 rID, ref GamepadState pPosition) { return _GetPosition(rID, ref pPosition); }

        #endregion Common API

        #region XInput helpers

        public static VJRESULT GetXInputState(UInt32 ledN, ref XInputState pState) { return _GetXInputState(ledN, ref pState); }

        #endregion XInput helpers
    }
}
