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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TJoy.Constants;
using TJoy.Enums;
using TJoy.Types;
using TJoy.Utilities;
using vJoyInterfaceWrap;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TJoy
{
  // TODO: abstract to support XOutput/ViGEm devices
  internal class JoyDevice
  {

    public DeviceType DeviceType      { get => _deviceType; }
    public uint DeviceId              { get => _vjoyInfo.id; }
    public bool IsConnected           { get => CheckConnected(_vjoyInfo.id); }
    public bool DeviceTypeExists      { get => _vjoy.vJoyEnabled(); }
    public string Manufacturer        { get => _vjoy.GetvJoyManufacturerString(); }
    public string Product             { get => _vjoy.GetvJoyProductString(); }
    public string SerialNumber        { get => _vjoy.GetvJoySerialNumberString(); }
    public uint ButtonCount           { get => _vjoyInfo.nButtons; }
    public uint DiscreteHatCount      { get => _vjoyInfo.nDiscPov; }
    public uint ContionousHatCount    { get => _vjoyInfo.nContPov; }
    public uint AxisCount             { get => (uint)_vjoyInfo.axes.Count; }
    public List<VJAxisInfo> AxisInfo  { get => _vjoyInfo.axes.Values.ToList(); }

    private readonly DeviceType _deviceType;
    private readonly vJoy _vjoy;
    private VJDeviceInfo _vjoyInfo = new();  // info about current device, need to change to support multiple devices
    private readonly ILogger<JoyDevice> _logger;
    private readonly object _devInfoStructLock = new();

    public JoyDevice(ILoggerFactory factory, DeviceType deviceType = DeviceType.VJoy)
    {
      _deviceType = deviceType;
      _logger = factory?.CreateLogger<JoyDevice>() ?? throw new ArgumentNullException(nameof(factory));

      _vjoy = new();
      _vjoyInfo.axes = new();
    }

    public bool CheckConnected(uint vjid) =>
        IsDeviceIdValid(vjid) && _vjoy?.GetVJDStatus(vjid) == VjdStat.VJD_STAT_OWN;

    public void RegisterRemovalCallback(vJoy.RemovalCbFunc cb, object data) =>
        _vjoy.RegisterRemovalCB(cb, data);

    public bool IsDeviceIdValid(uint devId) =>
        devId > 0 && devId < 17;

    public ref VJDeviceInfo DeviceInfo() =>
        ref _vjoyInfo;

    public ref vJoy.JoystickState DeviceState() =>
        ref _vjoyInfo.state;

    // Test if DLL matches the driver
    public bool CheckVersion(out UInt32 dllVer, out UInt32 drivVer)
    {
      dllVer = 0;
      drivVer = 0;
      return _vjoy.DriverMatch(ref dllVer, ref drivVer);
    }

    public bool CheckStatus(uint vjid)
    {
      VjdStat status = _vjoy.GetVJDStatus(vjid);
      switch (status) {
        case VjdStat.VJD_STAT_OWN:
          _logger.LogInformation("vJoy Device {0} is already owned by this feeder", vjid);
          return true;
        case VjdStat.VJD_STAT_FREE:
          _logger.LogInformation("vJoy Device {0} is free!", vjid);
          return true;
        case VjdStat.VJD_STAT_BUSY:
          _logger.LogWarning("vJoy Device {0} is already owned by another feeder. Cannot continue.", vjid);
          return false;
        case VjdStat.VJD_STAT_MISS:
          _logger.LogWarning("vJoy Device {0} is not installed or disabled. Cannot continue.", vjid);
          return false;
        default:
          _logger.LogWarning("vJoy Device {0} general error. Cannot continue.", vjid);
          return false;
      };
    }

    public bool Connect(uint vjid)
    {
      if (!IsDeviceIdValid(vjid))
          return false;
      if (CheckConnected(vjid))
        return true;

      if (vjid != _vjoyInfo.id && IsConnected)
        RelinquishDevice(_vjoyInfo.id);

      // Get the state of the requested device
      if (!CheckStatus(vjid))
        return false;

      // Acquire the target
      if (!_vjoy.AcquireVJD(vjid))
        return false;

      _vjoyInfo.id = vjid;
      //bool ok = _vjoy.ResetVJD(vjid);  // needed? doesn't seem to work right
      //_ = _vjoy.GetPosition(vjid, ref _vjoyInfo.state);  // debug
      _logger.LogDebug("Acquired: vJoy device number {0}.", vjid);

      LoadDeviceCapabilities();
      return true;
    }

    public void RelinquishDevice(uint vjid)
    {
      if (vjid > 0 && vjid < 17 && CheckConnected(vjid)) {
        _vjoy.RelinquishVJD(vjid);
        _vjoyInfo.id = 0;
      }
    }

    private void LoadDeviceCapabilities()
    {
      // Check and log device capabilities
      // Check which axes are supported
      uint devId = _vjoyInfo.id;
      _vjoyInfo.axes.Clear();
      foreach (var axe in Enum.GetValues<HID_USAGES>()) {
        long minval = 0, maxval = 0;
        if (_vjoy.GetVJDAxisExist(devId, axe) is bool exists) {
          _vjoy.GetVJDAxisMax(devId, axe, ref maxval);
          _vjoy.GetVJDAxisMin(devId, axe, ref minval);
          _vjoyInfo.axes.Add(axe, new VJAxisInfo { usage = axe, minValue = (int)minval, maxValue = (int)maxval });
        }
      }
      // Get the number of buttons and POV Hat switches
      _vjoyInfo.nButtons = (ushort)_vjoy.GetVJDButtonNumber(devId);
      _vjoyInfo.nContPov = (ushort)_vjoy.GetVJDContPovNumber(devId);
      _vjoyInfo.nDiscPov = (ushort)_vjoy.GetVJDDiscPovNumber(devId);
    }

    public string GetDeviceCapabilitiesReport(uint vjid = 0)
    {
      if (vjid == 0)
        vjid = _vjoyInfo.id;
      if (!IsDeviceIdValid(vjid))
        return "Invalid device ID.";

      var capsReport = new System.Text.StringBuilder();
      capsReport.Append($"\nvJoy Device {vjid} capabilities:\n");
      foreach (var axe in Enum.GetValues<HID_USAGES>()) {
        long minval = 0, maxval = 0;
        bool exists = _vjoy.GetVJDAxisExist(vjid, axe);
        _vjoy.GetVJDAxisMax(vjid, axe, ref maxval);
        _vjoy.GetVJDAxisMin(vjid, axe, ref minval);
        string axeName = axe.ToString().Split('_').Last();
        capsReport.Append($"Axis {axeName,-16}{(exists ? "Yes" : "No ")}\trange: {minval} - {maxval}\n");
      }
      capsReport.Append($"Number of buttons           {_vjoy.GetVJDButtonNumber(vjid)}\n");
      capsReport.Append($"Number of Continuous POVs   {_vjoy.GetVJDContPovNumber(vjid)}\n");
      capsReport.Append($"Number of Discrete POVs     {_vjoy.GetVJDDiscPovNumber(vjid)}\n");

      return capsReport.ToString();
    }

    public void ResetDevice(/*uint vjid*/)
    {
      lock (_devInfoStructLock) {
        uint vjid = _vjoyInfo.id;
        foreach (var axe in _vjoyInfo.axes.Values) {
          int val = C.AXES_RESET_TO_MIN.Contains(axe.usage) ? axe.minValue : (axe.maxValue - axe.minValue) / 2;
          _vjoy.SetAxis(val, vjid, axe.usage);
        }
        _vjoy.GetPosition(_vjoyInfo.id, ref _vjoyInfo.state);
        _vjoyInfo.state.bHats = 0xFFFFFFFF;
        _vjoyInfo.state.bHatsEx1 = 0xFFFFFFFF;
        _vjoyInfo.state.bHatsEx2 = 0xFFFFFFFF;
        _vjoyInfo.state.bHatsEx3 = 0xFFFFFFFF;
        _vjoyInfo.state.Buttons = 0;
        _vjoyInfo.state.ButtonsEx1 = 0;
        _vjoyInfo.state.ButtonsEx2 = 0;
        _vjoyInfo.state.ButtonsEx3 = 0;
        _vjoy.UpdateVJD(vjid, ref _vjoyInfo.state);
        _vjoyInfo.lastStateUpdate = Stopwatch.GetTimestamp();
      }
    }

    public bool RefreshState()
    {
      try {
        lock (_devInfoStructLock) {
          _vjoy.GetPosition(_vjoyInfo.id, ref _vjoyInfo.state);
          _vjoyInfo.lastStateUpdate = Stopwatch.GetTimestamp();
        }
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception while trying to vJoy.GetPostion().");
        return false;
      }
      return true;
    }

    public bool HasDeviceAxis(HID_USAGES axis)
    {
      return _vjoyInfo.axes.ContainsKey(axis);
    }

    // If the axis doesn't exist in the current device this returns false and
    // populates the info struct with general defaults for this device & axis type
    // from GetDefaultAxisRange().
    public bool TryGetAxisInfo(HID_USAGES axis, out VJAxisInfo info) {
      if (!_vjoyInfo.axes.TryGetValue(axis, out info)) {
        info.usage = axis;
        GetDefaultAxisRange(axis, out info.minValue, out info.maxValue);
        return false;
      }
      return true;
    }

    public void GetDefaultAxisRange(HID_USAGES axis, out int minValue, out int maxValue)
    {
      if (axis == HID_USAGES.HID_USAGE_POV) {
        minValue = _deviceType == DeviceType.VJoy ? C.VJ_CPOV_MIN_VALUE : C.XO_SLIDER_MIN_VALUE;
        maxValue = _deviceType == DeviceType.VJoy ? C.VJ_CPOV_MAX_VALUE : C.XO_SLIDER_MAX_VALUE;
        return;
      }
      minValue = _deviceType == DeviceType.VJoy ? C.VJ_AXIS_MIN_VALUE : C.XO_AXIS_MIN_VALUE;
      maxValue = _deviceType == DeviceType.VJoy ? C.VJ_AXIS_MAX_VALUE : C.XO_AXIS_MAX_VALUE;
    }

    // scale from a variable % range (inMin >= value <= inMax) to joystick axis range (axeMin >= value <= axeMax)
    // set `normAxis` to return the normalized value which is wrapped to fit within the allowed range (see also GetNormalizedAxisValue())
    public int ScaleInputToAxisRange(HID_USAGES axis, int value, int inMin, int inMax, bool normAxis = false)
    {
      if (axis == HID_USAGES.HID_USAGE_POV && value == -1)
        return -1;
      _ = TryGetAxisInfo(axis, out VJAxisInfo info);
      int ret = value;
      if (inMax - inMin != 100)
        ret = Utils.ConvertRange(ret, 0, 100, inMin, inMax);
      ret = Utils.ConvertRange(ret, 0, 100, info.minValue, info.maxValue);
      if (normAxis)
        ret = GetNormalizedAxisValue(ref info, ret);
      return ret;
    }

    // scale joystick axis range (axeMin >= value <= axeMax) onto a variable %age range (outMin >= value <= outMax)
    public int ScaleAxisToInputRange(HID_USAGES axis, int value, int outMin, int outMax)
    {
      _ = TryGetAxisInfo(axis, out VJAxisInfo info);
      var ret = Utils.ConvertRange(value, info.minValue, info.maxValue, 0, 100);
      if (outMax - outMin != 100)
        ret = Utils.ConvertRange(ret, outMin, outMax, 0, 100);
      return Math.Clamp(ret, 0, 100);
    }

    // returns a value which is notmalized to fit withing the given axis' actual range (value is wrapped as needed to conform)
    public int GetNormalizedAxisValue(HID_USAGES axis, int value)
    {
      _ = TryGetAxisInfo(axis, out VJAxisInfo info);
      return GetNormalizedAxisValue(ref info, value);
    }

    public int GetNormalizedAxisValue(ref VJAxisInfo info, int value)
    {
      // make a special exception for the vJoy hat which is -1 at center (not the actual axis range minimum of 0 reported)
      if (info.usage == HID_USAGES.HID_USAGE_POV && _deviceType == DeviceType.VJoy)
        info.minValue = -1;
      return Utils.ModAxis(value, info.minValue, info.maxValue);
    }


    #region Action Handlers       /////////////////////////////////////////////////////////////

    public void DispatchEvent(in VJEvent ev)
    {
      if (!IsConnected)
        return;
      switch (ev.type) {
        case ControlType.Button:
          VJoyButtonAction(ev);
          break;
        case ControlType.DiscPov:
          VJoyDPovAction(ev);
          break;
        case ControlType.ContPov:
          VJoyCPovAction(ev);
          break;
        case ControlType.Axis:
          VJoyAxisAction(ev);
          break;
      }
    }

    private void VJoyButtonAction(in VJEvent ev)
    {
      switch (ev.btnAction) {
        case ButtonAction.Down:
          _vjoy.SetBtn(true, ev.devId, ev.targetId);
          break;
        case ButtonAction.Up:
          _vjoy.SetBtn(false, ev.devId, ev.targetId);
          break;
        case ButtonAction.Click:
          _vjoy.SetBtn(true, ev.devId, ev.targetId);
          VJoyReleaseButtonLater(ev.type, ev.devId, ev.targetId);
          break;
        default:
          if (ev.value == 0 || ev.value == 1)
            _vjoy.SetBtn(ev.value == 1, ev.devId, ev.targetId);
          return;
      }
    }

    private void VJoyDPovAction(in VJEvent ev)
    {
      switch (ev.btnAction) {
        case ButtonAction.Down:
          _vjoy.SetDiscPov((int)ev.dpovDir, ev.devId, ev.targetId);
          break;
        case ButtonAction.Up:
          _vjoy.SetDiscPov((int)DPovDirection.Center, ev.devId, ev.targetId);
          break;
        case ButtonAction.Click:
          _vjoy.SetDiscPov((int)ev.dpovDir, ev.devId, ev.targetId);
          VJoyReleaseButtonLater(ev.type, ev.devId, ev.targetId);
          break;
        default:
          if (ev.value > -2 && ev.value < 4)
            _vjoy.SetDiscPov(ev.value, ev.devId, ev.targetId);
          return;
      }
    }

    private void VJoyCPovAction(in VJEvent ev)
    {
      //var value = ScaleInputToAxisRange(HID_USAGES.HID_USAGE_POV, ev.value, ev.rangeMin, ev.rangeMax, true);
      var value = GetNormalizedAxisValue(ev.axis, ev.value);
      _vjoy.SetContPov(value, ev.devId, ev.targetId);
    }

    private void VJoyAxisAction(in VJEvent ev)
    {
      //var value = ScaleInputToAxisRange(ev.axis, ev.value, ev.rangeMin, ev.rangeMax, true);
      var value = GetNormalizedAxisValue(ev.axis, ev.value);
      _vjoy.SetAxis(value, ev.devId, ev.axis);
    }

    private void VJoyReleaseButtonLater(ControlType evtype, uint devId, uint targetId)
    {
      System.Threading.Tasks.Task.Run(async delegate {
        await System.Threading.Tasks.Task.Delay(C.BUTTON_CLICK_WAIT_MS);
        if (evtype == ControlType.Button)
          _vjoy.SetBtn(false, devId, targetId);
        else
          _vjoy.SetDiscPov((int)DPovDirection.Center, devId, targetId);
      });
    }

    #endregion

  }
}
