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
    None = -1,
    VJoy = 0,
    VXBox = 1000,
    VBXBox = 2000,
    VBDS4 = 3000
  }

  internal enum DeviceStatus : short
  {
    Unknown, Connected, Disconnected
  }

  internal enum ControlType : short
  {
    None, Button, Axis, DiscPov, ContPov
  }

  internal enum ButtonAction : short
  {
    None, Click, Down, Up
  }

  internal enum DPovDirection : short
  {
    None = -2, Center,
    North, East, South, West,
    NorthEast, SouthEast, SouthWest, NorthWest,
    // for sliders/dials
    X = 10, Y, XY, YX,
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
