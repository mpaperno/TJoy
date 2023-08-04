/****************************************************************************
*																			*
*   vGenInterface.h -- This header file defines the Generic interface		*
*   that includes vJoy and vXbox											*
*		                                                                    *
*                                                                           *
*   Copyright (c)  Shaul Eizikovich									        *
*																			*
*****************************************************************************/

#pragma once

// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the VGENINTERFACE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// VGENINTERFACE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef VGENINTERFACE_EXPORTS
#define VGENINTERFACE_API __declspec(dllexport)
#else
#define VGENINTERFACE_API __declspec(dllimport)
#endif

// Definition to Device Handle
typedef INT HDEVICE;
#define  INVALID_DEV (HDEVICE)0
#define ValidDev(x) ((x) != INVALID_DEV)

typedef void (CALLBACK *RemovalCB)(BOOL, BOOL, PVOID);

#ifndef VJDSTAT
#define VJDSTAT
enum VjdStat  /* Declares an enumeration data type */
{
	VJD_STAT_OWN,	// The  vJoy Device is owned by this application.
	VJD_STAT_FREE,	// The  vJoy Device is NOT owned by any application (including this one).
	VJD_STAT_BUSY,	// The  vJoy Device is owned by another application. It cannot be acquired by this application.
	VJD_STAT_MISS,	// The  vJoy Device is missing. It either does not exist or the driver is down.
	VJD_STAT_UNKN	// Unknown
};
#endif

namespace vGenNS {

	// Device Type
	enum DevType {
		UnknownDevice = -1,
		vJoy = 0,
		vXbox = 1000,
		vgeXbox = 2000,
		vgeDS4 = 3000
	};

	enum PovType : BYTE
	{
		PovTypeUnknown    = 0,
		PovTypeDiscrete   = 0x01,
		PovTypeContinuous = 0x02,
		PovTypeAny = PovTypeDiscrete | PovTypeContinuous,
	};

	enum XINPUT_BUTTONS : USHORT
	{
		XBTN_NONE           = 0,
		XBTN_DPAD_UP        = 0x0001,
		XBTN_DPAD_DOWN      = 0x0002,
		XBTN_DPAD_LEFT      = 0x0004,
		XBTN_DPAD_RIGHT     = 0x0008,
		XBTN_START          = 0x0010,
		XBTN_BACK           = 0x0020,
		XBTN_LEFT_THUMB     = 0x0040,
		XBTN_RIGHT_THUMB    = 0x0080,
		XBTN_LEFT_SHOULDER  = 0x0100,
		XBTN_RIGHT_SHOULDER = 0x0200,
		XBTN_GUIDE          = 0x0400,
		XBTN_A              = 0x1000,
		XBTN_B              = 0x2000,
		XBTN_X              = 0x4000,
		XBTN_Y              = 0x8000,

		XBTN_DPAD_UP_RIGHT   = XBTN_DPAD_UP | XBTN_DPAD_RIGHT,
		XBTN_DPAD_UP_LEFT    = XBTN_DPAD_UP | XBTN_DPAD_LEFT,
		XBTN_DPAD_DOWN_RIGHT = XBTN_DPAD_DOWN | XBTN_DPAD_RIGHT,
		XBTN_DPAD_DOWN_LEFT  = XBTN_DPAD_DOWN | XBTN_DPAD_LEFT,
		XBTN_DPAD_MASK       = 0x000F,
	};

	enum HID_USAGES : USHORT
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
	};

	enum DPOV_DIRECTION : SHORT
	{
		DPOV_None = -2,
		DPOV_Center = -1,
		DPOV_North = 0,
		DPOV_East,
		DPOV_South,
		DPOV_West,
		DPOV_NorthEast,
		DPOV_SouthEast,
		DPOV_SouthWest,
		DPOV_NorthWest,
	};

	struct DeviceInfo
	{
		USHORT ProdId = 0;  // USB PID
		USHORT VendId = 0;  // USB VID
		DWORD Serial = 0;   // Serial No/ID
		DWORD ColorBar = 0;  // DS4
		BYTE LedNumber = 0;  // XBox
	};

}  // namespace vGenNS

#ifndef VJOYHEADERUSED

#pragma region HID
// HID Descriptor definitions - Axes
#define HID_USAGE_X		0x30
#define HID_USAGE_Y		0x31
#define HID_USAGE_Z		0x32
#define HID_USAGE_RX	0x33
#define HID_USAGE_RY	0x34
#define HID_USAGE_RZ	0x35
#define HID_USAGE_SL0	0x36
#define HID_USAGE_SL1	0x37
#define HID_USAGE_WHL	0x38
#define HID_USAGE_POV	0x39
#pragma endregion HID

#pragma region FFB Re-Definitions
// HID Descriptor definitions - FFB Report IDs
#define HID_ID_STATE	0x02	// Usage PID State report
#define HID_ID_EFFREP	0x01	// Usage Set Effect Report
#define HID_ID_ENVREP	0x02	// Usage Set Envelope Report
#define HID_ID_CONDREP	0x03	// Usage Set Condition Report
#define HID_ID_PRIDREP	0x04	// Usage Set Periodic Report
#define HID_ID_CONSTREP	0x05	// Usage Set Constant Force Report
#define HID_ID_RAMPREP	0x06	// Usage Set Ramp Force Report
#define HID_ID_CSTMREP	0x07	// Usage Custom Force Data Report
#define HID_ID_SMPLREP	0x08	// Usage Download Force Sample
#define HID_ID_EFOPREP	0x0A	// Usage Effect Operation Report
#define HID_ID_BLKFRREP	0x0B	// Usage PID Block Free Report
#define HID_ID_CTRLREP	0x0C	// Usage PID Device Control
#define HID_ID_GAINREP	0x0D	// Usage Device Gain Report
#define HID_ID_SETCREP	0x0E	// Usage Set Custom Force Report
#define HID_ID_NEWEFREP	0x01	// Usage Create New Effect Report
#define HID_ID_BLKLDREP	0x02	// Usage Block Load Report
#define HID_ID_POOLREP	0x03	// Usage PID Pool Report


enum FFBEType // FFB Effect Type
{

	// Effect Type
	ET_NONE = 0,	  //    No Force
	ET_CONST = 1,    //    Constant Force
	ET_RAMP = 2,    //    Ramp
	ET_SQR = 3,    //    Square
	ET_SINE = 4,    //    Sine
	ET_TRNGL = 5,    //    Triangle
	ET_STUP = 6,    //    Sawtooth Up
	ET_STDN = 7,    //    Sawtooth Down
	ET_SPRNG = 8,    //    Spring
	ET_DMPR = 9,    //    Damper
	ET_INRT = 10,   //    Inertia
	ET_FRCTN = 11,   //    Friction
	ET_CSTM = 12,   //    Custom Force Data
};

enum FFBPType // FFB Packet Type
{
	// Write
	PT_EFFREP = HID_ID_EFFREP,	// Usage Set Effect Report
	PT_ENVREP = HID_ID_ENVREP,	// Usage Set Envelope Report
	PT_CONDREP = HID_ID_CONDREP,	// Usage Set Condition Report
	PT_PRIDREP = HID_ID_PRIDREP,	// Usage Set Periodic Report
	PT_CONSTREP = HID_ID_CONSTREP,	// Usage Set Constant Force Report
	PT_RAMPREP = HID_ID_RAMPREP,	// Usage Set Ramp Force Report
	PT_CSTMREP = HID_ID_CSTMREP,	// Usage Custom Force Data Report
	PT_SMPLREP = HID_ID_SMPLREP,	// Usage Download Force Sample
	PT_EFOPREP = HID_ID_EFOPREP,	// Usage Effect Operation Report
	PT_BLKFRREP = HID_ID_BLKFRREP,	// Usage PID Block Free Report
	PT_CTRLREP = HID_ID_CTRLREP,	// Usage PID Device Control
	PT_GAINREP = HID_ID_GAINREP,	// Usage Device Gain Report
	PT_SETCREP = HID_ID_SETCREP,	// Usage Set Custom Force Report

									// Feature
									PT_NEWEFREP = HID_ID_NEWEFREP + 0x10,	// Usage Create New Effect Report
									PT_BLKLDREP = HID_ID_BLKLDREP + 0x10,	// Usage Block Load Report
									PT_POOLREP = HID_ID_POOLREP + 0x10,		// Usage PID Pool Report
};

enum FFBOP
{
	EFF_START = 1, // EFFECT START
	EFF_SOLO = 2, // EFFECT SOLO START
	EFF_STOP = 3, // EFFECT STOP
};

enum FFB_CTRL
{
	CTRL_ENACT = 1,	// Enable all device actuators.
	CTRL_DISACT = 2,	// Disable all the device actuators.
	CTRL_STOPALL = 3,	// Stop All Effects­ Issues a stop on every running effect.
	CTRL_DEVRST = 4,	// Device Reset– Clears any device paused condition, enables all actuators and clears all effects from memory.
	CTRL_DEVPAUSE = 5,	// Device Pause– The all effects on the device are paused at the current time step.
	CTRL_DEVCONT = 6,	// Device Continue– The all effects that running when the device was paused are restarted from their last time step.
};

enum FFB_EFFECTS {
	Constant = 0x0001,
	Ramp = 0x0002,
	Square = 0x0004,
	Sine = 0x0008,
	Triangle = 0x0010,
	Sawtooth_Up = 0x0020,
	Sawtooth_Dn = 0x0040,
	Spring = 0x0080,
	Damper = 0x0100,
	Inertia = 0x0200,
	Friction = 0x0400,
	Custom = 0x0800,
};

typedef struct _FFB_DATA {
	ULONG	size;
	ULONG	cmd;
	UCHAR	*data;
} FFB_DATA, *PFFB_DATA;

typedef struct _FFB_EFF_CONSTANT {
	BYTE EffectBlockIndex;
	LONG Magnitude; 			  // Constant force magnitude: 	-10000 - 10000
} FFB_EFF_CONSTANT, *PFFB_EFF_CONSTANT;

typedef struct _FFB_EFF_RAMP {
	BYTE		EffectBlockIndex;
	LONG 		Start;             // The Normalized magnitude at the start of the effect (-10000 - 10000)
	LONG 		End;               // The Normalized magnitude at the end of the effect	(-10000 - 10000)
} FFB_EFF_RAMP, *PFFB_EFF_RAMP;

//typedef struct _FFB_EFF_CONST {
typedef struct _FFB_EFF_REPORT {
	BYTE		EffectBlockIndex;
	FFBEType	EffectType;
	WORD		Duration;// Value in milliseconds. 0xFFFF means infinite
	WORD		TrigerRpt;
	WORD		SamplePrd;
	BYTE		Gain;
	BYTE		TrigerBtn;
	BOOL		Polar; // How to interpret force direction Polar (0-360°) or Cartesian (X,Y)
	union
	{
		BYTE	Direction; // Polar direction: (0x00-0xFF correspond to 0-360°)
		BYTE	DirX; // X direction: Positive values are To the right of the center (X); Negative are Two's complement
	};
	BYTE		DirY; // Y direction: Positive values are below the center (Y); Negative are Two's complement
} FFB_EFF_REPORT, *PFFB_EFF_REPORT;
//} FFB_EFF_CONST, *PFFB_EFF_CONST;

typedef struct _FFB_EFF_OP {
	BYTE		EffectBlockIndex;
	FFBOP		EffectOp;
	BYTE		LoopCount;
} FFB_EFF_OP, *PFFB_EFF_OP;

typedef struct _FFB_EFF_PERIOD {
	BYTE		EffectBlockIndex;
	DWORD		Magnitude;			// Range: 0 - 10000
	LONG 		Offset;				// Range: –10000 - 10000
	DWORD 		Phase;				// Range: 0 - 35999
	DWORD 		Period;				// Range: 0 - 32767
} FFB_EFF_PERIOD, *PFFB_EFF_PERIOD;

typedef struct _FFB_EFF_COND {
	BYTE		EffectBlockIndex;
	BOOL		isY;
	LONG 		CenterPointOffset; // CP Offset:  Range -­10000 ­- 10000
	LONG 		PosCoeff; // Positive Coefficient: Range -­10000 ­- 10000
	LONG 		NegCoeff; // Negative Coefficient: Range -­10000 ­- 10000
	DWORD 		PosSatur; // Positive Saturation: Range 0 – 10000
	DWORD 		NegSatur; // Negative Saturation: Range 0 – 10000
	LONG 		DeadBand; // Dead Band: : Range 0 – 1000
} FFB_EFF_COND, *PFFB_EFF_COND;

typedef struct _FFB_EFF_ENVLP {
	BYTE		EffectBlockIndex;
	DWORD 		AttackLevel;   // The Normalized magnitude of the stating point: 0 - 10000
	DWORD 		FadeLevel;	   // The Normalized magnitude of the stopping point: 0 - 10000
	DWORD 		AttackTime;	   // Time of the attack: 0 - 4294967295
	DWORD 		FadeTime;	   // Time of the fading: 0 - 4294967295
} FFB_EFF_ENVLP, *PFFB_EFF_ENVLP;

#define FFB_DATA_READY	 WM_USER+31

typedef void (CALLBACK *FfbGenCB)(PVOID, PVOID);
#pragma endregion

#endif // !VJOYHEADERUSED

//////////////////////////////////////////////////////////////////////////////////////
///
///  vJoy interface fuctions (Native vJoy)
///  If you wish to write GENERIC code for vJoy, vXbox, and ViGEm (XBox360 & DualShock4),
///  then you can use the Common API set of functions.
///
///  Axis & Button Mapping from vJoy to Gamepad:
///
///  vJoy      | XBox              | DualShock4
///  ------------------------------------------
///  X         | X
///  Y         | Y
///  Z         | Trigger (R)
///  RX        | Rx
///  RY        | Ry
///  RZ        | Trigger (L)
///  Button 1  | A                 | Cross
///  Button 2  | B                 | Circle
///  Button 3  | X                 | Square
///  Button 4  | Y                 | Triangle
///  Button 5  | Left Bumper (LB)  | Left Shoulder (L1)
///  Button 6  | Right Bumper (RB) | Right Shoulder (R1)
///  Button 7  | Back              | Share
///  Button 8  | Start             | Options
///  Button 9  | Guide             | PS
///  Button 10 | Left Thumb (LT)   | L3
///  Button 11 | Right Thumb (RT)  | R3
///  Button 12 | DPad Up
///  Button 13 | DPad Right
///  Button 14 | DPad Down
///  Button 15 | DPad Left
///  Button 16 | DPad NE
///  Button 17 | DPad SE
///  Button 18 | DPad SW
///  Button 19 | DPad NW
///  Button 20 | -                 | Left Trigger (L2)
///  Button 21 | -                 | Right Trigger (R2)
///  Button 22 | -                 | TouchPad (TP)
///
///  Axis Serial number is:
///    | vJoy    | XBox/DS4
///  --------------------------
///  1 | X       | Lx
///  2 | Y       | Ly
///  3 | Z       | Right Trigger / R2
///  4 | RX      | Rx
///  5 | RY      | Ry
///  6 | RZ      | Left Trigger / L2
///  7 | Slider0 | -
///  8 | Slider1 | -
///

extern "C" {
	///
#pragma region vJoy Backward compatibility API
	//////////////////////////////////////////////////////////////////////////////////////
	///  These legacy functions support vJoy and vXBox, and in some cases ViGEm devices as well (unless otherwise noted in the function comments).
	///  The device type is encoded into the "reference ID" (rID) and includes the type and index of the device.
	///
	///  Using the Common API instead is recommended!
	///
	///  vJoy device ID range: 1-16
	///  vXbox device ID range: 1001-1004
	///  ViGEm XBox device ID range: 2001-2004
	///  ViGEm DS4 device ID range: 3001-3004
	///
	// Version - vJoy ONLY!
	VGENINTERFACE_API	SHORT	__cdecl GetvJoyVersion(void);
	VGENINTERFACE_API	BOOL	__cdecl vJoyEnabled(void);
	VGENINTERFACE_API	PVOID	__cdecl	GetvJoyProductString(void);
	VGENINTERFACE_API	PVOID	__cdecl	GetvJoyManufacturerString(void);
	VGENINTERFACE_API	PVOID	__cdecl	GetvJoySerialNumberString(void);
	VGENINTERFACE_API	BOOL	__cdecl	DriverMatch(WORD * DllVer, WORD * DrvVer);
	VGENINTERFACE_API	VOID	__cdecl	RegisterRemovalCB(RemovalCB cb, PVOID data);
	VGENINTERFACE_API	BOOL	__cdecl	vJoyFfbCap(BOOL * Supported);	// Is this version of vJoy capable of FFB?
	VGENINTERFACE_API	BOOL	__cdecl	GetvJoyMaxDevices(int * n);	// What is the maximum possible number of vJoy devices
	VGENINTERFACE_API	BOOL	__cdecl	GetNumberExistingVJD(int * n);	// What is the number of vJoy devices currently enabled

	/////	vJoy/vXbox Device properties
	VGENINTERFACE_API int     __cdecl GetVJDButtonNumber(UINT rID);	// Get the number of buttons defined in the specified VDJ
	VGENINTERFACE_API int     __cdecl GetVJDDiscPovNumber(UINT rID);	// Get the number of descrete-type POV hats defined in the specified VDJ
	VGENINTERFACE_API int     __cdecl GetVJDContPovNumber(UINT rID);	// Get the number of descrete-type POV hats defined in the specified VDJ
	VGENINTERFACE_API BOOL    __cdecl GetVJDAxisExist(UINT rID, vGenNS::HID_USAGES Axis); // Test if given axis defined in the specified VDJ
	VGENINTERFACE_API BOOL    __cdecl GetVJDAxisMax(UINT rID, vGenNS::HID_USAGES Axis, LONG * Max); // Get logical Maximum value for a given axis defined in the specified VDJ
	VGENINTERFACE_API BOOL    __cdecl GetVJDAxisMin(UINT rID, vGenNS::HID_USAGES Axis, LONG * Min); // Get logical Minimum value for a given axis defined in the specified VDJ
	VGENINTERFACE_API BOOL    __cdecl GetVJDAxisRange(UINT rID, vGenNS::HID_USAGES Axis, LONG * Min, LONG * Max);
	VGENINTERFACE_API VjdStat __cdecl GetVJDStatus(UINT rID);			// Get the status of the specified vJoy Device.
	VGENINTERFACE_API BOOL    __cdecl isVJDExists(UINT rID);			// TRUE if the specified vJoy Device exists
	// vJoy ONLY
	VGENINTERFACE_API int     __cdecl GetOwnerPid(UINT rID);			// Reurn owner's Process ID if the specified vJoy Device exists

	/////	Acquire and write to Device  -- vJoy and vXbox ONLY
	VGENINTERFACE_API BOOL		__cdecl	AcquireVJD(UINT rID);				// Acquire the specified vJoy Device. deprecated, doesn't handle ViGEm
	VGENINTERFACE_API VOID		__cdecl	RelinquishVJD(UINT rID);			// Relinquish the specified vJoy Device. deprecated, doesn't handle ViGEm
	VGENINTERFACE_API BOOL		__cdecl	UpdateVJD(UINT rID, PVOID pData);	// Update the position data of the specified vJoy Device. vJoy only, returns false for other types

	/////	Write access to vJoy Device - Modifiers
	// This group of functions modify the current value of the position data
	// They replace the need to create a structure of position data then call UpdateVJD

	//// Device-Reset functions
	VGENINTERFACE_API BOOL		__cdecl	ResetVJD(UINT rID);			// Reset all controls to predefined values in the specified VDJ
	VGENINTERFACE_API VOID		__cdecl	ResetAll(void);				// Reset all controls to predefined values in all VDJ; vJoy and vXbox ONLY
	VGENINTERFACE_API BOOL		__cdecl	ResetButtons(UINT rID);		// Reset all buttons (To 0) in the specified VDJ;  vJoy and vXbox ONLY
	VGENINTERFACE_API BOOL		__cdecl	ResetPovs(UINT rID);		// Reset all POV Switches (To -1) in the specified VDJ;  vJoy and vXbox ONLY

	// Write data -- vJoy and vXbox ONLY
	VGENINTERFACE_API BOOL		__cdecl	SetAxis(LONG Value, UINT rID, vGenNS::HID_USAGES Axis);		// Write Value to a given axis defined in the specified VDJ
	VGENINTERFACE_API BOOL		__cdecl	SetBtn(BOOL Value, UINT rID, UCHAR nBtn);		// Write Value to a given button defined in the specified VDJ
	VGENINTERFACE_API BOOL		__cdecl	SetDiscPov(int Value, UINT rID, UCHAR nPov);	// Write Value to a given descrete POV defined in the specified VDJ
	VGENINTERFACE_API BOOL		__cdecl	SetContPov(DWORD Value, UINT rID, UCHAR nPov);	// Write Value to a given continuous POV defined in the specified VDJ

	#pragma region vJoy FFB
	// FFB function
	VGENINTERFACE_API FFBEType	__cdecl	FfbGetEffect();	// Returns effect serial number if active, 0 if inactive
	VGENINTERFACE_API VOID		__cdecl	FfbRegisterGenCB(FfbGenCB cb, PVOID data);
	//__declspec(deprecated("** FfbStart function was deprecated - you can remove it from your code **")) \
	//VGENINTERFACE_API BOOL		__cdecl	FfbStart(UINT rID);				  // Start the FFB queues of the specified vJoy Device.
	//__declspec(deprecated("** FfbStop function was deprecated - you can remove it from your code **")) \
	//VGENINTERFACE_API VOID		__cdecl	FfbStop(UINT rID);				  // Stop the FFB queues of the specified vJoy Device.

	// Added in 2.1.6
	VGENINTERFACE_API BOOL		__cdecl	IsDeviceFfb(UINT rID);
	VGENINTERFACE_API BOOL		__cdecl	IsDeviceFfbEffect(UINT rID, UINT Effect);

	//  Force Feedback (FFB) helper functions
	VGENINTERFACE_API DWORD 	__cdecl	Ffb_h_DeviceID(const FFB_DATA * Packet, int *DeviceID);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_Type(const FFB_DATA * Packet, FFBPType *Type);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_Packet(const FFB_DATA * Packet, WORD *Type, int *DataSize, BYTE *Data[]);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_EBI(const FFB_DATA * Packet, int *Index);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_Eff_Report(const FFB_DATA * Packet, FFB_EFF_REPORT*  Effect);
	//__declspec(deprecated("** Ffb_h_Eff_Const function was deprecated - Use function Ffb_h_Eff_Report **")) \
	//VGENINTERFACE_API DWORD 	__cdecl Ffb_h_Eff_Const(const FFB_DATA * Packet, FFB_EFF_REPORT*  Effect);
	VGENINTERFACE_API DWORD		__cdecl Ffb_h_Eff_Ramp(const FFB_DATA * Packet, FFB_EFF_RAMP*  RampEffect);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_EffOp(const FFB_DATA * Packet, FFB_EFF_OP*  Operation);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_DevCtrl(const FFB_DATA * Packet, FFB_CTRL *  Control);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_Eff_Period(const FFB_DATA * Packet, FFB_EFF_PERIOD*  Effect);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_Eff_Cond(const FFB_DATA * Packet, FFB_EFF_COND*  Condition);
	VGENINTERFACE_API DWORD 	__cdecl Ffb_h_DevGain(const FFB_DATA * Packet, BYTE * Gain);
	VGENINTERFACE_API DWORD		__cdecl Ffb_h_Eff_Envlp(const FFB_DATA * Packet, FFB_EFF_ENVLP*  Envelope);
	VGENINTERFACE_API DWORD		__cdecl Ffb_h_EffNew(const FFB_DATA * Packet, FFBEType * Effect);

	// Added in 2.1.6
	VGENINTERFACE_API DWORD		__cdecl Ffb_h_Eff_Constant(const FFB_DATA * Packet, FFB_EFF_CONSTANT *  ConstantEffect);
	#pragma endregion  vJoy FFB
#pragma endregion  vJoy Backward compatibility API

#pragma region vXbox API
	//////////////////////////////////////////////////////////////////////////////////////
	///
	///  Legacy vXbox-specific interface functions.
	///  Devices are accessed by their "user index" in the range of 1-4 (not necessarily related to Led number).
	///
	///  Using the Common API instead is recommended!
	///
	//////////////////////////////////////////////////////////////////////////////////////

	// Virtual vXbox bus information
	VGENINTERFACE_API	DWORD		__cdecl isVBusExist(void);
	VGENINTERFACE_API DWORD		__cdecl	GetVBusVersion(void);
	VGENINTERFACE_API	DWORD		__cdecl GetNumEmptyBusSlots(UCHAR * nSlots);

	// Device Status (Plugin/Unplug and check ownership)
	VGENINTERFACE_API	DWORD		__cdecl isControllerPluggedIn(UINT UserIndex, PBOOL Exist);
	VGENINTERFACE_API	DWORD		__cdecl isControllerOwned(UINT UserIndex, PBOOL Exist);
	VGENINTERFACE_API	DWORD		__cdecl GetVXAxisRange(UINT UserIndex, vGenNS::HID_USAGES Axis, LONG * Min, LONG * Max);
	VGENINTERFACE_API	DWORD		__cdecl PlugIn(UINT UserIndex);
	VGENINTERFACE_API	DWORD		__cdecl PlugInNext(UINT * UserIndex);
	VGENINTERFACE_API	DWORD		__cdecl UnPlug(UINT UserIndex);
	VGENINTERFACE_API	DWORD		__cdecl UnPlugForce(UINT UserIndex);

	// Reset Devices
	VGENINTERFACE_API	DWORD		__cdecl ResetController(UINT UserIndex);
	VGENINTERFACE_API	DWORD		__cdecl ResetAllControllers();
#ifdef SPECIFICRESET
	VGENINTERFACE_API	DWORD		__cdecl ResetControllerBtns(UINT UserIndex);
	VGENINTERFACE_API	DWORD		__cdecl ResetControllerDPad(UINT UserIndex);

#endif // SPECIFICRESET

	// Button functions: Per-button Press/Release
	VGENINTERFACE_API	DWORD		__cdecl SetButton(UINT UserIndex, WORD Button, BOOL Press);
#ifdef SPECIFICBUTTONS
	VGENINTERFACE_API	BOOL		__cdecl SetBtnA(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnB(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnX(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnY(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnLT(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnRT(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnLB(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnRB(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnStart(UINT UserIndex, BOOL Press);
	VGENINTERFACE_API	BOOL		__cdecl SetBtnBack(UINT UserIndex, BOOL Press);
#endif // SPECIFICBUTTONS

	// Trigger/Axis functions: Set value in the range
	VGENINTERFACE_API	DWORD		__cdecl SetGamepadAxis(UINT UserIndex, vGenNS::HID_USAGES Axis, SHORT Value);
#ifdef SPECIFICBUTTONS
	VGENINTERFACE_API	DWORD		__cdecl SetTriggerL(UINT UserIndex, BYTE Value);
	VGENINTERFACE_API	DWORD		__cdecl SetTriggerR(UINT UserIndex, BYTE Value);
	VGENINTERFACE_API	DWORD		__cdecl	SetAxisLx(UINT UserIndex, SHORT Value); // Left Stick X
	VGENINTERFACE_API	DWORD		__cdecl	SetAxisLy(UINT UserIndex, SHORT Value); // Left Stick Y
	VGENINTERFACE_API	DWORD		__cdecl	SetAxisRx(UINT UserIndex, SHORT Value); // Right Stick X
	VGENINTERFACE_API	DWORD		__cdecl	SetAxisRy(UINT UserIndex, SHORT Value); // Right Stick Y
#endif // SPECIFICBUTTONS

	// DPAD Functions
	VGENINTERFACE_API	DWORD		__cdecl	SetDpad(UINT UserIndex, UCHAR Value); // DPAD Set Value
#ifdef SPECIFICBUTTONS
	VGENINTERFACE_API	BOOL		__cdecl	SetDpadUp(UINT UserIndex); // DPAD Up
	VGENINTERFACE_API	BOOL		__cdecl	SetDpadRight(UINT UserIndex); // DPAD Right
	VGENINTERFACE_API	BOOL		__cdecl	SetDpadDown(UINT UserIndex); // DPAD Down
	VGENINTERFACE_API	BOOL		__cdecl	SetDpadLeft(UINT UserIndex); // DPAD Left
	VGENINTERFACE_API	BOOL		__cdecl	SetDpadOff(UINT UserIndex); // DPAD Off
#endif // SPECIFICBUTTONS

	// Feedback Polling: Assigned Led number / Vibration values
	VGENINTERFACE_API	DWORD		__cdecl	GetLedNumber(UINT UserIndex, PBYTE pLed);
	VGENINTERFACE_API	DWORD		__cdecl	GetVibration(UINT UserIndex, PXINPUT_VIBRATION pVib);
#pragma endregion  vXbox API

#pragma region Common API
	VGENINTERFACE_API void   __cdecl DeInit(void);  // disconnect/remove all devices, deallocate all resources
	// Device Administration, Manipulation and Information
	VGENINTERFACE_API DWORD   __cdecl AcquireDev(UINT DevId, vGenNS::DevType dType, HDEVICE * hDev);	// Acquire a Device.
	VGENINTERFACE_API DWORD   __cdecl RelinquishDev(HDEVICE hDev);			// Relinquish a Device.

	VGENINTERFACE_API VjdStat  __cdecl GetDevStatus(HDEVICE hDev);			// Get the status of the specified vJoy Device.
	VGENINTERFACE_API VjdStat  __cdecl GetDevTypeStatus(vGenNS::DevType dType, UINT DevId);			// Get the status of the specified vJoy Device.

	VGENINTERFACE_API DWORD   __cdecl GetDevType(HDEVICE hDev, vGenNS::DevType * dType);	// Get device type (vJoy/vXbox)
	VGENINTERFACE_API DWORD   __cdecl GetDevNumber(HDEVICE hDev, UINT * dNumber);	// If vJoy: Number=Id; If vXbox: Number=Led#
	VGENINTERFACE_API DWORD   __cdecl GetDevId(HDEVICE hDev, UINT * dID);					// Return Device ID to be used with vXbox API and Backward compatibility API
	VGENINTERFACE_API DWORD   __cdecl GetDevHandle(UINT DevId, vGenNS::DevType dType, HDEVICE * hDev);// Return device handle from Device ID and Device type

	VGENINTERFACE_API DWORD   __cdecl isDevOwned(UINT DevId, vGenNS::DevType dType, BOOL * Owned);	// Is device plugged-in/Configured by this feeder
	VGENINTERFACE_API DWORD   __cdecl isDevExist(UINT DevId, vGenNS::DevType dType, BOOL * Exist);	// Is device plugged-in/Configured
	VGENINTERFACE_API DWORD   __cdecl isDevFree(UINT DevId, vGenNS::DevType dType, BOOL * Free);	// Is device unplugged/Free
	VGENINTERFACE_API DWORD   __cdecl isAxisExist(HDEVICE hDev, vGenNS::HID_USAGES Axis, BOOL * Exist);	// Does Axis exist. See above table

	// Get logical Minimum and Maximum values for a given axis defined in the specified VJD. Always returns "vJoy ranges" which are used by `SetDevAxis()`.
	VGENINTERFACE_API DWORD   __cdecl GetDevAxisRange(HDEVICE hDev, vGenNS::HID_USAGES Axis, LONG * Min, LONG * Max);
	VGENINTERFACE_API DWORD   __cdecl GetDevButtonN(HDEVICE hDev, USHORT * nBtn);			// Get number of buttons in device
	VGENINTERFACE_API DWORD   __cdecl GetDevHatN(HDEVICE hDev, vGenNS::PovType povType, USHORT * nHat);	// Get number of Hats/POVs in device.
	VGENINTERFACE_API DWORD   __cdecl	GetPosition(HDEVICE hDev, PVOID pData);	          //  Read current positions vJoy device
	VGENINTERFACE_API DWORD   __cdecl GetDevInfo(HDEVICE hDev, vGenNS::DeviceInfo * DevInfo);

	VGENINTERFACE_API BOOL    __cdecl	IsDevTypeSupported(vGenNS::DevType dType);
	VGENINTERFACE_API DWORD   __cdecl	GetDriverVersion(vGenNS::DevType dType);
	VGENINTERFACE_API DWORD   __cdecl	GetXInputState(UINT ledN, PXINPUT_STATE pData);	 //  Read current positions XInput device by LED number. Should work for unowned devices as well.

	// Position Setting
	// The button number for gamepads corresponds to the button mapping described at the top of this file.
	VGENINTERFACE_API DWORD   __cdecl SetDevButton(HDEVICE hDev, UINT Button, BOOL Press);
	// Sets axis values based on vJoy value ranges, 0 - 0x7FFF. Auto-scales the range for other devices. See notes at top for mapping of HID axis names to gamepad axes.
	VGENINTERFACE_API DWORD   __cdecl	SetDevAxis(HDEVICE hDev, vGenNS::HID_USAGES Axis, LONG Value);
	// Sets axis values based on percentage, 0-100. Automatically scales for the appropriate device type.
	VGENINTERFACE_API DWORD   __cdecl SetDevAxisPct(HDEVICE hDev, vGenNS::HID_USAGES Axis, FLOAT Value);
	VGENINTERFACE_API DWORD   __cdecl SetDevDiscPov(HDEVICE hDev, UCHAR nPov, vGenNS::DPOV_DIRECTION Value);
	// The Value parameter in vJoy POV axis range 0-35900, or -1 for center. For DPAD the values are interpolated from degrees to the 8 available directions.
	VGENINTERFACE_API DWORD   __cdecl SetDevContPov(HDEVICE hDev, UCHAR nPov, DWORD Value);
	// The Value parameter in vJoy POV axis range 0-35900, or -1 for center. For discreet POV or DPAD the values are interpolated from degrees to the 8 available directions.
	// For gamepads this is basically the same as using SetDevContPov().
	VGENINTERFACE_API DWORD   __cdecl SetDevPov(HDEVICE hDev, UCHAR nPov, DWORD Value);
	// The Value parameter is either degrees between 0 and 360 (inclusive) or -1 for center. This is essentially an alias for calling SetDevPov(hDev, nPov, DWORD(Value * 100))
	VGENINTERFACE_API DWORD   __cdecl SetDevPovDeg(HDEVICE hDev, UCHAR nPov, FLOAT Value);

	VGENINTERFACE_API DWORD   __cdecl ResetDevPositions(HDEVICE hDev);
#pragma endregion  Common API
} // extern "C"
