// vGenInterface.cpp : Defines the exported functions for the DLL application.
//

#include "private.h"

using namespace vGenNS;

extern DevContainer_t DevContainer;
extern const DevContainer_t &DevContainer_cref;
extern PVIGEM_CLIENT VGE_Client;
extern std::atomic_bool g_isShuttingDown;


extern "C" {

#pragma region Interface Functions (vJoy)
VGENINTERFACE_API SHORT GetvJoyVersion(void)
{
	return vJoyNS::GetvJoyVersion();
}

VGENINTERFACE_API	BOOL		vJoyEnabled(void)
{
	return vJoyNS::vJoyEnabled();
}

VGENINTERFACE_API	PVOID	GetvJoyProductString(void)
{
	return vJoyNS::GetvJoyProductString();
}

VGENINTERFACE_API	PVOID	GetvJoyManufacturerString(void)
{
	return vJoyNS::GetvJoyManufacturerString();
}

VGENINTERFACE_API	PVOID	GetvJoySerialNumberString(void)
{
	return vJoyNS::GetvJoySerialNumberString();
}

VGENINTERFACE_API	BOOL  DriverMatch(WORD * DllVer, WORD * DrvVer)
{
	return vJoyNS::DriverMatch(DllVer, DrvVer);
}

VGENINTERFACE_API	VOID	RegisterRemovalCB(RemovalCB cb, PVOID data)
{
	return vJoyNS::RegisterRemovalCB(cb,  data);
}

VGENINTERFACE_API	BOOL	vJoyFfbCap(BOOL * Supported)
{
	return vJoyNS::vJoyFfbCap(Supported);
}

VGENINTERFACE_API	BOOL	GetvJoyMaxDevices(int * n)
{
	return vJoyNS::GetvJoyMaxDevices(n);
}

VGENINTERFACE_API	BOOL	GetNumberExistingVJD(int * n)	// What is the number of vJoy devices currently enabled
{
	return vJoyNS::GetNumberExistingVJD(n);
}

VGENINTERFACE_API int GetVJDButtonNumber(UINT rID)	// Get the number of buttons defined in the specified device
{
	if (Range_vJoy(rID))
		return vJoyNS::GetVJDButtonNumber(rID);
	if (Range_vXbox(rID) || Range_vgeXbox(rID))
		return XINPUT_NUM_BUTTONS;
	if (Range_vgeDS4(rID))
		return DS4_NUM_BUTTONS;
	return 0;
}

VGENINTERFACE_API int GetVJDDiscPovNumber(UINT rID)	// Get the number of POVs defined in the specified device
{
	if (Range_vJoy(rID))
		return vJoyNS::GetVJDDiscPovNumber(rID);
	return 1;
}

VGENINTERFACE_API int GetVJDContPovNumber(UINT rID)	// Get the number of POVs defined in the specified device
{
	if (Range_vJoy(rID))
		return vJoyNS::GetVJDContPovNumber(rID);
	return 1;
}

VGENINTERFACE_API BOOL GetVJDAxisExist(UINT rID, HID_USAGES Axis) // Test if given axis defined in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::GetVJDAxisExist(rID, Axis);

	// add the pov because we can use it as a "fake" axis to set the dpad using the "compat" API
	return ((Axis >= HID_USAGE_X && Axis <= HID_USAGE_RZ) || Axis == HID_USAGE_POV);
}

// Get logical Maximum value for a given axis defined in the specified VJD.
// The vJoy interface always uses the vJoy range.  See GetVXAxisRange for actual vXBox max value.
VGENINTERFACE_API BOOL GetVJDAxisMax(UINT rID, HID_USAGES Axis, LONG * Max)
{
	if (Range_vJoy(rID))
		return vJoyNS::GetVJDAxisMax(rID, Axis, Max);
	*Max = Axis == HID_USAGE_POV ? 35900 : 32767;
	return TRUE;
}

// Get logical Minimum value for a given axis defined in the specified VJD.
// This vJoy interface always uses the vJoy range.See GetVXAxisRange for actual vXBox min value.
VGENINTERFACE_API BOOL GetVJDAxisMin(UINT rID, HID_USAGES Axis, LONG * Min)
{
	if (Range_vJoy(rID))
		return vJoyNS::GetVJDAxisMin(rID, Axis, Min);
	*Min = 0;
	return TRUE;
}

// Get logical Minimum and Maximum values for a given axis defined in the specified VJD.
// This vJoy interface always uses the vJoy range.See GetVXAxisRange for actual vXBox min value.
VGENINTERFACE_API BOOL GetVJDAxisRange(UINT rId, HID_USAGES Axis, LONG * Min, LONG * Max)
{
	return GetVJDAxisMin(rId, Axis, Min) && GetVJDAxisMax(rId, Axis, Max);
}

// Get the status of the specified VJD.
VGENINTERFACE_API VjdStat GetVJDStatus(UINT rID)
{
	const auto devType = DeviceRangedIdToType(rID);
	if (devType.first == vGenNS::DevType::UnknownDevice || !devType.second)
		return VJD_STAT_MISS;
	return GetDevTypeStatus(devType.first, devType.second);
}

// TRUE if the specified VJD exists
VGENINTERFACE_API BOOL isVJDExists(UINT rID)
{
	if (Range_vJoy(rID))
		return vJoyNS::isVJDExists(rID);

	if (Range_vXbox(rID))
	{
		BOOL Exist;
		if SUCCEEDED(IX_isControllerPluggedIn(to_vXbox(rID), &Exist))
			return Exist;
	}

	if (Range_vgeXbox(rID) || Range_vgeDS4(rID))
		return GetVJDStatus(rID) != VJD_STAT_MISS;

	return FALSE;
}

// vJoy only, returns 0 for other types
VGENINTERFACE_API int GetOwnerPid(UINT rID)
{
	if (Range_vJoy(rID))
		return vJoyNS::GetOwnerPid(rID);
	return 0;
}

// deprecated, doesn't handle ViGEm
VGENINTERFACE_API BOOL AcquireVJD(UINT rID)				// Acquire the specified vJoy Device.
{
	if (Range_vJoy(rID))
		return vJoyNS::AcquireVJD(rID);
	if (Range_vXbox(rID))
		return (SUCCEEDED(IX_PlugIn(to_vXbox(rID))));

	return FALSE;
}

// deprecated, doesn't handle ViGEm
// Relinquish the specified vJoy Device.
VGENINTERFACE_API VOID RelinquishVJD(UINT rID)
{
	if (Range_vJoy(rID))
		vJoyNS::RelinquishVJD(rID);
	else if (Range_vXbox(rID))
		IX_UnPlug(to_vXbox(rID));
}

// Update the position data of the specified VJD.
// vJoy only, returns false for other types
VGENINTERFACE_API BOOL UpdateVJD(UINT rID, PVOID pData)
{
	if (Range_vJoy(rID))
		return vJoyNS::UpdateVJD(rID, pData);
	return FALSE;
}

// vJoy and vXbox ONLY
VGENINTERFACE_API BOOL SetAxis(LONG Value, UINT rID, HID_USAGES Axis)		// Write Value to a given axis defined in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::SetAxis(Value, rID, Axis);

	if (Range_vXbox(rID))
	{
		if (Value > 32767)
			Value = 32767;
		else if (Value < 0)
			Value = 0;

		// If Triggers (Z,RZ) then remap range:   0 - 32767  ==> 0 - 255
		if (Axis == HID_USAGE_LT || Axis == HID_USAGE_RT)
			return SUCCEEDED(IX_SetAxis(to_vXbox(rID), Axis, static_cast <BYTE>((Value - 1) / 128)));

		// If Axis is X,Y,RX,RY then remap range: 0 - 32767  ==> -32768 - 32767
		SHORT vx_Value = static_cast<SHORT>((Value - 16384) * 2);
		return SUCCEEDED(IX_SetAxis(to_vXbox(rID), Axis, vx_Value));
	}
	return FALSE;
}

// vJoy and vXbox ONLY
VGENINTERFACE_API BOOL SetBtn(BOOL Value, UINT rID, UCHAR nBtn)		// Write Value to a given button defined in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::SetBtn(Value, rID, nBtn);

	if (Range_vXbox(rID))
		return SUCCEEDED(IX_SetBtn(to_vXbox(rID), Value, nBtn));

	return FALSE;
}

// vJoy and vXbox ONLY
VGENINTERFACE_API BOOL SetDiscPov(int Value, UINT rID, UCHAR nPov)	// Write Value to a given descrete POV defined in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::SetDiscPov(Value, rID, nPov);

	if (Range_vXbox(rID) && (nPov==1))
	{
		switch (Value)
		{
			case DPOV_North:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_UP));

			case DPOV_East:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_RIGHT));

			case DPOV_South:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_DOWN));

			case DPOV_West:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_LEFT));

			case DPOV_NorthEast:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_UP_RIGHT));

			case DPOV_SouthEast:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_DOWN_RIGHT));

			case DPOV_SouthWest:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_DOWN_LEFT));

			case DPOV_NorthWest:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_UP_LEFT));

			case DPOV_Center:
			default:
				return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_NONE));
		}
	}

	return FALSE;
}

// vJoy and vXbox ONLY
VGENINTERFACE_API BOOL SetContPov(DWORD Value, UINT rID, UCHAR nPov)	// Write Value to a given continuous POV defined in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::SetContPov(Value, rID, nPov);

	if (Range_vXbox(rID) && nPov == 1)
	{
		if (Value == -1)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_NONE));

		if (static_cast<LONG>(Value) < 100 || static_cast<LONG>(Value) > 35900)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_UP));

		else if (abs(static_cast<LONG>(Value - 4500)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_UP_RIGHT));

		else if (abs(static_cast<LONG>(Value - 9000)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_RIGHT));

		else if (abs(static_cast<LONG>(Value - 13500)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_DOWN_RIGHT));

		else if (abs(static_cast<LONG>(Value - 18000)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_DOWN));

		else if (abs(static_cast<LONG>(Value - 22500)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_DOWN_LEFT));

		else if (abs(static_cast<LONG>(Value - 27000)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_LEFT));

		else if (abs(static_cast<LONG>(Value - 31500)) < 100)
			return SUCCEEDED(IX_SetDpad(to_vXbox(rID), XBTN_DPAD_UP_LEFT));

		else
			return FALSE;
	}

	return FALSE;

}

VGENINTERFACE_API BOOL ResetVJD(UINT rID)			// Reset all controls to predefined values in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::ResetVJD( rID);

	if (Range_vXbox(rID))
		return IX_ResetController(to_vXbox(rID));

	if (Range_vgeXbox(rID))
		return VGE_ResetController(DevType::vgeXbox, to_vgeXbox(rID));

	if (Range_vgeDS4(rID))
		return VGE_ResetController(DevType::vgeDS4, to_vgeDS4(rID));

	return FALSE;
}

// vJoy and vXbox ONLY
VGENINTERFACE_API VOID ResetAll(void) // Reset all controls to predefined values in all VDJ
{
	vJoyNS::ResetAll();
	IX_ResetAllControllers();
}

// vJoy and vXbox ONLY
VGENINTERFACE_API BOOL ResetButtons(UINT rID)		// Reset all buttons (To 0) in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::ResetButtons(rID);

	if (Range_vXbox(rID))
		return IX_ResetControllerBtns(to_vXbox(rID));

	return FALSE;

}

// vJoy and vXbox ONLY
VGENINTERFACE_API BOOL ResetPovs(UINT rID)		// Reset all POV Switches (To -1) in the specified VDJ
{
	if (Range_vJoy(rID))
		return vJoyNS::ResetPovs(rID);

	if (Range_vXbox(rID))
		return IX_ResetControllerDPad(to_vXbox(rID));

	return FALSE;

}

#pragma region FFB API
#pragma warning( push )
#pragma warning( disable : 4996 )
VGENINTERFACE_API FFBEType FfbGetEffect() { return  vJoyNS::FfbGetEffect(); }
VGENINTERFACE_API VOID FfbRegisterGenCB(FfbGenCB cb, PVOID data) { return  vJoyNS::FfbRegisterGenCB( cb,  data); }
//VGENINTERFACE_API BOOL 	FfbStart(UINT rID) { return  TRUE; }
//VGENINTERFACE_API VOID 	FfbStop(UINT rID) { return; }
VGENINTERFACE_API BOOL 	IsDeviceFfb(UINT rID) { return  vJoyNS::IsDeviceFfb(rID); }
VGENINTERFACE_API BOOL 	IsDeviceFfbEffect(UINT rID, UINT Effect) { return  vJoyNS::IsDeviceFfbEffect(rID, Effect); }
VGENINTERFACE_API DWORD Ffb_h_DeviceID(const FFB_DATA * Packet, int *DeviceID) { return  vJoyNS::Ffb_h_DeviceID(Packet, DeviceID); }
VGENINTERFACE_API DWORD Ffb_h_Type(const FFB_DATA * Packet, FFBPType *Type) { return  vJoyNS::Ffb_h_Type(Packet, Type); }
VGENINTERFACE_API DWORD Ffb_h_Packet(const FFB_DATA * Packet, WORD *Type, int *DataSize, BYTE *Data[]) { return  vJoyNS::Ffb_h_Packet(Packet, Type, DataSize, Data); }
VGENINTERFACE_API DWORD Ffb_h_EBI(const FFB_DATA * Packet, int *Index) { return  vJoyNS::Ffb_h_EBI(Packet, Index); }
VGENINTERFACE_API DWORD Ffb_h_Eff_Report(const FFB_DATA * Packet, FFB_EFF_REPORT*  Effect) { return  vJoyNS::Ffb_h_Eff_Report(Packet, Effect); }
//VGENINTERFACE_API DWORD Ffb_h_Eff_Const(const FFB_DATA * Packet, FFB_EFF_REPORT*  Effect) { return  vJoyNS::Ffb_h_Eff_Report(Packet, Effect); }
VGENINTERFACE_API DWORD Ffb_h_Eff_Ramp(const FFB_DATA * Packet, FFB_EFF_RAMP*  RampEffect) { return  vJoyNS::Ffb_h_Eff_Ramp(Packet,   RampEffect); }
VGENINTERFACE_API DWORD Ffb_h_EffOp(const FFB_DATA * Packet, FFB_EFF_OP*  Operation) { return  vJoyNS::Ffb_h_EffOp(Packet,  Operation); }
VGENINTERFACE_API DWORD Ffb_h_DevCtrl(const FFB_DATA * Packet, FFB_CTRL *  Control) { return  vJoyNS::Ffb_h_DevCtrl(Packet,  Control); }
VGENINTERFACE_API DWORD Ffb_h_Eff_Period(const FFB_DATA * Packet, FFB_EFF_PERIOD*  Effect) { return  vJoyNS::Ffb_h_Eff_Period(Packet,  Effect); }
VGENINTERFACE_API DWORD Ffb_h_Eff_Cond(const FFB_DATA * Packet, FFB_EFF_COND*  Condition) { return  vJoyNS::Ffb_h_Eff_Cond(Packet, Condition); }
VGENINTERFACE_API DWORD Ffb_h_DevGain(const FFB_DATA * Packet, BYTE * Gain) { return  vJoyNS::Ffb_h_DevGain(Packet, Gain); }
VGENINTERFACE_API DWORD Ffb_h_Eff_Envlp(const FFB_DATA * Packet, FFB_EFF_ENVLP*  Envelope) { return  vJoyNS::Ffb_h_Eff_Envlp(Packet, Envelope); }
VGENINTERFACE_API DWORD Ffb_h_EffNew(const FFB_DATA * Packet, FFBEType * Effect) { return  vJoyNS::Ffb_h_EffNew(Packet, Effect); }
VGENINTERFACE_API DWORD Ffb_h_Eff_Constant(const FFB_DATA * Packet, FFB_EFF_CONSTANT *  ConstantEffect) { return  vJoyNS::Ffb_h_Eff_Constant(Packet, ConstantEffect); }
#pragma warning( pop )
#pragma endregion  FFB API

#pragma endregion Interface Functions (vJoy)

#pragma region Interface Functions (vXbox)

VGENINTERFACE_API DWORD isVBusExist(void)
{
	return IX_isVBusExists();
}

// Get vXBox Bus version or zero if bus is not installed.
DWORD	GetVBusVersion(void)
{
	DWORD Version;
	if (SUCCEEDED(XOutputGetBusVersion(&Version)))
		return Version;
	return 0;
}

VGENINTERFACE_API DWORD GetNumEmptyBusSlots(UCHAR * nSlots)
{
	return IX_GetNumEmptyBusSlots(nSlots);
}

VGENINTERFACE_API DWORD isControllerPluggedIn(UINT UserIndex, PBOOL Exist)
{
	return IX_isControllerPluggedIn(UserIndex, Exist);
}

VGENINTERFACE_API DWORD isControllerOwned(UINT UserIndex, PBOOL Owned)
{
	return IX_isControllerOwned(UserIndex, Owned);
}

VGENINTERFACE_API DWORD GetVXAxisRange(UINT UserIndex, HID_USAGES Axis, LONG * Min, LONG * Max)
{
	switch (Axis) {
		case HID_USAGE_X:
		case HID_USAGE_Y:
		case HID_USAGE_RX:
		case HID_USAGE_RY:
			*Max = 32767;
			*Min = -32767;
			return STATUS_SUCCESS;
		case HID_USAGE_Z:
		case HID_USAGE_RZ:
			*Max = 255;
			*Min = 0;
			return STATUS_SUCCESS;
		default:
			return STATUS_UNSUCCESSFUL;
	}
}

VGENINTERFACE_API DWORD PlugIn(UINT UserIndex)
{
	return IX_PlugIn( UserIndex);
}

VGENINTERFACE_API DWORD PlugInNext(UINT * UserIndex)
{
	return IX_PlugInNext( UserIndex);
}

VGENINTERFACE_API DWORD UnPlug(UINT UserIndex)
{
	return IX_UnPlug(UserIndex);
}

VGENINTERFACE_API DWORD UnPlugForce(UINT UserIndex)
{
	return IX_UnPlugForce(UserIndex);
}

// Reset Devices
VGENINTERFACE_API DWORD ResetController(UINT UserIndex)
{
	return IX_ResetController(UserIndex);
}

VGENINTERFACE_API DWORD ResetAllControllers()
{
	return IX_ResetAllControllers();
}

#ifdef SPECIFICRESET

VGENINTERFACE_API DWORD ResetControllerBtns(UINT UserIndex)
{
	return IX_ResetControllerBtns(UserIndex);
}

VGENINTERFACE_API DWORD ResetControllerDPad(UINT UserIndex)
{
	return IX_ResetControllerDPad(UserIndex);
}

#endif // SPECIFICRESET


VGENINTERFACE_API DWORD SetButton(UINT UserIndex, WORD Button, BOOL Press)
{
	return IX_SetBtn(UserIndex, Press,  Button, TRUE);
}

#ifdef SPECIFICBUTTONS
VGENINTERFACE_API BOOL SetBtnA(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnA(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnB(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnB(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnX(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnX(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnY(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnY(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnStart(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnStart(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnBack(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnBack(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnLT(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnLT(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnRT(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnRT(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnLB(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnLB(UserIndex, Press);
}

VGENINTERFACE_API BOOL SetBtnRB(UINT UserIndex, BOOL Press)
{
	return IX_SetBtnRB(UserIndex, Press);
}
#endif // SPECIFICBUTTONS

VGENINTERFACE_API DWORD SetGamepadAxis(UINT UserIndex, HID_USAGES Axis, SHORT Value)
{
	return IX_SetAxis(UserIndex, Axis, Value);
}

#ifdef SPECIFICBUTTONS
VGENINTERFACE_API DWORD SetTriggerR(UINT UserIndex, BYTE Value) // Right Trigger
{
	return IX_SetTriggerR(UserIndex, Value);
}

VGENINTERFACE_API DWORD SetTriggerL(UINT UserIndex, BYTE Value) // Left Trigger
{
	return IX_SetTriggerL(UserIndex, Value);
}

VGENINTERFACE_API DWORD SetAxisLx(UINT UserIndex, SHORT Value) // Left Stick X
{
	return IX_SetAxisLx(UserIndex, Value);
}

VGENINTERFACE_API DWORD SetAxisLy(UINT UserIndex, SHORT Value) // Left Stick Y
{
	return IX_SetAxisLy(UserIndex, Value);
}

VGENINTERFACE_API DWORD SetAxisRx(UINT UserIndex, SHORT Value) // Right Stick X
{
	return IX_SetAxisRx(UserIndex, Value);
}

VGENINTERFACE_API DWORD SetAxisRy(UINT UserIndex, SHORT Value) // Right Stick Y
{
	return IX_SetAxisRy(UserIndex, Value);
}
#endif // SPECIFICBUTTONS

VGENINTERFACE_API DWORD SetDpad(UINT UserIndex, UCHAR Value) // DPAD Set Value
{
	return IX_SetDpad(UserIndex, Value);
}

#ifdef SPECIFICBUTTONS
VGENINTERFACE_API BOOL SetDpadUp(UINT UserIndex) // DPAD Up
{
	return IX_SetDpadUp(UserIndex);
}

VGENINTERFACE_API BOOL SetDpadRight(UINT UserIndex) // DPAD Right
{
	return IX_SetDpadRight(UserIndex);
}

VGENINTERFACE_API BOOL SetDpadDown(UINT UserIndex) // DPAD Down
{
	return IX_SetDpadDown(UserIndex);
}

VGENINTERFACE_API BOOL SetDpadLeft(UINT UserIndex) // DPAD Left
{
	return IX_SetDpadLeft(UserIndex);
}

VGENINTERFACE_API BOOL SetDpadOff(UINT UserIndex) // DPAD Off
{
	return IX_SetDpadOff(UserIndex);
}
#endif // SPECIFICBUTTONS

VGENINTERFACE_API DWORD GetLedNumber(UINT UserIndex, PBYTE pLed)
{
	return IX_GetLedNumber(UserIndex, pLed);
}

VGENINTERFACE_API DWORD GetVibration(UINT UserIndex, PXINPUT_VIBRATION pVib)
{
	return IX_GetVibration(UserIndex, pVib);
}

#pragma endregion Interface Functions (vXbox)

#pragma region Interface Functions (Common)

VGENINTERFACE_API void DeInit(void)
{
	if (g_isShuttingDown)
		return;
	g_isShuttingDown = true;

	std::vector<HDEVICE> devs;
	devs.reserve(DevContainer.size());
	for (auto const &dev : DevContainer_cref)
		devs.push_back(dev.first);
	for (HDEVICE hDev : const_cast<const std::vector<HDEVICE> &>(devs)) {
		if (RelinquishDev(hDev) != STATUS_SUCCESS)
			DestroyDevice(hDev);
	}

	if (VGE_Client) {
		vigem_disconnect(VGE_Client);
		vigem_free(VGE_Client);
		VGE_Client = nullptr;
	}

	g_isShuttingDown = false;
}

VGENINTERFACE_API DWORD AcquireDev(UINT DevId, DevType dType, HDEVICE * hDev)
{
	*hDev = INVALID_DEV;
	if (dType == DevType::vJoy)
	{
		*hDev = IJ_AcquireVJD(DevId);
		return BOOL_TO_STATUS(*hDev != INVALID_DEV);
	};

	DWORD res;
	if (dType == DevType::vXbox)
		res = IX_PlugIn(DevId);
	else if (dType == DevType::vgeXbox || dType == DevType::vgeDS4)
		res = VGE_PlugIn(dType, DevId);
	else
		res = STATUS_INVALID_PARAMETER_2;

	if (res == STATUS_SUCCESS)
		return GetDevHandle(DevId, dType, hDev);
	return res;

	return STATUS_INVALID_PARAMETER_2;
}

VGENINTERFACE_API DWORD RelinquishDev(HDEVICE hDev)
{
	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	switch (pDev->Type) {
		case DevType::vJoy:
			return IJ_RelinquishVJD(hDev, pDev);

		case DevType::vXbox:
			return IX_UnPlug(GetDeviceId(hDev));

		case DevType::vgeXbox:
		case DevType::vgeDS4:
			return VGE_UnPlug(hDev, pDev, g_isShuttingDown);

		default:
			return STATUS_INVALID_HANDLE;
	}
}

VGENINTERFACE_API VjdStat GetDevStatus(HDEVICE hDev)
{
	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return VJD_STAT_MISS;

	switch (pDev->Type) {
		case DevType::vJoy:
			return vJoyNS::GetVJDStatus(pDev->Id);

		case DevType::vXbox: {
			BOOL Exist, Owned;
			if SUCCEEDED(IX_isControllerOwned(pDev->Id, &Owned)) {
				if (Owned)
					return VJD_STAT_OWN;
			}

			if SUCCEEDED(IX_isControllerPluggedIn(pDev->Id, &Exist)) {
				if (Exist)
					return VJD_STAT_BUSY;
			}
			return VJD_STAT_FREE;
		}

		case DevType::vgeXbox:
		case DevType::vgeDS4: {
			if (VGE_BusExists() != STATUS_SUCCESS)
				return VJD_STAT_MISS;

			if (pDev->VGE_Target && vigem_target_is_attached(pDev->VGE_Target))
				return VJD_STAT_OWN;

			return VJD_STAT_FREE;
		}

		default:
			return VJD_STAT_MISS;
	}
}

VGENINTERFACE_API VjdStat GetDevTypeStatus(vGenNS::DevType dType, UINT DevId)
{
	switch (dType) {
		case DevType::vJoy:
			return vJoyNS::GetVJDStatus(DevId);

		case DevType::vXbox: {
			BOOL Exist, Owned;
			if SUCCEEDED(IX_isControllerOwned(DevId, &Owned)) {
				if (Owned)
					return VJD_STAT_OWN;
			}

			if SUCCEEDED(IX_isControllerPluggedIn(DevId, &Exist)) {
				if (Exist)
					return VJD_STAT_BUSY;
			}
			return VJD_STAT_FREE;
		}

		case DevType::vgeXbox:
		case DevType::vgeDS4: {
			if (VGE_BusExists() != STATUS_SUCCESS)
				return VJD_STAT_MISS;

			PDEVICE pDev = GetDevice(dType, DevId);
			if (pDev && pDev->VGE_Target && vigem_target_is_attached(pDev->VGE_Target))
				return VJD_STAT_OWN;

			return VJD_STAT_FREE;
		}

		default:
			return VJD_STAT_MISS;
	}
}

VGENINTERFACE_API DWORD GetDevType(HDEVICE hDev, DevType * dType)
{
	if (!dType)
		return STATUS_INVALID_PARAMETER_2;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	*dType = pDev->Type;
	BOOL Owned;
	if SUCCEEDED(isDevOwned(pDev->Id, *dType, &Owned)) {
		if (!Owned)
			return STATUS_DEVICE_REMOVED;
	}

	return STATUS_SUCCESS;
}

// If vJoy: Number=Id; If vXbox: Number=Led#
VGENINTERFACE_API DWORD GetDevNumber(HDEVICE hDev, UINT * dNumber)
{
	if (!dNumber)
		return STATUS_INVALID_PARAMETER_2;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	// If not owned - return
	BOOL Owned;
	if (SUCCEEDED(isDevOwned(pDev->Id, pDev->Type, &Owned)) && !Owned)
		return STATUS_DEVICE_REMOVED;

	if (pDev->Type == DevType::vJoy) {
		*dNumber = pDev->Id;
		return STATUS_SUCCESS;
	}

	if (pDev->Type == DevType::vXbox)
	{
		BYTE Led = 0;
		DWORD res = IX_GetLedNumber(pDev->Id, &Led);
		if (res == STATUS_SUCCESS)
			*dNumber = Led;
		return res;
	}

	if (pDev->Type == DevType::vgeXbox) {
		*dNumber = pDev->DevInfo.LedNumber;
		return STATUS_SUCCESS;
	}

	if (pDev->Type == DevType::vgeDS4) {
		*dNumber = pDev->DevInfo.Serial;
		return STATUS_SUCCESS;
	}

	*dNumber = 0;
	return STATUS_NOT_SUPPORTED;
}

VGENINTERFACE_API DWORD GetDevId(HDEVICE hDev, UINT * dID)	// Return Device ID to be used with vXbox API and Backward compatibility API
{
	DWORD res;
	DevType dType;

	if (!dID)
		return STATUS_INVALID_PARAMETER_2;

	if (!ValidDev(hDev))
		return STATUS_INVALID_HANDLE;

	*dID = GetDeviceId(hDev);
	res = GetDevType(hDev, &dType);
	if FAILED(res)
		return res;

	// If not owned - return
	BOOL Owned;
	if SUCCEEDED(isDevOwned(*dID, dType, &Owned))
	{
		if (!Owned)
			return STATUS_DEVICE_REMOVED;
	}

	if (!dID)
		return STATUS_INVALID_HANDLE;
	else
		return STATUS_SUCCESS;
}

VGENINTERFACE_API DWORD GetDevHandle(UINT DevId, DevType dType, HDEVICE * hDev) // Return device handle from Device ID and Device type
{
	// If not owned - return
	BOOL Owned;

	if (!hDev)
		return STATUS_INVALID_PARAMETER_3;

	// Get handle from container
	*hDev = GetDeviceHandle(dType, DevId);

	// If handle is valid check that device still owned
	if ValidDev(*hDev)
	{
		if SUCCEEDED(isDevOwned(DevId, dType, &Owned))
		{
			if (Owned)
				return STATUS_SUCCESS; // Owned
		}

		// Handle is OK but device was removed so we remove the entry from the container
		DestroyDevice(*hDev);
		return STATUS_DEVICE_REMOVED;
	}
	else
		return STATUS_UNSUCCESSFUL;
}

VGENINTERFACE_API DWORD isDevOwned(UINT DevId, DevType dType, BOOL * Owned)
{
	if (!Owned)
		return STATUS_INVALID_PARAMETER_3;

	if (dType == DevType::vJoy) {
		*Owned = (vJoyNS::GetVJDStatus(DevId) == VJD_STAT_OWN);
		return STATUS_SUCCESS;
	}

	if (dType == DevType::vXbox)
		return IX_isControllerOwned(DevId, Owned);

	if (dType == DevType::vgeXbox || dType == DevType::vgeDS4) {
		PDEVICE pDev = GetDevice(dType, DevId);
		if (!pDev)
			return STATUS_INVALID_HANDLE;
		*Owned = (pDev->VGE_Target && vigem_target_is_attached(pDev->VGE_Target));
		return STATUS_SUCCESS;
	}

	return STATUS_UNSUCCESSFUL;
}

VGENINTERFACE_API DWORD isDevExist(UINT DevId, DevType dType, BOOL * Exist)
{
	DWORD res;

	if (!Exist)
		return STATUS_INVALID_PARAMETER_3;

	if (dType == DevType::vJoy)
	{
		VjdStat stat = vJoyNS::GetVJDStatus(DevId);
		*Exist = (stat == VJD_STAT_OWN || stat == VJD_STAT_BUSY || stat == VJD_STAT_FREE);
		return STATUS_SUCCESS;
	};

	if (!DevId || DevId > 4) {
		*Exist = FALSE;
		return STATUS_SUCCESS;
	}

	if (dType == DevType::vXbox)
	{
		res = IX_isControllerPluggedIn(DevId, Exist);
		return res;
	}

	// ViGEm doesn't have a way to check an arbitrary device unless we own it.
	return isDevOwned(DevId, dType, Exist);
}

VGENINTERFACE_API DWORD isDevFree(UINT DevId, DevType dType, BOOL * Free)
{
	if (!Free)
		return STATUS_INVALID_PARAMETER_3;

	if (dType == DevType::vJoy)
	{
		VjdStat stat = vJoyNS::GetVJDStatus(DevId);
		if ((stat == VJD_STAT_FREE))
			*Free = TRUE;
		else
			*Free = FALSE;
		return STATUS_SUCCESS;
	};

	if (!DevId || DevId > 4) {
		*Free = FALSE;
		return STATUS_SUCCESS;
	}

	BOOL Exist = FALSE;
	DWORD res = isDevOwned(DevId, dType, &Exist);
	*Free = !Exist;
	return res;
}

// Cannot implement isDevOwned(h) because only an OWNED device has a handle
// BUSY device is is owned by another feeder so it does not have a handle
#if 0
VGENINTERFACE_API BOOL isDevOwned(HDEVICE hDev)
{
	if (isDevice_vJoy(hDev))
	{
		VjdStat stat = IJ_GetVJDStatus(hDev);
		if (stat == VJD_STAT_OWN)
			return TRUE;
		else
			return FALSE;
	}

	if (isDevice_vXbox(hDev))
		return IX_isControllerOwned(hDev);

	return FALSE;
}
#endif // 0

VGENINTERFACE_API DWORD isAxisExist(HDEVICE hDev, HID_USAGES Axis, BOOL * Exist)	// Does Axis exist.
{

	if (!Exist)
		return STATUS_INVALID_PARAMETER_3;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	BOOL Owned;
	const DWORD res = isDevOwned(pDev->Id, pDev->Type, &Owned);
	if FAILED(res)
		return res;
	if (!Owned)
		return STATUS_DEVICE_REMOVED;

	if (pDev->Type == DevType::vJoy)
		*Exist = IJ_GetVJDAxisExist(hDev, Axis);
	else
		*Exist = (Axis >= HID_USAGE_LX && Axis <= HID_USAGE_RT) || Axis == HID_USAGE_POV;

	return STATUS_SUCCESS;
}

// Get logical Minimum and Maximum values for a given axis defined in the specified VJD.
VGENINTERFACE_API DWORD GetDevAxisRange(HDEVICE hDev, vGenNS::HID_USAGES Axis, LONG * Min, LONG * Max)
{
	if (!Min || !Max)
		return STATUS_INVALID_PARAMETER_3;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	if (pDev->Type == DevType::vJoy) {
		if (!vJoyNS::GetVJDAxisMin(pDev->Id, Axis, Min) || !vJoyNS::GetVJDAxisMax(pDev->Id, Axis, Max))
			return STATUS_UNSUCCESSFUL;
	}
	else {
		*Min = 0;
		*Max = Axis == HID_USAGE_POV ? 35900 : 32767;
	}

	return STATUS_SUCCESS;
}

VGENINTERFACE_API DWORD GetDevButtonN(HDEVICE hDev, USHORT * nBtn)			// Get number of buttons in device
{
	if (!nBtn)
		return STATUS_INVALID_PARAMETER_2;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	switch (pDev->Type) {
		case DevType::vJoy:
			*nBtn = vJoyNS::GetVJDButtonNumber(pDev->Id);
			break;

		case DevType::vXbox:
		case DevType::vgeXbox:
			*nBtn = XINPUT_NUM_BUTTONS;
			break;

		case DevType::vgeDS4:
			*nBtn = DS4_NUM_BUTTONS;
			break;

		default:
			*nBtn = 0;
			return STATUS_INVALID_DEVICE_REQUEST;
	}
	return STATUS_SUCCESS;
}

VGENINTERFACE_API DWORD GetDevHatN(HDEVICE hDev, PovType povType, USHORT * nHat)				// Get number of Hat Switches in device
{
	if (!nHat)
		return STATUS_INVALID_PARAMETER_2;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	*nHat = 0;
	switch (pDev->Type) {
		case DevType::vJoy:
			if (povType & PovType::PovTypeDiscrete)
				*nHat += vJoyNS::GetVJDDiscPovNumber(pDev->Id);
			if (povType & PovType::PovTypeContinuous)
				*nHat += vJoyNS::GetVJDContPovNumber(pDev->Id);
			break;
		case DevType::vXbox:
		case DevType::vgeXbox:
		case DevType::vgeDS4:
			*nHat = 1;
			break;

		default:
			return STATUS_INVALID_DEVICE_REQUEST;
	}
	return STATUS_SUCCESS;
}

/*
Get current position report
*/
VGENINTERFACE_API DWORD	GetPosition(HDEVICE hDev, PVOID pData)
{
	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return ERROR_INVALID_HANDLE;

	//if (pDev->Type == DevType::vJoy)
	//	return vJoyNS::GetPosition(pDev->Id, pData);

	PVOID position = GetDevicePos(pDev);
	if (!position)
		return ERROR_DEVICE_NOT_AVAILABLE;

	if (pDev->Type == DevType::vJoy)
		memcpy(pData, position, sizeof(JOYSTICK_POSITION_V2));
	else if (pDev->Type == DevType::vgeDS4)
		memcpy(pData, position, sizeof(DS4_REPORT));
	else
		memcpy(pData, position, sizeof(XINPUT_GAMEPAD));
	return ERROR_SUCCESS;

}

VGENINTERFACE_API DWORD GetDevInfo(HDEVICE hDev, vGenNS::DeviceInfo * DevInfo)
{
	if (!DevInfo)
		return STATUS_INVALID_PARAMETER_2;

	PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return ERROR_INVALID_HANDLE;

	memcpy(DevInfo, &pDev->DevInfo, sizeof(DeviceInfo));
	return STATUS_SUCCESS;
}

VGENINTERFACE_API BOOL IsDevTypeSupported(vGenNS::DevType dType)
{
	switch (dType) {
		case DevType::vJoy:
			return vJoyEnabled();
		case DevType::vXbox:
			return isVBusExist() == STATUS_SUCCESS;
		case DevType::vgeXbox:
		case DevType::vgeDS4:
			return VGE_BusExists() == STATUS_SUCCESS;
		default:
			return 0;
	}
}

VGENINTERFACE_API DWORD GetDriverVersion(vGenNS::DevType dType)
{
	switch (dType) {
		case DevType::vJoy:
			return (DWORD)GetvJoyVersion();
		case DevType::vXbox:
			return GetVBusVersion();
		case DevType::vgeXbox:
		case DevType::vgeDS4:
			return VGE_Version();
		default:
			return 0;
	}
}

// Read current positions XInput device by LED number  (helper function)
VGENINTERFACE_API DWORD GetXInputState(UINT ledN, PXINPUT_STATE pData)
{
	return XInputGetState(ledN, pData);
}


VGENINTERFACE_API DWORD SetDevButton(HDEVICE hDev, UINT Button, BOOL Press)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	switch (pDev->Type) {
		case DevType::vJoy:
			return BOOL_TO_STATUS(vJoyNS::SetBtn(Press, pDev->Id, Button));
		case DevType::vXbox:
			return IX_SetBtn(pDev, Press, Button);
		case DevType::vgeXbox:
		case DevType::vgeDS4:
			return VGE_SetBtn(pDev, Press, Button);
		default:
			return STATUS_INVALID_HANDLE;
	}
}

VGENINTERFACE_API DWORD SetDevAxis(HDEVICE hDev, HID_USAGES Axis, LONG Value)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	if (pDev->Type == DevType::vJoy)
		return BOOL_TO_STATUS(vJoyNS::SetAxis(Value, pDev->Id, Axis));

	if (Value > 32767)
		Value = 32767;
	else if (Value < 0)
		Value = 0;

	if (pDev->Type == DevType::vXbox || pDev->Type == DevType::vgeXbox)
	{
		// If Triggers (Z,RZ) then remap range:   0 - 32767  ==> 0 - 255
		// If Axis is X,Y,RX,RY then remap range: 0 - 32767  ==> -32768 - 32767
		SHORT vx_Value = static_cast<SHORT>( Axis == HID_USAGE_LT || Axis == HID_USAGE_RT ? ((Value - 1) / 128) & 0xFF : (Value - 16384) * 2 );

		if (pDev->Type == DevType::vXbox)
			return IX_SetAxis(pDev, Axis, vx_Value);
		else
			return VGE_SetAxis(pDev, Axis, vx_Value);
	}

	if (pDev->Type == DevType::vgeDS4) {
		// Scale all axes to byte range: 0 - 32767  ==> 0 - 255
		BYTE vx_Value = ((Value - 1) / 128) & 0xFF;
		if (Axis == HID_USAGE_LY || Axis == HID_USAGE_RY)
			vx_Value = (0xFF - vx_Value);  // reverse the value
		return VGE_SetAxis(pDev, Axis, vx_Value);
	}

	return STATUS_INVALID_HANDLE;
}

VGENINTERFACE_API DWORD SetDevAxisPct(HDEVICE hDev, HID_USAGES Axis, FLOAT Value)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	if (pDev->Type == DevType::vJoy)
	{
		// Convert Value from range 0-100 to range 0-32768
		const LONG vj_Value = static_cast <LONG>(32768 * Value * .01f);
		return BOOL_TO_STATUS(vJoyNS::SetAxis(vj_Value, pDev->Id, Axis));
	}

	if (pDev->Type == DevType::vXbox || pDev->Type == DevType::vgeXbox)
	{
		// Convert Value from range (0 - 100) to range (0 - 255) for Triggers
		{
			if (Axis == HID_USAGE_LT || Axis == HID_USAGE_RT) {
				const BYTE bVal = (BYTE)(255 * Value * .01f);
				if (pDev->Type == DevType::vXbox)
					return IX_SetAxis(pDev, Axis, bVal);
				else
					return VGE_SetAxis(pDev, Axis, bVal);
			}
		}

		const BYTE bVal = (BYTE)(255 * Value * .01f);
		const SHORT sVal = static_cast <SHORT>((65535.0f * Value * .01f) - 32768);
		if (pDev->Type == DevType::vXbox)
			return IX_SetAxis(pDev, Axis, sVal);
		else
			return VGE_SetAxis(pDev, Axis, sVal);
	}

	if (pDev->Type == DevType::vgeDS4) {
		// Scale all axes to byte range: 0 - 32767  ==> 0 - 255
		BYTE bVal = (BYTE)(255 * Value * .01f);
		if (Axis == HID_USAGE_LY || Axis == HID_USAGE_RY)
			bVal = (0xFF - bVal);  // reverse the value
		return VGE_SetAxis(pDev, Axis, bVal);
	}

	return STATUS_INVALID_HANDLE;
}

static BYTE DPOV_to_DPAD(vGenNS::DPOV_DIRECTION Value, bool ds4 = false)
{
	switch (Value)
	{
		case DPOV_North:
			return ds4 ? DS4_BUTTON_DPAD_NORTH : XBTN_DPAD_UP;
		case DPOV_East:
			return ds4 ? DS4_BUTTON_DPAD_EAST : XBTN_DPAD_RIGHT;
		case DPOV_South:
			return ds4 ? DS4_BUTTON_DPAD_SOUTH : XBTN_DPAD_DOWN;
		case DPOV_West:
			return ds4 ? DS4_BUTTON_DPAD_WEST : XBTN_DPAD_LEFT;
		case DPOV_NorthEast:
			return ds4 ? DS4_BUTTON_DPAD_NORTHEAST : XBTN_DPAD_UP_RIGHT;
		case DPOV_SouthEast:
			return ds4 ? DS4_BUTTON_DPAD_SOUTHEAST : XBTN_DPAD_DOWN_RIGHT;
		case DPOV_SouthWest:
			return ds4 ? DS4_BUTTON_DPAD_SOUTHWEST : XBTN_DPAD_DOWN_LEFT;
		case DPOV_NorthWest:
			return ds4 ? DS4_BUTTON_DPAD_NORTHWEST : XBTN_DPAD_UP_LEFT;
		case DPOV_Center:
		default:
			return ds4 ? DS4_BUTTON_DPAD_NONE : XBTN_NONE;
	}
}

// Write Value to a given discrete POV defined in the specified device handle
VGENINTERFACE_API DWORD SetDevDiscPov(HDEVICE hDev, UCHAR nPov, vGenNS::DPOV_DIRECTION Value)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	if (pDev->Type == DevType::vJoy)
		return BOOL_TO_STATUS(vJoyNS::SetDiscPov((int)Value, pDev->Id, nPov));

	if (nPov > 1)
		return STATUS_INVALID_PARAMETER_2;

	if (pDev->Type == DevType::vXbox)
		return IX_SetDpad(pDev, DPOV_to_DPAD(Value));

	if (pDev->Type == DevType::vgeXbox)
		return VGE_SetDpad(pDev, DPOV_to_DPAD(Value));

	if (pDev->Type == DevType::vgeDS4)
		return VGE_SetDpad(pDev, DPOV_to_DPAD(Value, true));

	return STATUS_INVALID_HANDLE;
}

static BYTE CPOV_to_DPAD(DWORD Value, bool ds4 = false)
{
	if (Value == -1)
		return ds4 ? DS4_BUTTON_DPAD_NONE : XBTN_NONE;

	const LONG lVal = static_cast<LONG>(Value);
	if (lVal < 100 || lVal > 35900)
		return ds4 ? DS4_BUTTON_DPAD_NORTH : XBTN_DPAD_UP;

	if (abs(lVal - 4500) < 100)
		return ds4 ? DS4_BUTTON_DPAD_NORTHEAST : XBTN_DPAD_UP_RIGHT;

	if (abs(lVal - 9000) < 100)
		return ds4 ? DS4_BUTTON_DPAD_EAST : XBTN_DPAD_RIGHT;

	if (abs(lVal - 13500) < 100)
		return ds4 ? DS4_BUTTON_DPAD_SOUTHEAST : XBTN_DPAD_DOWN_RIGHT;

	if (abs(lVal - 18000) < 100)
		return ds4 ? DS4_BUTTON_DPAD_SOUTH : XBTN_DPAD_DOWN;

	if (abs(lVal - 22500) < 100)
		return ds4 ? DS4_BUTTON_DPAD_SOUTHWEST : XBTN_DPAD_DOWN_LEFT;

	if (abs(lVal - 27000) < 100)
		return ds4 ? DS4_BUTTON_DPAD_WEST : XBTN_DPAD_LEFT;

	if (abs(lVal - 31500) < 100)
		return ds4 ? DS4_BUTTON_DPAD_NORTHWEST : XBTN_DPAD_UP_LEFT;

	return ds4 ? DS4_BUTTON_DPAD_NONE : XBTN_NONE;
}

// Write Value to a given continuous POV defined in the specified device handle
VGENINTERFACE_API DWORD SetDevContPov(HDEVICE hDev, UCHAR nPov, DWORD Value)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	if (pDev->Type == DevType::vJoy)
		return BOOL_TO_STATUS(vJoyNS::SetContPov(Value, pDev->Id, nPov));

	if (nPov > 1)
		return STATUS_INVALID_PARAMETER_2;

	if (pDev->Type == DevType::vXbox)
		return IX_SetDpad(pDev, CPOV_to_DPAD(Value));

	if (pDev->Type == DevType::vgeXbox)
		return VGE_SetDpad(pDev, CPOV_to_DPAD(Value));

	if (pDev->Type == DevType::vgeDS4)
		return VGE_SetDpad(pDev, CPOV_to_DPAD(Value, true));

	return STATUS_INVALID_HANDLE;
}

static BYTE Degrees_to_DPAD(LONG Value, bool ds4 = false)
{
	switch (Value)
	{
		case 0:
		case 360:
			return ds4 ? DS4_BUTTON_DPAD_NORTH : XBTN_DPAD_UP;
		case 45:
			return ds4 ? DS4_BUTTON_DPAD_NORTHEAST : XBTN_DPAD_UP_RIGHT;
		case 90:
			return ds4 ? DS4_BUTTON_DPAD_EAST : XBTN_DPAD_RIGHT;
		case 135:
			return ds4 ? DS4_BUTTON_DPAD_SOUTHEAST : XBTN_DPAD_DOWN_RIGHT;
		case 180:
			return ds4 ? DS4_BUTTON_DPAD_SOUTH : XBTN_DPAD_DOWN;
		case 225:
			return ds4 ? DS4_BUTTON_DPAD_SOUTHWEST : XBTN_DPAD_DOWN_LEFT;
		case 270:
			return ds4 ? DS4_BUTTON_DPAD_NORTHWEST : XBTN_DPAD_UP_LEFT;
		case 315:
			return ds4 ? DS4_BUTTON_DPAD_WEST : XBTN_DPAD_LEFT;
		default:
			return ds4 ? DS4_BUTTON_DPAD_NONE : XBTN_NONE;
	}
}

VGENINTERFACE_API DWORD SetDevPov(HDEVICE hDev, UCHAR nPov, DWORD Value)
{
	const PDEVICE pDev = GetDevice(hDev);
	if (!pDev)
		return STATUS_INVALID_HANDLE;

	if (pDev->Type == DevType::vJoy)
	{
		// Don't test for type - just try
		if (vJoyNS::SetContPov(Value, pDev->Id, nPov))
			return STATUS_SUCCESS;

		// Discrete: Convert Value from range 0-360 to discrete values (-1 means Reset)
		switch (Value)
		{
			case 0:
			case 36000:
				return BOOL_TO_STATUS(vJoyNS::SetDiscPov(DPOV_North, pDev->Id, nPov));
			case 9000:
				return BOOL_TO_STATUS(vJoyNS::SetDiscPov(DPOV_East, pDev->Id, nPov));
			case 18000:
				return BOOL_TO_STATUS(vJoyNS::SetDiscPov(DPOV_South, pDev->Id, nPov));
			case 27000:
				return BOOL_TO_STATUS(vJoyNS::SetDiscPov(DPOV_West, pDev->Id, nPov));
			default:
				return BOOL_TO_STATUS(vJoyNS::SetDiscPov(DPOV_Center, pDev->Id, nPov));
		}
	}

	if (nPov != 1)
		return STATUS_INVALID_PARAMETER_2;

	if (pDev->Type == DevType::vXbox)
		return IX_SetDpad(pDev, CPOV_to_DPAD(Value));

	if (pDev->Type == DevType::vgeXbox)
		return VGE_SetDpad(pDev, CPOV_to_DPAD(Value));

	if (pDev->Type == DevType::vgeDS4)
		return VGE_SetDpad(pDev, CPOV_to_DPAD(Value, true));

	return STATUS_INVALID_HANDLE;
}

VGENINTERFACE_API DWORD SetDevPovDeg(HDEVICE hDev, UCHAR nPov, FLOAT Value)
{
	return SetDevPov(hDev, nPov, (Value >= 0.0f ? static_cast <DWORD>(Value * 100) : -1));
}

VGENINTERFACE_API DWORD __cdecl ResetDevPositions(HDEVICE hDev)
{
	switch (GetDeviceType(hDev)) {
		case DevType::vJoy:
			return IJ_ResetPositions(hDev);
		case DevType::vXbox:
			return IX_ResetController(hDev);
		case DevType::vgeXbox:
		case DevType::vgeDS4:
			return VGE_ResetController(hDev);
		default:
			return STATUS_INVALID_HANDLE;
	}
}

#pragma endregion  Interface Functions (Common)

} //extern "C"
