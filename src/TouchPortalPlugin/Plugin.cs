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


namespace TJoy.TouchPortalPlugin
{
  public class Plugin : ITouchPortalEventHandler
  {
    public string PluginId => C.PLUGIN_ID;   // for ITouchPortalEventHandler

    // current vJoy device ID, zero if not connected (the configured device ID is in _settings)
    private uint VJoyCurrDevId { get { return _vjoyDevice?.DeviceId ?? 0; } }

    private readonly ILogger<Plugin> _logger;
    private readonly ITouchPortalClient _client;

    private readonly Task _eventWorkerTask;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly CancellationToken _shutdownToken;
    private readonly ManualResetEventSlim _eventQueueReadyEvent = new();

    private Task _stateUpdateTask = null;
    private CancellationTokenSource _stateTaskCts = null;
    private CancellationToken _stateTaskShutdownToken;

    private readonly JoyDevice _vjoyDevice;
    private readonly PluginSettings _settings = new();
    private static readonly System.Data.DataTable _expressionEvaluator = new();  // used to evaluate basic math in action data
    private readonly ConcurrentQueue<DataContainerEventBase> _eventQ = new();
    private readonly ConcurrentDictionary<string, ConnectorTrackingData> _connectorsDict = new();
    private readonly ConcurrentDictionary<string, string> _connectorsLongToShortMap = new();
    private readonly Dictionary<string, int> _joystickStatesDict = new();

    // automagic contructor arguments... woot!?
    public Plugin(ITouchPortalClientFactory clientFactory, ILoggerFactory logFactory)
    {
      _logger = logFactory?.CreateLogger<Plugin>() ?? throw new ArgumentNullException(nameof(logFactory));
      _client = clientFactory?.Create(this) ?? throw new ArgumentNullException(nameof(clientFactory));

      _eventWorkerTask = new(ProcessEventQueue);
      _eventWorkerTask.ConfigureAwait(false);
      _shutdownToken = _shutdownCts.Token;

      // TODO: constuct device(s) as needed
      // hackish way to pass a logger... sigh. yea yea, DI, but JoyDevice is not a service or interface (yet?) so nothing I tried worked. all the SO answers are
      // half-baked at best and docs are useless. also I want to construct it with a parameter and have multiple instances later w/out jumping through hoops?
      _vjoyDevice = new JoyDevice(logFactory, DeviceType.VJoy);

      //Environment.SetEnvironmentVariable("VJOYINTERFACELOGLEVEL", "1");  // uncomment to enable vJoy SDK logging (rather verbose)
      //Environment.SetEnvironmentVariable("VJOYINTERFACELOGFILE", "logs\\vjoy.log");
    }

    public void Run()
    {
      // regiter ctrl-c exit handler first
      Console.CancelKeyPress += (_, _) => {
        _logger.LogInformation("Quitting due to keyboard interrupt.");
        Quit();
        Environment.Exit(0);
      };

      // TODO: only check once vJoy is enabled in settings
      // Get the driver attributes (Vendor ID, Product ID, Version Number)
      if (!_vjoyDevice.DeviceTypeExists) {
        _logger.LogError("vJoy driver not enabled: Failed Getting vJoy attributes.");
        Quit();
        return;
      }

      _logger.LogInformation("vJoy Driver Vendor: {0}, Product: {1}, Version: {2}", _vjoyDevice.Manufacturer, _vjoyDevice.Product, _vjoyDevice.SerialNumber);

      // seems vj likes it best if we do this first thing
      _vjoyDevice.RegisterRemovalCallback(VJoyDeviceChangedCB, null);

      // Test if DLL matches the driver
      if (_vjoyDevice.CheckVersion(out var DllVer, out var DrvVer))
        _logger.LogInformation("Version of Driver Matches DLL Version ({0:X})", DllVer);
      else
        _logger.LogWarning("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})", DrvVer, DllVer);   // hope they check this log to see why their computer exploded.... lol

      //Connect to Touch Portal:
      if (!_client.Connect()) {
        _logger.LogError("Could not connect to Touch Portal, quitting now.");
        Quit();
        return;
      }

      _eventWorkerTask.Start();

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
      _shutdownCts?.Cancel();

      _logger?.LogDebug("Shutting down the event worker...");
      var sw = Stopwatch.StartNew();
      while (_eventWorkerTask.Status == TaskStatus.Running && sw.ElapsedMilliseconds < 5000)
        Thread.Sleep(1);
      if (sw.ElapsedMilliseconds > 5000)
        _logger.LogWarning("Event worker timed out!");

      DisconnectVJoy();  // also stop status updates if needed

      _eventWorkerTask?.Dispose();
      _shutdownCts?.Dispose();
      _eventQueueReadyEvent?.Dispose();
      _expressionEvaluator?.Dispose();

      _logger?.LogInformation("All finished, shutting down TP client now.");
      if (_client?.IsConnected ?? false) {
        try { _client.Close(); }  // exits the event loop keeping us alive
        catch (Exception) { /* ignore */ }
      }
    }

    #region vJoy Interface           ///////////////////////////////////////////

    private bool VJoyConnected() => VJoyCurrDevId > 0;
    private bool VJoyConnected(uint vjid) => _vjoyDevice.CheckConnected(vjid);

    private void SetVJoyDevice(uint vjid)
    {
      if (vjid == _settings.VjDeviceId)
        return;

      if (vjid > 16) {
        _logger.LogWarning($"vJoy Device ID {vjid} is out of range (1-16).");
        return;
      }

      _settings.VjDeviceId = vjid;

      // Relinquish old device, if any.
      if (VJoyCurrDevId > 0)
        DisconnectVJoy();

      if (vjid != 0)
        SetupVJoyDevice(vjid);
    }

    private void SetupVJoyDevice(uint vjid)
    {
      if (vjid == 0 || VJoyConnected(vjid))
        return;

      UpdateTPState(C.IDSTR_STATE_VJSTATE, "0");
      ClearAllTpJoystickStates();

      // Attempt connection
      if (!_vjoyDevice.Connect(vjid)) {
        _logger.LogInformation("Failed to acquire vJoy device number {0}. Cannot continue.", vjid);
        return;
      }

      UpdateTPState(C.IDSTR_STATE_VJSTATE, vjid.ToString());
      _logger.LogInformation("Acquired: vJoy device number {0}.", vjid);
      _logger.LogInformation(_vjoyDevice.GetDeviceCapabilitiesReport(vjid));

      _vjoyDevice.ResetDevice();
      StartStatusDataTask();  // unless it's disabled
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
      if (removed != first || _settings.VjDeviceId == 0 || (removed && !VJoyConnected()))
        return;
      if (removed)
        DisconnectVJoy();
      else
        SetupVJoyDevice(_settings.VjDeviceId);
    }

    #endregion
    #region Joystick values state updates                 ///////////////////////////////////////////////

    private void StartStatusDataTask()
    {
      if (_settings.StateRefreshRate == 0 || _stateUpdateTask != null || VJoyCurrDevId == 0)
        return;

      _logger?.LogDebug("Starting state updater task...");
      _stateTaskCts = new CancellationTokenSource();
      _stateTaskShutdownToken = _stateTaskCts.Token;
      _stateUpdateTask = Task.Run(VJoyCollectStateDataTask, _stateTaskShutdownToken);
    }

    private void StoptatusDataTask()
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
      if (_settings.StateRefreshRate > 0 && _stateUpdateTask == null)
        StartStatusDataTask();
      else if (_settings.StateRefreshRate == 0 && _stateUpdateTask != null)
        StoptatusDataTask();
    }

    // Thread pool task
    private async void VJoyCollectStateDataTask()
    {
      _logger.LogDebug("State updater task started.");
      try {
        while (_settings.StateRefreshRate > 0 && !_stateTaskShutdownToken.IsCancellationRequested) {

          if (!_vjoyDevice.RefreshState())
            break;
          //_logger.LogDebug(JsonSerializer.Serialize<vJoy.JoystickState>(_vjoyInfo.state, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));

          var state = _vjoyDevice.DeviceState();
          // Axis
          foreach (VJAxisInfo axe in _vjoyDevice.AxisInfo) {
            var val = Utils.GetVJoyStateReportAxisValue(state, axe.usage);
            if (val < 0)
              continue;
            if (!_settings.StateReportAxisRaw)
              val = _vjoyDevice.ScaleAxisToInputRange(axe.usage, val, 0, 100);
            UpdateTpJoystickState(ControlType.Axis, axe.usage.ToString().Split('_').Last(), val);
          }
          // CPOV
          for (uint i=1, e = _vjoyDevice.ContionousHatCount; i <= e; ++i) {
            var val = Utils.GetVJoyStateReportCPovValue(state, i);
            if (val < -1)
              continue;
            if (!_settings.StateReportAxisRaw && val > -1)
              val = _vjoyDevice.ScaleAxisToInputRange(HID_USAGES.HID_USAGE_POV, val, 0, 100);
            UpdateTpJoystickState(ControlType.ContPov, i.ToString(), val);
          }
          // DPOV
          for (uint i = 1, e = _vjoyDevice.DiscreteHatCount; i <= e; ++i) {
            var val = Utils.GetVJoyStateReportDPovValue(state, i);
            if (val < -1)
              continue;
            UpdateTpJoystickState(ControlType.DiscPov, i.ToString(), val);
          }
          // Buttons
          if (_settings.MinBtnNumForState > 0 && _settings.MaxBtnNumForState >= _settings.MinBtnNumForState) {
            for (uint i = _settings.MinBtnNumForState, e = Math.Min(_settings.MaxBtnNumForState, _vjoyDevice.ButtonCount); i <= e; ++i) {
              var val = Utils.GetVJoyStateReportButtonValue(state, i);
              if (val < 0)
                continue;
              UpdateTpJoystickState(ControlType.Button, i.ToString(), val);
            }
          }

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

    private void UpdateTpJoystickState(ControlType ev, string devName, int value)
    {
      string stateName = Utils.EventTypeToTpStateName(ev);
      string stateId = stateName + "." + devName;
      if (!_joystickStatesDict.TryGetValue(stateId, out int prevValue)) {
        prevValue = -2;
        _joystickStatesDict.Add(stateId, value);
        CreateTPState(stateId, $"{C.IDSTR_CATEGORY_VJOY} - {Utils.EventTypeToControlName(ev)} {devName} value", Utils.GetDefaultValueForEventType(ev).ToString());
      }
      if (prevValue != value) {
        _joystickStatesDict[stateId] = value;
        UpdateTPState(stateId, value.ToString());
      }
    }

    private void ClearAllTpJoystickStates()
    {
      foreach (string key in _joystickStatesDict.Keys)
        RemoveTPState(key);
      _joystickStatesDict.Clear();
    }

    #endregion
    #region Misc. event handlers          ///////////////////////////////////////////

    private void VJoyConnectAction(string act)
    {
      switch (act) {
        case "On":
          ConnectVJoy();
          break;
        case "Off":
          DisconnectVJoy();
          break;
        case "Toggle":
          if (VJoyConnected())
            DisconnectVJoy();
          else
            ConnectVJoy();
          break;
      }
    }

    private void ConnectVJoy() {
      SetupVJoyDevice(_settings.VjDeviceId);
    }

    private void DisconnectVJoy() {
      if (_vjoyDevice?.IsConnected ?? false) {
        StoptatusDataTask();  // if it's running
        _logger.LogDebug($"Relinquishing Device ID {VJoyCurrDevId}.");
        UpdateTPState(C.IDSTR_STATE_VJSTATE, "0");
        _vjoyDevice.RelinquishDevice(VJoyCurrDevId);
      }
    }

    private void SetStateRefreshRate(uint valueMs) {
      if (valueMs > 0 && valueMs < 100)
        valueMs = 100;
      if (_settings.StateRefreshRate != valueMs) {
        _settings.StateRefreshRate = valueMs;
        ToggletatusDataTask();
      }
    }

    private void UpdateTPState(string id, string value) {
      try {
        _client.StateUpdate(PluginId + "." + C.IDSTR_CATEGORY_VJOY + ".state." + id, value);
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception in UpdateTPState");
        Quit();
      }
    }

    private void CreateTPState(string id, string descript, string defValue)
    {
      try {
        _client.CreateState(PluginId + "." + C.IDSTR_CATEGORY_VJOY + ".state." + id, descript, defValue);
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
        _client.RemoveState(PluginId + "." + C.IDSTR_CATEGORY_VJOY + ".state." + id);
      }
      catch (Exception) { }
    }

    private void UpdateTPConnector(string shortId, int value)
    {
      try {
        _client.ConnectorUpdateShort(shortId, value);
      }
      catch (Exception e) {
        _logger.LogError(e, "Exception in UpdateTPConnector");
        Quit();
      }
    }

    /// <summary>
    /// Handles an array of `Setting` types sent from TP. This could come from either the
    /// initial `OnInfoEvent` message, or the dedicated `OnSettingsEvent` message.
    /// </summary>
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
          case C.IDSTR_SETTING_VJDEVID:
            if (!uint.TryParse(trimmedVal, out value))
              continue;
            SetVJoyDevice(value);
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

    #endregion
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
            if (!ParseAndValidateEvent(message, out var ev))
              continue;
            switch (message) {
              case ActionEvent e:
                HandleActionEvent(ev, e);
                break;
              case ConnectorChangeEvent e:
                HandleConnectorEvent(ev, e);
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
    private bool ParseAndValidateEvent(DataContainerEventBase message, out VJEvent ev)
    {
      var isConn = message.GetType() == typeof(ConnectorChangeEvent);
      ev = new() { devId = VJoyCurrDevId, btnAction = ButtonAction.None, tpId = message.Id.Split('.').Last() };
      ev.type = Utils.TpStateNameToEventType(ev.tpId);
      if (ev.type == ControlType.None) {
        // check for special actions
        if (ev.tpId == C.IDSTR_ACTION_VJCONN)
          VJoyConnectAction(message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_ACT_VAL)));
        else
          _logger.LogWarning($"Unknown Action/Connector ID: '{message.Id}'.");
        return false;
      }
      var idStr = message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_TARGET_ID, isConn));
      if (string.IsNullOrWhiteSpace(idStr)) {
        _logger.LogWarning($"Required target control ID '{idStr}' is empty or invalid for action ID '{message.Id}'.");
        return false;
      }

      switch (ev.type) {
        case ControlType.Button:
          if (ev.targetId > _vjoyDevice.ButtonCount) {
            _logger.LogWarning($"Button Index out of range: {ev.targetId} out of {_vjoyDevice.ButtonCount} for vJoy device {ev.devId}.");
            return false;
          }
          break;

        case ControlType.DiscPov:
          if (ev.targetId > _vjoyDevice.DiscreteHatCount) {
            _logger.LogWarning($"D-POV Index out of range: {ev.targetId} out of {_vjoyDevice.DiscreteHatCount} for vJoy device {ev.devId}.");
            return false;
          } {
            var dirStr = message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_DPOV_DIR, isConn));
            if (string.IsNullOrWhiteSpace(dirStr) || !Enum.TryParse(dirStr, true, out ev.dpovDir)) {
              _logger.LogWarning($"Could not parse D-POV direction data for POV#: '{ev.targetId}'; direction: '{dirStr}' action: '{message.Id}'.");
              return false;
            }
          }
          break;

        case ControlType.ContPov:
          if (ev.targetId > _vjoyDevice.ContionousHatCount) {
            _logger.LogWarning($"C-POV Index out of range: {ev.targetId} out of {_vjoyDevice.ContionousHatCount} for vJoy device {ev.devId}.");
            return false;
          }
          ev.axis = HID_USAGES.HID_USAGE_POV;
          break;

        case ControlType.Axis:
          if (!Enum.TryParse("HID_USAGE_" + idStr, out ev.axis)) {
            _logger.LogWarning($"Axis ID is invalid: '{idStr}'.");
            return false;
          }
          if (!_vjoyDevice.HasDeviceAxis(ev.axis)) {
            _logger.LogWarning($"Axis {ev.axis} not available for vJoy device {ev.devId}.");
            return false;
          }
          ev.targetId = (uint)ev.axis;
          break;
      }

      if (ev.type != ControlType.Axis && !uint.TryParse(idStr, out ev.targetId)) {
        _logger.LogWarning($"Target control ID/number is invalid: '{idStr}'.");
        return false;
      }

      if (!isConn) {
        // actions always have the full range of a slider
        ev.rangeMin = 0;
        ev.rangeMax = 100;
        ev.valueStr = message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_ACT_VAL, isConn));
        if (string.IsNullOrWhiteSpace(ev.valueStr)) {
          _logger.LogWarning($"Primary value is invalid: '{ev.valueStr}'.");
          return false;
        }
        return true;
      }
      // Connector, get min/max range for axis or cpov type
      if (ev.type != ControlType.DiscPov) {
        var minStr = message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_RNG_MIN, true));
        var maxStr = message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_RNG_MAX, true));
        var revStr = message.GetValue(Utils.FullActionDataID(ev.tpId, C.IDSTR_DIR_REVERSE, true));
        AxisMovementDir revType = AxisMovementDir.Normal;
        if (string.IsNullOrWhiteSpace(minStr) || string.IsNullOrWhiteSpace(maxStr) ||
            (!string.IsNullOrWhiteSpace(revStr) && !Enum.TryParse(revStr, out revType)) ||
            !TryEvaluateValue(minStr, out ev.rangeMin) ||
            !TryEvaluateValue(maxStr, out ev.rangeMax)) {
          _logger.LogWarning($"Required range data  values are empty or invalid for Connector ID '{message.Id}' with control ID '{idStr}';  min/max/rev: '{minStr}'/'{maxStr}/{revStr}.");
          return false;
        }
        if (revType == AxisMovementDir.Reverse) {
          var tmpMax = ev.rangeMax;
          ev.rangeMax = ev.rangeMin;
          ev.rangeMin = tmpMax;
        }
      }
      return true;
    }

    private void HandleActionEvent(VJEvent ev, ActionEvent message)
    {
      // force button up/pov center/axis reset up on "up" held action
      if (message.GetPressState() == Press.Up)
        ev.btnAction = ButtonAction.Up;

      switch (ev.type) {
        case ControlType.Button:
          if (ev.btnAction == ButtonAction.None && !Enum.TryParse(ev.valueStr, true, out ev.btnAction)) {
            _logger.LogWarning($"Could not parse button action type: BTN#: '{ev.targetId}'; act: '{ev.valueStr}'.");
            return;
          }
          break;

        case ControlType.DiscPov:
          if (ev.btnAction == ButtonAction.None && !Enum.TryParse(ev.valueStr, true, out ev.btnAction)) {
            _logger.LogWarning($"Could not parse button action type for POV#: '{ev.targetId}'; act: '{ev.valueStr}'.");
            return;
          }
          break;

        case ControlType.ContPov:
          if (ev.btnAction == ButtonAction.Up) {
            if ((ev.value = GetResetValueFromEvent(message, ev.tpId, ev.type)) < -1)
              return;
          }
          else if (!TryEvaluateValue(ev.valueStr, out ev.value)) {
            _logger.LogWarning($"Cannot parse C-POV value for POV# {ev.targetId} from '{ev.valueStr}'.");
            return;
          }
          if (ev.value > -1)
            ev.value = _vjoyDevice.ScaleInputToAxisRange(HID_USAGES.HID_USAGE_POV, ev.value, 0, 100);
          break;

        case ControlType.Axis:
          if (ev.btnAction == ButtonAction.Up) {
            if ((ev.value = GetResetValueFromEvent(message, ev.tpId, ev.type)) < 0)
              return;
          }
          else if (!TryEvaluateValue(ev.valueStr, out ev.value)) {
            _logger.LogWarning($"Cannot parse Axis value for {ev.axis} from '{ev.valueStr}'.");
            return;
          }
          ev.value = _vjoyDevice.ScaleInputToAxisRange(ev.axis, ev.value, 0, 100);
          break;

        default:
          return;
      }
      _vjoyDevice.DispatchEvent(ev);
    }

    private void HandleConnectorEvent(VJEvent ev, ConnectorChangeEvent message)
    {
      bool needUpdate = true;
      string ckey = Utils.ConnectorDictKey(ev);
      ev.value = message.Value;

      if (!_connectorsDict.TryGetValue(ckey, out ConnectorTrackingData cdata)) {
        // in theory this shouldn't happen... we didn't find our connector in the table built from "short connector" reports.
        _logger.LogWarning($"Could not find Connector ID '{message.ConnectorId}' in the connectors cache!");
        cdata = ConnectorTrackingData.CreateFromEvent(ev, true);
        _connectorsDict[ckey] = cdata;
      }

      // Try to determine when a connector is released, the equivalent on an action "up" event, but a lot more complicated :(
      long ts = Stopwatch.GetTimestamp();
      if (!cdata.isDown || Utils.TicksToSecs(ts - cdata.lastUpdate) > C.CONNECTOR_MOVE_TO_SEC) {
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
          ev.value = _vjoyDevice.ScaleInputToAxisRange(ev.axis, ev.value, ev.rangeMin, ev.rangeMax); // message.Value; // Utils.SliderRange2AxisRange(message.Value, ev.rangeMin, ev.rangeMax);
          _logger.LogDebug($"Axis Connector Event: axe: {ev.axis}; orig val: {message.Value}; new value {ev.value}; range min/max: {ev.rangeMin}/{ev.rangeMax}");
          break;

        case ControlType.DiscPov:
          // translate axis slider value to actual dpov direction and a button action
          if (cdata.isDown) {
            ev.dpovDir = Utils.SliderRangeToDpovRange(ev.value, ev.dpovDir);
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
        _vjoyDevice.DispatchEvent(ev);

      // special case for POV sliders
      if (ev.type == ControlType.ContPov && ev.value == -1)
        cdata.lastValue = _vjoyDevice.ScaleInputToAxisRange(ev.axis, 50, ev.rangeMin, ev.rangeMax);
      else
        cdata.lastValue = ev.value;

      // Now do something with all this connector tracking data we've been collecting... update some sliders!
      cdata.currentShortId = string.Empty;
      if (cdata.isDown) {
        // find the short id the connector currently being moved, so that we can exclude it from any "live" updates
        var mappingId = ev.tpId + "|" + string.Join("|", message.Data.Select(d => d.Id + "=" + d.Value));
        _ = _connectorsLongToShortMap.TryGetValue(mappingId, out cdata.currentShortId);
      }
      //else    // uncomment to disable "live" connector updates of related sliders while one is moving
      UpdateRelatedConnectors(cdata);
    }

    // Gets a vJoy device value for the reset attributes specified in an action/connector.
    // Returns -2 if no reset is to be done.
    private int GetResetValueFromEvent(DataContainerEventBase message, string actId, ControlType evtype, int startValue = 0)
    {
      // Get reset type and value
      var isConn = message.GetType() == typeof(ConnectorChangeEvent);
      var rstStr = message.GetValue(Utils.FullActionDataID(actId, C.IDSTR_RESET_TYP, isConn))?.Replace(" ", string.Empty) ?? "None";
      var rvalStr = message.GetValue(Utils.FullActionDataID(actId, C.IDSTR_RESET_VAL, isConn)) ?? "-2";
      if (Enum.TryParse(rstStr, true, out CtrlResetMethod rstType) && rstType != CtrlResetMethod.None && TryEvaluateValue(rvalStr, out var customVal))
        return Utils.GetResetValueForType(rstType, evtype, customVal, startValue);
      return -2;
    }

    private void UpdateRelatedConnectors(/*object obj*/ ConnectorTrackingData data)
    {
      //if (obj?.GetType() != typeof(ConnectorTrackingData))
        //return;
      //var data = (ConnectorTrackingData)obj;
      foreach (var instance in data.relations) {
        if (string.IsNullOrEmpty(instance.shortId) || instance.shortId == data.currentShortId)
          continue;

        int value = data.lastValue;
        if (data.type == ControlType.DiscPov) {
          value = Utils.DPovRange2SliderRange(value, instance.dpovDir);
        }
        else {
          if (data.type == ControlType.ContPov && data.lastValue < 0)  // center POV
            value = Math.Abs(instance.rangeMax - instance.rangeMin) / 2;
          value = _vjoyDevice.ScaleAxisToInputRange(data.axis, value, instance.rangeMin, instance.rangeMax); // Utils.ConvertRange(data.lastValue, instance.rangeMin, instance.rangeMax, 0, C.TP_SLIDER_MAX_VALUE, true);
        }
        _logger.LogDebug($"[UpdateRelatedConnectors] Sending update for {instance.shortId} ({data.id}, {data.type}) with val {value}; orig val: {data.lastValue}; range: {instance.rangeMin}/{instance.rangeMax}");
        if (value > -1)
          UpdateTPConnector(instance.shortId, value);
      }
    }

    private bool TryEvaluateValue(string strValue, out int value)
    {
      value = 0;
      try {
        value = Convert.ToInt32(_expressionEvaluator.Compute(strValue, null));
      }
      catch (Exception e) {
        _logger.LogWarning(e, $"Failed to convert Action data value '{strValue}' to numeric value.");
        return false;
      }
      return true;
    }

    #endregion
    #region TP Event Handlers      ///////////////////////////////////////////

    public void OnInfoEvent(InfoEvent message)
    {
      _logger?.LogInformation($"[Info] VersionCode: '{message.TpVersionCode}', VersionString: '{message.TpVersionString}', SDK: '{message.SdkVersion}', PluginVersion: '{message.PluginVersion}', Status: '{message.Status}'");
      ProcessPluginSettings(message.Settings);
      _logger?.LogDebug($"[Info] Settings: {JsonSerializer.Serialize(_settings.tpSettings)}");
    }

    public void OnSettingsEvent(SettingsEvent message)
    {
      ProcessPluginSettings(message.Values);
      _logger?.LogDebug($"[OnSettings] Settings: {JsonSerializer.Serialize(_settings.tpSettings)}");
    }

    public void OnClosedEvent(string message) {
      _logger?.LogInformation(message);
      _settings.ClosedByTp = true;   // may not actually be true but in that case we have already quit
      Quit();
    }

    public void OnActionEvent(ActionEvent message)
    {
      _logger?.LogDebug("[OnAction] PressState: {0}, ActionId: {1}, Data: '{2}'",
          message.GetPressState(), message.ActionId, string.Join(", ", message.Data.Select(dataItem => $"'{dataItem.Id}': '{dataItem.Value}'")));
      _eventQ.Enqueue(message);
      _eventQueueReadyEvent.Set();
    }

    public void OnConnecterChangeEvent(ConnectorChangeEvent message)
    {
      _logger?.LogDebug("[OnConnecterChangeEvent] ConnectorId: {0}, Value: {1}, Data: '{2}'",
        message.ConnectorId, message.Value,
        string.Join(", ", message.Data.Select(dataItem => $"'{dataItem.Id}': '{dataItem.Value}'")));
      _eventQ.Enqueue(message);
      _eventQueueReadyEvent.Set();
    }

    // convoluted malarkey to try and track which slider instances operate on the same axes, and the whole "short ID" business
    public void OnShortConnectorIdNotificationEvent(ShortConnectorIdNotificationEvent message)
    {
      _logger.LogDebug($"[ShortConnectorIdNotificationEvent] ConnectorId: {message.ConnectorId}; shortId: {message.ShortId};");
      // split by pipe
      var values = message.ConnectorId?.Split('|');
      if (!values.Any())
        return;
      // get our actual connector id
      string connId = values.First().Split('.').Last();
      if (string.IsNullOrWhiteSpace(connId) || values.Length < 2)
        return;

      var evtype = Utils.TpStateNameToEventType(connId);
      if (evtype == ControlType.None || evtype == ControlType.Button) {
        _logger.LogWarning($"[OnShortConnectorIdNotificationEvent] Unknown ConnectorId '{connId}' (full: '{message.ConnectorId}')");
        return;
      }
      uint targetId = 0;
      //uint devId = 0;
      HID_USAGES axis = 0;
      AxisMovementDir reverse = AxisMovementDir.Normal;
      var idata = new ConnectorInstanceData { shortId = message.ShortId };

      // extract the attributes we are interested in from the remaining key=value pairs
      for (int i = 1, e = values.Length; i < e; ++i) {
        var keyVal = values[i].Split('=');
        if (keyVal.Length != 2)
          continue;
        var did = keyVal[0].Split('.').Last();
        var dval = keyVal[1];
        switch (did) {
          case C.IDSTR_TARGET_ID:
            if (evtype == ControlType.Axis) {
              if (Enum.TryParse("HID_USAGE_" + dval, out axis))
                targetId = (uint)axis;
            }
            else if (!uint.TryParse(dval, out targetId)) {
              targetId = 0;
            }
            else {
              axis = HID_USAGES.HID_USAGE_POV;
            }
            break;

          case C.IDSTR_RNG_MIN:
            if (!int.TryParse(dval, out idata.rangeMin))
              _vjoyDevice.GetDefaultAxisRange(axis, out idata.rangeMin, out _); // idata.rangeMin = 0;
            break;

          case C.IDSTR_RNG_MAX:
            if (!int.TryParse(dval, out idata.rangeMax))
              _vjoyDevice.GetDefaultAxisRange(axis, out _, out idata.rangeMax); // idata.rangeMax = Utils.GetMaxValueForEventType(evtype);
            break;

          case C.IDSTR_DPOV_DIR:
            if (!Enum.TryParse(dval, out idata.dpovDir))
              idata.dpovDir = DPovDirection.Center;
            break;

          case C.IDSTR_DIR_REVERSE:
            if (!Enum.TryParse(dval, out reverse))
              reverse = AxisMovementDir.Normal;
            break;

          //case IDSTR_DEVICE_ID:
          //  if (dval != "default" && !uint.TryParse(dval, out devId))
          //    devId = 0;
          //  break;

          default:
            continue;
        }
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

      string key = Utils.ConnectorDictKey(connId, targetId);
      if (!_connectorsDict.TryGetValue(key, out var cdata)) {
        cdata = new ConnectorTrackingData {
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
      var mappingId = connId + "|" + string.Join("|", values[1..^0]);
      _connectorsLongToShortMap.TryAdd(mappingId, message.ShortId);

      // Send the current axis value to the connector. Refresh the vJoy state first if needed.
      if (Utils.TicksToSecs(Stopwatch.GetTimestamp() - _vjoyDevice.DeviceInfo().lastStateUpdate) > 10)
        _vjoyDevice.RefreshState();
      switch (evtype) {
        case ControlType.Axis:
          cdata.lastValue = Utils.GetVJoyStateReportAxisValue(_vjoyDevice.DeviceState(), (HID_USAGES)cdata.targetId);
          break;
        case ControlType.ContPov:
          cdata.lastValue = Utils.GetVJoyStateReportCPovValue(_vjoyDevice.DeviceState(), cdata.targetId);
          break;
        case ControlType.DiscPov:
          cdata.lastValue = Utils.GetVJoyStateReportDPovValue(_vjoyDevice.DeviceState(), cdata.targetId);
          break;
        default:
          return;
      }
      UpdateRelatedConnectors(cdata);
    }

    // Unused handlers below here.

    public void OnListChangedEvent(ListChangeEvent message) {
      _logger.LogDebug($"[OnListChanged] {message.ListId}/{message.ActionId}/{message.InstanceId} '{message.Value}'");
    }

    public void OnBroadcastEvent(BroadcastEvent message) {
      _logger.LogDebug($"[Broadcast] Event: '{message.Event}', PageName: '{message.PageName}'");
    }

    public void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message) {
      _logger.LogDebug($"[OnNotificationOptionClickedEvent] NotificationId: '{message.NotificationId}', OptionId: '{message.OptionId}'");
    }

    public void OnUnhandledEvent(string jsonMessage) {
      _logger.LogDebug($"Unhanded message: {jsonMessage}");
    }

    #endregion
    #region Archive        ///////////////////////////////////////////

    #endregion
  }
}
