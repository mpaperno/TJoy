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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TJoy.Constants;
using TJoy.Enums;
using TJoy.Types;
using TJoy.Utilities;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;
using TouchPortalSDK.Messages.Models.Enums;
using Stopwatch = System.Diagnostics.Stopwatch;
//using Timer = System.Threading.Timer;
#if USE_VGEN
using vJoy = vGenInterfaceWrap.vGen;
#else
using vJoy = vJoyInterfaceWrap.vJoy;
#endif


namespace TJoy.TouchPortalPlugin
{
  public class Plugin : ITouchPortalEventHandler
  {
    public string PluginId => C.PLUGIN_ID;   // for ITouchPortalEventHandler

    private uint DefaultDevId => _settings.DefaultDeviceId;

    private readonly ILogger<Plugin> _logger;
    private readonly ITouchPortalClient _client;

    private readonly Task _eventWorkerTask;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly CancellationToken _shutdownToken;
    private readonly ManualResetEventSlim _eventQueueReadyEvent = new();

    private Task _stateUpdateTask = null;
    private CancellationTokenSource _stateTaskCts = null;
    private CancellationToken _stateTaskShutdownToken;

    private readonly Dictionary<uint, JoyDevice> _devices = new();
    private readonly PluginSettings _settings = new();
    private static readonly System.Data.DataTable _expressionEvaluator = new();  // used to evaluate basic math in action data
    private readonly ConcurrentQueue<DataContainerEventBase> _eventQ = new();
    private readonly ConcurrentDictionary<string, ConnectorTrackingData> _connectorsDict = new();
    private readonly ConcurrentDictionary<string, string> _connectorsLongToShortMap = new();
    private readonly Dictionary<string, int> _joystickStatesDict = new();
    private readonly ILoggerFactory _loggerFactory;

    // automagic contructor arguments... woot!?
    public Plugin(ITouchPortalClientFactory clientFactory, ILoggerFactory logFactory)
    {
      _logger = logFactory?.CreateLogger<Plugin>() ?? throw new ArgumentNullException(nameof(logFactory));
      _client = clientFactory?.Create(this) ?? throw new ArgumentNullException(nameof(clientFactory));
      _loggerFactory = logFactory;

      _eventWorkerTask = new(ProcessEventQueue);
      _eventWorkerTask.ConfigureAwait(false);
      _shutdownToken = _shutdownCts.Token;

      TouchPortalOptions.ActionDataIdSeparator = '.';
      TouchPortalOptions.ValidateCommandParameters = false;

      //Environment.SetEnvironmentVariable("VJOYINTERFACELOGLEVEL", "1");  // uncomment to enable vJoy SDK logging (rather verbose)
      //Environment.SetEnvironmentVariable("VJOYINTERFACELOGFILE", "logs\\vjoy.log");
    }

    #region Core Process                   ///////////////////////////////////////////

    public void Run()
    {
      // register ctrl-c exit handler first
      Console.CancelKeyPress += (_, _) => {
        _logger.LogInformation("Quitting due to keyboard interrupt.");
        Quit();
        Environment.Exit(0);
      };

      vJoy vjoy = new();
      if (vjoy.vJoyEnabled()) {
        _settings.HaveVJoy = true;
        // Get the driver attributes (Vendor ID, Product ID, Version Number)
        _logger.LogInformation("Virtual Joystick Driver Vendor: {0}, Product: {1}, Version: {2}",
          vjoy.GetvJoyManufacturerString(), vjoy.GetvJoyProductString(), vjoy.GetvJoySerialNumberString());

        // Test if DLL matches the driver
        UInt32 dllVer = 0;
        UInt32 drivVer = 0;
        if (vjoy.DriverMatch(ref dllVer, ref drivVer))
          _logger.LogInformation($"Version of Driver Matches DLL Version ({dllVer:X})");
        else
          _logger.LogWarning($"Version of Driver ({drivVer:X}) does NOT match DLL Version ({dllVer:X})");   // hope they check this log to see why their computer exploded.... lol

        // subscribe to device config change events
        vjoy.RegisterRemovalCB(VJoyDeviceChangedCB, null);
        DetectVJoyDevices();
      }
#if USE_VGEN
      _settings.HaveVXbox = vjoy.isVBusExist() == VJRESULT.SUCCESS;
#endif
#if USE_VIGEM
      try {
        vBus = new ViGEmClient();
        _settings.HaveVBus = true;
        _logger.LogInformation($"Found ViGEm Bus driver {}.");
        vBus.Dispose();
      }
      catch (Exception e) {
        _logger.LogWarning(e, "Failed to create ViGEmClient, driver not installed or already in use.");
      }
#endif

      _eventWorkerTask.Start();

      //Connect to Touch Portal:
      if (!_client.Connect()) {
        _logger.LogError("Could not connect to Touch Portal, quitting now.");
        Quit();
        return;
      }

      UpdateDeviceChoices();   // send list of initial devices

      // at this point the client's socket it spinning an event loop and on this thread we're just reacting to TP events via the handlers.
      // the socket/client fire event callbacks synchronously from a separate message delivery task.
    }

    private bool _disposed = false;

    public void Quit()
    {
      if (_disposed)
        return;
      _disposed = true;
      _logger?.LogInformation("Shutting down...");

      _eventQ.Clear();       // prevent further events
      StopStatusDataTask();  // if it's running
      _logger?.LogDebug("Removing all devices...");
      RemoveAllDevices();    // before stopping worker
      //ClearAllTpJoystickStates();  // not sure about this.. or maybe we only remove states when device disconnects

      _logger?.LogDebug("Shutting down the event worker...");
      _shutdownCts?.Cancel();
      var sw = Stopwatch.StartNew();
      while (_eventWorkerTask.Status == TaskStatus.Running && sw.ElapsedMilliseconds < 5000)
        Thread.Sleep(1);
      if (sw.ElapsedMilliseconds > 5000)
        _logger.LogWarning("Event worker timed out!");

      _logger?.LogDebug("Object disposal...");
      _eventWorkerTask?.Dispose();
      _shutdownCts?.Dispose();
      _eventQueueReadyEvent?.Dispose();
      _expressionEvaluator?.Dispose();

      _logger?.LogInformation("All finished, shutting down TP client now.");
      if (!_settings.ClosedByTp && (_client?.IsConnected ?? false)) {
        try { _client.Close(); }  // exits the event loop keeping us alive
        catch (Exception) { /* ignore */ }
      }
    }

    #endregion  Core Process

    #region Helpers                  ///////////////////////////////////////////

    private DeviceType DefaultDeviceType(uint devId)
    {
      if (devId == 0)
        return DeviceType.None;
      if (DefaultDevId > 0)
        return Util.DeviceIdToType(DefaultDevId);
      if (_settings.AvailableVJoyDevs.Contains(devId))
        return DeviceType.VJoy;
      if (_settings.HaveVXbox && devId < 5)
        return DeviceType.VXBox;
      if (_settings.HaveVBus && devId < 5)
        return DeviceType.VBXBox;
      return DeviceType.None;
    }

    private uint GetFullDeviceIdOrDefault(string devIdStr)
    {
      if (string.IsNullOrWhiteSpace(devIdStr) || devIdStr.Equals(C.IDSTR_DEVID_DFLT, StringComparison.OrdinalIgnoreCase))
        return DefaultDevId;
      if (Util.TryParseDeviceType(devIdStr, out DeviceType type, out uint id)) {
        if (type == DeviceType.None && (type = DefaultDeviceType(id)) == DeviceType.None)
          return 0;
        return id + (uint)type;
      }
      return 0;
    }

    private bool CheckDeviceId(uint vjid)
    {
      if (vjid == 0) {
        _logger.LogWarning($"Invalid device ID 0 (zero).");
        return false;
      }
      DeviceType devType = Util.DeviceIdToType(vjid);
      if (devType == DeviceType.None) {
        _logger.LogWarning($"Uknown Device Type for Device ID {vjid}.");
        return false;
      }
      vjid -= (uint)devType;
      if (vjid > Util.MaxDevices(devType)) {
        _logger.LogWarning($"{Util.DeviceTypeName(devType)} Device ID {vjid} is out of range (1 - {Util.MaxDevices(devType)}).");
        return false;
      }
      if ((devType == DeviceType.VJoy && !_settings.HaveVJoy) ||
          (devType == DeviceType.VXBox && !_settings.HaveVXbox) ||
          ((devType == DeviceType.VBXBox || devType == DeviceType.VBDS4) && !_settings.HaveVBus)) {
        _logger.LogWarning($"Driver for device type {Util.DeviceTypeName(devType)} is apparently not installed.");
        return false;
      }
      return true;
    }

    private bool TryEvaluateValue(string strValue, out int value)
    {
      value = 0;
      if (TryEvaluateValue(strValue, out float val)) {
        value = (int)Math.Round(val);
        return true;
      }
      return false;
    }

    private bool TryEvaluateValue(string strValue, out float value)
    {
      value = 0;
      try {
        value = (float)Convert.ToDecimal(_expressionEvaluator.Compute(strValue, null));
      }
      catch (Exception e) {
        _logger.LogWarning(e, $"Failed to convert Action data value '{strValue}' to numeric value.");
        return false;
      }
      return true;
    }

    // Gets a vJoy device value for the reset attributes specified in an action/connector.
    // Returns -2 if no reset is to be done.
    private int GetResetValueFromEvent(DataContainerEventBase message, string actId, ControlType evtype, int startValue = 0)
    {
      // Get reset type and value
      var isConn = message.GetType() == typeof(ConnectorChangeEvent);
      var rstStr = message.GetValue(C.IDSTR_RESET_TYP)?.Replace(" ", string.Empty) ?? "None";
      var rvalStr = message.GetValue(C.IDSTR_RESET_VAL) ?? "-2";
      if (Enum.TryParse(rstStr, true, out CtrlResetMethod rstType) && rstType != CtrlResetMethod.None && TryEvaluateValue(rvalStr, out int customVal))
        return Util.GetResetValueForType(rstType, evtype, customVal, startValue);
      return -2;
    }

    private bool RefreshDeviceStateIfNeeded(JoyDevice device)
    {
      if (Util.TicksToSecs(Stopwatch.GetTimestamp() - device.LastStateUpdate) < C.VJD_STATE_MAX_AGE_SEC)
        return true;
      if (device.RefreshState())
        return true;
      if (device.SupportsStateReport)
        RemoveDevice(device.Id);
      return false;
    }

    private void UpdateTPChoicesFromListId(string listId, string[] values, string instanceId = null, string target = C.IDSTR_TARGET_ID)
    {
      UpdateTPChoices($"{listId[0..^C.IDSTR_DEVICE_ID.Length]}{target}", values, instanceId);
    }

    #endregion Helpers

    #region VJD Interface           ///////////////////////////////////////////

    private bool VJoyConnected(uint vjid) => Device(vjid)?.IsConnected ?? false;

    private JoyDevice Device(uint id = 0)
    {
      if (id == 0)
        id = DefaultDevId;
      return _devices.GetValueOrDefault(id, null);
    }

    private bool TryGetDevice(uint id, out JoyDevice dev)
    {
      return _devices.TryGetValue(id, out dev);
    }

    private void SetDefaultDevice(uint vjid)
    {
      if (_settings.DefaultDeviceId == vjid)
        return;

      if (vjid != 0 && !CheckDeviceId(vjid))
        vjid = 0;

      // Relinquish old device, if any.
      if (_settings.DefaultDeviceId > 0)
        RemoveDevice(_settings.DefaultDeviceId);

      _settings.DefaultDeviceId = vjid;
      if (vjid != 0)
        AddDevice(vjid);
    }

    private JoyDevice AddDevice(uint vjid)
    {
      if (_devices.ContainsKey(vjid))
        return Device(vjid);

      if (!CheckDeviceId(vjid))
        return null;

      JoyDevice device;
      try {
        // Attempt creation
        device = new JoyDevice(_loggerFactory, vjid);
        // Attempt connection
        if (!device.Connect()) {
          _logger.LogError($"Failed to acquire {device.Name}.");
          return null;
        }
      }
      catch (Exception e) {
        _logger.LogError(e, "Failed to create JoyDevice for device number {0}.", vjid);
        return null;
      }

      _devices.Add(vjid, device);
      _logger.LogInformation($"Acquired: {device.Name}");
      _logger.LogInformation(device.GetDeviceCapabilitiesReport());

      UpdateTPState(C.IDSTR_STATE_LAST_CONNECT, $"{device.Name}");
      if (device.IsGamepad)
        UpdateTPState($"{C.IDSTR_GAMEPAD}.{device.Index}.{C.IDSTR_STATE_GAMEPAD_LED}", device.LedNumber.ToString());
      UpdateDeviceConnectors(vjid);

      device.ResetDevice();
      StartStatusDataTask();  // if needed
      UpdateTPState(C.IDSTR_STATE_LAST_CONNECT, "");
      return device;
    }

    private void RemoveDevice(uint vjid)
    {
      if (!TryGetDevice(vjid, out JoyDevice oldDev))
        return;
      _logger.LogDebug($"Relinquishing Device ID {vjid}.");
      oldDev.RelinquishDevice();
      _devices.Remove(vjid);
      UpdateTPState(C.IDSTR_STATE_LAST_DISCNCT, $"{oldDev.Name}");
      if (oldDev.IsGamepad)
        UpdateTPState($"{C.IDSTR_GAMEPAD}.{oldDev.Index}.{C.IDSTR_STATE_GAMEPAD_LED}", "0");
      UpdateTPState(C.IDSTR_STATE_LAST_DISCNCT, "");
    }

    private void RemoveAllDevices(DeviceType devType = DeviceType.None)
    {
      foreach (var device in _devices.Values) {
        if (devType == DeviceType.None || devType == device.DeviceType)
          RemoveDevice(device.Id);
      }
    }

    private void ResetDevice(uint vjid)
    {
      Device(vjid)?.ResetDevice();
    }

    private void RefreshDeviceState(uint vjid)
    {
      if (TryGetDevice(vjid, out var dev))
        SendStateReport(dev);
    }

    private void ForceUnplugDevice(uint vjid)
    {
      DeviceType devType = Util.DeviceIdToType(vjid);
      if (devType == DeviceType.VJoy)
        return;
      if (devType == DeviceType.VXBox) {
        vJoy vjoy = new();
        var res = vjoy.UnPlugForce(vjid - (uint)devType);
        if (res != VJRESULT.SUCCESS)
          _logger.LogError("Force Unplug of device {0} failed with error code {1}.", vjid, res);
      }
    }

    private void DetectVJoyDevices()
    {
      _settings.AvailableVJoyDevs.Clear();
      if (!_settings.HaveVJoy)
        return;
      try {
        vJoy vjoy = new();
        for (uint i = 1; i <= 16; ++i) {
          if (vjoy.isVJDExists(i))
            _settings.AvailableVJoyDevs.Add(i);
        }
      }
      catch (Exception e) {
        _logger.LogWarning(e, "Something went wrong trying to get the available devices with vJoy.isVJDExists()");
      }
    }

    // vJoy device change notification callback.
    // Note that _all_ driver devices are reloaded whenever any change is applied in the vJ configurator.
    // We disconnect when the first removal notice comes, then try reconnecting to "our" device
    // after the final (!first) arrival notice. From the vJ docs:
    // When a process of vJoy device removal starts, Removed = TRUE and First = TRUE.
    // When a process of vJoy device removal ends, Removed = TRUE and First = FALSE.
    // When a process of vJoy device arrival starts, Removed = FALSE and First = TRUE.
    // When a process of vJoy device arrival ends, Removed = FALSE and First = FALSE
    private void VJoyDeviceChangedCB(bool removed, bool first, object _)
    {
      //_logger.LogDebug($"[VJoyDeviceChangedCB] r:{removed} f:{first} c:{VJoyConnected()}; vjid:{_vjdId}; want: {_settings.VjDeviceId}");
      if (removed != first || (removed && !_devices.Any()))
        return;

      if (removed) {
        RemoveAllDevices(DeviceType.VJoy);
      }
      else {
        DetectVJoyDevices();
        if (Util.DeviceIdToType(DefaultDevId) == DeviceType.VJoy && _settings.AvailableVJoyDevs.Contains(DefaultDevId))
          AddDevice(DefaultDevId);
        UpdateDeviceChoices();
      }
    }

    #endregion  VJD Interface

    #region Joystick values state updates                 ///////////////////////////////////////////////

    private void StartStatusDataTask()
    {
      if (_settings.StateRefreshRate == 0 || !_devices.Any())
        return;
      if (_stateUpdateTask != null)
        StopStatusDataTask();
      _logger?.LogDebug("Starting state updater task...");
      _stateTaskCts = new CancellationTokenSource();
      _stateTaskShutdownToken = _stateTaskCts.Token;
      _stateUpdateTask = Task.Run(VJoyCollectStateDataTask, _stateTaskShutdownToken);
    }

    private void StopStatusDataTask()
    {
      if (_stateUpdateTask == null)
        return;

      _logger?.LogDebug("Shutting down state updater task...");
      _stateTaskCts.Cancel();
      if (!_stateUpdateTask.IsCompleted)
        if (!_stateUpdateTask.Wait(2000, _shutdownToken))
          _logger.LogWarning("State update task timed out!");

      _stateUpdateTask.Dispose();
      _stateUpdateTask = null;
      _stateTaskCts.Dispose();
      _stateTaskCts = null;
    }

    private void ToggletatusDataTask()
    {
      if (_settings.StateRefreshRate > 0 && (_stateUpdateTask == null || _stateUpdateTask.IsCompleted))
        StartStatusDataTask();
      else if (_settings.StateRefreshRate == 0 && _stateUpdateTask != null)
        StopStatusDataTask();
    }

    // Thread pool task
    private async void VJoyCollectStateDataTask()
    {
      _logger.LogDebug("State updater task started.");
      try {
        while (_settings.StateRefreshRate > 0 && !_stateTaskShutdownToken.IsCancellationRequested) {
          foreach (var device in _devices.Values)
            SendStateReport(device);
          await Task.Delay((int)_settings.StateRefreshRate, _stateTaskShutdownToken);
        }
      }
      catch (TaskCanceledException) { /* ignore... why is this an exception anyway? */ }
      catch (ObjectDisposedException) { /* ignore */ }
      catch (Exception e) {
        _logger.LogError("Exception in joystick driver status update thread, cannot continue.", e);
      }
      _logger.LogDebug("State updater task exited.");
    }

    private void SendStateReport(JoyDevice device)
    {
      if (_client == null || device == null || !_client.IsConnected)
        return;
      if (!device.RefreshState()) {
        if (device.SupportsStateReport)
          RemoveDevice(device.Id);  // terminal error
        return;
      }

      VJDeviceInfo devInfo = device.DeviceInfo();
      var axisInfo = device.AxisInfo;
      //_logger.LogDebug(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));

      // Axis
      foreach (VJAxisInfo axe in axisInfo) {
        var val = Util.GetStateReportAxisValue(devInfo.deviceType, devInfo.state, axe.usage);
        if (val < 0)
          continue;
        if (!_settings.StateReportAxisRaw)
          val = Util.RangeValueToPercent(val, axe.minValue, axe.maxValue);
        UpdateTpJoystickState(ControlType.Axis, val, devInfo.typeName, devInfo.index, Util.AxisName(devInfo.deviceType, axe.usage));
      }
      // CPOV
      for (uint i = 1; i <= devInfo.nContPov; ++i) {
        var val = Util.GetStateReportCPovValue(devInfo.deviceType, devInfo.state, i);
        if (val < -1)
          continue;
        if (!_settings.StateReportAxisRaw && val > -1)
          val = Util.RangeValueToPercent(val, C.VJ_CPOV_MIN_VALUE, C.VJ_CPOV_MAX_VALUE);
        UpdateTpJoystickState(ControlType.ContPov, val, devInfo.typeName, devInfo.index, i.ToString());
      }
      // DPOV
      for (uint i = 1; i <= devInfo.nDiscPov; ++i) {
        var val = Util.GetStateReportDPovValue(devInfo.deviceType, devInfo.state, i);
        if (val == DPovDirection.None)
          continue;
        UpdateTpJoystickState(ControlType.DiscPov, (int)val, devInfo.typeName, devInfo.index, i.ToString());
      }
      // Buttons
      if (_settings.MinBtnNumForState > 0 && _settings.MaxBtnNumForState >= _settings.MinBtnNumForState) {
        for (uint i = _settings.MinBtnNumForState, e = Math.Min(_settings.MaxBtnNumForState, devInfo.nButtons); i <= e; ++i) {
          var val = Util.GetStateReporButtonValue(devInfo.deviceType, devInfo.state, i);
          if (val < 0)
            continue;
          UpdateTpJoystickState(ControlType.Button, val, devInfo.typeName, devInfo.index, Util.ButtonName(devInfo.deviceType, i));
        }
      }
    }

    private void UpdateTpJoystickState(ControlType ev, int value, string devName, uint devIndex, string ctrlName)
    {
      string stateId = $"{devName}.{devIndex}.{Util.EventTypeToTpStateId(ev)}.{ctrlName}";
      if (!_joystickStatesDict.TryGetValue(stateId, out int prevValue)) {
        prevValue = -2;
        _joystickStatesDict.Add(stateId, value);
        CreateTPState(stateId, $"{C.PLUGIN_SHORT_NAME} - {devName} {devIndex} - {Util.EventTypeToControlName(ev)} {ctrlName} value", Util.GetDefaultValueForEventType(ev).ToString());
      }
      if (prevValue != value) {
        _joystickStatesDict[stateId] = value;
        if (ev == ControlType.DiscPov)  // send direction enum name instead of value
          UpdateTPState(stateId, ((DPovDirection)value).ToString());
        else
          UpdateTPState(stateId, value.ToString());
      }
    }

    private void ClearAllTpJoystickStates()
    {
      foreach (string key in _joystickStatesDict.Keys)
        RemoveTPState(key);
      _joystickStatesDict.Clear();
    }

    #endregion  Joystick values state updates

    #region List Updaters                ///////////////////////////////////////////////

    private void UpdateDeviceChoices()
    {
      List<string> values = new() { C.IDSTR_DEVID_DFLT };
      string devName;
      if (_settings.HaveVJoy) {
        devName = Util.DeviceTypeName(DeviceType.VJoy);
        foreach (uint devId in _settings.AvailableVJoyDevs)
          values.Add($"{devName} {devId}");
      }
      if (_settings.HaveVXbox) {
        devName = Util.DeviceTypeName(DeviceType.VXBox);
        for (uint i = 1; i <= 4; ++i)
          values.Add($"{devName} {i}");
      }
      if (_settings.HaveVBus) {
        devName = Util.DeviceTypeName(DeviceType.VBXBox);
        for (uint i = 1; i <= 4; ++i)
          values.Add($"{devName} {i}");
        devName = Util.DeviceTypeName(DeviceType.VBDS4);
        for (uint i = 1; i <= 4; ++i)
          values.Add($"{devName} {i}");
      }

      var valarry = values.ToArray();
      foreach (var eltype in new string[] { C.IDSTR_EL_ACTION, C.IDSTR_EL_CONNECTOR }) {
        foreach (var ctrlType in new string[] { C.IDSTR_DEVTYPE_AXIS, C.IDSTR_DEVTYPE_BTN, C.IDSTR_DEVTYPE_CPOV, C.IDSTR_DEVTYPE_DPOV }) {
          var listId = C.PLUGIN_ID + "." + eltype + "." + ctrlType + "." + C.IDSTR_DEVICE_ID;
          UpdateTPChoices(listId, valarry);
          //if (ctrlType == C.IDSTR_DEVTYPE_AXIS)
            //UpdateAxisChoices(DefaultDevId, listId, null);
        }
      }
      // also update the device control action
      UpdateTPChoices(C.PLUGIN_ID + "." + C.IDSTR_EL_ACTION + "." + C.IDSTR_ACTION_DEVICE_CTRL + "." + C.IDSTR_DEVICE_ID, valarry);
      UpdateTPChoices(C.PLUGIN_ID + "." + C.IDSTR_EL_ACTION + "." + C.IDSTR_ACTION_SET_POS + "." + C.IDSTR_DEVICE_ID, valarry);
    }

    string[] GetAxisChoices(uint devId)
    {
      DeviceType devType = Util.DeviceIdToType(devId);
      IReadOnlyCollection<VJAxisInfo> axeInfo;
      if (TryGetDevice(devId, out JoyDevice device))
        axeInfo = device.AxisInfo;
      else
        axeInfo = Util.GetDefaultAxisInfo(devType);
      List<string> values = new();
      foreach (VJAxisInfo axe in axeInfo) {
        if (axe.usage != HID_USAGES.HID_USAGE_POV)
          values.Add($"{Util.AxisName(devType, axe.usage)}");
      }
      return values.ToArray();
    }

    private void UpdateAxisChoices(uint devId, string listId, string instanceId)
    {
      UpdateTPChoicesFromListId(listId, GetAxisChoices(devId), instanceId);
    }

    private void UpdateButtonChoices(uint devId, string listId, string instanceId)
    {
      DeviceType devType = Util.DeviceIdToType(devId);
      uint btnCnt;
      if (TryGetDevice(devId, out JoyDevice device))
        btnCnt = device.ButtonCount;
      else
        btnCnt = Util.GetDefaultButtonCount(devType);
      string[] values = new string[btnCnt];
      for (uint i = 1; i <= btnCnt; ++i)
        values[i-1] = ($"{Util.ButtonName(devType, i)}");
      UpdateTPChoicesFromListId(listId, values, instanceId);
    }

    private void UpdatePovCountChoices(uint devId, string listId, string instanceId)
    {
      DeviceType devType = Util.DeviceIdToType(devId);
      int maxPovs = Util.GetMaxPovs(devType);
      string[] values = new string[maxPovs];
      for (var i = 1; i <= maxPovs; ++i)
        values[i-1] = i.ToString();
      UpdateTPChoicesFromListId(listId, values, instanceId);
    }

    private void UpdateDPovChoices(uint devId, string listId, string instanceId)
    {
      DeviceType devType = Util.DeviceIdToType(devId);
      DPovDirection maxDir = devType == DeviceType.VJoy ? DPovDirection.West : DPovDirection.NorthWest;
      string[] values = new string[(int)maxDir + 2];
      for (var i = DPovDirection.Center; i <= maxDir; ++i)
        values[(int)i + 1] = i.ToString();
      UpdateTPChoicesFromListId(listId, values, instanceId, C.IDSTR_DPOV_DIR);
    }

    // Updates list of exes and POVs for selected device in Set Slider Position action.
    private void UpdateAllAxisChoices(uint devId, string listId, string instanceId)
    {
      List<string> list = new (GetAxisChoices(devId));
      for (int i = 1, e = Util.GetMaxPovs(Util.DeviceIdToType(devId)); i <= e; ++i)
        list.Add($"POV {i}");
      UpdateTPChoicesFromListId(listId, list.ToArray(), instanceId);
    }

    #endregion List Updaters

    #region Connector Updaters                ///////////////////////////////////////////////

    private void UpdateDeviceConnectors(uint devId)
    {
      // Send the current axis values to any related connector(s). Refresh the VJD state first if needed.
      if (TryGetDevice(devId, out JoyDevice device) && RefreshDeviceStateIfNeeded(device)) {
        var state = device.StateReport();
        foreach (ConnectorTrackingData cdata in _connectorsDict.Values)
          if (cdata.devId == devId)
            UpdateConnectorsFromState(cdata, state);
      }
    }

    private void UpdateConnectorsFromState(in ConnectorTrackingData cdata, in VJDState state)
    {
      switch (cdata.type) {
        case ControlType.Axis:
          cdata.lastValue = Util.GetStateReportAxisValue(Util.DeviceIdToType(cdata.devId), state, cdata.axis);
          break;
        case ControlType.ContPov:
          cdata.lastValue = Util.GetStateReportCPovValue(Util.DeviceIdToType(cdata.devId), state, cdata.targetId);
          break;
        case ControlType.DiscPov:
          cdata.lastValue = (int)Util.GetStateReportDPovValue(Util.DeviceIdToType(cdata.devId), state, cdata.targetId);
          break;
        default:
          return;
      }
      UpdateRelatedConnectors(cdata);
    }

    private void UpdateRelatedConnectors(/*object obj*/ ConnectorTrackingData data)
    {
      //if (obj?.GetType() != typeof(ConnectorTrackingData))
      //return;
      //var data = (ConnectorTrackingData)obj;
      if (!TryGetDevice(data.devId, out JoyDevice device))
        return;

      foreach (var instance in data.relations) {
        if (string.IsNullOrEmpty(instance.shortId) || instance.shortId == data.currentShortId)
          continue;

        int value = data.lastValue;
        if (data.type == ControlType.DiscPov) {
          value = device.ScaleDpovToAxis(value, instance.dpovDir);
        }
        else {
          if (data.type == ControlType.ContPov && data.lastValue < 0)  // center POV
            value = device.ScaleInputToAxisRange(data.axis, 50.0f, instance.rangeMin, instance.rangeMax);
          value = device.ScaleAxisToInputRange(data.axis, value, instance.rangeMin, instance.rangeMax);
        }
        _logger.LogDebug($"[UpdateRelatedConnectors] Sending update for {instance.shortId} ({data.devId}, {data.type}, {data.targetId}) with val {value}; orig val: {data.lastValue}; range: {instance.rangeMin}/{instance.rangeMax}");
        if (value > -1)
          UpdateTPConnector(instance.shortId, value);
      }
    }

    #endregion Connector Updaters

    #region Misc. event handlers          ///////////////////////////////////////////

    private void DeviceControlAction(string act, string devId)
    {
      if (string.IsNullOrWhiteSpace(act) || (GetFullDeviceIdOrDefault(devId) is uint id) && id == 0)
        return;

      switch (act) {
        case C.STR_CONNECT:
          AddDevice(id);
          break;
        case C.STR_DISCONNECT:
          RemoveDevice(id);
          break;
        case C.STR_TOGGLE_CONN:
          if (VJoyConnected(id))
            RemoveDevice(id);
          else
            AddDevice(id);
          break;
        case C.STR_RESET:
          ResetDevice(id);
          break;
        case C.STR_REFRESH_REP:
          RefreshDeviceState(id);
          break;
        case C.STR_FORCE_UNPLUG:
          ForceUnplugDevice(id);
          break;
      }
    }

    private void SetStateRefreshRate(uint valueMs)
    {
      if (valueMs > 0 && valueMs < 100)
        valueMs = 100;
      if (_settings.StateRefreshRate != valueMs) {
        _settings.StateRefreshRate = valueMs;
        ToggletatusDataTask();
      }
    }

    private void ProcessPluginSettings(IReadOnlyCollection<Setting> settings)
    {
      if (settings == null)
        return;
      uint value;
      foreach (var s in settings) {
        if (string.IsNullOrEmpty(s.Value))
          continue;
        var setting = _settings.tpSettings?.FirstOrDefault(cs => cs.Name == s.Name);
        if (setting != null && s.Value == setting.Value)
          continue;
        string trimmedVal = s.Value.Trim();
        switch (s.Name) {
          case C.IDSTR_SETTING_DEF_DEVID:
            SetDefaultDevice(GetFullDeviceIdOrDefault(trimmedVal));
            break;

          case C.IDSTR_SETTING_AUTO_CONNECT:
            if (!uint.TryParse(trimmedVal, out value))
              continue;
            _settings.AutoConnectDevice = value > 0;
            break;

          case C.IDSTR_SETTING_STATE_RATE:
            if (!uint.TryParse(trimmedVal, out value))
              continue;
            SetStateRefreshRate(value);
            break;

          case C.IDSTR_SETTING_RPRT_AS_RAW:
            if (!uint.TryParse(trimmedVal, out value))
              continue;
            _settings.StateReportAxisRaw = value > 0;
            break;

          case C.IDSTR_SETTING_RPRT_BTN_RNG:
            if (uint.TryParse(trimmedVal, System.Globalization.NumberStyles.Any, null, out value)) {
              value = Math.Clamp(value, 0, 128);
              _settings.MinBtnNumForState = (ushort)(value > 0 ? 1 : 0);
              _settings.MaxBtnNumForState = (ushort)value;
              break;
            }
            {
              var range = trimmedVal.Split(new char[] { '-', ',', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
              if (range.Length != 2 ||
                  !ushort.TryParse(range[0], System.Globalization.NumberStyles.Any, null, out var min) ||
                  !ushort.TryParse(range[1], System.Globalization.NumberStyles.Any, null, out var max)) {
                _logger.LogWarning($"Could not parse Buttons to Report range string '{s.Value}'");
                continue;
              }
              _settings.MinBtnNumForState = min;
              _settings.MaxBtnNumForState = max;
            }
            break;

          default:
            break;
        }
      }
      _settings.tpSettings = settings;
    }

    void SetConnectorPosition(ActionEvent message, VJEvent ev)
    {
      // Get Device ID, or default
      var devId = message.GetValue(C.IDSTR_DEVICE_ID, C.IDSTR_DEVID_DFLT);
      ev.devId = GetFullDeviceIdOrDefault(devId);
      if (ev.devId == 0 || !TryGetDevice(ev.devId, out JoyDevice device)) {
        _logger.LogWarning($"Device ID '{ev.devId}' is empty or invalid for action ID '{message.Id}'.");
        return;
      }

      // validate the target control ID
      var idStr = message.GetValue(C.IDSTR_TARGET_ID);
      if (string.IsNullOrWhiteSpace(idStr)) {
        _logger.LogWarning($"Required target control ID is empty or invalid for action ID '{message.Id}' with device {device.Name}.");
        return;
      }

      ev.type = ControlType.Axis;
      if (idStr.StartsWith("POV")) {
        idStr = idStr.Substring(4);

        if (!uint.TryParse(idStr, out ev.targetId)) {
          _logger.LogWarning($"Could not parse POV Index from string '{idStr}' for action ID '{message.Id}' with device {device.Name}.");
          return;
        }
        ev.axis = HID_USAGES.HID_USAGE_POV;
        ev.tpId = device.ContinuousHatCount > 0 ? C.IDSTR_DEVTYPE_CPOV : C.IDSTR_DEVTYPE_DPOV;
      }
      else {
        // for axes the ID is the ending of HID_USAGS enum names
        if (!Enum.TryParse("HID_USAGE_" + idStr, true, out ev.axis)) {
          _logger.LogWarning($"Axis {ev.axis} from string '{idStr}' not valid for action ID '{message.Id}' with device {device.Name}.");
          return;
        }
        ev.targetId = (uint)ev.axis;
        ev.tpId = C.IDSTR_DEVTYPE_AXIS;
      }

      // get the position value
      ev.valueStr = message.GetValue(C.IDSTR_ACT_VAL);
      if (string.IsNullOrWhiteSpace(ev.valueStr)) {
        _logger.LogWarning($"Position value is empty for action ID '{message.Id}'.");
        return;
      }
      if (!TryEvaluateValue(ev.valueStr, out float fValue)) {
        _logger.LogWarning($"Cannot parse {Util.EventTypeToControlName(ev.type)} value for target '{(ev.type == ControlType.Axis ? ev.axis : ev.targetId)}' from '{ev.valueStr}'.");
        return;
      }
      //ev.value = (int)Math.Round(fValue, 0);
      ev.value = device.ScaleInputToAxisRange(ev.axis, fValue, ev.rangeMin, ev.rangeMax, true);

      // check for related connectors
      if (!_connectorsDict.TryGetValue(Util.ConnectorDictKey(ev), out ConnectorTrackingData cdata) || cdata.lastValue == ev.value)
        return;
      cdata.lastValue = ev.value;
      // assume no connectors are being moved at the same time
      //cdata.isDown = false;
      //cdata.currentShortId = default;
      UpdateRelatedConnectors(cdata);

    }

    #endregion Misc. event handlers

    #region Actions & Connectors handlers         ///////////////////////////////////////////
    // These run on a separate task/thread

    private void ProcessEventQueue()
    {
      _logger.LogDebug("Starting event queue processing task.");
      try {
        while (!_shutdownToken.IsCancellationRequested) {

          _eventQueueReadyEvent.Wait(_shutdownToken);
          _eventQueueReadyEvent.Reset();
          while (!_shutdownToken.IsCancellationRequested && _eventQ.TryDequeue(out DataContainerEventBase message)) {
            if (!ParseAndValidateEvent(message, out VJEvent ev, out JoyDevice device))
              continue;
            switch (message) {
              case ActionEvent e:
                HandleActionEvent(ev, device, e);
                break;
              case ConnectorChangeEvent e:
                HandleConnectorEvent(ev, device, e);
                break;
            }
          }
        }
      }
      catch (OperationCanceledException) { /* ignore */ }
      catch (ObjectDisposedException)    { /* ignore */ }
      catch (Exception e) {
        _logger.LogError(e, "Exception in Event queue processing task, cannot continue.");
      }
      _logger.LogDebug("Event queue processing task exited.");
    }

    // Tries to create a valid VJEvent from a TP action or connector message.
    // Does parsing and validation of attributes common to both message types.
    private bool ParseAndValidateEvent(DataContainerEventBase message, out VJEvent ev, out JoyDevice device)
    {
      var isConn = message.GetType() == typeof(ConnectorChangeEvent);
      device = null;
      // Set up the VJEvent struct with some defaults.
      ev = new VJEvent() {
        devId = DefaultDevId,
        btnAction = ButtonAction.None,
        tpId = message.Id.Split('.').Last(),
        // default range is the full range of a slider
        rangeMin = 0,
        rangeMax = 100
      };

      // Try determine the event's control type, button/axis/hat
      ev.type = Util.TpStateNameToEventType(ev.tpId);
      if (ev.type == ControlType.None) {
        // check for special actions
        if (ev.tpId == C.IDSTR_ACTION_DEVICE_CTRL)
          DeviceControlAction(message.GetValue(C.IDSTR_ACT_VAL), message.GetValue(C.IDSTR_DEVICE_ID, DefaultDevId.ToString()));
        else if (ev.tpId == C.IDSTR_ACTION_SET_POS)
          SetConnectorPosition((ActionEvent)message, ev);
        else
          _logger.LogWarning($"Unknown Action/Connector ID: '{message.Id}'.");
        return false;
      }

      // Get Device ID, or default
      var devId = message.GetValue(C.IDSTR_DEVICE_ID, C.IDSTR_DEVID_DFLT);
      ev.devId = GetFullDeviceIdOrDefault(devId);
      if (ev.devId == 0) {
        _logger.LogWarning($"Device ID '{ev.devId}' is empty or invalid for action ID '{message.Id}'.");
        return false;
      }

      // Check if device already exists, if not try to add it here.
      if (!TryGetDevice(ev.devId, out device) && (!_settings.AutoConnectDevice || (device = AddDevice(ev.devId)) == null)) {
        _logger.LogWarning($"The Device with ID '{ev.devId}' does not exist or is not configured.");
        return false;
      }

      // validate the target control ID
      var idStr = message.GetValue(C.IDSTR_TARGET_ID);
      if (string.IsNullOrWhiteSpace(idStr)) {
        _logger.LogWarning($"Required target control ID is empty or invalid for action ID '{message.Id}' with device {device.Name}.");
        return false;
      }

      switch (ev.type) {
        case ControlType.Axis:
          // for axes the ID is the ending of HID_USAGS enum names
          if (!Enum.TryParse("HID_USAGE_" + idStr, true, out ev.axis) || !device.HasDeviceAxis(ev.axis)) {
            _logger.LogWarning($"Axis {ev.axis} from string '{idStr}' not valid/available for device {device.Name}.");
            return false;
          }
          ev.targetId = (uint)ev.axis;  // keep a copy here also for convenience
          break;

        case ControlType.Button:
          if ((ev.targetId = Util.ButtonIndex(device.DeviceType, idStr)) == 0 || ev.targetId > device.ButtonCount) {
            _logger.LogWarning($"Button Index out of range: {ev.targetId} (from string '{idStr}') out of {device.ButtonCount} for {device.Name}.");
            return false;
          }
          break;

        case ControlType.DiscPov:
          if (!uint.TryParse(idStr, out ev.targetId) || (ev.targetId > device.DiscreteHatCount && ev.targetId > device.ContinuousHatCount)) {
            _logger.LogWarning($"D-POV Index out of range: {ev.targetId} (from string '{idStr}') out of {device.DiscreteHatCount} for {device.Name}.");
            return false;
          }
          {
            // Dpad/dpov also needs a direction
            var dirStr = message.GetValue(C.IDSTR_DPOV_DIR);
            if (string.IsNullOrWhiteSpace(dirStr) || !Enum.TryParse(dirStr, true, out ev.dpovDir)) {
              _logger.LogWarning($"Could not parse D-POV direction in '{message.Id}' for '{device.Name}', control ID '{ev.targetId}', direction: '{dirStr}'.");
              return false;
            }
          }
          break;

        case ControlType.ContPov:
          if (!uint.TryParse(idStr, out ev.targetId) || ev.targetId > device.ContinuousHatCount) {
            _logger.LogWarning($"C-POV Index out of range {ev.targetId} (from string '{idStr}') out of {device.ContinuousHatCount} for {device.Name}.");
            return false;
          }
          ev.axis = HID_USAGES.HID_USAGE_POV;
          break;

      }

      if (!isConn) {
        ev.valueStr = message.GetValue(C.IDSTR_ACT_VAL);
        if (string.IsNullOrWhiteSpace(ev.valueStr)) {
          _logger.LogWarning($"Primary value is invalid: '{ev.valueStr}'.");
          return false;
        }
        return true;
      }

      // Connector. Get min/max range and reverse flag for axis or cpov type.
      if (ev.type != ControlType.DiscPov) {
        var minStr = message.GetValue(C.IDSTR_RNG_MIN);
        if (!string.IsNullOrWhiteSpace(minStr) && !TryEvaluateValue(minStr, out ev.rangeMin)) {
          _logger.LogWarning($"Range minimum value invalid for Connector ID '{message.Id}' for '{device.Name}' with control ID '{idStr}', value:  '{minStr}'");
          return false;
        }
        var maxStr = message.GetValue(C.IDSTR_RNG_MAX);
        if (!string.IsNullOrWhiteSpace(maxStr) && !TryEvaluateValue(maxStr, out ev.rangeMax)) {
          _logger.LogWarning($"Range maximum value invalid for Connector ID '{message.Id}' for '{device.Name}' with control ID '{idStr}', value:  '{maxStr}'");
          return false;
        }
        var revStr = message.GetValue(C.IDSTR_DIR_REVERSE);
        AxisMovementDir revType = AxisMovementDir.Normal;
        if (!string.IsNullOrWhiteSpace(revStr) && !Enum.TryParse(revStr, true, out revType)) {
          _logger.LogWarning($"Reversing type invalid for Connector ID '{message.Id}' for '{device.Name}' with control ID '{idStr}', value: '{revStr}'");
          return false;
        }
        if (revType == AxisMovementDir.Reverse) {
          var tmpMax = ev.rangeMax;
          ev.rangeMax = ev.rangeMin;
          ev.rangeMin = tmpMax;
        }
      }

      return true;
    }  // ParseAndValidateEvent

    private void HandleActionEvent(VJEvent ev, JoyDevice device, ActionEvent message)
    {
      // force button up/pov center/axis reset up on "up" held action
      if (message.GetPressState() == Press.Up)
        ev.btnAction = ButtonAction.Up;

      float fValue;
      switch (ev.type) {
        case ControlType.Button:
          if (ev.btnAction == ButtonAction.None && !Enum.TryParse(ev.valueStr, true, out ev.btnAction)) {
            _logger.LogWarning($"Could not parse button action type: BTN#: '{ev.targetId}'; act: '{ev.valueStr}'.");
            return;
          }
          break;

        case ControlType.DiscPov:
          if (ev.btnAction == ButtonAction.Up) {
            ev.dpovDir = DPovDirection.Center;
            ev.value = (int)ev.dpovDir;
            if (ev.targetId > device.DiscreteHatCount) {
              // convert to CPOV
              ev.type = ControlType.ContPov;
              ev.axis = HID_USAGES.HID_USAGE_POV;
            }
            //ev.value = (int)DPovDirection.Center;
            break;
          }
          if (ev.targetId > device.DiscreteHatCount) {
            // convert to CPOV
            ev.type = ControlType.ContPov;
            ev.axis = HID_USAGES.HID_USAGE_POV;
            fValue = device.ConvertDpovToCpov(ev.dpovDir) + 50.0f;
            ev.value = device.ScaleInputToAxisRange(ev.axis, fValue, ev.rangeMin, ev.rangeMax, true);
            break;
          }
          if (!Enum.TryParse(ev.valueStr, true, out ev.btnAction)) {
            _logger.LogWarning($"Could not parse button action type for POV#: '{ev.targetId}'; act: '{ev.valueStr}'.");
            return;
          }
          ev.value = (int)ev.dpovDir;
          break;

        case ControlType.Axis:
        case ControlType.ContPov:
          if (ev.btnAction == ButtonAction.Up) {
            if ((fValue = GetResetValueFromEvent(message, ev.tpId, ev.type)) < -1.0f)
              return;
          }
          else if (!TryEvaluateValue(ev.valueStr, out fValue)) {
            _logger.LogWarning($"Cannot parse {Util.EventTypeToControlName(ev.type)} value for target '{(ev.type == ControlType.Axis ? ev.axis : ev.targetId)}' from '{ev.valueStr}'.");
            return;
          }
          if (ev.type == ControlType.ContPov && ev.targetId > device.ContinuousHatCount) {
            // convert to DPOV
            ev.type = ControlType.DiscPov;
            ev.dpovDir = device.ConvertCpovToDpov(fValue);
            ev.value = (int)ev.dpovDir;
            break;
          }
          if (fValue < 0.0f) {
            // center POV
            ev.value = (int)DPovDirection.Center;
            break;
          }
          if (ev.type == ControlType.ContPov)
            fValue += 50.0f;
          ev.value = device.ScaleInputToAxisRange(ev.axis, fValue, ev.rangeMin, ev.rangeMax, true);
          break;

        default:
          return;
      }
      device.DispatchEvent(ev);

      // check for related connectors
      if (!_connectorsDict.TryGetValue(Util.ConnectorDictKey(ev), out ConnectorTrackingData cdata) || cdata.lastValue == ev.value)
        return;
      cdata.lastValue = ev.value;
      // assume no connectors are being moved at the same time
      cdata.isDown = false;
      cdata.currentShortId = default;
      UpdateRelatedConnectors(cdata);
    }  // HandleActionEvent

    private void HandleConnectorEvent(VJEvent ev, JoyDevice device, ConnectorChangeEvent message)
    {
      bool needUpdate = true;
      string ckey = Util.ConnectorDictKey(ev);
      ev.value = message.Value;

      if (!_connectorsDict.TryGetValue(ckey, out ConnectorTrackingData cdata)) {
        // in theory this shouldn't happen... we didn't find our connector in the table built from "short connector" reports.
        _logger.LogWarning($"Could not find Connector ID '{message.ConnectorId}' in the connectors cache!");
        cdata = ConnectorTrackingData.CreateFromEvent(ev, true);
        _connectorsDict[ckey] = cdata;
      }

      // Try to determine when a connector is released, the equivalent on an action "up" event, but a lot more complicated :(
      long ts = Stopwatch.GetTimestamp();
      if (!cdata.isDown || Util.TicksToSecs(ts - cdata.lastUpdate) > C.CONNECTOR_MOVE_TO_SEC) {
        // wasn't pressed, or hasn't changed for a "long" time, so I guess now it is... so far so good.
        cdata.isDown = true;
        cdata.startValue = message.Value;
      }
      else if (cdata.lastTpValue == (byte)message.Value) {
        // Gets dicey here... only way to distinguish an "up" event is if the sent value is the same as the last value.
        // Except sometimes (often) this doesn't work and we get the packets out of order, or something, so the value changes anyway.
        cdata.isDown = false;

        // Get reset type and value
        if (GetResetValueFromEvent(message, ev.tpId, ev.type, cdata.startValue) is var resetVal && resetVal > -2)
          ev.value = resetVal;
        else
          needUpdate = false;  // connector is finished, don't schedule event
      }

      switch (ev.type) {
        case ControlType.Axis:
        case ControlType.ContPov:
          ev.value = device.ScaleInputToAxisRange(ev.axis, ev.value, ev.rangeMin, ev.rangeMax, true);
          _logger.LogDebug($"Axis Connector Event: axe: {ev.axis}; orig val: {message.Value}; new value {ev.value}; range min/max: {ev.rangeMin}/{ev.rangeMax}");
          break;

        case ControlType.DiscPov:
          // translate axis slider value to actual dpov direction and a button action
          if (cdata.isDown) {
            ev.dpovDir = device.ScaleAxisToDpov(ev.value, ev.dpovDir);
            ev.btnAction = ev.dpovDir == DPovDirection.Center ? ButtonAction.Click : ButtonAction.Down;
            ev.value = (int)ev.dpovDir;
          }
          break;

        default:
          _logger.LogWarning($"Connectors do not support vJoy device type {ev.type}.");
          return;
      }

      // so yea we keep track of the last value and hope it gets repeated when user releases the slider
      cdata.lastTpValue = (byte)message.Value;
      cdata.lastUpdate = ts;

      // in some cases the actual joystick value isn't going to change, so we can bail out now and not update anything
      if (cdata.lastValue == ev.value)
        return;

      if (needUpdate)
        device.DispatchEvent(ev);

      cdata.lastValue = ev.value;

      // Now do something with all this connector tracking data we've been collecting... update some sliders!
      cdata.currentShortId = string.Empty;
      if (cdata.isDown) {
        // find the short id the connector currently being moved, so that we can exclude it from any "live" updates
        var mappingId = ev.tpId + "|" + string.Join("|", message.Data.Select(d => d.Key + "=" + d.Value));
        _ = _connectorsLongToShortMap.TryGetValue(mappingId, out cdata.currentShortId);
      }
      //else    // uncomment to disable "live" connector updates of related sliders while one is moving
      UpdateRelatedConnectors(cdata);
    }  // HandleConnectorEvent

    #endregion Actions & Connectors handlers

    #region TP Event Handlers      ///////////////////////////////////////////

    public void OnInfoEvent(InfoEvent message)
    {
      _logger?.LogInformation($"Touch Portal Connected with: TP v{message.TpVersionString}, SDK v{message.SdkVersion}, {C.PLUGIN_SHORT_NAME} Plugin Entry v{message.PluginVersion}, {C.PLUGIN_SHORT_NAME} Client v{Util.GetProductVersionString()} ({Util.GetProductVersionNumber():X})");
      _logger?.LogDebug($"[Info] Settings: {JsonSerializer.Serialize(message.Settings)}");
      ProcessPluginSettings(message.Settings);
    }

    public void OnSettingsEvent(SettingsEvent message)
    {
      _logger?.LogDebug($"[OnSettings] Settings: {JsonSerializer.Serialize(message.Values)}");
      ProcessPluginSettings(message.Values);
    }

    public void OnClosedEvent(string message) {
      _logger?.LogInformation(message);
      _settings.ClosedByTp = true;   // may not actually be true but in that case we have already quit
      Quit();
    }

    public void OnActionEvent(ActionEvent message)
    {
      _logger?.LogDebug("[OnAction] PressState: {0}, ActionId: {1}, Data: '{2}'",
          message.GetPressState(), message.ActionId, string.Join(", ", message.Data.Select(dataItem => $"'{dataItem.Key}' = '{dataItem.Value}'")));
      _eventQ.Enqueue(message);
      _eventQueueReadyEvent.Set();
    }

    public void OnConnecterChangeEvent(ConnectorChangeEvent message)
    {
      _logger?.LogDebug("[OnConnecterChangeEvent] ConnectorId: {0}, Value: {1}, Data: '{2}'", message.ConnectorId, message.Value,
        string.Join(", ", message.Data.Select(dataItem => $"'{dataItem.Key}' = '{dataItem.Value}'")));
      _eventQ.Enqueue(message);
      _eventQueueReadyEvent.Set();
    }

    // convoluted malarkey to try and track which slider instances operate on the same axes, and the whole "short ID" business
    public void OnShortConnectorIdNotificationEvent(ShortConnectorIdNotificationEvent message)
    {
      _logger.LogDebug($"[ShortConnectorIdNotificationEvent] ConnectorId: {message.ConnectorId}; shortId: {message.ShortId};");
      // we only use the last part of the connectorId which is meaningful
      // note that ShortConnectorIdNotificationEvent.ActualConnectorId invokes the connectorId parser.
      string connId = message.ActualConnectorId?.Split('.').Last();
      var connData = message.Data;
      var evtype = Util.TpStateNameToEventType(connId);
      if (evtype == ControlType.None || evtype == ControlType.Button || !connData.Any()) {
        _logger.LogWarning($"[OnShortConnectorIdNotificationEvent] Unknown ConnectorId '{connId}' (full: '{message.ConnectorId}')");
        return;
      }

      // stuff we may need to store
      uint targetId = 0;
      uint devId = DefaultDevId;
      HID_USAGES axis = 0;
      AxisMovementDir reverse = AxisMovementDir.Normal;
      // the related device, if any (this may change during parsing)
      JoyDevice device = Device(devId);  // could be null
      // set up connector instance data with defaults
      var idata = new ConnectorInstanceData { shortId = message.ShortId, rangeMin = 0, rangeMax = 100, dpovDir = DPovDirection.None };

      // extract the attributes we are interested in from the key=value pairs
      foreach (var data in connData) {
        switch (data.Key.Split('.').Last()) {
          case C.IDSTR_DEVICE_ID:
            // Device ID, or default
            if ((devId = GetFullDeviceIdOrDefault(data.Value)) == 0)
              return;   // totally invalid, ignore this whole connector
            if (devId != DefaultDevId && CheckDeviceId(devId))
              device = Device(devId);  // could be null, but still track this connector in case the device becomes valid later
            break;

          case C.IDSTR_TARGET_ID:
            if (evtype == ControlType.Axis) {
              if (Enum.TryParse("HID_USAGE_" + data.Value, true, out axis))
                targetId = (uint)axis;
            }
            else if (!uint.TryParse(data.Value, out targetId)) {
              targetId = 0;
            }
            else {
              axis = HID_USAGES.HID_USAGE_POV;
            }
            break;

          case C.IDSTR_RNG_MIN:
            if (!int.TryParse(data.Value, out idata.rangeMin))
              idata.rangeMin = 0;
            break;

          case C.IDSTR_RNG_MAX:
            if (!int.TryParse(data.Value, out idata.rangeMax))
              idata.rangeMax = 100;
            break;

          case C.IDSTR_DPOV_DIR:
            if (!Enum.TryParse(data.Value, true, out idata.dpovDir))
              idata.dpovDir = DPovDirection.Center;
            break;

          case C.IDSTR_DIR_REVERSE:
            if (!Enum.TryParse(data.Value, true, out reverse))
              reverse = AxisMovementDir.Normal;
            break;

          default:
            break;
        }  // switch
      }  // loop

      if (devId == 0) {
        _logger.LogWarning($"[OnShortConnectorIdNotificationEvent] Unknown Device ID '{devId}' for connector ID '{message.ConnectorId}')");
        return;
      }
      if (targetId == 0) {
        _logger.LogWarning($"[OnShortConnectorIdNotificationEvent] Unknown target ID '{targetId}' for connector ID '{message.ConnectorId}')");
        return;
      }

      if (reverse == AxisMovementDir.Reverse) {
        int tmpMax = idata.rangeMax;
        idata.rangeMax = idata.rangeMin;
        idata.rangeMin = tmpMax;
      }

      // Get existing connector tracking data based on parsed device, connector, and target fields, or start a new entry.
      string key = Util.ConnectorDictKey(devId, connId, targetId);
      if (!_connectorsDict.TryGetValue(key, out var cdata)) {
        cdata = new ConnectorTrackingData {
          devId = devId,
          id = connId,
          type = evtype,
          axis = axis,
        };
        _connectorsDict[key] = cdata;
      }
      cdata.targetId = targetId;
      //cdata.devId = devId;
      if (cdata.relations.FindIndex(r => r.shortId == idata.shortId) is var relIdx && relIdx > -1)
        cdata.relations.RemoveAt(relIdx);
      cdata.relations.Add(idata);
      // sanity check... seems like the short IDs could really add up since we don't know when they become invalid. Use a FIFO
      if (cdata.relations.Count > 20)
        cdata.relations.RemoveAt(0);

      // record the mapping of the (almost) full connector ID to the short ID
      // How best to limit size? Dicts are unordered so no real FIFO, but better than nothing?
      if (_connectorsLongToShortMap.Count > 100) {
        var enmr = _connectorsLongToShortMap.Keys.GetEnumerator();
        // might as well get rid of a few while we're here
        for (var i=0; i < 10 && enmr.MoveNext(); ++i)
          _connectorsLongToShortMap.TryRemove(enmr.Current, out _);
      }
      var mappingId = connId + "|" + string.Join("|", connData.Select(d => $"{d.Key}={d.Value}"));
      _connectorsLongToShortMap.TryAdd(mappingId, message.ShortId);

      // If the device exists, send the current axis value to the connector. Refresh the VJD state first if needed.
      // do not create a device here, connector data could be ancient
      if (device != null && RefreshDeviceStateIfNeeded(device))
        UpdateConnectorsFromState(cdata, device.StateReport());
    }

    public void OnListChangedEvent(ListChangeEvent message) {
      _logger.LogDebug($"[OnListChanged] {message.ActionId} / {message.ListId} / {message.InstanceId} = '{message.Value}'  shutdown: {_disposed}");
      if (string.IsNullOrWhiteSpace(message.Value) || _disposed)
        return;
      var listParts = message.ListId.Split('.');
      if (listParts.Length < 3)
        return;
      listParts = listParts[^3..];

      if (listParts[2] == C.IDSTR_DEVICE_ID && (GetFullDeviceIdOrDefault(message.Value) is uint id) && id > 0) {
        if (listParts[1] == C.IDSTR_DEVTYPE_AXIS)
          UpdateAxisChoices(id, message.ListId, message.InstanceId);
        else if (listParts[1] == C.IDSTR_DEVTYPE_BTN)
          UpdateButtonChoices(id, message.ListId, message.InstanceId);
        else if ((((listParts[1] == C.IDSTR_DEVTYPE_DPOV) is bool isDpov) && isDpov) || listParts[1] == C.IDSTR_DEVTYPE_CPOV) {
          UpdatePovCountChoices(id, message.ListId, message.InstanceId);
          if (isDpov && listParts[0] == C.IDSTR_EL_ACTION)
            UpdateDPovChoices(id, message.ListId, message.InstanceId);
        }
        else if (listParts[1] == C.IDSTR_ACTION_SET_POS)
          UpdateAllAxisChoices(id, message.ListId, message.InstanceId);
      }
    }

    // Unused handlers below here.

    public void OnBroadcastEvent(BroadcastEvent message) {
      _logger.LogDebug($"[Broadcast] Event: '{message.Event}', PageName: '{message.PageName}'");
    }

    public void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message) {
      _logger.LogDebug($"[OnNotificationOptionClickedEvent] NotificationId: '{message.NotificationId}', OptionId: '{message.OptionId}'");
    }

    public void OnUnhandledEvent(string jsonMessage) {
      _logger.LogDebug($"Unhanded message: {jsonMessage}");
    }

    #endregion TP Event Handlers

    #region TP Client commands                  ///////////////////////////////////////////

    private void UpdateTPState(string id, string value)
    {
      try {
        if (_client.IsConnected)
          _client.StateUpdate(Util.StateIdStr(id), value);
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception in UpdateTPState");
        Quit();
      }
    }

    private void CreateTPState(string id, string descript, string defValue)
    {
      try {
        if (_client.IsConnected)
          _client.CreateState(Util.StateIdStr(id), descript, defValue);
        _logger.LogDebug($"Created state '{id}' '{descript}' '{defValue}'");
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception in CreateTPState");
        Quit();
      }
    }

    private void RemoveTPState(string id)
    {
      try {
        if (_client.IsConnected)
          _client.RemoveState(Util.StateIdStr(id));
      }
      catch { /* we may be doing this after the socket is closed, so ignore errors */ }
    }

    private void UpdateTPConnector(string shortId, int value)
    {
      try {
        if (_client.IsConnected)
          _client.ConnectorUpdateShort(shortId, value);
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception in UpdateTPConnector");
        Quit();
      }
    }

    private void UpdateTPChoices(string stateId, string[] values, string instanceId = null)
    {
      try {
        if (_client.IsConnected)
          _client.ChoiceUpdate(stateId, values, instanceId);
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception in UpdateTPChoices");
        Quit();
      }
    }

    #endregion TP Client commands

  }
}
