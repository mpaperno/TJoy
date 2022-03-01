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

namespace TJoy.Enums
{
  internal enum DeviceType : short
  {
    None, VJoy, XBox360, DualShock4
  }

  internal enum ControlType : short
  {
    None, Button, Axis, ContPov, DiscPov, Slider
  }

  internal enum ButtonAction : short
  {
    None, Click, Down, Up
  }

  internal enum DPovDirection : short
  {
    None = -2, Center, North, East, South, West,
    X, Y, XY, YX  // for sliders/dials
  }

  internal enum CtrlResetMethod : short
  {
    None, Button,
    Start,
    Center,
    Max, Min,
    OneQuarter, ThreeQuarters,
    North, East, South, West,    // D-POV
    Custom
  }

  internal enum AxisMovementDir : short
  {
    Normal, Reverse
  }
}
