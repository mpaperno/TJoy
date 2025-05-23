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
    internal const string PLUGIN_SHORT_NAME  = "TJoy";
    internal const string PLUGIN_LONG_NAME   = PLUGIN_SHORT_NAME + " Touch Portal Plugin";

    internal const string IDSTR_EL_ACTION    = "act";
    internal const string IDSTR_EL_CONNECTOR = "conn";
    internal const string IDSTR_EL_STATE     = "state";
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
    internal const string IDSTR_GAMEPAD      = "gamepad";
    internal const string IDSTR_DEVICE_ID    = "devId";
    internal const string IDSTR_DEVID_DFLT   = "Default";
    internal const string IDSTR_RUN_STOPPED  = "stopped";
    internal const string IDSTR_RUN_STARTING = "starting";
    internal const string IDSTR_RUN_STARTED  = "started";

    internal const string IDSTR_SETTING_DEF_DEVID    = "Default Device (0 to disable)";
    internal const string IDSTR_SETTING_AUTO_CONNECT = "Auto-Connect Device On Action/Slider Event (0/1)";
    internal const string IDSTR_SETTING_STATE_RATE   = "Position State Report Update Interval (ms)";
    internal const string IDSTR_SETTING_RPRT_AS_RAW  = "Report Raw Axis Values (0/1)";
    internal const string IDSTR_SETTING_RPRT_BTN_RNG = "Buttons To Report (max. # or range)";

    internal const string IDSTR_STATE_LAST_CONNECT   = "lastConnectedDevice";
    internal const string IDSTR_STATE_LAST_DISCNCT   = "lastDisconnectedDevice";
    internal const string IDSTR_STATE_GAMEPAD_LED    = "led";
    internal const string IDSTR_STATE_PLUGIN_RUN     =  "plugin.runState";

    internal const string IDSTR_ACTION_DEVICE_CTRL   = "device";
    internal const string IDSTR_ACTION_SET_POS       = "setPos";

    internal const string STR_ON           = "On";
    internal const string STR_OFF          = "Off";
    internal const string STR_TOGGLE       = "Toggle";
    internal const string STR_RESET        = "Reset";
    internal const string STR_REFRESH      = "Refresh";
    internal const string STR_REPORT       = "Report";
    internal const string STR_NONE         = "None";
    internal const string STR_CONNECT      = "Connect";
    internal const string STR_DISCONNECT   = "Disconnect";
    internal const string STR_CONNECTION   = "Connection";
    internal const string STR_TOGGLE_CONN  = STR_TOGGLE + " " + STR_CONNECTION;
    internal const string STR_REFRESH_REP  = STR_REFRESH + " " + STR_REPORT;
    internal const string STR_FORCE_UNPLUG = "Force Unplug";

    internal const string STR_DEVNAME_VJOY   = "vJoy";
    internal const string STR_DEVNAME_VXBOX  = "vXBox";
    internal const string STR_DEVNAME_VBXBOX = "vgeXBox";
    internal const string STR_DEVNAME_VBDS4  = "vgeDS4";

    internal const string STR_AXIS        = "Axis";
    internal const string STR_CONT_POV    = "Continuous Hat";
    internal const string STR_DISC_POV    = "Discrete Hat";
    internal const string STR_DPAD        = "D-Pad";
    internal const string STR_BUTTON      = "Button";

    internal const string STR_GPAXIS_LX   = "Lx";
    internal const string STR_GPAXIS_LY   = "Ly";
    internal const string STR_GPAXIS_LT   = "LT";
    internal const string STR_GPAXIS_RT   = "RT";
    internal const string STR_GPAXIS_RX   = "Rx";
    internal const string STR_GPAXIS_RY   = "Ry";

    internal const string STR_XBOX_BTN1   = "A";
    internal const string STR_XBOX_BTN2   = "B";
    internal const string STR_XBOX_BTN3   = "X";
    internal const string STR_XBOX_BTN4   = "Y";
    internal const string STR_XBOX_BTN5   = "LB";
    internal const string STR_XBOX_BTN6   = "RB";
    internal const string STR_XBOX_BTN7   = "Back";
    internal const string STR_XBOX_BTN8   = "Start";
    internal const string STR_XBOX_BTN9   = "Guide";
    internal const string STR_XBOX_BTN10  = "LT";
    internal const string STR_XBOX_BTN11  = "RT";
    internal const string STR_XBOX_BTN12  = "DP UP";
    internal const string STR_XBOX_BTN13  = "DP RIGHT";
    internal const string STR_XBOX_BTN14  = "DP DOWN";
    internal const string STR_XBOX_BTN15  = "DP LEFT";
    internal const string STR_XBOX_BTN16  = "DP NE";
    internal const string STR_XBOX_BTN17  = "DP SE";
    internal const string STR_XBOX_BTN18  = "DP SW";
    internal const string STR_XBOX_BTN19  = "DP NW";

    internal const string STR_PS4_BTN1    = "⛌";
    internal const string STR_PS4_BTN2    = "◯";
    internal const string STR_PS4_BTN3    = "⬜";
    internal const string STR_PS4_BTN4    = "△";
    internal const string STR_PS4_BTN5    = "L1";
    internal const string STR_PS4_BTN6    = "R1";
    internal const string STR_PS4_BTN7    = "Share";
    internal const string STR_PS4_BTN8    = "Options";
    internal const string STR_PS4_BTN9    = "PS";
    internal const string STR_PS4_BTN10   = "L3";
    internal const string STR_PS4_BTN11   = "R3";
    internal const string STR_PS4_BTN12   = "DP N";
    internal const string STR_PS4_BTN13   = "DP E";
    internal const string STR_PS4_BTN14   = "DP S";
    internal const string STR_PS4_BTN15   = "DP W";
    internal const string STR_PS4_BTN16   = "DP NE";
    internal const string STR_PS4_BTN17   = "DP SE";
    internal const string STR_PS4_BTN18   = "DP SW";
    internal const string STR_PS4_BTN19   = "DP NW";
    internal const string STR_PS4_BTN20   = "L2";
    internal const string STR_PS4_BTN21   = "R2";
    internal const string STR_PS4_BTN22   = "TPad";

    internal const int BUTTON_CLICK_WAIT_MS  = 50;  // ms to wait between button down and up for a "click" event
    internal const int CONNECTOR_MOVE_TO_SEC = 7;   // timeout secs after which to assume a connector is no longer being moved
    internal const int VJD_STATE_MAX_AGE_SEC = 10;  // joystick state data is considered old after this many seconds (used for updating connectors, not the TP state updates)


    // vJoy
    internal const int VJ_AXIS_MIN_VALUE = 0;
    internal const int VJ_AXIS_MAX_VALUE = 0x7FFF;  // this is what my device reports and some docs mention though I've also seen the range 1-0x8000 mentioned.
    internal const int VJ_CPOV_MIN_VALUE = 0;       // actually -1 = center and 0 = North but we have to deal with this in code as an exception to the general rules
    internal const int VJ_CPOV_MAX_VALUE = 35900;   // docs claim 35999 but anything over 35900 causes it to center (as if it wraps to -1) and device info confirms 35900 is max.

    // XOutput
    internal const short XO_MAX_BTNS = 19;
    internal const int XO_SLIDER_MIN_VALUE = 0;
    internal const int XO_SLIDER_MAX_VALUE = 0xFF;
    internal const int XO_AXIS_MIN_VALUE   = -0x8000;
    internal const int XO_AXIS_MAX_VALUE   = 0x7FFF;

    // DS4
    internal const short DS4_MAX_BTNS = 22;
    internal const int DS4_AXIS_MIN_VALUE = 0;
    internal const int DS4_AXIS_MAX_VALUE = 0xFF;

    internal const int DS4_SPECIAL_BUTTON_FLAG = 1 << 16;

#if VJOY_API_2_1
    internal static readonly uint VJOY_API_VERSION = 0x0219;  // same as vGen
#else
    internal static readonly uint VJoyApiVersion = 0x0220;
#endif

  }

}
