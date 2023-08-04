/**********************************************************************
This file is part of the TJoy project.
Copyright Maxim Paperno; all rights reserved.
https://github.com/mpaperno/TJoy

This file may be used under the terms of the GNU
General Public License as published by the Free Software Foundation,
either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

A copy of the GNU General Public License is included with this project
and is available at <http://www.gnu.org/licenses/>.

This project may also use 3rd-party Open Source software under the terms
of their respective licenses. The copyright notice above does not apply
to any 3rd-party components used within.
************************************************************************/

using System.Collections.Generic;
using TJoy.Enums;
using TJoy.Constants;
using TJoy.Types;
using Math = System.Math;
using Stopwatch = System.Diagnostics.Stopwatch;
using System;
#if USE_VGEN || USE_VIGEM
using vJoy = vGenInterfaceWrap.vGen;
#else
using vJoy = vJoyInterfaceWrap.vJoy;
#endif

namespace TJoy.Utilities
{
  using JoystickState = vJoy.JoystickState;
#if USE_VGEN || USE_VIGEM
  using GamepadState = vJoy.GamepadState;
#endif
#if USE_VIGEM
  using DS4State = vJoy.DualShock4State;
#endif

  internal static class Util
  {

    internal static string StateIdStr(string id) => C.PLUGIN_ID + "." + C.IDSTR_EL_STATE + "." + id;
    internal static string ConnectorDictKey(uint devId, string actId, uint tgtId) => $"{devId}:{actId}:{tgtId}";
    internal static string ConnectorDictKey(in VJEvent ev) => ConnectorDictKey(ev.devId, ev.tpId, ev.targetId);
    //internal static string ConnectorDictKey(in ConnectorTrackingData td) => ConnectorDictKey(td.devId, td.id, td.targetId);
    internal static long TicksToSecs(long ticks) => ticks / Stopwatch.Frequency;
    //internal static long TicksToMs(long ticks) => ticks / (Stopwatch.Frequency / 1000L);
    internal static uint MaxDevices(DeviceType devType) => devType == DeviceType.VJoy ? 16U : 4U;

    // integer which represents version number in hex notation, eg. 1.22.3.0 => 0x1220300
    internal static uint GetProductVersionNumber()
    {
      var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
      return (uint)((byte)(vi.ProductMajorPart & 0xFF) << 24 | (byte)(vi.ProductMinorPart & 0xFF) << 16 | (byte)(vi.ProductBuildPart & 0xFF) << 8 | (byte)(vi.ProductPrivatePart & 0xFF));
    }

    internal static string GetProductVersionString()
    {
      var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
      return $"{vi.ProductMajorPart}.{vi.ProductMinorPart}.{vi.ProductBuildPart}.{vi.ProductPrivatePart}";
    }

    #region Lookups              //////////////////////////////////////////////////

    internal static DeviceType DeviceIdToType(uint deviceId)
    {
      return deviceId switch {
        > 0 and < 1000 => DeviceType.VJoy,
        > 1000 and < 2000 => DeviceType.VXBox,
        > 2000 and < 3000 => DeviceType.VBXBox,
        > 3000 and < 4000 => DeviceType.VBDS4,
        _ => DeviceType.None
      };
    }

    private readonly static string[] _deviceTypeNames = new [] { C.STR_DEVNAME_VJOY, C.STR_DEVNAME_VXBOX, C.STR_DEVNAME_VBXBOX, C.STR_DEVNAME_VBDS4 };
    internal static string DeviceTypeName(DeviceType devType)
    {
      if (devType > DeviceType.None && devType <= DeviceType.VBDS4)
        return _deviceTypeNames[(int)devType / 1000];
      return C.STR_NONE;
    }

    private readonly static string[] _gamepadAxisNames = new[] { C.STR_GPAXIS_LX, C.STR_GPAXIS_LY, C.STR_GPAXIS_LT, C.STR_GPAXIS_RX, C.STR_GPAXIS_RY, C.STR_GPAXIS_RT };
    internal static string AxisName(DeviceType devType, HID_USAGES axis)
    {
      if (devType == DeviceType.VJoy || ((uint)(axis - HID_USAGES.HID_USAGE_X) is var idx) && idx > 5)
        return axis.ToString().Split('_')[^1];
      return _gamepadAxisNames[idx];
    }

    private readonly static string[] _xboxButtonNames = new string[C.XO_MAX_BTNS + 1] {  C.STR_NONE,
      C.STR_XBOX_BTN1, C.STR_XBOX_BTN2, C.STR_XBOX_BTN3, C.STR_XBOX_BTN4,      // A, B, X, Y
      C.STR_XBOX_BTN5, C.STR_XBOX_BTN6,                                        // LB, RB
      C.STR_XBOX_BTN7, C.STR_XBOX_BTN8, C.STR_XBOX_BTN9,                       // Back, Start, Guide
      C.STR_XBOX_BTN10, C.STR_XBOX_BTN11,                                      // Thumb Left, Right
      C.STR_XBOX_BTN12, C.STR_XBOX_BTN13, C.STR_XBOX_BTN14, C.STR_XBOX_BTN15,  // DPAD N, E, S, W
      C.STR_XBOX_BTN16, C.STR_XBOX_BTN17, C.STR_XBOX_BTN18, C.STR_XBOX_BTN19,  // DPAD NE, SE, SW, NW
    };
    private readonly static string[] _ps4ButtonNames = new string[C.DS4_MAX_BTNS+1] {  C.STR_NONE,
      C.STR_PS4_BTN1, C.STR_PS4_BTN2, C.STR_PS4_BTN3, C.STR_PS4_BTN4,      // cross, circle, square, triangle
      C.STR_PS4_BTN5, C.STR_PS4_BTN6,                                      // L1, R1
      C.STR_PS4_BTN7, C.STR_PS4_BTN8, C.STR_PS4_BTN9,                      // Opt, Share, PS
      C.STR_PS4_BTN10, C.STR_PS4_BTN11,                                    // L3, R3
      C.STR_PS4_BTN12, C.STR_PS4_BTN13, C.STR_PS4_BTN14, C.STR_PS4_BTN15,  // DPAD N, E, S, W
      C.STR_PS4_BTN16, C.STR_PS4_BTN17, C.STR_PS4_BTN18, C.STR_PS4_BTN19,  // DPAD NE, SE, SW, NW
      C.STR_PS4_BTN20, C.STR_PS4_BTN21, C.STR_PS4_BTN22                    // L2, R2, TPad
    };

    internal static string ButtonName(DeviceType devType, uint targetId)
    {
      switch (devType) {
        case DeviceType.VJoy:
          return targetId.ToString("000");
        case DeviceType.VXBox:
        case DeviceType.VBXBox:
          if (targetId <= C.XO_MAX_BTNS)
            return _xboxButtonNames[targetId];
          return C.STR_NONE;
        case DeviceType.VBDS4:
          if (targetId <= C.DS4_MAX_BTNS)
            return _ps4ButtonNames[targetId];
          return C.STR_NONE;
        default:
          return C.STR_NONE;
      }
    }

    // parses a button "name" string into an index number. The name can be just a number.
    internal static uint ButtonIndex(DeviceType devType, string buttonName)
    {
      // try a numeric eval first, this is vjoy or the default collection of button numbers
      if (uint.TryParse(buttonName, out var btnNum))
        return btnNum;

      switch (devType) {
        case DeviceType.VXBox:
        case DeviceType.VBXBox:
          if (System.Array.IndexOf(_xboxButtonNames, buttonName) is int idx && idx > -1)
            return (uint)idx;
          return 0;
        case DeviceType.VBDS4:
          if (System.Array.IndexOf(_ps4ButtonNames, buttonName) is int ps4idx && ps4idx > -1)
            return (uint)ps4idx;
          return 0;
        default:
          return 0;
      }
    }

    internal static ControlType TpStateNameToEventType(string actId)
    {
      return actId switch {
        C.IDSTR_DEVTYPE_BTN  => ControlType.Button,
        C.IDSTR_DEVTYPE_AXIS => ControlType.Axis,
        C.IDSTR_DEVTYPE_DPOV => ControlType.DiscPov,
        C.IDSTR_DEVTYPE_CPOV => ControlType.ContPov,
        _ => ControlType.None,
      };
    }

    private readonly static string[] _controlTypeEventIds = new[] { C.STR_NONE, C.IDSTR_DEVTYPE_BTN, C.IDSTR_DEVTYPE_AXIS, C.IDSTR_DEVTYPE_DPOV, C.IDSTR_DEVTYPE_CPOV };
    internal static string EventTypeToTpStateId(ControlType type)
    {
      if (type >= ControlType.None && type <= ControlType.ContPov)
        return _controlTypeEventIds[(int)type];
      return string.Empty;
    }

    private readonly static string[] _controlTypeNames = new[] { C.STR_NONE, C.STR_BUTTON, C.STR_AXIS, C.STR_DISC_POV, C.STR_CONT_POV };
    internal static string EventTypeToControlName(ControlType type)
    {
      if (type >= ControlType.None && type <= ControlType.ContPov)
        return _controlTypeNames[(int)type];
      return C.STR_NONE;
    }

    #endregion  Lookups

    #region Device and Action helpers          /////////////////////////////////////////////////

    internal static bool TryParseDeviceType(string devStr, out DeviceType type, out uint id)
    {
      type = DeviceType.None;
      id = 0;
      if (string.IsNullOrWhiteSpace(devStr))
        return false;
      var devSpec = devStr.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
      if (devSpec.Length == 1)
        return uint.TryParse(devSpec[0], out id);
      if (devSpec.Length != 2)
        return false;

      type = devSpec[0] switch {
        C.STR_DEVNAME_VJOY   => DeviceType.VJoy,
        C.STR_DEVNAME_VXBOX  => DeviceType.VXBox,
        C.STR_DEVNAME_VBXBOX => DeviceType.VBXBox,
        C.STR_DEVNAME_VBDS4  => DeviceType.VBDS4,
        _ => DeviceType.None,
      };
      return type != DeviceType.None && uint.TryParse(devSpec[1], out id);
    }

    internal static void GetDefaultAxisRange(DeviceType devType, HID_USAGES axis, out int minValue, out int maxValue)
    {
      if (axis == HID_USAGES.HID_USAGE_POV) {
        minValue = C.VJ_CPOV_MIN_VALUE;
        maxValue = C.VJ_CPOV_MAX_VALUE;
        return;
      }
#if USE_VGEN
      // vGen always uses vJoy axis ranges
      minValue = C.VJ_AXIS_MIN_VALUE;
      maxValue = C.VJ_AXIS_MAX_VALUE;
#else
      if (devType == DeviceType.VJoy) {
        minValue = C.VJ_AXIS_MIN_VALUE;
        maxValue = C.VJ_AXIS_MAX_VALUE;
        return;
      }
      if (devType == DeviceType.VBDS4) {
        minValue = C.DS4_AXIS_MIN_VALUE;
        maxValue = C.DS4_AXIS_MAX_VALUE;
        return;
      }
      // XBox
      if (axis == HID_USAGES.HID_USAGE_RT || axis == HID_USAGES.HID_USAGE_LT) {
        minValue = C.XO_SLIDER_MIN_VALUE;
        maxValue = C.XO_SLIDER_MAX_VALUE;
      }
      else {
        minValue = C.XO_AXIS_MIN_VALUE;
        maxValue = C.XO_AXIS_MAX_VALUE;
      }
#endif  // USE_VGEN
    }

    internal static IReadOnlyCollection<VJAxisInfo> GetDefaultAxisInfo(DeviceType devType)
    {
      List<VJAxisInfo> ret = new();
      HID_USAGES lastId = 0;
      foreach (var axe in System.Enum.GetValues<HID_USAGES>()) {
        if (axe == lastId)
          continue;  // skip duplicates/aliases
        lastId = axe;
        VJAxisInfo ai;
        ai.usage = axe;
        GetDefaultAxisRange(devType, axe, out ai.minValue, out ai.maxValue);
        ret.Add(ai);
        if (devType != DeviceType.VJoy && ret.Count == 6)
          break;
      }
      return ret;
    }

    internal static uint GetDefaultButtonCount(DeviceType devType)
    {
      return devType switch {
        DeviceType.VJoy => 64,    // 128 really, but that's a lot!
        DeviceType.VXBox or DeviceType.VBXBox => (uint)C.XO_MAX_BTNS,
        DeviceType.VBDS4 => (uint)C.DS4_MAX_BTNS,
        _ => 0,
      };
    }

    internal static int GetMaxPovs(DeviceType devType)
    {
      return devType switch {
        DeviceType.VJoy => 4,
        _ => 1,
      };
    }

    internal static int GetDefaultValueForEventType(ControlType evtype)
    {
      return evtype switch {
        ControlType.Axis or ControlType.Button => 0,
        _ => -1  // POVs
      };
    }

    internal static int GetResetValueForType(CtrlResetMethod method, ControlType ev, int customValue = 0, int startValue = 0)
    {
      int range = 100; //ev == ControlType.Axis ? C.VJ_AXIS_MAX_VALUE : C.VJ_CPOV_MAX_VALUE;
      return method switch {
        CtrlResetMethod.Custom => customValue,
        CtrlResetMethod.Start => startValue,
        CtrlResetMethod.Center => ev == ControlType.Axis ? range / 2 : -1,
        CtrlResetMethod.Min or CtrlResetMethod.Button => 0,
        CtrlResetMethod.Max => range,
        CtrlResetMethod.OneQuarter => range / 4,
        CtrlResetMethod.ThreeQuarters => range / 4 * 3,
        CtrlResetMethod.North => 0,
        CtrlResetMethod.East => 1,
        CtrlResetMethod.South => 2,
        CtrlResetMethod.West => 3,
        _ => -2,
      };
    }

    #endregion Device and Action helpers

    #region Joystick State Report helpers          /////////////////////////////////////////////////


    // Returns -2 if no value (eg. POV axis)
    internal static int GetStateReportAxisValue(DeviceType devType, in VJDState state, HID_USAGES axis)
    {
      return devType switch {
        DeviceType.VJoy => GetVJoyStateReportAxisValue(state.vJoyState, axis),
#if USE_VGEN || USE_VIGEM
        DeviceType.VXBox or DeviceType.VBXBox => GetXInputStateAxisValue(state.xInputState.Gamepad, axis),
#endif
#if USE_VIGEM
        DeviceType.VBDS4 => GetDS4StateAxisValue(state.DS4State, axis),
#endif
        _ => 0
      };
    }

    // Returns -2 if no value
    internal static int GetStateReportCPovValue(DeviceType devType, in VJDState state, uint targetId)
    {
      return devType switch {
        DeviceType.VJoy => GetVJoyStateReportCPovValue(state.vJoyState, targetId),
#if USE_VGEN || USE_VIGEM
        DeviceType.VXBox or DeviceType.VBXBox => GetXInputStateCPovValue(state.xInputState.Gamepad, targetId),
#endif
#if USE_VIGEM
        DeviceType.VBDS4 => GetDS4StateCPovValue(state.DS4State, targetId),
#endif
        _ => 0
      };
    }

    // Returns -2 if no value (eg. invalid POV). Returns -1 when the POV is centered.
    internal static DPovDirection GetStateReportDPovValue(DeviceType devType, in VJDState state, uint targetId)
    {
      return devType switch {
        DeviceType.VJoy => GetVJoyStateReportDPovValue(state.vJoyState, targetId),
#if USE_VGEN || USE_VIGEM
        DeviceType.VXBox or DeviceType.VBXBox => GetXInputStateDpadValue(state.xInputState.Gamepad, targetId),
#endif
#if USE_VIGEM
        DeviceType.VBDS4 => GetDS4StateDpadValue(state.DS4State, targetId),
#endif
        _ => 0
      };
    }

    // Returns -2 if no value (eg. invalid POV). Returns -1 when the POV is centered.
    internal static int GetStateReportButtonValue(DeviceType devType, in VJDState state, uint targetId)
    {
      return devType switch {
        DeviceType.VJoy => GetVJoyStateReportButtonValue(state.vJoyState, targetId),
#if USE_VGEN || USE_VIGEM
        DeviceType.VXBox or DeviceType.VBXBox => GetXInputStateButtonValue(state.xInputState.Gamepad, targetId),
#endif
#if USE_VIGEM
        DeviceType.VBDS4 => GetDS4StateButtonValue(state.DS4State, targetId),
#endif
        _ => 0
      };
    }

    // vJoy

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding axis.
    // Returns -2 if no value (eg. POV axis)
    internal static int GetVJoyStateReportAxisValue(in JoystickState state, HID_USAGES axis)
    {
      return axis switch {
        HID_USAGES.HID_USAGE_X   => state.AxisX,
        HID_USAGES.HID_USAGE_Y   => state.AxisY,
        HID_USAGES.HID_USAGE_Z   => state.AxisZ,
        HID_USAGES.HID_USAGE_RX  => state.AxisXRot,
        HID_USAGES.HID_USAGE_RY  => state.AxisYRot,
        HID_USAGES.HID_USAGE_RZ  => state.AxisZRot,
        HID_USAGES.HID_USAGE_SL0 => state.Slider,
        HID_USAGES.HID_USAGE_SL1 => state.Dial,
        HID_USAGES.HID_USAGE_WHL => state.Wheel,
        HID_USAGES.HID_USAGE_POV => (int)state.bHats, // -2,  // not an actual axis in the struct
#if VJOY_API_2_2
        HID_USAGES.HID_USAGE_AILERON => state.Aileron,
        HID_USAGES.HID_USAGE_RUDDER => state.Rudder,
        HID_USAGES.HID_USAGE_THROTTLE => state.Throttle,
        HID_USAGES.HID_USAGE_ACCELERATOR => state.Accelerator,
        HID_USAGES.HID_USAGE_BRAKE => state.Brake,
        HID_USAGES.HID_USAGE_CLUTCH => state.Clutch,
        HID_USAGES.HID_USAGE_STEERING => state.Steering,
#endif
        _ => -2
      };
    }

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding C-POV number (1-indexed).
    // Returns -2 if no value (eg. invalid POV). Returns -1 when the POV is centered.
    internal static int GetVJoyStateReportCPovValue(in JoystickState state, uint targetId)
    {
      return targetId switch {
        1 => (int)state.bHats,
        2 => (int)state.bHatsEx1,
        3 => (int)state.bHatsEx2,
        4 => (int)state.bHatsEx3,
        _ => -2
      };
    }

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding D-POV number (1-indexed).
    // Returns -2 if no value (eg. invalid POV). Returns -1 when the POV is centered.
    internal static DPovDirection GetVJoyStateReportDPovValue(in JoystickState state, uint targetId)
    {
      if (targetId-- < 1)
        return DPovDirection.None;
      return (DPovDirection)((state.bHats & (0xF << (int)targetId)) >> (int)(4 * targetId));
    }

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding button number (1-indexed).
    // Returns -2 if no value (eg. invalid button).
    internal static int GetVJoyStateReportButtonValue(in JoystickState state, uint targetId)
    {
      if (targetId < 1)
        return -2;
      uint stateVal = 0;
      switch (targetId) {
        case <= 32:
          --targetId;
          stateVal = state.Buttons;
          break;
        case <= 64:
          targetId -= 33;
          stateVal = state.ButtonsEx1;
          break;
        case <= 96:
          targetId -= 65;
          stateVal = state.ButtonsEx2;
          break;
        case <= 128:
          targetId -= 97;
          stateVal = state.ButtonsEx3;
          break;
      }
      return (int)((stateVal & (1 << (int)targetId)) >> (int)(1 * targetId));
    }

    // XInput
#if USE_VGEN || USE_VIGEM
    internal static int GetXInputStateAxisValue(in GamepadState state, HID_USAGES axis)
    {
      // If Axis is X,Y,RX,RY then remap range:  -32768 - 32767 ==> 0 - 32767
      // If Triggers (Z,RZ) then remap range:           0 - 255 ==> 0 - 32767
      return axis switch {
        HID_USAGES.HID_USAGE_LX => ConvertRange(state.ThumbLX, C.XO_AXIS_MIN_VALUE, C.XO_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_LY => ConvertRange(state.ThumbLY, C.XO_AXIS_MIN_VALUE, C.XO_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_RX => ConvertRange(state.ThumbRX, C.XO_AXIS_MIN_VALUE, C.XO_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_RY => ConvertRange(state.ThumbRY, C.XO_AXIS_MIN_VALUE, C.XO_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_LT => ConvertRange(state.LeftTrigger,  C.XO_SLIDER_MIN_VALUE, C.XO_SLIDER_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_RT => ConvertRange(state.RightTrigger, C.XO_SLIDER_MIN_VALUE, C.XO_SLIDER_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        _ => -2
      };
    }

    private static readonly int[] _dpovDirToDegrees = new[] { -2, -1, 0, 9000, 18000, 27000, 4500, 13500, 22500, 31500 };
    internal static int GetXInputStateCPovValue(in GamepadState state, uint targetId)
    {
      var dir = GetXInputStateDpadValue(state, targetId);
      if (dir >= DPovDirection.None && dir <= DPovDirection.NorthWest)
        return _dpovDirToDegrees[(int)dir + 2];
      return -2;
    }

    private static readonly DPovDirection[] _xinputDPadToDirectionMap = new[] {
      DPovDirection.Center,       // NONE       0
      DPovDirection.North,        // DPAD_UP    1
      DPovDirection.South,        // DPAD_DOWN  2
      DPovDirection.None,         // invalid    3
      DPovDirection.West,         // DPAD_LEFT  4
      DPovDirection.NorthWest,    // UP_LEFT    5
      DPovDirection.SouthWest,    // DOWN_LEFT  6
      DPovDirection.None,         // invalid    7
      DPovDirection.East,         // DPAD_RIGHT 8
      DPovDirection.NorthEast,    // UP_RIGHT   9
      DPovDirection.SouthEast,    // DOWN_RIGHT 10
    };

    internal static DPovDirection GetXInputStateDpadValue(in GamepadState state, uint targetId)
    {
      if (targetId == 0 || targetId > 1)
        return DPovDirection.None;
      if ((state.Buttons & XINPUT_BUTTONS.DPAD_MASK) is var mask && mask <= XINPUT_BUTTONS.DPAD_DOWN_RIGHT)
        return _xinputDPadToDirectionMap[(int)mask];
      return DPovDirection.None;
    }

    private readonly static XINPUT_BUTTONS[] _xboxButtonMap = new XINPUT_BUTTONS[C.XO_MAX_BTNS + 1] {
      XINPUT_BUTTONS.NONE,
      XINPUT_BUTTONS.A, XINPUT_BUTTONS.B, XINPUT_BUTTONS.X, XINPUT_BUTTONS.Y,
      XINPUT_BUTTONS.LEFT_SHOULDER, XINPUT_BUTTONS.RIGHT_SHOULDER,
      XINPUT_BUTTONS.BACK, XINPUT_BUTTONS.START, XINPUT_BUTTONS.GUIDE,
      XINPUT_BUTTONS.LEFT_THUMB, XINPUT_BUTTONS.RIGHT_THUMB,
      XINPUT_BUTTONS.DPAD_UP, XINPUT_BUTTONS.DPAD_DOWN, XINPUT_BUTTONS.DPAD_LEFT, XINPUT_BUTTONS.DPAD_RIGHT,
      XINPUT_BUTTONS.DPAD_UP_RIGHT, XINPUT_BUTTONS.DPAD_DOWN_RIGHT, XINPUT_BUTTONS.DPAD_DOWN_LEFT, XINPUT_BUTTONS.DPAD_UP_LEFT,
    };

    internal static int GetXInputStateButtonValue(in GamepadState state, uint targetId)
    {
      if (targetId <= C.XO_MAX_BTNS)
        return state.Buttons.HasFlag(_xboxButtonMap[targetId]) ? 1 : 0;
      return -2;
    }
#endif  // USE_VGEN || USE_VIGEM

    // DualShock4
#if USE_VIGEM
    internal static int GetDS4StateAxisValue(in DS4State state, HID_USAGES axis)
    {
      // If Axis is X,Y,RX,RY then remap range:  -32768 - 32767 ==> 0 - 32767
      // If Triggers (Z,RZ) then remap range:           0 - 255 ==> 0 - 32767
      return axis switch {
        HID_USAGES.HID_USAGE_LX => ConvertRange(state.ThumbLX,      C.DS4_AXIS_MIN_VALUE, C.DS4_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_LY => ConvertRange(state.ThumbLY,      C.DS4_AXIS_MIN_VALUE, C.DS4_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_RX => ConvertRange(state.ThumbRX,      C.DS4_AXIS_MIN_VALUE, C.DS4_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_RY => ConvertRange(state.ThumbRY,      C.DS4_AXIS_MIN_VALUE, C.DS4_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_LT => ConvertRange(state.LeftTrigger,  C.DS4_AXIS_MIN_VALUE, C.DS4_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        HID_USAGES.HID_USAGE_RT => ConvertRange(state.RightTrigger, C.DS4_AXIS_MIN_VALUE, C.DS4_AXIS_MAX_VALUE, C.VJ_AXIS_MIN_VALUE, C.VJ_AXIS_MAX_VALUE, true),
        _ => -2
      };
    }

    internal static int GetDS4StateCPovValue(in DS4State state, uint targetId)
    {
      var dir = GetDS4StateDpadValue(state, targetId);
      if (dir >= DPovDirection.None && dir <= DPovDirection.NorthWest)
        return _dpovDirToDegrees[(int)dir + 2];
      return -2;
    }

    private static readonly DPovDirection[] _ds4DPadToDirectionMap = new[] {
      DPovDirection.North,        // NORTH       = 0x0000
      DPovDirection.NorthEast,    // NORTHEAST   = 0x0001
      DPovDirection.East,         // EAST        = 0x0002
      DPovDirection.SouthEast,    // SOUTHEAST   = 0x0003
      DPovDirection.South,        // SOUTH       = 0x0004
      DPovDirection.SouthWest,    // SOUTHWEST   = 0x0005
      DPovDirection.West,         // WEST        = 0x0006
      DPovDirection.NorthWest,    // NORTHWEST   = 0x0007
      DPovDirection.Center,       // NONE        = 0x0008
    };

    internal static DPovDirection GetDS4StateDpadValue(in DS4State state, uint targetId)
    {
      if (targetId == 0 || targetId > 1)
        return DPovDirection.None;
      if ((state.Buttons & (ushort)DS4_BUTTONS.DS4_DPAD_MASK) is var mask && mask <= (ushort)DS4_BUTTONS.DS4_BUTTON_DPAD_NONE)
        return _ds4DPadToDirectionMap[(int)mask];
      return DPovDirection.None;
    }

    private readonly static DS4_BUTTONS[] _ps4ButtonMap = new DS4_BUTTONS[C.DS4_MAX_BTNS + 1] {
      DS4_BUTTONS.DS4_BUTTON_DPAD_NONE,
      DS4_BUTTONS.DS4_BUTTON_CROSS, DS4_BUTTONS.DS4_BUTTON_CIRCLE, DS4_BUTTONS.DS4_BUTTON_SQUARE, DS4_BUTTONS.DS4_BUTTON_TRIANGLE,
      DS4_BUTTONS.DS4_BUTTON_SHOULDER_LEFT, DS4_BUTTONS.DS4_BUTTON_SHOULDER_RIGHT,
      DS4_BUTTONS.DS4_BUTTON_SHARE, DS4_BUTTONS.DS4_BUTTON_OPTIONS, (DS4_BUTTONS)((int)DS4_SPECIAL_BUTTONS.DS4_SPECIAL_BUTTON_PS | C.DS4_SPECIAL_BUTTON_FLAG),
      DS4_BUTTONS.DS4_BUTTON_THUMB_LEFT, DS4_BUTTONS.DS4_BUTTON_THUMB_RIGHT,
      DS4_BUTTONS.DS4_BUTTON_DPAD_NORTH, DS4_BUTTONS.DS4_BUTTON_DPAD_SOUTH, DS4_BUTTONS.DS4_BUTTON_DPAD_WEST, DS4_BUTTONS.DS4_BUTTON_DPAD_EAST,
      DS4_BUTTONS.DS4_BUTTON_DPAD_NORTHEAST, DS4_BUTTONS.DS4_BUTTON_DPAD_SOUTHEAST, DS4_BUTTONS.DS4_BUTTON_DPAD_SOUTHWEST, DS4_BUTTONS.DS4_BUTTON_DPAD_NORTHEAST,
      DS4_BUTTONS.DS4_BUTTON_TRIGGER_LEFT, DS4_BUTTONS.DS4_BUTTON_TRIGGER_RIGHT, (DS4_BUTTONS)((int)DS4_SPECIAL_BUTTONS.DS4_SPECIAL_BUTTON_TOUCHPAD | C.DS4_SPECIAL_BUTTON_FLAG),
    };

    internal static int GetDS4StateButtonValue(in DS4State state, uint targetId)
    {
      if (targetId == 0 || targetId > C.DS4_MAX_BTNS)
        return -2;
      DS4_BUTTONS mask = _ps4ButtonMap[targetId];
      if (mask.HasFlag((DS4_BUTTONS)C.DS4_SPECIAL_BUTTON_FLAG))
        return state.Special.HasFlag((DS4_SPECIAL_BUTTONS)(mask & ~(DS4_BUTTONS)C.DS4_SPECIAL_BUTTON_FLAG)) ? 1 : 0;
      return (state.Buttons & (ushort)_ps4ButtonMap[targetId]) > 0 ? 1 : 0;
    }
#endif  // USE_VIGEM

    #endregion Joystick State Report helpers

    #region Range and Position converters       /////////////////////////////////////////////////////

    internal static int ModAxis(int value, int min, int max)
    {
      if (value >= min && value <= max)
        return value;
      int ret = value % (++max);
      return ret >= min ? ret : ret + max;
    }

    internal static int PercentOfRange(float value, int rangeMin, int rangeMax)
    {
      return (int)Math.Round((rangeMax - rangeMin) / 100.0f * value) + rangeMin;
    }

    internal static int RangeValueToPercent(int value, int rangeMin, int rangeMax)
    {
      return (int)Math.Round((float)value / (rangeMax - rangeMin) * 100.0f);
    }

    // scale a value from one range into another
    internal static int ConvertRange(float value, int inMin, int inMax, int outMin, int outMax, bool clamp = false)
    {
      float scale = (float)(outMax - outMin) / (inMax - inMin);
      var ret = (int)Math.Round(outMin + ((value - inMin) * scale));
      if (clamp)
        ret = Math.Clamp(ret, outMin, outMax);
      return ret;
    }

    // translate slider value to dpov direction
    internal static DPovDirection SliderRangeToDpovRange(int value, DPovDirection fromAxis)
    {
      if (value > 40 && value < 60)
        return DPovDirection.Center;

      return fromAxis switch {
        DPovDirection.X => value >= 60 ? DPovDirection.East : DPovDirection.West,
        DPovDirection.Y => value >= 60 ? DPovDirection.North : DPovDirection.South,
        // slider right/up goes clockwise from N to W, left/down goes clockwise(!) S to E
        DPovDirection.YX => value switch {
          >= 90 or (> 20 and <= 30) => DPovDirection.West,
          >= 80 or (> 30 and <= 40) => DPovDirection.South,
          >= 70 or <= 10 => DPovDirection.East,
          _ => DPovDirection.North   // >= 60 || (> 10 && <= 20)
        },
        // slider right/up goes clockwise from E to N, left/down goes counter-clockwise W to N
        DPovDirection.XY => value switch {
          >= 90 or <= 10 => DPovDirection.North,
          >= 80 or (> 30 and <= 40) => DPovDirection.West,
          >= 70 or (> 20 and <= 30) => DPovDirection.South,
          _ => DPovDirection.East    // >= 60 || (> 10 && <= 20)
        },
        DPovDirection.None => value switch {
          > 87 or < 13 => DPovDirection.North,
          >=13 and < 39 => DPovDirection.East,
          >= 39 and < 63 =>DPovDirection.South,
          _ => DPovDirection.West
        },
        _ => DPovDirection.None,
      };
    }

    // translate dpov direction to slider value; return of -1 means the direction doesn't apply (eg.North on a X axis)
    internal static int DPovRange2SliderRange(int value, DPovDirection fromAxis)
    {
      DPovDirection fromDir = (DPovDirection)value;
      return fromDir switch {
        DPovDirection.Center => 50,

        DPovDirection.North => fromAxis switch {
          DPovDirection.Y => 100,
          DPovDirection.YX => 65,
          DPovDirection.XY => 100,
          DPovDirection.None => 0,
          _ => -1
        },
        DPovDirection.East => fromAxis switch {
          DPovDirection.X => 100,
          DPovDirection.YX => 75,
          DPovDirection.XY => 65,
          DPovDirection.None => 25,
          _ => -1
        },
        DPovDirection.South => fromAxis switch {
          DPovDirection.Y => 0,
          DPovDirection.YX => 85,
          DPovDirection.XY => 75,
          DPovDirection.None => 50,
          _ => -1
        },
        DPovDirection.West => fromAxis switch {
          DPovDirection.X => 0,
          DPovDirection.YX => 100,
          DPovDirection.XY => 85,
          DPovDirection.None => 75,
          _ => -1
        },
        _ => -1,
      };
    }

    internal static DPovDirection SliderRangeToDPadRange(int value, DPovDirection fromAxis)
    {
      if (value >= 48 && value <= 52)
        return DPovDirection.Center;

      return fromAxis switch {
        DPovDirection.X => value >= 60 ? DPovDirection.East : DPovDirection.West,
        DPovDirection.Y => value >= 60 ? DPovDirection.North : DPovDirection.South,
        // slider right/up goes clockwise from N to W, left/down goes clockwise(!) S to E
        DPovDirection.YX => value switch {
          >= 92 or (> 26 and <= 32) => DPovDirection.NorthWest,
          >= 86 or (> 32 and <= 38) => DPovDirection.West,
          >= 80 or (> 38 and <= 43) => DPovDirection.SouthWest,
          >= 74 or (> 43 and <= 48) => DPovDirection.South,
          >= 68 or <= 8 => DPovDirection.SouthEast,
          >= 62 or (> 8 and <= 14) => DPovDirection.East,
          >= 56 or (> 14 and <= 20) => DPovDirection.NorthEast,
          _ => DPovDirection.North   // >= 53 || (> 20 && <= 26)
        },
        // slider right/up goes clockwise from E to N, left/down goes counter-clockwise W to N
        DPovDirection.XY => value switch {
          >= 92 or (> 14 and <= 20) => DPovDirection.NorthEast,
          >= 86 or (> 8 and <= 14) => DPovDirection.North,
          >= 80 or <= 8 => DPovDirection.NorthWest,
          >= 74 or (> 43 and <= 48) => DPovDirection.West,
          >= 68 or (> 38 and <= 43) => DPovDirection.SouthWest,
          >= 62 or (> 32 and <= 38) => DPovDirection.South,
          >= 56 or (> 26 and <= 32) => DPovDirection.SouthEast,
          _ => DPovDirection.East   // >= 53 || (> 20 && <= 26)
        },
        DPovDirection.None => value switch {
          > 87 or < 13 => DPovDirection.North,
          >= 13 and < 39 => DPovDirection.East,
          >= 39 and < 63 => DPovDirection.South,
          _ => DPovDirection.West
        },
        _ => DPovDirection.None,
      };
    }

    // translate dpov direction to slider value; return of -1 means the direction doesn't apply (eg.North on a X axis)
    internal static int DPadRange2SliderRange(int value, DPovDirection fromAxis)
    {
      DPovDirection fromDir = (DPovDirection)value;
      return fromDir switch {
        DPovDirection.Center => 50,

        DPovDirection.North => fromAxis switch {
          DPovDirection.Y => 100,
          DPovDirection.YX => 55,
          DPovDirection.XY => 90,
          DPovDirection.None => 0,
          _ => -1
        },
        DPovDirection.NorthEast => fromAxis switch {
          DPovDirection.YX => 59,
          DPovDirection.XY => 100,
          DPovDirection.None => 13,
          _ => -1
        },
        DPovDirection.East => fromAxis switch {
          DPovDirection.X => 100,
          DPovDirection.YX => 68,
          DPovDirection.XY => 55,
          DPovDirection.None => 25,
          _ => -1
        },
        DPovDirection.SouthEast => fromAxis switch {
          DPovDirection.YX => 71,
          DPovDirection.XY => 59,
          DPovDirection.None => 38,
          _ => -1
        },
        DPovDirection.South => fromAxis switch {
          DPovDirection.Y => 0,
          DPovDirection.YX => 77,
          DPovDirection.XY => 65,
          DPovDirection.None => 50,
          _ => -1
        },
        DPovDirection.SouthWest => fromAxis switch {
          DPovDirection.YX => 83,
          DPovDirection.XY => 71,
          DPovDirection.None => 63,
          _ => -1
        },
        DPovDirection.West => fromAxis switch {
          DPovDirection.X => 0,
          DPovDirection.YX => 90,
          DPovDirection.XY => 77,
          DPovDirection.None => 75,
          _ => -1
        },
        DPovDirection.NorthWest => fromAxis switch {
          DPovDirection.YX => 100,
          DPovDirection.XY => 83,
          DPovDirection.None => 13,
          _ => -1
        },
        _ => -1,
      };
    }

    internal static float DPadToCPov(DPovDirection dpadDir)
    {
      return dpadDir switch {
        DPovDirection.North       => 0.0f,
        DPovDirection.NorthEast   => 12.5f,
        DPovDirection.East        => 25.0f,
        DPovDirection.SouthEast   => 37.5f,
        DPovDirection.South       => 50.0f,
        DPovDirection.SouthWest   => 62.5f,
        DPovDirection.West        => 75.0f,
        DPovDirection.NorthWest   => 87.5f,
        DPovDirection.Center or _ => -1.0f
      };
    }

    // converts 0-100 to 4-way DPov direction
    internal static DPovDirection CPovToDPov(int percent)
    {
      return percent switch {
        < 0 => DPovDirection.Center,
        >= 82 or < 21 => DPovDirection.North,
        >= 21 and < 45 => DPovDirection.East,
        >= 45 and < 70 => DPovDirection.South,
        >= 70 and < 82 => DPovDirection.West
      };
    }

    // converts 0-100 to 8-way dpad direction
    internal static DPovDirection CPovToDPad(int percent)
    {
      return percent switch {
        < 0 => DPovDirection.Center,
        >= 94 or < 7    => DPovDirection.North,
        >= 7 and < 21  => DPovDirection.NorthEast,
        >= 21 and < 32 => DPovDirection.East,
        >= 32 and < 45 => DPovDirection.SouthEast,
        >= 45 and < 57 => DPovDirection.South,
        >= 57 and < 70 => DPovDirection.SouthWest,
        >= 70 and < 82 => DPovDirection.West,
        >= 82 and < 94 => DPovDirection.NorthWest
      };
    }

    #endregion  Range and Position converters

  }
}
