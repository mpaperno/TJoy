# TJoy - Touch Joystick Virtual Controller Project

## 1.0.0.1 (12-Jul-2022)
* Added "Set Slider Position" action to visually reflect the value of any variable, for example a throttle lever which is set externally.
* Dynamic position report states are now visually sorted into categories per device (requires TP v3.0.10 or higher).
* Fixed (hopefully) vJoy initial re-connection issue after manually disconnecting from a device (had to try twice).
* Fixed DPOV selector in actions/connectors showing directions instead of available POVs.
* Prevent log spamming when position reports are enabled but the polled device doesn't exist or has a driver issue.
* Fixed missing plugin icon in TP Actions list.
* Improve logging output template.
* Update to .NET v6 runtime.

## 0.9.5 (18-Mar-2022)
* Added support for `vXBox` (a.k.a. ScpVBus) virtual XBox 360 gamepad driver with up to 4 devices.
* Added support for using multiple devices at the same time, up to 16 vJoy joysticks and/or 4 gamepads.
* Due to naming changes and new features, any current pages/control which used previous versions of TJoy
	will need to be	re-create the actions/sliders and any used states. Some of the settings names have also
	changed and will be reset to defaults.
* Please see the README for full details of current features and settings.

## 0.9.1 (7-Mar-2022)
* Fixed that vJoy Continuous POV hat direction, when used with a slider, was rotated 180 degrees from expected.
* Any Slider(s) on the same axis/POV as a Button action will now update their position(s) when that action is triggered.

## 0.9.0 (2-Mar-2022)
Initial release
