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
    public int Handle                => _vjdInfo.handle;
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
    public bool IsXBox    => DeviceType == DeviceType.VXBox || DeviceType == DeviceType.VBXBox;

    public bool IsConnected => CheckConnected();
    public bool DeviceTypeExists => IsVJoy ? vJoy.IsDevTypeSupported(VGEN_DEV_TYPE.vJoy) :
                                    IsVXBox ? vJoy.IsDevTypeSupported(VGEN_DEV_TYPE.vXbox) :
                                    IsVBus && vJoy.IsDevTypeSupported(VGEN_DEV_TYPE.vgeXbox);
    public bool SupportsStateReport => IsGamepad || DriverVersion <= C.VJOY_API_VERSION;

    public string Name { get; set; }  // by default it's TypeName + " " + Index

    //////////////////////////////////

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

      _vjdInfo.id = deviceId;
      _vjdInfo.index = deviceId - (uint)_vjdInfo.deviceType;
      _vjdInfo.typeName = Util.DeviceTypeName(_vjdInfo.deviceType);
      Name = TypeName + " " + Index;

      _logger = factory?.CreateLogger($"{typeof(JoyDevice)}.{TypeName}.{Index}") ?? throw new ArgumentNullException(nameof(factory));
    }

    public bool CheckConnected() =>
        Handle > 0 && vJoy.GetDevStatus(Handle) == VJDSTATUS.VJD_STAT_OWN;

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
      VJDSTATUS status = vJoy.GetDevTypeStatus((VGEN_DEV_TYPE)DeviceType, Index);
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
        var res = vJoy.AcquireDev(_vjdInfo.index, (VGEN_DEV_TYPE)DeviceType, ref _vjdInfo.handle);

        if (res != VJRESULT.SUCCESS && res != VJRESULT.DEVICE_ALREADY_ATTACHED) {
          _logger.LogWarning("Connect() failed with error: {res:X}", res);
          return false;
        }
      }

      _logger.LogDebug("Acquired device {name}.", Name);
      LoadDeviceInfo();
      return true;
    }

    public void RelinquishDevice()
    {
      if (CheckConnected()) {
        var res = vJoy.RelinquishDev(Handle);
        _vjdInfo.handle = 0;
        if (res == VJRESULT.SUCCESS)
          _logger.LogDebug("Relinquished device {name}.", Name);
        else
          _logger.LogWarning("Relinquish device {name} returned error: {res:X}", Name, res);
      }
    }

    private void LoadDeviceInfo()
    {
      // Check and log device capabilities
      _vjdInfo.driverVersion = vJoy.GetDriverVersion((VGEN_DEV_TYPE)DeviceType);
      _logger.LogDebug("{name} device driver version {v:X}.", TypeName, _vjdInfo.driverVersion);

      if (IsXBox) {
        //vJoy.DeviceInfo devInfo = new();
        //if (vJoy.GetDevInfo(Handle, ref devInfo) is var res && res == VJRESULT.SUCCESS) {
        //  _vjdInfo.ledNumber = devInfo.LedNumber;
        //  _vjdInfo.colorBar = devInfo.ColorBar;
        //  _logger.LogDebug("Got DeviceInfo for {device}: VID: {vId:X}; PID: {pId:X}; Serial: {serial}; LED: {led}; Color: {color:X};",
        //    Name, devInfo.VendId, devInfo.ProdId, devInfo.Serial, (uint)_vjdInfo.ledNumber, devInfo.ColorBar);
        //}
        //else {
        //  _logger.LogWarning("GetDevInfo() failed with error: {res:X}", res);
        //  _vjdInfo.ledNumber = 0;
        //}

        uint devNum = 0;
        if (vJoy.GetDevNumber(Handle, ref devNum) is var res && res == VJRESULT.SUCCESS) {
          _vjdInfo.ledNumber = (byte)devNum;
          _logger.LogDebug("Got DeviceNumber for {device}: {devNum}", Name, devNum);
        }
        else {
          _logger.LogWarning("GetDevNumber() failed with error: {res:X}", res);
        }
      }

      // Check which axes are supported
      _axisInfo.Clear();
      foreach (var axe in Enum.GetValues<HID_USAGES>()) {
        if (_axisInfo.ContainsKey(axe))
          continue;  // skip duplicates/aliases
        bool exists = false;
        int minval = 0, maxval = 0;
        if (vJoy.isAxisExist(Handle, axe, ref exists) == VJRESULT.SUCCESS && exists) {
          vJoy.GetDevAxisRange(Handle, axe, ref minval, ref maxval);
          _axisInfo.Add(axe, new VJAxisInfo { usage = axe, minValue = minval, maxValue = maxval });
        }
      }
      _vjdInfo.nAxes = (ushort)_axisInfo.Count;
      // Get the number of buttons and POV Hat switches
      vJoy.GetDevButtonN(Handle, ref _vjdInfo.nButtons);
      vJoy.GetDevHatN(Handle, VGEN_POV_TYPE.PovTypeContinuous, ref _vjdInfo.nContPov);
      vJoy.GetDevHatN(Handle, VGEN_POV_TYPE.PovTypeDiscrete, ref _vjdInfo.nDiscPov);
      //RefreshState();  // debug
    }

    public string GetDeviceCapabilitiesReport()
    {
      HID_USAGES lastId = 0;
      var capsReport = new System.Text.StringBuilder();
      capsReport.Append($"\n{Name} Device capabilities:\n");
      // we don't use the stored axis info for this loop because we want to report any "missing" axes as well
      foreach (var axe in Enum.GetValues<HID_USAGES>()) {
        if (axe == lastId)
          continue;  // skip duplicates/aliases
        lastId = axe;
        int minval = 0, maxval = 0;
        bool exists = false;
        vJoy.isAxisExist(Handle, axe, ref exists);
        if (exists)
          vJoy.GetDevAxisRange(Handle, axe, ref minval, ref maxval);
        capsReport.Append($"Axis {Util.AxisName(DeviceType, axe),-16}{(exists ? "Yes" : "No ")}\trange: {minval} - {maxval}\n");
      }
      capsReport.Append($"Number of buttons           {ButtonCount}\n");
      capsReport.Append($"Number of Continuous POVs   {ContinuousHatCount}\n");
      capsReport.Append($"Number of Discrete POVs     {DiscreteHatCount}\n");

      return capsReport.ToString();
    }

    public void ResetDevice()
    {
      if (vJoy.ResetDevPositions(Handle) is var res && res == VJRESULT.SUCCESS)
        _logger.LogInformation("Device reset completed for {name}.", Name);
      else
        _logger.LogWarning("ResetDevPositions() on {name} returned error: {res:X}", Name, res);
    }

    public bool RefreshState()
    {
      if (!SupportsStateReport)
        return false;
      VJRESULT res;
      try {
        lock (_devInfoStructLock) {
          if (IsVJoy)
            res = vJoy.GetPosition(Handle, ref _vjdInfo.state.vJoyState);
          else if (IsVbDS4)
            res = vJoy.GetPosition(Handle, ref _vjdInfo.state.DS4State);
          else
            res = vJoy.GetPosition(Handle, ref _vjdInfo.state.xInputState.Gamepad);
            //res = vJoy.GetXInputState((uint)_vjdInfo.ledNumber - 1, ref _vjdInfo.state.xInputState);
        }
        _vjdInfo.lastStateUpdate = Stopwatch.GetTimestamp();
        if (res != VJRESULT.SUCCESS)
          _logger.LogWarning($"GetPosition() returned an error: {res}.");
        //_logger.LogDebug("res: {0}\n{1}", res, System.Text.Json.JsonSerializer.Serialize(_vjoyInfo.state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception while trying to GetPosition().");
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
        Util.GetDefaultAxisRange(DeviceType, axis, out info.minValue, out info.maxValue);
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

    // scale joystick axis range (axeMin >= value <= axeMax) onto a variable percentage range (outMin >= value <= outMax)
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

    // returns a value which is normalized to fit withing the given axis' actual range (value is wrapped as needed to conform)
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
          vJoy.SetDevButton(Handle, ev.targetId, true);
          break;
        case ButtonAction.Up:
          vJoy.SetDevButton(Handle, ev.targetId, false);
          break;
        case ButtonAction.Click:
          vJoy.SetDevButton(Handle, ev.targetId, true);
          VJoyReleaseButtonLater(ev.type, ev.targetId);
          break;
        default:
          if (ev.value == 0 || ev.value == 1)
            vJoy.SetDevButton(Handle, ev.targetId, ev.value == 1);
          return;
      }
    }

    private void VJoyDPovAction(in VJEvent ev)
    {
      switch (ev.btnAction) {
        case ButtonAction.Down:
          vJoy.SetDevDiscPov(Handle, (byte)ev.targetId, (DPOV_DIRECTION)ev.dpovDir);
          break;
        case ButtonAction.Up:
          vJoy.SetDevDiscPov(Handle, (byte)ev.targetId, DPOV_DIRECTION.DPOV_Center);
          //_vjoy.SetDiscPov((int)DPovDirection.Center, ev.devId, ev.targetId);
          break;
        case ButtonAction.Click:
          vJoy.SetDevDiscPov(Handle, (byte)ev.targetId, (DPOV_DIRECTION)ev.dpovDir);
          VJoyReleaseButtonLater(ev.type, ev.targetId);
          break;
        default:
          if (ev.value > -2 && ev.value < 4)
            vJoy.SetDevDiscPov(Handle, (byte)ev.targetId, (DPOV_DIRECTION)ev.value);
          return;
      }
    }

    private void VJoyCPovAction(in VJEvent ev)
    {
      vJoy.SetDevContPov(Handle, (byte)ev.targetId, unchecked((uint)ev.value));
    }

    private void VJoyAxisAction(in VJEvent ev)
    {
      //_vjoy.SetAxis(ev.value, ev.devId, ev.axis);
      vJoy.SetDevAxis(Handle, ev.axis, ev.value);
    }

    private void VJoyReleaseButtonLater(ControlType evtype, uint targetId)
    {
      System.Threading.Tasks.Task.Run(async delegate {
        await System.Threading.Tasks.Task.Delay(C.BUTTON_CLICK_WAIT_MS);
        if (evtype == ControlType.Button)
          vJoy.SetDevButton(Handle, targetId, false);
        else
          vJoy.SetDevDiscPov(Handle, (byte)targetId, DPOV_DIRECTION.DPOV_Center);
      });
    }

  #endregion

  }
}
