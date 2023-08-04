// vGenPrivate.cpp : Defines internal functions supporting the public API.
//

//#include <iomanip>
//#include <iostream>

#include "stdafx.h"
#include "public.h"
#include "Private.h"

#pragma comment(lib, "vJoyInterfaceStat.lib")
#pragma comment(lib, "XOutputStatic_1_2.lib")
#pragma comment(lib, "XInput")
#pragma comment(lib, "setupapi.lib")

using namespace vGenNS;

////////  Globals  (the first 4 are imported as extern declarations in vGenInterface.cpp)

PVIGEM_CLIENT VGE_Client = nullptr;
std::atomic_bool g_isShuttingDown = false;
DevContainer_t DevContainer;
const DevContainer_t &DevContainer_cref = DevContainer;

// Mapping the buttons to an array, dpad at end
WORD g_xButtons[XINPUT_NUM_BUTTONS] = {
	vGenNS::XBTN_A ,
	vGenNS::XBTN_B,
	vGenNS::XBTN_X,
	vGenNS::XBTN_Y,
	vGenNS::XBTN_LEFT_SHOULDER,
	vGenNS::XBTN_RIGHT_SHOULDER,
	vGenNS::XBTN_BACK,
	vGenNS::XBTN_START,
	vGenNS::XBTN_GUIDE,
	vGenNS::XBTN_LEFT_THUMB,
	vGenNS::XBTN_RIGHT_THUMB,
	vGenNS::XBTN_DPAD_UP,
	vGenNS::XBTN_DPAD_RIGHT,
	vGenNS::XBTN_DPAD_DOWN,
	vGenNS::XBTN_DPAD_LEFT,
	vGenNS::XBTN_DPAD_UP_RIGHT,
	vGenNS::XBTN_DPAD_DOWN_RIGHT,
	vGenNS::XBTN_DPAD_DOWN_LEFT,
	vGenNS::XBTN_DPAD_UP_LEFT
};

#define DS4_SPECIAL_BUTTON_FLAG  1 << 16

DWORD g_ds4Buttons[DS4_NUM_BUTTONS] = {
	DS4_BUTTON_CROSS,
	DS4_BUTTON_CIRCLE,
	DS4_BUTTON_SQUARE,
	DS4_BUTTON_TRIANGLE,
	DS4_BUTTON_SHOULDER_LEFT,
	DS4_BUTTON_SHOULDER_RIGHT,
	DS4_BUTTON_SHARE,
	DS4_BUTTON_OPTIONS,
	DS4_SPECIAL_BUTTON_PS | DS4_SPECIAL_BUTTON_FLAG,
	DS4_BUTTON_THUMB_LEFT,
	DS4_BUTTON_THUMB_RIGHT,
	DS4_BUTTON_DPAD_NORTH,
	DS4_BUTTON_DPAD_EAST,
	DS4_BUTTON_DPAD_SOUTH,
	DS4_BUTTON_DPAD_WEST,
	DS4_BUTTON_DPAD_NORTHEAST,
	DS4_BUTTON_DPAD_SOUTHEAST,
	DS4_BUTTON_DPAD_SOUTHWEST,
	DS4_BUTTON_DPAD_NORTHWEST,
	DS4_BUTTON_TRIGGER_LEFT,
	DS4_BUTTON_TRIGGER_RIGHT,
	DS4_SPECIAL_BUTTON_TOUCHPAD | DS4_SPECIAL_BUTTON_FLAG,
};

#pragma region Internal vXbox

DWORD	IX_isVBusExists(void)
{
	DWORD Version;
	DWORD res = XOutputGetBusVersion(&Version);
	return IX_ErrorToStatus(res);
}

DWORD	IX_GetNumEmptyBusSlots(UCHAR * nSlots)
{
	DWORD res = XOutputGetFreeSlots(1, nSlots);
	return IX_ErrorToStatus(res);
}

DWORD	IX_isControllerPluggedIn(UINT UserIndex, PBOOL Exist)
{
	DWORD res = XOutputIsPluggedIn(UserIndex - 1, Exist);
	return IX_ErrorToStatus(res);
}

BOOL	IX_isControllerPluggedIn(HDEVICE hDev)
{
	UINT UserIndex = GetDeviceId(hDev);
	if (!UserIndex)
		return FALSE;

	BOOL Exist;
	return XOutputIsPluggedIn(UserIndex - 1, &Exist) == ERROR_SUCCESS && Exist;
}

DWORD	IX_isControllerOwned(UINT UserIndex, PBOOL Owned)
{
	DWORD res = XOutputIsOwned(UserIndex - 1, Owned);
	return IX_ErrorToStatus(res);
}

BOOL	IX_isControllerOwned(HDEVICE hDev)
{
	BOOL Owned;
	if (!hDev)
		return FALSE;

	UINT UserIndex = GetDeviceId(hDev);
	if (!UserIndex)
		return FALSE;

	if (ERROR_SUCCESS == XOutputIsOwned(UserIndex - 1, &Owned))
		return Owned;
	else
		return FALSE;
}

DWORD	IX_PlugIn(UINT UserIndex)
{
	// Test is it is possible to Plug-In
	BOOL Exist;
	DWORD res;

	res = IX_isControllerPluggedIn(UserIndex, &Exist);
	if (res != ERROR_SUCCESS)
		return IX_ErrorToStatus(res);
	if (Exist)
		return STATUS_DEVICE_ALREADY_ATTACHED;

	// Plug-in
	res = XOutputPlugIn(UserIndex - 1);
	if (res != ERROR_SUCCESS)
		return IX_ErrorToStatus(res);

	// Wait for device to start - try up to 2 seconds
	BYTE Led;
	for (int i = 0; i < 2000; i++)
	{
		res = XoutputGetLedNumber(UserIndex - 1, &Led);

		// If device not ready then wait and try again
		if (res == XOUTPUT_VBUS_DEVICE_NOT_READY)
		{
			Sleep(1);
			continue;
		}

		// Device is ready or error occured
		break;
	}

	// If still not ready
	if (res == XOUTPUT_VBUS_DEVICE_NOT_READY)
		return STATUS_DEVICE_NOT_READY;

	// Create the device data structure and insert it into the device-container
	if (HDEVICE hDev = CreateDevice(vXbox, UserIndex)) {
		PDEVICE pDev = GetDevice(hDev);
		pDev->DevInfo.LedNumber = Led;
		DWORD serial;
		if (XOutputGetRealUserIndex(UserIndex - 1, &serial) == STATUS_SUCCESS)
			pDev->DevInfo.Serial = serial;
		return STATUS_SUCCESS;
	}

	// Failed to create device
	XOutputUnPlug(UserIndex - 1);
	return STATUS_INVALID_HANDLE;

}

DWORD	IX_PlugInNext(UINT * UserIndex)
{
	// Look for an empty slot
	BOOL Exist;
	UINT i = 0;
	DWORD res;
	do {
		res = IX_isControllerPluggedIn(++i, &Exist);
		if (!Exist)
		{
			*UserIndex = i;
			break;
		}
	} while (res == STATUS_SUCCESS);

	// Slot not found?
	if (res != STATUS_SUCCESS)
		return res;

	// Found, now plugin
	return IX_PlugIn(i);
}

DWORD	IX_UnPlug(UINT UserIndex)
{
	DWORD res;

	// Owned?
	BOOL Owned;
	res = IX_isControllerOwned(UserIndex, &Owned);
	if (res != STATUS_SUCCESS)
		return res;
	if (!Owned)
		return STATUS_RESOURCE_NOT_OWNED;

	// Unplug
	res = XOutputUnPlug(UserIndex - 1);
	res = IX_ErrorToStatus(res);

	// Wait for device to be unplugged
	for (int i = 0; i < 2000; i++)
	{
		if (!IX_isControllerPluggedIn(UserIndex))
			break;
		Sleep(2);
	}

	//Sleep(1000); // Temporary - replace with detection code

	// If still exists - error
	if (IX_isControllerPluggedIn(UserIndex))
		return STATUS_TIMEOUT;

	// Get handle to device and destroy it
	HDEVICE hDev = GetDeviceHandle(vXbox, UserIndex);
	DestroyDevice(hDev);
	return res;
}

DWORD	IX_UnPlugForce(UINT UserIndex)
{
	DWORD res;
	BOOL Exist;

	// Exists?
	res = IX_isControllerPluggedIn(UserIndex, &Exist);
	if (res != STATUS_SUCCESS)
		return res;
	if (!Exist)
		return STATUS_SUCCESS; // STATUS_DEVICE_DOES_NOT_EXIST;

	// Unplug
	res = XOutputUnPlugForce(UserIndex - 1);
	if (res != ERROR_SUCCESS)
		return IX_ErrorToStatus(res);

	// Wait for device to be unplugged
	for (int i = 0; i < 2000; i++)
	{
		if (!IX_isControllerPluggedIn(UserIndex))
			break;
		Sleep(2);
	}

	//Sleep(1000); // Temporary - replace with detection code

	// If still exists - error
	if (IX_isControllerPluggedIn(UserIndex))
		return STATUS_TIMEOUT;


	// Get handle to device and destroy it
	HDEVICE h = GetDeviceHandle(vXbox, UserIndex);
	DestroyDevice(h);
	return STATUS_SUCCESS;
}

// IX Reset                        ////////////////////////////////////////////////////////

DWORD	IX_ResetController(HDEVICE hDev)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev || !pDev->Id)
		return STATUS_INVALID_HANDLE;
	if (!pDev->PPosition.vXboxPos)
		return STATUS_MEMORY_NOT_ALLOCATED;

	memset(pDev->PPosition.vXboxPos, 0, sizeof(XINPUT_GAMEPAD));
	DWORD res = XOutputSetState(pDev->Id - 1, pDev->PPosition.vXboxPos);
	return IX_ErrorToStatus(res);
}

DWORD	IX_ResetController(UINT UserIndex)
{
	return IX_ResetController(GetDeviceHandle(vXbox, UserIndex));
}

DWORD	IX_ResetAllControllers()
{
	DWORD res[4] = {0};
	res[0] = IX_ResetController((UINT)1);
	res[1] = IX_ResetController((UINT)2);
	res[2] = IX_ResetController((UINT)3);
	res[3] = IX_ResetController((UINT)4);

	for (int i = 0; i < 4; i++)
		if (res[i] != STATUS_SUCCESS)
			return res[i];
	return STATUS_SUCCESS;
}

DWORD	IX_ResetControllerBtns(HDEVICE hDev)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev || !pDev->Id)
		return STATUS_INVALID_HANDLE;
	if (!pDev->PPosition.vXboxPos)
		return STATUS_MEMORY_NOT_ALLOCATED;

	// Change position value
	pDev->PPosition.vXboxPos->wButtons &= XBTN_DPAD_MASK;
	const DWORD res = XOutputSetState(pDev->Id - 1, pDev->PPosition.vXboxPos);
	return IX_ErrorToStatus(res);
}

DWORD	IX_ResetControllerBtns(UINT UserIndex)
{
	return IX_ResetControllerBtns(GetDeviceHandle(vXbox, UserIndex));
}

DWORD	IX_ResetControllerDPad(HDEVICE hDev)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev || !pDev->Id)
		return STATUS_INVALID_HANDLE;
	if (!pDev->PPosition.vXboxPos)
		return STATUS_MEMORY_NOT_ALLOCATED;

	// Change position value
	pDev->PPosition.vXboxPos->wButtons &= ~XBTN_DPAD_MASK;
	const DWORD res = XOutputSetState(pDev->Id - 1, pDev->PPosition.vXboxPos);
	return IX_ErrorToStatus(res);
}

DWORD	IX_ResetControllerDPad(UINT UserIndex)
{
	return IX_ResetControllerDPad(GetDeviceHandle(vXbox, UserIndex));
}

// IX Buttons                        ////////////////////////////////////////////////////////

DWORD	IX_SetBtn(const PDEVICE pDev, BOOL Press, WORD Button, BOOL XInput)
{
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	// Get  position
	PXINPUT_GAMEPAD position = pDev->PPosition.vXboxPos;
	if (!position)
		return STATUS_MEMORY_NOT_ALLOCATED;

	WORD Mask;
	if (!XInput && Button <= XINPUT_NUM_BUTTONS)
		Mask = g_xButtons[Button - 1];
	else
		Mask = Button;

	// Change position value
	if (Press)
		position->wButtons |= Mask;
	else
		position->wButtons &= ~Mask;
	const DWORD res = XOutputSetState(pDev->Id - 1, position);
	return IX_ErrorToStatus(res);
}

#ifdef SPECIFICBUTTONS
BOOL	IX_SetBtnA(HDEVICE hDev, BOOL Press)
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_A);
}

BOOL	IX_SetBtnB(HDEVICE hDev, BOOL Press)
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_B);
}

BOOL	IX_SetBtnX(HDEVICE hDev, BOOL Press)
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_X);
}

BOOL	IX_SetBtnY(HDEVICE hDev, BOOL Press)
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_Y);
}

BOOL	IX_SetBtnStart(HDEVICE hDev, BOOL Press)
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_START);
}

BOOL	IX_SetBtnBack(HDEVICE hDev, BOOL Press)
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_BACK);
}

BOOL	IX_SetBtnLT(HDEVICE hDev, BOOL Press) // Left Thumb/Stick
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_LEFT_THUMB);
}

BOOL	IX_SetBtnRT(HDEVICE hDev, BOOL Press) // Right Thumb/Stick
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_RIGHT_THUMB);
}

BOOL	IX_SetBtnLB(HDEVICE hDev, BOOL Press) // Left Bumper
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_LEFT_SHOULDER);
}

BOOL	IX_SetBtnRB(HDEVICE hDev, BOOL Press) // Right Bumper
{
	return IX_SetBtn(hDev, Press, XINPUT_GAMEPAD_RIGHT_SHOULDER);
}

BOOL	IX_SetBtnA(UINT UserIndex, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_A);
}

BOOL	IX_SetBtnB(UINT UserIndex, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_B);
}

BOOL	IX_SetBtnX(UINT UserIndex, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_X);
}

BOOL	IX_SetBtnY(UINT UserIndex, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_Y);
}

BOOL	IX_SetBtnStart(UINT UserIndex, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_START);
}

BOOL	IX_SetBtnBack(UINT UserIndex, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_BACK);
}

BOOL	IX_SetBtnLT(UINT UserIndex, BOOL Press) // Left Thumb/Stick
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_LEFT_THUMB);
}

BOOL	IX_SetBtnRT(UINT UserIndex, BOOL Press) // Right Thumb/Stick
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_RIGHT_THUMB);
}

BOOL	IX_SetBtnLB(UINT UserIndex, BOOL Press) // Left Bumper
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_LEFT_SHOULDER);
}

BOOL	IX_SetBtnRB(UINT UserIndex, BOOL Press) // Right Bumper
{
	return IX_SetBtn(UserIndex, Press, XINPUT_GAMEPAD_RIGHT_SHOULDER);
}

#endif // SPECIFICBUTTONS

// IX Axis                //////////////////////////////////////////////

DWORD	IX_SetAxis(const PDEVICE pDev, HID_USAGES Axis, SHORT Value)
{
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	// Get  position
	PXINPUT_GAMEPAD position = pDev->PPosition.vXboxPos;
	if (!position)
		return STATUS_MEMORY_NOT_ALLOCATED;

	// Change position value
	switch (Axis) {
		case HID_USAGE_LT:
			position->bLeftTrigger = Value & 0xFF;
			break;
		case HID_USAGE_RT:
			position->bRightTrigger = Value & 0xFF;
			break;
		case HID_USAGE_LX:
			position->sThumbLX = Value;
			break;
		case HID_USAGE_LY:
			position->sThumbLY = Value;
			break;
		case HID_USAGE_RX:
			position->sThumbRX = Value;
			break;
		case HID_USAGE_RY:
			position->sThumbRY = Value;
			break;
		default:
			return STATUS_INVALID_PARAMETER_2;
	};

	const DWORD res = XOutputSetState(pDev->Id - 1, position);
	return IX_ErrorToStatus(res);
}

#ifdef SPECIFICBUTTONS
DWORD	IX_SetTriggerL(HDEVICE hDev, BYTE Value) // Left Trigger
{
	return IX_SetAxis(hDev, HID_USAGE_LT, Value);
}

DWORD	IX_SetTriggerL(UINT UserIndex, BYTE Value) // Left Trigger
{
	return IX_SetTriggerL(GetDeviceHandle(vXbox, UserIndex), Value);
}

DWORD	IX_SetTriggerR(HDEVICE hDev, BYTE Value) // Right Trigger
{
	return IX_SetAxis(hDev, HID_USAGE_RT, Value);
}

DWORD	IX_SetTriggerR(UINT UserIndex, BYTE Value) // Right Trigger
{
	return IX_SetTriggerR(GetDeviceHandle(vXbox, UserIndex), Value);
}

DWORD	IX_SetAxisLx(HDEVICE hDev, SHORT Value) // Left Stick X
{
	return IX_SetAxis(hDev, HID_USAGE_LX, Value);
}

DWORD	IX_SetAxisLx(UINT UserIndex, SHORT Value) // Left Stick X
{
	return IX_SetAxisLx(GetDeviceHandle(vXbox, UserIndex), Value);
}

DWORD	IX_SetAxisLy(HDEVICE hDev, SHORT Value) // Left Stick Y
{
	return IX_SetAxis(hDev, HID_USAGE_LY, Value);
}

DWORD	IX_SetAxisLy(UINT UserIndex, SHORT Value) // Left Stick Y
{
	return IX_SetAxisLy(GetDeviceHandle(vXbox, UserIndex), Value);
}

DWORD	IX_SetAxisRx(HDEVICE hDev, SHORT Value) // Right Stick X
{
	return IX_SetAxis(hDev, HID_USAGE_RX, Value);
}

DWORD	IX_SetAxisRx(UINT UserIndex, SHORT Value) // Right Stick X
{
	return IX_SetAxisRx(GetDeviceHandle(vXbox, UserIndex), Value);
}

DWORD	IX_SetAxisRy(HDEVICE hDev, SHORT Value) // Right Stick Y
{
	return IX_SetAxis(hDev, HID_USAGE_RY, Value);
}

DWORD	IX_SetAxisRy(UINT UserIndex, SHORT Value) // Right Stick Y
{
	return IX_SetAxisRy(GetDeviceHandle(vXbox, UserIndex), Value);
}
#endif // SPECIFICBUTTONS

// IX DPAD               ///////////////////////////////////////

// This lets any of the 4 dpov button bits be set at any one time but will not allow
// individual bits to be set one at a time, like you get with the actual "button" types.
// Subsequent calls to this function will clear any previously set DPOV button bits (0xF)
DWORD	IX_SetDpad(const PDEVICE pDev, UCHAR Value)
{
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	// Get  position
	PXINPUT_GAMEPAD position = pDev->PPosition.vXboxPos;
	if (!position)
		return STATUS_MEMORY_NOT_ALLOCATED;

	// Change position value
	position->wButtons &= ~XBTN_DPAD_MASK;
	position->wButtons |= Value;
	const DWORD res = XOutputSetState(pDev->Id - 1, position);
	return IX_ErrorToStatus(res);
}

#ifdef SPECIFICBUTTONS
BOOL	IX_SetDpadUp(HDEVICE hDev)
{
	return IX_SetDpad(hDev, XBTN_DPAD_UP);
}

BOOL	IX_SetDpadUp(UINT UserIndex)
{
	return IX_SetDpad(UserIndex, XBTN_DPAD_UP);
}

BOOL	IX_SetDpadRight(HDEVICE hDev)
{
	return IX_SetDpad(hDev, XBTN_DPAD_RIGHT);
}

BOOL	IX_SetDpadRight(UINT UserIndex)
{
	return IX_SetDpad(UserIndex, XBTN_DPAD_RIGHT);
}

BOOL	IX_SetDpadDown(HDEVICE hDev)
{
	return IX_SetDpad(hDev, XBTN_DPAD_DOWN);
}

BOOL	IX_SetDpadDown(UINT UserIndex)
{
	return IX_SetDpad(UserIndex, XBTN_DPAD_DOWN);
}

BOOL	IX_SetDpadLeft(HDEVICE hDev)
{
	return IX_SetDpad(hDev, XBTN_DPAD_LEFT);
}

BOOL	IX_SetDpadLeft(UINT UserIndex)
{
	return IX_SetDpad(UserIndex, XBTN_DPAD_LEFT);
}

BOOL	IX_SetDpadOff(HDEVICE hDev)
{
	return IX_SetDpad(hDev, XBTN_NONE);
}

BOOL	IX_SetDpadOff(UINT UserIndex)
{
	return IX_SetDpad(UserIndex, XBTN_NONE);
}
#endif // SPECIFICBUTTONS

// IX Get infos                 /////////////////////////////////////////

DWORD	IX_GetLedNumber(UINT UserIndex, PBYTE pLed)
{
	BOOL Exist;
	DWORD res;

	// Test if device is plugged-in
	res = IX_isControllerPluggedIn(UserIndex, &Exist);
	if (res != STATUS_SUCCESS)
		return res;
	if (!Exist)
		return STATUS_DEVICE_DOES_NOT_EXIST;

	HDEVICE h = GetDeviceHandle(vXbox, UserIndex);
	if (!h)
		return STATUS_INVALID_HANDLE;

	if (!pLed)
		return STATUS_INVALID_PARAMETER_2;

	res = XoutputGetLedNumber(UserIndex - 1, pLed);
	return IX_ErrorToStatus(res);
}

DWORD	IX_GetVibration(UINT UserIndex, PXINPUT_VIBRATION pVib)
{
	HDEVICE h = GetDeviceHandle(vXbox, UserIndex);
	if (!h)
		return STATUS_INVALID_HANDLE;

	if (!pVib)
		return STATUS_INVALID_PARAMETER_2;

	const DWORD res = XoutputGetVibration(UserIndex - 1, pVib);
	return IX_ErrorToStatus(res);
}

#pragma endregion Internal vXbox

/////////////////////////////////////////

#pragma region Internal vJoy

HDEVICE	IJ_AcquireVJD(UINT rID)
{
	if (vJoyNS::AcquireVJD(rID))
		return CreateDevice(vJoy, rID);

	return INVALID_DEV;
}

DWORD IJ_RelinquishVJD(HDEVICE hDev, PDEVICE pDev)			// Relinquish the specified vJoy Device.
{
	if (pDev && pDev->Type == DevType::vJoy)
	{
		vJoyNS::RelinquishVJD(pDev->Id);
		DestroyDevice(hDev);
		return STATUS_SUCCESS;
	}
	return STATUS_UNSUCCESSFUL;
}

BOOL IJ_isVJDExists(HDEVICE hDev)
{
	const PDEVICE pDev = GetDevice(hDev);
	return pDev && pDev->Type == DevType::vJoy &&
		vJoyNS::isVJDExists(pDev->Id);
}

VjdStat IJ_GetVJDStatus(HDEVICE hDev)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (pDev && pDev->Type == DevType::vJoy)
		return vJoyNS::GetVJDStatus(pDev->Id);
	else
		return VJD_STAT_MISS;
}

BOOL IJ_GetVJDAxisExist(HDEVICE hDev, HID_USAGES Axis)
{
	const PDEVICE pDev = GetDevice(hDev);
	return pDev && pDev->Type == DevType::vJoy &&
		vJoyNS::GetVJDAxisExist(pDev->Id, Axis);
}

int	IJ_GetVJDButtonNumber(HDEVICE hDev)	// Get the number of buttons defined in the specified VDJ
{
	const PDEVICE pDev = GetDevice(hDev);
	if (pDev && pDev->Type == DevType::vJoy)
		return vJoyNS::GetVJDButtonNumber(pDev->Id);
	return 0;
}

int IJ_GetVJDDiscPovNumber(HDEVICE hDev)   // Get the number of POVs defined in the specified device
{
	const PDEVICE pDev = GetDevice(hDev);
	if (pDev && pDev->Type == DevType::vJoy)
		return vJoyNS::GetVJDDiscPovNumber(pDev->Id);
	return 0;
}

int IJ_GetVJDContPovNumber(HDEVICE hDev)	// Get the number of descrete-type POV hats defined in the specified VDJ
{
	const PDEVICE pDev = GetDevice(hDev);
	if (pDev && pDev->Type == DevType::vJoy)
		return vJoyNS::GetVJDContPovNumber(pDev->Id);
	return 0;
}

BOOL IJ_SetAxis(LONG Value, HDEVICE hDev, HID_USAGES Axis)		// Write Value to a given axis defined in the specified VDJ
{
	const PDEVICE pDev = GetDevice(hDev);
	return pDev && pDev->Type == DevType::vJoy && vJoyNS::GetVJDAxisExist(pDev->Id, Axis) &&
		vJoyNS::SetAxis(Value, pDev->Id, Axis);
}

BOOL IJ_SetBtn(BOOL Value, HDEVICE hDev, UCHAR nBtn)		// Write Value to a given button defined in the specified VDJ
{
	const PDEVICE pDev = GetDevice(hDev);
	return pDev && pDev->Type == DevType::vJoy && vJoyNS::GetVJDButtonNumber(pDev->Id) >= nBtn &&
		vJoyNS::SetBtn(Value, pDev->Id, nBtn);
}

BOOL IJ_SetDiscPov(int Value, HDEVICE hDev, UCHAR nPov)	// Write Value to a given descrete POV defined in the specified VDJ
{
	const PDEVICE pDev = GetDevice(hDev);
	return pDev && pDev->Type == DevType::vJoy && vJoyNS::GetVJDDiscPovNumber(pDev->Id) >= nPov &&
		vJoyNS::SetDiscPov(Value, pDev->Id, nPov);
}

BOOL IJ_SetContPov(DWORD Value, HDEVICE hDev, UCHAR nPov)	// Write Value to a given continuous POV defined in the specified VDJ
{
	const PDEVICE pDev = GetDevice(hDev);
	return pDev && pDev->Type == DevType::vJoy && vJoyNS::GetVJDContPovNumber(pDev->Id) >= nPov &&
		vJoyNS::SetContPov(Value, pDev->Id, nPov);
}

DWORD IJ_ResetPositions(HDEVICE hDev)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	IJ_JoystickReportInit(pDev->PPosition.vJoyPos);
	return BOOL_TO_STATUS(vJoyNS::UpdateVJD(pDev->Id, pDev->PPosition.vJoyPos));
}

#pragma endregion

#pragma region ViGEm Internal Functions

DWORD VGE_InitClient(void)
{
	if (VGE_Client)
		return STATUS_SUCCESS;

	VGE_Client = vigem_alloc();
	if (!VGE_Client)
		return STATUS_MEMORY_NOT_ALLOCATED;

	const VIGEM_ERROR res = vigem_connect(VGE_Client);
	if (res == VIGEM_ERROR_NONE || res == VIGEM_ERROR_BUS_ALREADY_CONNECTED)
		return STATUS_SUCCESS;

	vigem_free(VGE_Client);
	VGE_Client = nullptr;
	return VGE_ErrorToStatus(res);
}

DWORD VGE_BusExists(void)
{
	// There's no better way to check if the bus exists than to init it.
	return VGE_InitClient();
}

#if 0
static VOID CALLBACK VGE_notification_x360(
	PVIGEM_CLIENT Client, PVIGEM_TARGET Target,
	UCHAR LargeMotor, UCHAR SmallMotor, UCHAR LedNumber,
	LPVOID UserData
)
{
	//static int count = 1;
	//std::cout << count++ << " " << (int)LedNumber << " " << UserData << std::endl;

	if (PDEVICE pDev = (PDEVICE)UserData)
		pDev->DevInfo.LedNumber = LedNumber + 1;
	//if (LedNumber)
	//vigem_target_x360_unregister_notification(Target);
}

VOID CALLBACK VGE_notification_ds4(
	PVIGEM_CLIENT Client, PVIGEM_TARGET Target,
	UCHAR LargeMotor, UCHAR SmallMotor, DS4_LIGHTBAR_COLOR LightbarColor,
	LPVOID UserData
)
{
	static int count = 1;
	std::cout << '\n' << count++ << " " << std::hex << std::setfill('0')
		<< std::setw(2) << (int)LightbarColor.Red << ':'
		<< std::setw(2) << (int)LightbarColor.Green << ':'
		<< std::setw(2) << (int)LightbarColor.Blue << std::endl;
	if (PDEVICE pDev = (PDEVICE)UserData) {
		pDev->DevInfo.ColorBar = (0xFF << 24) | (LightbarColor.Red << 16) | (LightbarColor.Green << 8) | LightbarColor.Blue;
		//if (pDev->DevInfo.ColorBar & 0x00FFFFFF)
		//vigem_target_x360_unregister_notification(Target);
	}
}
#endif

DWORD VGE_PlugIn(vGenNS::DevType dType, UINT DevId)
{
	PDEVICE pDev = GetDevice(dType, DevId);
	if (!pDev) {
		HDEVICE hDev = CreateDevice(dType, DevId);
		pDev = GetDevice(hDev);
		if (!pDev)
			return STATUS_IO_DEVICE_ERROR;
	}
	else {
		pDev->DevInfo.LedNumber = 0;
		pDev->DevInfo.Serial = 0;
	}

	if (!pDev->VGE_Target) {
		DWORD stat = VGE_InitClient();
		if (stat != STATUS_SUCCESS)
			return stat;

		pDev->VGE_Target = dType == DevType::vgeXbox ? vigem_target_x360_alloc() : vigem_target_ds4_alloc();
		if (!pDev->VGE_Target)
			return STATUS_MEMORY_NOT_ALLOCATED;
	}
	else if (vigem_target_is_attached(pDev->VGE_Target)) {
		return STATUS_DEVICE_ALREADY_ATTACHED;
	}

	const VIGEM_ERROR res = vigem_target_add(VGE_Client, pDev->VGE_Target);
	if (res == VIGEM_ERROR_NONE) {
		pDev->DevInfo.Serial = vigem_target_get_index(pDev->VGE_Target);
		pDev->DevInfo.VendId = vigem_target_get_vid(pDev->VGE_Target);
		pDev->DevInfo.ProdId = vigem_target_get_pid(pDev->VGE_Target);
		if (dType == DevType::vgeXbox) {
			//vigem_target_x360_register_notification(VGE_Client, pDev->VGE_Target, &VGE_notification_x360, pDev);
			ULONG led;
			if (vigem_target_x360_get_user_index(VGE_Client, pDev->VGE_Target, &led) == VIGEM_ERROR_NONE)
				pDev->DevInfo.LedNumber = (BYTE)led + 1;
			//std::cout << pDev->DevInfo.Serial << " " << (int)pDev->DevInfo.LedNumber << std::endl;
		}
#if 0   // none of this is working to get the lightbar color
		else {
			/*DS4_OUTPUT_BUFFER ds4Rep;
			RtlZeroMemory(&ds4Rep, sizeof(DS4_OUTPUT_BUFFER));
			VIGEM_ERROR res2 = vigem_target_ds4_await_output_report_timeout(VGE_Client, pDev->VGE_Target, 500, &ds4Rep);
			pDev->DevInfo.ColorBar = (ds4Rep.Buffer[2] << 16) | (ds4Rep.Buffer[3] << 8) | ds4Rep.Buffer[4];
			std::cout << std::hex << std::setfill('0') << std::setw(8) << res2 << " " << std::setw(8) << pDev->DevInfo.ColorBar << " "
			<< std::setw(2) << (int)ds4Rep.Buffer[0] << ":" << std::setw(2) << (int)ds4Rep.Buffer[1] << ":"<< std::setw(2) << (int)ds4Rep.Buffer[2] << ":" << std::setw(2) << (int)ds4Rep.Buffer[3] << ":" << std::setw(2) << (int)ds4Rep.Buffer[4] << std::endl;*/
			//vigem_target_ds4_register_notification(VGE_Client, pDev->VGE_Target, &VGE_notification_ds4, pDev);
		}
#endif
	}
	return VGE_ErrorToStatus(res);
}

DWORD VGE_UnPlug(HDEVICE hDev, PDEVICE pDev, BOOL destroy)
{
	if (!hDev || !pDev)
		return STATUS_INVALID_HANDLE;

	DWORD ret;
	if (pDev->VGE_Target && vigem_target_is_attached(pDev->VGE_Target)) {
		//if (pDev->Type == DevType::vgeXbox)
		//vigem_target_x360_unregister_notification(pDev->VGE_Target);
		const VIGEM_ERROR res = vigem_target_remove(VGE_Client, pDev->VGE_Target);
		ret = VGE_ErrorToStatus(res);
	}
	else {
		ret = STATUS_DEVICE_NOT_CONNECTED;
	}
	if (destroy /*&& res == VIGEM_ERROR_NONE*/)
		DestroyDevice(hDev);

	return ret;
}

DWORD VGE_ResetController(HDEVICE hDev)
{
	PDEVICE pDev = GetDevice(hDev);
	if (!pDev || !pDev->VGE_Target)
		return STATUS_INVALID_HANDLE;

	VIGEM_ERROR res;

	if (pDev->Type == DevType::vgeXbox) {
		RtlZeroMemory(pDev->PPosition.vXboxPos, sizeof(XINPUT_GAMEPAD));
		res = vigem_target_x360_update(VGE_Client, pDev->VGE_Target, *((PXUSB_REPORT)pDev->PPosition.vXboxPos));
	}
	// DS4
	else {
		DS4_REPORT_INIT(pDev->PPosition.ds4Pos);
		res = vigem_target_ds4_update(VGE_Client, pDev->VGE_Target, *pDev->PPosition.ds4Pos);
	}

	return VGE_ErrorToStatus(res);
}

DWORD VGE_ResetController(vGenNS::DevType dType, UINT DevId)
{
	return VGE_ResetController(GetDeviceHandle(dType, DevId));
}

DWORD	VGE_SetBtn(const PDEVICE pDev, BOOL Press, WORD Button, BOOL XInput)
{
	if (!pDev || !pDev->VGE_Target)
		return STATUS_INVALID_HANDLE;

	DWORD Mask;
	if (XInput)
		Mask = Button;
	else if (pDev->Type == DevType::vgeXbox && Button <= XINPUT_NUM_BUTTONS)
		Mask = g_xButtons[Button - 1];
	else if (pDev->Type == DevType::vgeDS4 && Button <= DS4_NUM_BUTTONS)
		Mask = g_ds4Buttons[Button - 1];
	else
		return STATUS_INVALID_PARAMETER_3;

	VIGEM_ERROR res;

	if (pDev->Type == DevType::vgeXbox) {
		PXUSB_REPORT position = (PXUSB_REPORT)pDev->PPosition.vXboxPos;
		if (!position)
			return STATUS_MEMORY_NOT_ALLOCATED;

		// Change position value
		if (Press)
			position->wButtons |= (WORD)Mask;
		else
			position->wButtons &= ~(WORD)Mask;
		res = vigem_target_x360_update(VGE_Client, pDev->VGE_Target, *position);
	}
	// DS4
	else {
		if (Mask <= XBTN_DPAD_MASK)
			return VGE_SetDpad(pDev, Press ? (SHORT)Mask : DS4_BUTTON_DPAD_NONE);

		PDS4_REPORT position = pDev->PPosition.ds4Pos;
		if (!position)
			return STATUS_MEMORY_NOT_ALLOCATED;

		// special buttons?
		if (Mask & DS4_SPECIAL_BUTTON_FLAG) {
			if (Press)
				position->bSpecial |= (BYTE)Mask;
			else
				position->bSpecial &= ~(BYTE)Mask;
		}
		// normal buttons
		else {
			if (Press)
				position->wButtons |= (WORD)Mask;
			else
				position->wButtons &= ~(WORD)Mask;
		}
		res = vigem_target_ds4_update(VGE_Client, pDev->VGE_Target, *position);
	}

	return VGE_ErrorToStatus(res);
}

DWORD	VGE_SetDpad(const PDEVICE pDev, USHORT Value)
{
	if (!pDev || !pDev->VGE_Target)
		return STATUS_INVALID_HANDLE;

	VIGEM_ERROR res;

	if (pDev->Type == DevType::vgeXbox) {
		PXUSB_REPORT position = (PXUSB_REPORT)pDev->PPosition.vXboxPos;
		if (!position)
			return STATUS_MEMORY_NOT_ALLOCATED;

		position->wButtons &= ~XBTN_DPAD_MASK;
		position->wButtons |= Value;
		res = vigem_target_x360_update(VGE_Client, pDev->VGE_Target, *position);
	}
	// DS4
	else {
		PDS4_REPORT position = pDev->PPosition.ds4Pos;
		if (!position)
			return STATUS_MEMORY_NOT_ALLOCATED;

		position->wButtons &= ~XBTN_DPAD_MASK;
		position->wButtons |= Value;
		res = vigem_target_ds4_update(VGE_Client, pDev->VGE_Target, *position);
	}

	return VGE_ErrorToStatus(res);
}

DWORD	VGE_SetAxis(const PDEVICE pDev, HID_USAGES Axis, SHORT Value)
{
	if (!pDev || !pDev->VGE_Target)
		return STATUS_INVALID_HANDLE;

	VIGEM_ERROR res;

	if (pDev->Type == DevType::vgeXbox) {
		PXUSB_REPORT position = (PXUSB_REPORT)pDev->PPosition.vXboxPos;
		if (!position)
			return STATUS_MEMORY_NOT_ALLOCATED;

		switch (Axis) {
			case HID_USAGE_LT:
				position->bLeftTrigger = Value & 0xFF;
				break;
			case HID_USAGE_RT:
				position->bRightTrigger = Value & 0xFF;
				break;
			case HID_USAGE_LX:
				position->sThumbLX = Value;
				break;
			case HID_USAGE_LY:
				position->sThumbLY = Value;
				break;
			case HID_USAGE_RX:
				position->sThumbRX = Value;
				break;
			case HID_USAGE_RY:
				position->sThumbRY = Value;
				break;
			default:
				return STATUS_INVALID_PARAMETER_2;
		};

		res = vigem_target_x360_update(VGE_Client, pDev->VGE_Target, *position);
	}
	// DS4
	else {
		PDS4_REPORT position = pDev->PPosition.ds4Pos;
		if (!position)
			return STATUS_MEMORY_NOT_ALLOCATED;

		const BYTE bValue = Value & 0xFF;
		switch (Axis) {
			case HID_USAGE_LT:
				position->bTriggerL = bValue;
				break;
			case HID_USAGE_RT:
				position->bTriggerR = bValue;
				break;
			case HID_USAGE_LX:
				position->bThumbLX = bValue;
				break;
			case HID_USAGE_LY:
				position->bThumbLY = bValue;
				break;
			case HID_USAGE_RX:
				position->bThumbRX = bValue;
				break;
			case HID_USAGE_RY:
				position->bThumbRY = bValue;
				break;
			default:
				return STATUS_INVALID_PARAMETER_2;
		};

		res = vigem_target_ds4_update(VGE_Client, pDev->VGE_Target, *position);
	}

	return VGE_ErrorToStatus(res);
}

#pragma endregion  ViGEm Internal Functions


#pragma region Helper Functions

HDEVICE CreateDevice(vGenNS::DevType Type, UINT i)
{
	HDEVICE h;
	// If found then exit
	if ((h = GetDeviceHandle(Type, i)))
		return h;

	// Create device structure
	h = i + Type + ((rand() % 1000 + 1) << 16);
	DEVICE dev = {h, Type, i};

	switch (Type) {
		case DevType::vJoy:
			dev.PPosition.vJoyPos = new JOYSTICK_POSITION_V2;
			IJ_JoystickReportInit(dev.PPosition.vJoyPos);
			break;
		case DevType::vXbox:
		case DevType::vgeXbox:
			dev.PPosition.vXboxPos = new XINPUT_GAMEPAD;
			RtlZeroMemory(dev.PPosition.vXboxPos, sizeof(XINPUT_GAMEPAD));
			break;
		case DevType::vgeDS4:
			dev.PPosition.ds4Pos = new DS4_REPORT;
			DS4_REPORT_INIT(dev.PPosition.ds4Pos);
			break;

		default:
			return INVALID_DEV;
	}

	// Insert in container
	if (DevContainer.emplace(h, dev).second)
		return h;

	return INVALID_DEV;
}

void DestroyDevice(HDEVICE & dev)
{
	DevContainer_cit it = DevContainer_cref.find(dev);
	dev = INVALID_DEV;
	if (it == DevContainer.cend())
		return;

	const DEVICE &device = it->second;
	switch (device.Type) {
		case DevType::vJoy:
			delete device.PPosition.vJoyPos;
			break;
		case DevType::vXbox:
		case DevType::vgeXbox:
			delete device.PPosition.vXboxPos;
			break;
		case DevType::vgeDS4:
			delete device.PPosition.ds4Pos;
			break;
	}

	if (device.VGE_Target) {
		if (vigem_target_is_attached(device.VGE_Target))
			vigem_target_remove(VGE_Client, device.VGE_Target);
		vigem_target_free(device.VGE_Target);
	}

	DevContainer.erase(it);
}

#if 0
BOOL ConvertPosition_vJoy2vXbox(void *vJoyPos, void *vXboxPos)
{
	if (!vJoyPos || !vXboxPos)
		return FALSE;

	// Convert the input position
	JOYSTICK_POSITION_V2 * inPos = (JOYSTICK_POSITION_V2 *)vJoyPos;

	// Convert the output position
	XINPUT_GAMEPAD * position = (XINPUT_GAMEPAD *)vXboxPos;

	///////// Convert values from vJoy to vXbox
	/////  Axes
	position->sThumbLX = 2 * ((SHORT)inPos->wAxisX - 1) - 32767;
	position->sThumbLY = 2 * ((SHORT)inPos->wAxisY - 1) - 32767;
	position->bLeftTrigger = ((SHORT)inPos->wAxisZ - 1) / 128;
	position->sThumbRX = 2 * ((SHORT)inPos->wAxisXRot - 1) - 32767;
	position->sThumbRY = 2 * ((SHORT)inPos->wAxisYRot - 1) - 32767;
	position->bRightTrigger = ((SHORT)inPos->wAxisZRot - 1) / 128;

	//// Dpad / Discrete POV #1
	switch (inPos->bHats & 0xF)
	{
		case 0:
			position->wButtons |= XBTN_DPAD_UP;
			break;
		case 1:
			position->wButtons |= XBTN_DPAD_RIGHT;
			break;
		case 2:
			position->wButtons |= XBTN_DPAD_DOWN;
			break;
		case 4:
			position->wButtons |= XBTN_DPAD_LEFT;
			break;
		default:
			position->wButtons &= ~XBTN_DPAD_MASK;
	}

	// Buttons (may override hats)
	for (UINT i = 0; i < XINPUT_NUM_BUTTONS; ++i)
		position->wButtons = ConvertButton(inPos->lButtons, position->wButtons, i + 1, g_xButtons[i]);

	return TRUE;
}

WORD ConvertButton(LONG vBtns, WORD xBtns, UINT vBtn, UINT xBtn)
{
	WORD out;
	out = ((vBtns&(1 << (vBtn - 1))) == 0) ? xBtns & ~xBtn : xBtns | xBtn;
	return out;
}
#endif

#pragma endregion // Helper Functions
