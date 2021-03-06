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
using Stopwatch = System.Diagnostics.Stopwatch;
#if USE_VGEN
using vJoy = vGenInterfaceWrap.vGen;
#else
using vJoy = vJoyInterfaceWrap.vJoy;
#endif

namespace TJoy
{

  // TODO: abstract to support ViGEm devices
  internal class JoyDevice
  {
    public DeviceType DeviceType     => _vjdInfo.deviceType;
    public uint Id                   => _vjdInfo.id;
    public uint Index                => _vjdInfo.index;
    public uint LedNumber            => _vjdInfo.ledNumber;
    public long LastStateUpdate      => _vjdInfo.lastStateUpdate;
    public uint DriverVersion        => _vjdInfo.driverVersion;
    public ushort ButtonCount        => _vjdInfo.nButtons;
    public ushort DiscreteHatCount   => _vjdInfo.nDiscPov;
    public ushort ContinuousHatCount => _vjdInfo.nContPov;
    public ushort AxisCount          => _vjdInfo.nAxes;
    public string TypeName           => _vjdInfo.typeName;
    public IReadOnlyCollection<VJAxisInfo> AxisInfo => _axisInfo.Values;

    public bool IsVJoy    => DeviceType == DeviceType.VJoy;
    public bool IsVXBox   => DeviceType == DeviceType.VXBox;
    public bool IsVbXBox  => DeviceType == DeviceType.VBXBox;
    public bool IsVbDS4   => DeviceType == DeviceType.VBDS4;
    public bool IsGamepad => DeviceType != DeviceType.VJoy;
    public bool IsVBus    => DeviceType == DeviceType.VBXBox || DeviceType == DeviceType.VBDS4;

    public bool IsConnected => CheckConnected();
    public bool DeviceTypeExists => IsVJoy ? _vjoy.vJoyEnabled() : _vjoy.isVBusExist() == VJRESULT.SUCCESS;
    public bool SupportsStateReport => IsGamepad || DriverVersion <= C.VJOY_API_VERSION;

    public string Name { get; set; }  // by default it's TypeName + " " + Index

    //////////////////////////////////

    private readonly vJoy _vjoy;
    private readonly ILogger _logger;
    private VJDeviceInfo _vjdInfo = new();  // info about current device
    private readonly object _devInfoStructLock = new();
    private readonly Dictionary<HID_USAGES, VJAxisInfo> _axisInfo = new();

    public JoyDevice(ILoggerFactory factory, uint deviceId, DeviceType deviceType = DeviceType.None)
    {
      if (deviceType == DeviceType.None)
        _vjdInfo.deviceType = Util.DeviceIdToType(deviceId);
      else
        _vjdInfo.deviceType = deviceType;
      if (_vjdInfo.deviceType == DeviceType.None)
        throw new ArgumentException("Unknown DeviceType either from deviceId or deviceType argument.", nameof(deviceType));

      _vjoy = new();
      _vjdInfo.id = deviceId;
      _vjdInfo.index = deviceId - (uint)_vjdInfo.deviceType;
      _vjdInfo.typeName = Util.DeviceTypeName(_vjdInfo.deviceType);
      Name = TypeName + " " + Index;

      _logger = factory?.CreateLogger($"{typeof(JoyDevice)}.{TypeName}.{Index}") ?? throw new ArgumentNullException(nameof(factory));
    }

    public bool CheckConnected() =>
        IsDeviceIdValid(Id) && _vjoy?.GetVJDStatus(Id) == VJDSTATUS.VJD_STAT_OWN;

    public bool IsDeviceIdValid(uint devId) =>
        devId > 0 && devId - (uint)DeviceType <= Util.MaxDevices(DeviceType);

    public VJDeviceInfo DeviceInfo() {
      VJDeviceInfo info;
      lock (_devInfoStructLock) {
        info = _vjdInfo;
      }
      return info;
    }

    public VJDState StateReport()
    {
      VJDState state;
      lock (_devInfoStructLock) {
        state = _vjdInfo.state;
      }
      return state;
    }

    public bool CheckStatus()
    {
      VJDSTATUS status = _vjoy.GetVJDStatus(Id);
      switch (status) {
        case VJDSTATUS.VJD_STAT_OWN:
          _logger.LogInformation("Device {name} is already owned by this feeder", Name);
          return true;
        case VJDSTATUS.VJD_STAT_FREE:
          _logger.LogInformation("Device {name} is free!", Name);
          return true;
        case VJDSTATUS.VJD_STAT_BUSY:
          _logger.LogWarning("Device {name} is already owned by another feeder. Cannot continue.", Name);
          return false;
        case VJDSTATUS.VJD_STAT_MISS:
          _logger.LogWarning("Device {name} is not installed or disabled. Cannot continue.", Name);
          return false;
        default:
          _logger.LogWarning("Device {name} general error. Cannot continue.", Name);
          return false;
      };
    }

    public bool Connect()
    {
      if (!IsDeviceIdValid(Id))
        return false;

      if (!DeviceTypeExists) {
        _logger.LogError($"Driver not installed or enabled.");
        return false;
      }

      // Get the state of the requested device
      if (!CheckStatus())
        return false;

      // Acquire the target
      if (!CheckConnected()) {
        int hDev = 0;
        var res = _vjoy.AcquireDev(_vjdInfo.index, IsVJoy ? VGEN_DEV_TYPE.vJoy : VGEN_DEV_TYPE.vXbox, ref hDev);
        if (res == VJRESULT.UNSUCCESSFUL) {
          // workaround issue with attempting to re-connect right after disconnect will first return wrong result.
          System.Threading.Thread.Yield();
          if (CheckConnected())
            res = VJRESULT.SUCCESS;
        }
        if (res != VJRESULT.SUCCESS && res != VJRESULT.DEVICE_ALREADY_ATTACHED) {
          _logger.LogWarning($"Connect() failed with error: {res}.");
          return false;
        }
      }

      _logger.LogDebug($"Acquired device {Name}.");
      LoadDeviceInfo();
      return true;
    }

    public void RelinquishDevice()
    {
      if (CheckConnected()) {
        _vjoy.RelinquishVJD(Id);
        _logger.LogDebug($"Relinquished device {Name}.");
      }
    }

    private void LoadDeviceInfo()
    {
      // Check and log device capabilities
      if (IsVJoy) {
        _vjdInfo.driverVersion = (uint)_vjoy.GetvJoyVersion();
        _logger.LogDebug($"vJoy driver version {_vjdInfo.driverVersion:X}.");
      }
      else if (IsVXBox) {
        _vjdInfo.driverVersion = _vjoy.GetVBusVersion();
        _logger.LogDebug($"vXBus driver version {_vjdInfo.driverVersion:X}.");
        if (_vjoy.GetLedNumber(_vjdInfo.index, ref _vjdInfo.ledNumber) is var res && res != VJRESULT.SUCCESS) {
          _logger.LogWarning($"GetLedNumber() failed with error: {res}.");
          _vjdInfo.ledNumber = 0;
        }
      }
      // Check which axes are supported
      _axisInfo.Clear();
      foreach (var axe in Enum.GetValues<HID_USAGES>()) {
        if (_axisInfo.ContainsKey(axe))
          continue;  // skip duplicates/aliases
        int minval = 0, maxval = 0;
        if (_vjoy.GetVJDAxisExist(_vjdInfo.id, axe)) {
          _vjoy.GetVJDAxisRange(_vjdInfo.id, axe, ref minval, ref maxval);
          _axisInfo.Add(axe, new VJAxisInfo { usage = axe, minValue = minval, maxValue = maxval });
        }
      }
      _vjdInfo.nAxes = (ushort)_axisInfo.Count;
      // Get the number of buttons and POV Hat switches
      _vjdInfo.nButtons = (ushort)_vjoy.GetVJDButtonNumber(_vjdInfo.id);
      _vjdInfo.nContPov = (ushort)_vjoy.GetVJDContPovNumber(_vjdInfo.id);
      _vjdInfo.nDiscPov = (ushort)_vjoy.GetVJDDiscPovNumber(_vjdInfo.id);

      //RefreshState();  // debug
    }

    public string GetDeviceCapabilitiesReport()
    {
      var vjid = _vjdInfo.id;
      HID_USAGES lastId = 0;
      var capsReport = new System.Text.StringBuilder();
      capsReport.Append($"\n{Name} Device capabilities:\n");
      // we don't use the stored axis info for this loop because we want to report any "missing" axes as well
      foreach (var axe in Enum.GetValues<HID_USAGES>()) {
        if (axe == lastId)
          continue;  // skip duplicates/aliases
        lastId = axe;
        int minval = 0, maxval = 0;
        bool exists = _vjoy.GetVJDAxisExist(vjid, axe);
        if (exists)
          _vjoy.GetVJDAxisRange(vjid, axe, ref minval, ref maxval);
        capsReport.Append($"Axis {Util.AxisName(DeviceType, axe),-16}{(exists ? "Yes" : "No ")}\trange: {minval} - {maxval}\n");
      }
      capsReport.Append($"Number of buttons           {ButtonCount}\n");
      capsReport.Append($"Number of Continuous POVs   {ContinuousHatCount}\n");
      capsReport.Append($"Number of Discrete POVs     {DiscreteHatCount}\n");

      return capsReport.ToString();
    }

    public void ResetDevice()
    {
      if (IsGamepad) {
        if (_vjoy.ResetController(Index) is var res && res != VJRESULT.SUCCESS)
          _logger.LogWarning($"ResetController() returned error: {res}.");
        return;
      }
      lock (_devInfoStructLock) {
        foreach (var axe in _axisInfo.Values) {
          int val = C.AXES_RESET_TO_MIN.Contains(axe.usage) ? axe.minValue : (axe.maxValue - axe.minValue) / 2;
          _vjoy.SetAxis(val, _vjdInfo.id, axe.usage);
        }
        _vjoy.GetPosition(_vjdInfo.id, ref _vjdInfo.state.vJoyState);
        _vjdInfo.state.vJoyState.bHats = 0xFFFFFFFF;
        _vjdInfo.state.vJoyState.bHatsEx1 = 0xFFFFFFFF;
        _vjdInfo.state.vJoyState.bHatsEx2 = 0xFFFFFFFF;
        _vjdInfo.state.vJoyState.bHatsEx3 = 0xFFFFFFFF;
        _vjdInfo.state.vJoyState.Buttons = 0;
        _vjdInfo.state.vJoyState.ButtonsEx1 = 0;
        _vjdInfo.state.vJoyState.ButtonsEx2 = 0;
        _vjdInfo.state.vJoyState.ButtonsEx3 = 0;
        _vjoy.UpdateVJD(_vjdInfo.id, ref _vjdInfo.state.vJoyState);
        _vjdInfo.lastStateUpdate = Stopwatch.GetTimestamp();
      }
    }

    public bool RefreshState()
    {
      if (!SupportsStateReport)
        return false;
      VJRESULT res;
      try {
        lock (_devInfoStructLock) {
          if (IsVJoy)
            res = _vjoy.GetPosition(_vjdInfo.id, ref _vjdInfo.state.vJoyState);
          else
            res = _vjoy.GetPosition(_vjdInfo.id, ref _vjdInfo.state.xInputState.Gamepad);
          //res = vJoy.GetXInputState((uint)_vjdInfo.ledNumber - 1, ref _vjdInfo.state.xInputState);
        }
        _vjdInfo.lastStateUpdate = Stopwatch.GetTimestamp();
        if (res != VJRESULT.SUCCESS)
          _logger.LogWarning($"GetPostion() returned an error: {res}.");
        //_logger.LogDebug("res: {0}\n{1}", res, System.Text.Json.JsonSerializer.Serialize(_vjoyInfo.state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception while trying to GetPostion().");
        return false;
      }
      return res == VJRESULT.SUCCESS;
    }

    public bool HasDeviceAxis(HID_USAGES axis)
    {
      return _axisInfo.ContainsKey(axis);
    }

    // If the axis doesn't exist in the current device this returns false and
    // populates the info struct with general defaults for this device & axis type
    // from GetDefaultAxisRange().
    public bool TryGetAxisInfo(HID_USAGES axis, out VJAxisInfo info) {
      if (!_axisInfo.TryGetValue(axis, out info)) {
        info.usage = axis;
        Util.GetDefaultAxisRange(axis, IsVJoy, out info.minValue, out info.maxValue);
        return false;
      }
      return true;
    }

    // scale from a variable % range (inMin >= value <= inMax) to joystick axis range (axeMin >= value <= axeMax)
    // set `normAxis` to return the normalized value which is wrapped to fit within the allowed range (see also GetNormalizedAxisValue())
    public int ScaleInputToAxisRange(HID_USAGES axis, float value, int inMin, int inMax, bool normAxis = false)
    {
      if (axis == HID_USAGES.HID_USAGE_POV && value == -1)
        return -1;

      _ = TryGetAxisInfo(axis, out VJAxisInfo info);
      float ret = value;
      if (inMax - inMin != 100)
        ret = Util.ConvertRange(ret, 0, 100, inMin, inMax);
      ret = Util.PercentOfRange(ret, info.minValue, info.maxValue);
      if (axis == HID_USAGES.HID_USAGE_POV)
        ret -= (C.VJ_CPOV_MAX_VALUE / 2);
      if (normAxis)
        ret = GetNormalizedAxisValue(ref info, (int)ret);
      return (int)ret;
    }

    // scale joystick axis range (axeMin >= value <= axeMax) onto a variable %age range (outMin >= value <= outMax)
    public int ScaleAxisToInputRange(HID_USAGES axis, int value, int outMin, int outMax)
    {
      _ = TryGetAxisInfo(axis, out VJAxisInfo info);
      int ret = value;
      if (axis == HID_USAGES.HID_USAGE_POV)
        ret = Util.ModAxis(ret + (info.maxValue / 2), info.minValue, info.maxValue);
      ret = Util.RangeValueToPercent(ret, info.minValue, info.maxValue);
      if (outMax - outMin != 100)
        return Util.ConvertRange(ret, outMin, outMax, 0, 100, true);
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
      if (info.usage == HID_USAGES.HID_USAGE_POV && IsVJoy)
        info.minValue = -1;
      return Util.ModAxis(value, info.minValue, info.maxValue);
    }

    public DPovDirection ScaleAxisToDpov(int value, DPovDirection dpovDir)
    {
      if (IsVJoy)
        return Util.SliderRangeToDpovRange(value, dpovDir);
      return Util.SliderRangeToDPadRange(value, dpovDir);
    }

    public int ScaleDpovToAxis(int value, DPovDirection dpovDir)
    {
      if (IsVJoy)
        return Util.DPovRange2SliderRange(value, dpovDir);
      return Util.DPadRange2SliderRange(value, dpovDir);
    }

    public DPovDirection ConvertCpovToDpov(float value)
    {
      if (IsVJoy)
        return Util.CPovToDPov((int)value);
      return Util.CPovToDPad((int)value);
    }

    public float ConvertDpovToCpov(DPovDirection dpovDir)
    {
      return Util.DPadToCPov(dpovDir);
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
      _vjoy.SetContPov(ev.value, ev.devId, ev.targetId);
    }

    private void VJoyAxisAction(in VJEvent ev)
    {
      _vjoy.SetAxis(ev.value, ev.devId, ev.axis);
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
