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

namespace TJoy.Constants
{

  internal static class C
  {
    internal const string PLUGIN_ID          = "us.wdg.max.tpp.tjoy";
    internal const string IDSTR_TARGET_ID    = "id";
    internal const string IDSTR_RNG_MIN      = "min";
    internal const string IDSTR_RNG_MAX      = "max";
    internal const string IDSTR_ACT_VAL      = "value";
    internal const string IDSTR_DPOV_DIR     = "dir";
    internal const string IDSTR_DIR_REVERSE  = "reverse";
    internal const string IDSTR_RESET_TYP    = "reset";
    internal const string IDSTR_RESET_VAL    = "resetValue";
    internal const string IDSTR_DEVTYPE_BTN  = "button";
    internal const string IDSTR_DEVTYPE_DPOV = "dpov";
    internal const string IDSTR_DEVTYPE_CPOV = "cpov";
    internal const string IDSTR_DEVTYPE_AXIS = "axis";
    //internal const string IDSTR_DEVICE_ID = "devId";

    internal const string IDSTR_CATEGORY_VJOY        = "vJoy";
    internal const string IDSTR_SETTING_VJDEVID      = "vJoy Device ID (1-16, 0 to disable)";
    internal const string IDSTR_SETTING_STATE_RATE   = "vJoy State Update Interval (ms)";
    internal const string IDSTR_SETTING_RPRT_AS_RAW  = "Report Raw Axis Values (0/1)";
    internal const string IDSTR_SETTING_RPRT_BTN_RNG = "Buttons To Report (max. # or range)";
    internal const string IDSTR_STATE_VJSTATE        = "vJoyConnection";
    internal const string IDSTR_ACTION_VJCONN        = "connection";

    internal const int BUTTON_CLICK_WAIT_MS = 50;   // ms to wait between button down and up for a "click" event
    internal const int CONNECTOR_MOVE_TO_SEC = 15;  // timeout secs after which to assume a connector is no longer being moved

    // vJoy
    internal const int VJ_AXIS_MIN_VALUE = 0;
    internal const int VJ_AXIS_MAX_VALUE = 0x7FFF;  // this is what my device reports and some docs mention though I've also seen the range 1-0x8000 mentioned.
    internal const int VJ_CPOV_MIN_VALUE = 0;       // actually -1 = center and 0 = North but we have to deal with this in code as an exception to the general rules
    internal const int VJ_CPOV_MAX_VALUE = 35900;   // docs claim 35999 but anything over 35900 causes it to center (as if it wraps to -1) and device info confirms 35900 is max.

    // XOutput
    internal const int XO_SLIDER_MIN_VALUE = 0;
    internal const int XO_SLIDER_MAX_VALUE = 0xFF;
    internal const int XO_AXIS_MIN_VALUE   = -0x7FFF;
    internal const int XO_AXIS_MAX_VALUE   = 0x7FFF;

    internal const int TP_SLIDER_MAX_VALUE = 100;   // no controversy there!

    // list of axes which should (probably) be reset to zero vs. centered
    internal static readonly HID_USAGES[] AXES_RESET_TO_MIN = new[] { HID_USAGES.HID_USAGE_THROTTLE, HID_USAGES.HID_USAGE_SL0, HID_USAGES.HID_USAGE_CLUTCH, HID_USAGES.HID_USAGE_BRAKE };
  }

}
