
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TJoy.Enums;
using TouchPortalSDK.Messages.Models;
using Stopwatch = System.Diagnostics.Stopwatch;
#if USE_VGEN
using vJoy = vGenInterfaceWrap.vGen;
#else
using vJoy = vJoyInterfaceWrap.vJoy;
#endif

namespace TJoy.Types
{
  using JoystickState = vJoy.JoystickState;
  using XInputState = vJoy.XInputState;
  using DualShock4State = vJoy.DualShock4State;

  [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
  internal struct VJDState
  {
    [System.Runtime.InteropServices.FieldOffset(0)]
    public JoystickState vJoyState;
    [System.Runtime.InteropServices.FieldOffset(0)]
    public XInputState xInputState;
    [System.Runtime.InteropServices.FieldOffset(0)]
    public DualShock4State DS4State;
  }

  internal struct VJAxisInfo
  {
    public HID_USAGES usage;
    public int minValue;
    public int maxValue;
  }

  internal struct VJDeviceInfo
  {
    public DeviceType deviceType;
    public uint id;
    public uint index;       // id minus device type
    public int handle;       // device handle ID, from AcquireDev()
    public byte ledNumber;   // for XBox
    //public uint colorBar;    // for PS4
    public ushort nButtons;
    public ushort nContPov;
    public ushort nDiscPov;
    public ushort nAxes;     // it's just the length of the axes array but it's used often
    public uint driverVersion;
    public long lastStateUpdate;
    public string typeName;  // name of this device type
    public VJDState state;
  }

  internal struct VJEvent
  {
    public uint devId;
    public ControlType type;
    public ButtonAction btnAction;
    public DPovDirection dpovDir;
    public HID_USAGES axis;
    public uint targetId;
    public int value;
    public int rangeMin;
    public int rangeMax;
    //public uint repeat;
    //public uint repeatCnt;
    public string tpId;
    public string valueStr;
  }

  internal class ConnectorInstanceData
  {
    public string shortId;
    public int rangeMin;
    public int rangeMax;
    public int lastValue;
    public DPovDirection dpovDir;
  }

  internal class ConnectorTrackingData
  {
    public uint devId;
    public string id;
    public ControlType type = ControlType.None;
    public HID_USAGES axis = 0;
    public uint targetId = 0;
    public int startValue;
    public int lastValue = -2;
    public byte lastTpValue = 0xFF;
    public bool isDown;
    public long lastUpdate = 0;
    public string currentShortId;
    public List<ConnectorInstanceData> relations = new();

    public static ConnectorTrackingData CreateFromEvent(in VJEvent ev, bool isdown = false)
    {
      var idata = new ConnectorInstanceData {
        shortId = string.Empty,
        rangeMin = ev.rangeMin,
        rangeMax = ev.rangeMax,
        lastValue = -1,
      };
      ConnectorTrackingData ret = new() {
        devId = ev.devId,
        id = ev.tpId,
        type = ev.type,
        axis = ev.axis,
        targetId = ev.targetId > 0 ? ev.targetId : (uint)ev.axis,
        isDown = isdown,
        lastValue = ev.value,
        lastUpdate = Stopwatch.GetTimestamp(),
        relations = new List<ConnectorInstanceData> { idata },
      };
      return ret;
    }
  }

  internal class PluginSettings
  {
    public bool StateReportAxisRaw = false;    // report raw axis values instead of percent
    public uint DefaultDeviceId = 0;           // default device ID to use from plugin settings
    public uint StateRefreshRate = 0;          // how often to send joystick device state updates, 0 to disable
    public ushort MinBtnNumForState = 1;       // start button state reports at this button, zero to disable
    public ushort MaxBtnNumForState = 128;     // end button state reports at this button
    public bool ClosedByTp = false;            // flag indicating TP requested the exit (vs. unexpected)
    public bool HaveVJoy = false;              // vJoy driver installed
    public bool HaveVXbox = false;             // vXBox driver installed
    public bool HaveVBus = false;              // ViGEm Bus driver installed
    public bool AutoConnectDevice = true;      // Auto-connect new devices on demand (from action/connector event)
    public List<uint> AvailableVJoyDevs = new();  // list of vJoy devices configured on the system.
    //public List<uint> UseDevices = new();      // list of configured device IDs to use
    public IReadOnlyCollection<Setting> tpSettings;  // keep a copy for ease of comparing for changed values when TP sends a settings update
  }
}
