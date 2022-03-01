
using System.Collections.Generic;
using TJoy.Enums;
using TouchPortalSDK.Messages.Models;
using vJoyInterfaceWrap;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TJoy.Types
{
  internal struct VJAxisInfo
  {
    public HID_USAGES usage;
    public int minValue;
    public int maxValue;
  }

  internal struct VJDeviceInfo
  {
    public uint id;
    public ushort nButtons;
    public ushort nContPov;
    public ushort nDiscPov;
    public long lastStateUpdate;
    public Dictionary<HID_USAGES, VJAxisInfo> axes;
    public vJoy.JoystickState state;
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

  internal struct ConnectorInstanceData
  {
    public string shortId;
    public int rangeMin;
    public int rangeMax;
    public DPovDirection dpovDir;
  }

  internal class ConnectorTrackingData
  {
    //public uint devId;
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
        rangeMax = ev.rangeMax
      };
      ConnectorTrackingData ret = new() {
        //devId = ev.devId,
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
    public uint VjDeviceId = 0;                // configured vJoy device ID to use
    public uint StateRefreshRate = 0;          // how often to send joystick device state updates, 0 to disable
    public ushort MinBtnNumForState = 1;       // start button state reports at this button, zero to disable
    public ushort MaxBtnNumForState = 128;     // end button state reports at this button
    public bool ClosedByTp = false;            // flag indicating TP requested the exit (vs. unexpected)
    public IReadOnlyCollection<Setting> tpSettings;
  }
}
