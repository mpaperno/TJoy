//////////////////////////////////////////////////////////
//
// vGenInterface private header file
//
// Use it to declare internal global variables
// and internal function declarations
//
//////////////////////////////////////////////////////////
#pragma once

#include <atomic>
#include <map>
#include <vector>

#include "stdafx.h"

// Compilation directives
#define USE_STATIC
#define STATIC
#define VJOYHEADERUSED

#include "public.h"
#include <Xinput.h>
#include "vjoyinterface.h"
#include "XOutput.h"
#include "vGenInterface.h"
#include "ViGEm/km/BusShared.h"
#include "ViGEM/Client.h"

//////////////////////////////////

// Device Structure
typedef struct _DEVICE
{
	HDEVICE Handle;
	vGenNS::DevType Type;
	UINT Id;		// vJoy ID or vXbox Index
	PVIGEM_TARGET VGE_Target = nullptr;
	union
	{
		XINPUT_GAMEPAD * vXboxPos;
		JOYSTICK_POSITION_V2 * vJoyPos;
		DS4_REPORT *ds4Pos;
	} PPosition;
	vGenNS::DeviceInfo DevInfo;
} DEVICE, *PDEVICE;

using DevContainer_t = std::map<HDEVICE, DEVICE>;
using DevContainer_it = DevContainer_t::iterator;
using DevContainer_cit = DevContainer_t::const_iterator;

extern const DevContainer_t &DevContainer_cref;

// Macros
#define Range_vJoy(x) (((x) > 0 && (x) <= 16))
#define Range_vXbox(x) (((x) > vGenNS::DevType::vXbox && (x) <= vGenNS::DevType::vXbox + 4))
#define Range_vgeXbox(x) (((x) > vGenNS::DevType::vgeXbox && (x) <= vGenNS::DevType::vgeXbox + 4))
#define Range_vgeDS4(x) (((x) > vGenNS::DevType::vgeDS4 && (x) <= vGenNS::DevType::vgeDS4 + 4))

#define to_vXbox(x) ((x) - vGenNS::DevType::vXbox)
#define to_vgeXbox(x) ((x) - vGenNS::DevType::vgeXbox)
#define to_vgeDS4(x) ((x) - vGenNS::DevType::vgeDS4)

#define BOOL_TO_STATUS(x)  ((x) ? STATUS_SUCCESS : STATUS_UNSUCCESSFUL)

#define XINPUT_NUM_BUTTONS  19
#define DS4_NUM_BUTTONS  22

//// Device Container and Device Handle functions

// Resolves a "ranged" device ID to its actual type and device ID/index.
inline std::pair<vGenNS::DevType, UINT> DeviceRangedIdToType(UINT rID)
{
	if (Range_vJoy(rID))
		return std::make_pair(vGenNS::DevType::vJoy, rID);

	if (Range_vXbox(rID))
		return std::make_pair(vGenNS::DevType::vXbox, to_vXbox(rID));

	if (to_vgeXbox(rID))
		return std::make_pair(vGenNS::DevType::vgeXbox, to_vgeXbox(rID));

	if (Range_vgeDS4(rID))
		return std::make_pair(vGenNS::DevType::vgeDS4, to_vgeDS4(rID));

	return std::make_pair(vGenNS::DevType::UnknownDevice, 0);
}

HDEVICE CreateDevice(vGenNS::DevType Type, UINT i);
void DestroyDevice(HDEVICE & dev);

inline HDEVICE GetDeviceHandle(vGenNS::DevType Type, UINT i)
{
	// Search the device-container for an existing structure
	// that fits the description.
	for (DevContainer_cit it = DevContainer_cref.cbegin(); it != DevContainer_cref.cend(); ++it) {
		if ((it->second.Id == i) && (it->second.Type == Type))
			return it->first;
	}
	return 0;
}

inline HDEVICE GetDeviceHandle(UINT rID)
{
	const auto devType = DeviceRangedIdToType(rID);
	if (devType.first == vGenNS::DevType::UnknownDevice || !devType.second)
		return INVALID_DEV;
	return GetDeviceHandle(devType.first, devType.second);
}

inline const PDEVICE GetDevice(vGenNS::DevType Type, UINT i)
{
	// Search the device-container for an existing structure
	// that fits the description.
	for (DevContainer_cit it = DevContainer_cref.cbegin(); it != DevContainer_cref.cend(); ++it) {
		if (it->second.Type == Type && it->second.Id == i)
			return const_cast<const PDEVICE>(&it->second);
	}
	return nullptr;
}

inline const PDEVICE GetDevice(HDEVICE hDev)
{
	const DevContainer_cit it = DevContainer_cref.find(hDev);
	return (it == DevContainer_cref.cend() ? nullptr : const_cast<const PDEVICE>(&it->second));
}

inline UINT GetDeviceId(HDEVICE h)
{
	const PDEVICE dev = GetDevice(h);
	return dev ? dev->Id : 0;
}

inline vGenNS::DevType GetDeviceType(HDEVICE h)
{
	const PDEVICE dev = GetDevice(h);
	return dev ? dev->Type : vGenNS::DevType::UnknownDevice;
}

inline DWORD isDevice_vJoy(HDEVICE h) {
	return BOOL_TO_STATUS(GetDeviceType(h) == vGenNS::DevType::vJoy);
}

inline DWORD isDevice_vXbox(HDEVICE h) {
	return BOOL_TO_STATUS(GetDeviceType(h) == vGenNS::DevType::vXbox);
}

inline void *GetDevicePos(const PDEVICE dev) {
	if (!dev)
		return nullptr;

	switch (dev->Type) {
		case vGenNS::DevType::vJoy:
			vJoyNS::GetPosition(dev->Id, (PVOID)dev->PPosition.vJoyPos);
			return (void *)dev->PPosition.vJoyPos;

		case vGenNS::DevType::vXbox:
		case vGenNS::DevType::vgeXbox:
			return (void *)dev->PPosition.vXboxPos;

		case vGenNS::DevType::vgeDS4:
			return (void *)dev->PPosition.ds4Pos;

		default:
			return nullptr;
	}
}

inline void * GetDevicePos(HDEVICE h) {
	return GetDevicePos(GetDevice(h));
}

#pragma region vXbox Internal Functions
				//////////// vXbox Internal Functions ////////////

inline DWORD IX_ErrorToStatus(DWORD err)
{
	switch (err) {
		case ERROR_SUCCESS:
			return STATUS_SUCCESS;

		case XOUTPUT_VBUS_NOT_CONNECTED:
			return STATUS_NO_SUCH_DEVICE;

		case XOUTPUT_VBUS_INVALID_STATE_INFO:
			return STATUS_INVALID_DEVICE_STATE;

		case XOUTPUT_VBUS_DEVICE_NOT_READY:
			return STATUS_DEVICE_NOT_READY;

		case XOUTPUT_VBUS_IOCTL_REQUEST_FAILED:
			return STATUS_IO_DEVICE_ERROR;

		case XOUTPUT_VBUS_INDEX_OUT_OF_RANGE:
			return STATUS_INVALID_PARAMETER;

		default:
			return STATUS_IO_DEVICE_ERROR;
	}
}

/// Status
DWORD	IX_isVBusExists(void);
DWORD	IX_GetNumEmptyBusSlots(UCHAR * nSlots);
DWORD	IX_isControllerPluggedIn(UINT UserIndex, PBOOL Exist);
BOOL	IX_isControllerPluggedIn(HDEVICE hDev);
DWORD	IX_isControllerOwned(UINT UserIndex, PBOOL Owned);
BOOL	IX_isControllerOwned(HDEVICE hDev);
// Virtual device Plug-In/Unplug
DWORD	IX_PlugIn(UINT UserIndex);
DWORD	IX_PlugInNext(UINT * UserIndex);
DWORD	IX_UnPlug(UINT UserIndex);
DWORD	IX_UnPlugForce(UINT UserIndex);
// Reset Devices
DWORD	IX_ResetController(UINT UserIndex);
DWORD	IX_ResetController(HDEVICE hDev);
DWORD	IX_ResetAllControllers();
DWORD	IX_ResetControllerBtns(UINT UserIndex);
DWORD	IX_ResetControllerBtns(HDEVICE hDev);
DWORD	IX_ResetControllerDPad(UINT UserIndex);
DWORD	IX_ResetControllerDPad(HDEVICE hDev);

// Data Transfer (Data to the device)

DWORD	IX_SetBtn(const PDEVICE pDev, BOOL Press, WORD Button, BOOL XInput=FALSE);
inline DWORD IX_SetBtn(HDEVICE hDev, BOOL Press, WORD Button, BOOL XInput = FALSE) {
	return IX_SetBtn(GetDevice(hDev), Press, Button, XInput);
}
inline DWORD IX_SetBtn(UINT UserIndex, BOOL Press, WORD Button, BOOL XInput = FALSE) {
	return IX_SetBtn(GetDevice(vGenNS::DevType::vXbox, UserIndex), Press, Button, XInput);
}
#ifdef SPECIFICBUTTONS
BOOL	IX_SetBtnA(HDEVICE hDev, BOOL Press);
BOOL	IX_SetBtnA(UINT UserIndex, BOOL Press);
BOOL	IX_SetBtnB(HDEVICE hDev, BOOL Press);
BOOL	IX_SetBtnB(UINT UserIndex, BOOL Press);
BOOL	IX_SetBtnX(HDEVICE hDev, BOOL Press);
BOOL	IX_SetBtnX(UINT UserIndex, BOOL Press);
BOOL	IX_SetBtnY(HDEVICE hDev, BOOL Press);
BOOL	IX_SetBtnY(UINT UserIndex, BOOL Press);
BOOL	IX_SetBtnStart(HDEVICE hDev, BOOL Press);
BOOL	IX_SetBtnStart(UINT UserIndex, BOOL Press);
BOOL	IX_SetBtnBack(HDEVICE hDev, BOOL Press);
BOOL	IX_SetBtnBack(UINT UserIndex, BOOL Press);
BOOL	IX_SetBtnLT(HDEVICE hDev, BOOL Press); // Left Thumb/Stick
BOOL	IX_SetBtnLT(UINT UserIndex, BOOL Press); // Left Thumb/Stick
BOOL	IX_SetBtnRT(HDEVICE hDev, BOOL Press); // Right Thumb/Stick
BOOL	IX_SetBtnRT(UINT UserIndex, BOOL Press); // Right Thumb/Stick
BOOL	IX_SetBtnLB(HDEVICE hDev, BOOL Press); // Left Bumper
BOOL	IX_SetBtnLB(UINT UserIndex, BOOL Press); // Left Bumper
BOOL	IX_SetBtnRB(HDEVICE hDev, BOOL Press); // Right Bumper
BOOL	IX_SetBtnRB(UINT UserIndex, BOOL Press); // Right Bumper
#endif

DWORD	IX_SetAxis(const PDEVICE pDev, vGenNS::HID_USAGES Axis, SHORT Value);
inline DWORD IX_SetAxis(HDEVICE hDev, vGenNS::HID_USAGES Axis, SHORT Value) {
	return IX_SetAxis(GetDevice(hDev), Axis, Value);
}
inline DWORD IX_SetAxis(UINT UserIndex, vGenNS::HID_USAGES Axis, SHORT Value) {
	return IX_SetAxis(GetDevice(vGenNS::DevType::vXbox, UserIndex), Axis, Value);
}
#ifdef SPECIFICBUTTONS
DWORD	IX_SetTriggerL(HDEVICE hDev, BYTE Value); // Left Trigger
DWORD	IX_SetTriggerL(UINT UserIndex, BYTE Value); // Left Trigger
DWORD	IX_SetTriggerR(HDEVICE hDev, BYTE Value); // Right Trigger
DWORD	IX_SetTriggerR(UINT UserIndex, BYTE Value); // Right Trigger
DWORD	IX_SetAxisLx(HDEVICE hDev, SHORT Value); // Left Stick X
DWORD	IX_SetAxisLx(UINT UserIndex, SHORT Value); // Left Stick X
DWORD	IX_SetAxisLy(HDEVICE hDev, SHORT Value); // Left Stick Y
DWORD	IX_SetAxisLy(UINT UserIndex, SHORT Value); // Left Stick Y
DWORD	IX_SetAxisRx(HDEVICE hDev, SHORT Value); // Right Stick X
DWORD	IX_SetAxisRx(UINT UserIndex, SHORT Value); // Right Stick X
DWORD	IX_SetAxisRy(HDEVICE hDev, SHORT Value); // Right Stick Y
DWORD	IX_SetAxisRy(UINT UserIndex, SHORT Value); // Right Stick Y
#endif // SPECIFICBUTTONS

DWORD	IX_SetDpad(const PDEVICE pDev, UCHAR Value);
inline DWORD IX_SetDpad(HDEVICE hDev, UCHAR Value) {
	return IX_SetDpad(GetDevice(hDev), Value);
}
inline DWORD IX_SetDpad(UINT UserIndex, UCHAR Value) {
	return IX_SetDpad(GetDevice(vGenNS::DevType::vXbox, UserIndex), Value);
}
#ifdef SPECIFICBUTTONS
BOOL	IX_SetDpadUp(HDEVICE hDev);
BOOL	IX_SetDpadUp(UINT UserIndex);
BOOL	IX_SetDpadRight(HDEVICE hDev);
BOOL	IX_SetDpadRight(UINT UserIndex);
BOOL	IX_SetDpadDown(HDEVICE hDev);
BOOL	IX_SetDpadDown(UINT UserIndex);
BOOL	IX_SetDpadLeft(HDEVICE hDev);
BOOL	IX_SetDpadLeft(UINT UserIndex);
BOOL	IX_SetDpadOff(HDEVICE hDev);
BOOL	IX_SetDpadOff(UINT UserIndex);
#endif // SPECIFICBUTTONS

// Data Transfer (Feedback from the device)
DWORD	IX_GetLedNumber(UINT UserIndex, PBYTE pLed);
DWORD	IX_GetVibration(UINT UserIndex, PXINPUT_VIBRATION pVib);

#pragma endregion vXbox Internal Functions

#pragma region vJoy Internal Functions

inline void IJ_JoystickReportInit(PJOYSTICK_POSITION_V2 pPos)
{
	RtlZeroMemory(pPos, sizeof(JOYSTICK_POSITION_V2));
	pPos->wAxisX = pPos->wAxisY = pPos->wAxisZ = pPos->wAxisXRot = pPos->wAxisYRot = pPos->wAxisZRot = 0x7FFF / 2 + 1;
	pPos->bHats = pPos->bHatsEx1  = pPos->bHatsEx2  = pPos->bHatsEx3 = (DWORD)-1;
}

HDEVICE	IJ_AcquireVJD(UINT rID);				// Acquire the specified vJoy Device.
DWORD IJ_RelinquishVJD(HDEVICE hDev, PDEVICE pDev);			// Relinquish the specified vJoy Device.
BOOL IJ_isVJDExists(HDEVICE hDev);
enum VjdStat IJ_GetVJDStatus(HDEVICE hDev);			// Get the status of the specified vJoy Device.
BOOL IJ_GetVJDAxisExist(HDEVICE hDev, vGenNS::HID_USAGES Axis); // Test if given axis defined in the specified VDJ
int	IJ_GetVJDButtonNumber(HDEVICE hDev);	// Get the number of buttons defined in the specified VDJ
int IJ_GetVJDDiscPovNumber(HDEVICE hDev);   // Get the number of POVs defined in the specified device
int IJ_GetVJDContPovNumber(HDEVICE hDev);	// Get the number of descrete-type POV hats defined in the specified VDJ
BOOL IJ_SetAxis(LONG Value, HDEVICE hDev, vGenNS::HID_USAGES Axis);		// Write Value to a given axis defined in the specified VDJ
BOOL IJ_SetBtn(BOOL Value, HDEVICE hDev, UCHAR nBtn);		// Write Value to a given button defined in the specified VDJ
BOOL IJ_SetDiscPov(int Value, HDEVICE hDev, UCHAR nPov);	// Write Value to a given descrete POV defined in the specified VDJ
BOOL IJ_SetContPov(DWORD Value, HDEVICE hDev, UCHAR nPov);	// Write Value to a given continuous POV defined in the specified VDJ
DWORD IJ_ResetPositions(HDEVICE hDev);  // manual reset of all values

#pragma endregion vJoy Internal Functions

#pragma region ViGEm Internal Functions

inline DWORD VGE_ErrorToStatus(VIGEM_ERROR err)
{
	switch (err) {
		case VIGEM_ERROR_NONE:
			return STATUS_SUCCESS;

		case VIGEM_ERROR_BUS_NOT_FOUND:
		case VIGEM_ERROR_BUS_VERSION_MISMATCH:
			return STATUS_NO_SUCH_DEVICE;

		case VIGEM_ERROR_INVALID_TARGET:
			return STATUS_DEVICE_DOES_NOT_EXIST;

		case VIGEM_ERROR_REMOVAL_FAILED:
			return STATUS_DEVICE_HUNG;

		case VIGEM_ERROR_TARGET_UNINITIALIZED:
			return STATUS_DEVICE_NOT_READY;

		case VIGEM_ERROR_TARGET_NOT_PLUGGED_IN:
			return STATUS_DEVICE_NOT_CONNECTED;

		case VIGEM_ERROR_BUS_ALREADY_CONNECTED:
			return STATUS_DEVICE_ALREADY_ATTACHED;

		case VIGEM_ERROR_BUS_ACCESS_FAILED:
		case VIGEM_ERROR_BUS_INVALID_HANDLE:
			return STATUS_IO_DEVICE_ERROR;

		case VIGEM_ERROR_XUSB_USERINDEX_OUT_OF_RANGE:
		case VIGEM_ERROR_INVALID_PARAMETER:
			return STATUS_INVALID_PARAMETER;

		case VIGEM_ERROR_NOT_SUPPORTED:
			return STATUS_NOT_SUPPORTED;

		case VIGEM_ERROR_TIMED_OUT:
			return STATUS_TIMEOUT;

		case VIGEM_ERROR_IS_DISPOSING:
			return STATUS_DELETE_PENDING;

		default:
			return STATUS_UNSUCCESSFUL;
	}
}

DWORD VGE_InitClient(void);  // Try to connect to ViGEm Bus
DWORD VGE_BusExists(void);
inline DWORD VGE_Version(void) { return VIGEM_COMMON_VERSION; }
DWORD VGE_PlugIn(vGenNS::DevType dType, UINT DevId);
DWORD VGE_UnPlug(HDEVICE hDev, PDEVICE pDev, BOOL destroy = FALSE);
DWORD	VGE_ResetController(HDEVICE hDev);
DWORD	VGE_ResetController(vGenNS::DevType dType, UINT DevId);

DWORD VGE_SetBtn(const PDEVICE pDev, BOOL Press, WORD Button, BOOL XInput = FALSE);
DWORD VGE_SetDpad(const PDEVICE pDev, USHORT Value);
DWORD	VGE_SetAxis(const PDEVICE pDev, vGenNS::HID_USAGES Axis, SHORT Value);

#pragma endregion  ViGEm Internal Functions

// Other helper functions
//BOOL ConvertPosition_vJoy2vXbox(void *vJoyPos, void *vXboxPos);
//WORD ConvertButton(LONG vBtns, WORD xBtns, UINT vBtn, UINT xBtn);

