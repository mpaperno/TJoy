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

using TJoy.Enums;
using TJoy.Constants;
using TJoy.Types;
using vJoyInterfaceWrap;
using Math = System.Math;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TJoy.Utilities
{

  internal static class Utils
  {

    internal static string FullActionID(string id, bool isConn = false) => C.PLUGIN_ID + "." + C.IDSTR_CATEGORY_VJOY + (isConn ? ".conn." : ".act.") + id;
    internal static string FullActionDataID(string actId, string dataId, bool isConn = false) => FullActionID(actId, isConn) + "." + dataId;
    internal static string ConnectorDictKey(string actId, uint tgtId) => $"{actId}:{tgtId}";   // {ev.devId}:  Add these back as first mbr if going to multiple device support
    internal static string ConnectorDictKey(in VJEvent ev) => ConnectorDictKey(ev.tpId, ev.targetId /*, ev.devId*/);
    //internal static string ConnectorDictKey(in ConnectorTrackingData td) => ConnectorDictKey(td.id, td.targetId /*, td.devId*/);
    internal static long TicksToSecs(long ticks) => ticks / Stopwatch.Frequency;
    //internal static long TicksToMs(long ticks) => ticks / (Stopwatch.Frequency / 1000L);

    internal static ControlType TpStateNameToEventType(string actId)
    {
      return actId switch {
        C.IDSTR_DEVTYPE_AXIS => ControlType.Axis,
        C.IDSTR_DEVTYPE_CPOV => ControlType.ContPov,
        C.IDSTR_DEVTYPE_DPOV => ControlType.DiscPov,
        C.IDSTR_DEVTYPE_BTN => ControlType.Button,
        _ => ControlType.None,
      };
    }

    internal static string EventTypeToTpStateName(ControlType type)
    {
      return type switch {
        ControlType.Axis => C.IDSTR_DEVTYPE_AXIS,
        ControlType.ContPov => C.IDSTR_DEVTYPE_CPOV,
        ControlType.DiscPov => C.IDSTR_DEVTYPE_DPOV,
        ControlType.Button => C.IDSTR_DEVTYPE_BTN,
        _ => string.Empty,
      };
    }

    internal static string EventTypeToControlName(ControlType type)
    {
      return type switch {
        ControlType.Axis => "Axis",
        ControlType.ContPov => "Continuous POV",
        ControlType.DiscPov => "Discrete POV",
        ControlType.Button => "Button",
        _ => string.Empty,
      };
    }

    // FIXME
    //internal static int GetMaxValueForEventType(ControlType evtype)
    //{
    //  return evtype switch {
    //    ControlType.Axis => C.VJ_AXIS_MAX_VALUE,
    //    ControlType.ContPov => C.VJ_CPOV_MAX_VALUE,
    //    ControlType.Slider => C.XO_SLIDER_MAX_VALUE,
    //    ControlType.DiscPov => 3,
    //    _ => 1  // button
    //  };
    //}

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

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding axis.
    // Returns -2 if no value (eg. POV axis)
    internal static int GetVJoyStateReportAxisValue(in vJoy.JoystickState state, HID_USAGES axis)
    {
      return axis switch {
        HID_USAGES.HID_USAGE_X => state.AxisX,
        HID_USAGES.HID_USAGE_Y => state.AxisY,
        HID_USAGES.HID_USAGE_Z => state.AxisZ,
        HID_USAGES.HID_USAGE_RX => state.AxisXRot,
        HID_USAGES.HID_USAGE_RY => state.AxisYRot,
        HID_USAGES.HID_USAGE_RZ => state.AxisZRot,
        HID_USAGES.HID_USAGE_SL0 => state.Slider,
        HID_USAGES.HID_USAGE_SL1 => state.Dial,
        HID_USAGES.HID_USAGE_WHL => state.Wheel,
        HID_USAGES.HID_USAGE_POV => -2,  // not an actual axis in the struct
        HID_USAGES.HID_USAGE_AILERON => state.Aileron,
        HID_USAGES.HID_USAGE_RUDDER => state.Rudder,
        HID_USAGES.HID_USAGE_THROTTLE => state.Throttle,
        HID_USAGES.HID_USAGE_ACCELERATOR => state.Accelerator,
        HID_USAGES.HID_USAGE_BRAKE => state.Brake,
        HID_USAGES.HID_USAGE_CLUTCH => state.Clutch,
        HID_USAGES.HID_USAGE_STEERING => state.Steering,
        _ => -2
      };
    }

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding C-POV number (1-indexed).
    // Returns -2 if no value (eg. invalid POV). Returns -1 when the POV is centered.
    internal static int GetVJoyStateReportCPovValue(in vJoy.JoystickState state, uint targetId)
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
    internal static int GetVJoyStateReportDPovValue(in vJoy.JoystickState state, uint targetId)
    {
      if (targetId-- < 1) return -2;
      return (int)((state.bHats & (0xF << (int)targetId)) >> (int)(4 * targetId));
    }

    // Gets the correct data member value from a vJoy JOYSTICK_POSITION struct based on the corresponding D-POV number (1-indexed).
    // Returns -2 if no value (eg. invalid POV). Returns -1 when the POV is centered.
    internal static int GetVJoyStateReportButtonValue(in vJoy.JoystickState state, uint targetId)
    {
      if (targetId < 1) return 0;
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

    internal static int ModAxis(int value, int min, int max)
    {
      if (value >= min && value <= max)
        return value;
      int ret = value % (++max);
      return ret >= min ? ret : ret + max;
    }

    // scale a value from one range into another
    internal static int ConvertRange(int value, int inMin, int inMax, int outMin, int outMax, bool clamp = false)
    {
      double scale = (double)(outMax - outMin) / (inMax - inMin);
      var ret = (int)Math.Round(outMin + ((value - inMin) * scale));
      if (clamp)
        ret = Math.Clamp(ret, outMin, outMax);
      return ret;
    }

    // scale TP slider range to joystick axis range
    //internal static int SliderRange2AxisRange(int value, int outMin, int outMax)
    //{
    //  return ConvertRange(value, 0, C.TP_SLIDER_MAX_VALUE, outMin, outMax);
    //}

    // scale joystick axis range onto TP slider range
    //internal static int AxisRange2SliderRange(int value, int outMin, int outMax)
    //{
    //  var ret = ConvertRange(value, outMin, outMax, 0, C.TP_SLIDER_MAX_VALUE);
    //  return Math.Clamp(ret, 0, 100);
    //}

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
      return value switch {
        // Center
        -1 => 50,
        // North
        0 => fromAxis switch {
          DPovDirection.Y => 100,
          DPovDirection.YX => 65,
          DPovDirection.XY => 100,
          DPovDirection.None => 0,
          _ => -1
        },
        // East
        1 => fromAxis switch {
          DPovDirection.X => 100,
          DPovDirection.YX => 75,
          DPovDirection.XY => 65,
          DPovDirection.None => 25,
          _ => -1
        },
        // South
        2 => fromAxis switch {
          DPovDirection.Y => 0,
          DPovDirection.YX => 85,
          DPovDirection.XY => 75,
          DPovDirection.None => 50,
          _ => -1
        },
        // West
        3 => fromAxis switch {
          DPovDirection.X => 0,
          DPovDirection.YX => 100,
          DPovDirection.XY => 85,
          DPovDirection.None => 75,
          _ => -1
        },
        _ => -1,
      };
    }

  }
}
