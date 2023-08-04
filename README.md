[![Made for Touch Portal](https://img.shields.io/static/v1?style=flat&labelColor=5884b3&color=black&label=made%20for&message=Touch%20Portal&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAetJREFUeNp0UruqWlEQXUePb1HERi18gShYWVqJYGeXgF+Qzh9IGh8QiOmECIYkpRY21pZWFnZaqWBhUG4KjWih4msys8FLbrhZMOfsx6w1e9beWjAYBOMtx0eOGBEZzuczrtcreAyTyQSz2QxN04j3f3J84vim8+cNR4s3rKfTSUQQi8UQjUYlGYvFAtPpVIQ0u90eZrGvnHLXuOKcB1GpkkqlUCqVEA6HsVqt4HA4EAgEMJvNUC6XMRwOwWTRfhIi3e93WK1W1Go1dbTBYIDj8YhOp4NIJIJGo4FEIoF8Po/JZAKLxQIIUSIUChGrEy9Sr9cjQTKZJJvNRtlsVs3r9Tq53W6Vb+Cy0rQyQtd1OJ1O9b/dbpCTyHoul1O9z+dzGI1Gla7jFUiyGBWPx9FsNpHJZNBqtdDtdlXfAv3vZLmCB6SiJIlJhUIB/X7/cS0viXI8n8+nrBcRIblcLlSrVez3e4jrD6LsK3O8Xi8Vi0ViJ4nVid2kB3a7HY3HY2q325ROp8nv94s5d0XkSsR90OFwoOVySaPRiF6DiHs8nmdXn+QInIxKpaJclWe4Xq9fxGazAQvDYBAKfssDeMeD7zITc1gR/4M8isvlIn2+F3N+cIjMB76j4Ha7fb7bf8H7v5j0hYef/wgwAKl+FUPYXaLjAAAAAElFTkSuQmCC)](https://www.touch-portal.com/)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/mpaperno/TJoy?include_prereleases)](https://github.com/mpaperno/TJoy/releases)
[![Downloads](https://img.shields.io/github/downloads/mpaperno/TJoy/total.svg)](https://github.com/mpaperno/TJoy/releases)
[![License](https://img.shields.io/badge/license-GPL3-blue.svg)](LICENSE)
[![Discord](https://img.shields.io/static/v1?style=flat&color=7289DA&&labelColor=7289DA&message=Discord%20Chat&label=&logo=discord&logoColor=white)](https://discord.gg/424r5M8cKy)

# TJoy - Touch Joystick Virtual Controller Project

<p align="center"><img src="https://user-images.githubusercontent.com/1366615/178935496-19a2dac5-84fe-4d93-ab24-385bed2f243a.png" /></p>

## TJoy Touch Portal Plugin

This project is currently implemented as a "plugin" for the [Touch Portal](https://www.touch-portal.com) macro launcher software,
designed for integrating with virtual gaming controllers. It currently fully supports the [vJoy](https://github.com/jshafer817/vJoy) virtual joystick driver,
[ScpVBus](https://github.com/shauleiz/ScpVBus) (a.k.a _vXBox_) virtual XBox 360 gamepad driver,
and [ViGEm Bus](https://github.com/ViGEm/ViGEmBus) (XBox 360 and DualShock 4 controller emulation) in the works.

This project is in the beginning phase so please bear with me while I expand the documentation, refine features, or fix bugs.
Your testing and feedback is important, please help!

## Features

* Supports all controls provided by joystick driver(s):
  * `vJoy`: Up to 16 joystick devices, each with up to 8 axes, 4 continuous (360 degree) or 4-way POV hats, and 128 buttons.
  * `ScpVBus`: Up to 4 "XBox 360" type gamepads each with 4 joystick axes, 2 slider (trigger) axes, one 8-way D-Pad, and 11 other buttons.
  * `ViGEm Bus`: Up to 4 each of "XBox 360" and "PS DualShock 4" type gamepads with all associated controls.
  * (That's up to 192 axes, 76 Hats/D-Pads, and 2,192 possible buttons, in case you were wondering :)
* Use Touch Portal Actions and Sliders for all joystick controls (joystick buttons are actions only).
* Multiple options for each joystick control. Click, hold, or toggle buttons/hats, adjust axis range and precision,
  reverse axis direction, set up custom mixes, button sequences, and much more.
* Control multiple joystick buttons/axes/hats simultaneously (usually up to 10, depending on your touchscreen and device performance).
* Use calculations in action data fields, for example to control a joystick axis value based on the value of
  a variable or state, with basic math operators available (+, -, *, /, % (modulo)).
* Use multiple linked controls for the same axis, for example a "coarse" slider over the full range
  of an axis and a "fine" slider which controls a smaller range of the same axis. Linked controls are
  updated in real-time.
* Optional reporting of current joystick axis positions and button states at a configurable update frequency.
* Connect and disconnect to joystick devices on demand (automatically or via an action) and monitor current connection status via events.


## Examples

Check what's in the [assets](https://github.com/mpaperno/TJoy/tree/main/assets) folder in this repo. There are a few demo/test pages with screenshots
and a small icon pack. These are not included with the release download, so download them separately from that folder if you want.


## Setup

### Supported Virtual Joystick/Gamepad Drivers

  * **vJoy v2.1.x** series driver for virtual joysticks. Latest signed and working version is here: [v2.1.9.1](https://github.com/jshafer817/vJoy/releases/tag/v2.1.9.1).
    * Reports are that it works on Windows 11, but it may need a few tries to install.  I have personally only tested on Windows 10 21H2.
    * In theory _TJoy_ _should_ work with older 2.x versions but I haven't tested.
    * **vJoy v2.2.x series drivers are currently only partially supported.** Specifically the joystick states are not available. It does not currently support Win11 either.
      This is pending more testing and may change if it eventually gets fixed for Win11. I recommend using v2.1.9 for now.
  * **ScpVbus v1.7.1.2** ("vXBox") for emulating up to 4 XBox 360-style Gamepads: [Download at https://github.com/shauleiz/ScpVBus/releases](https://github.com/shauleiz/ScpVBus/releases).
  * **ViGEm Bus v1.21.442** for emulating up to 4 XBox 360 and/or PS DualShock4 style Gamepads: [Download at https://github.com/ViGEm/ViGEmBus/releases](https://github.com/ViGEm/ViGEmBus/releases).

### Requirements:
* **One or more virtual joystick/gamepad driver** from the list of supported ones above. You **must** install driver(s) separately otherwise _TJoy_ can do nothing useful.
* [Touch Portal](https://www.touch-portal.com) Pro (paid version) for Windows, v3.0.6 or newer.
* The latest version of this plugin: get the `TJoy-TouchPortal-Plugin-X.X.X.X.tpp` file from the latest release on the [Releases](https://github.com/mpaperno/TJoy/releases) page.

### Install:
1. Install (or already have installed) one or more of the supported virtual joystick drivers listed above. Also of course you will need Touch Portal installed.
2. The _TJoy_ plugin is distributed and installed as a standard Touch Portal `.tpp` plugin file. If you know how to import a plugin,
just do that and skip to the next _Configure_ section.
3. Import the plugin:
    1. Start/open _Touch Portal_.
    2. Click the Settings "gear" icon at the top-right and select "Import plugin..." from the menu.
    3. Browse to where you downloaded this plugin's `.tpp` file and select it.
    4. When prompted by _Touch Portal_ to trust the plugin startup script, select "Trust Always" or "Yes" (the source code is public!).
       * "Trust Always" will automatically start the plugin each time Touch Portal starts.
       * "Yes" will start the plugin this time and then prompt again each time Touch Portal starts.
       * If you select "No" then you can still start the plugin manually from Touch Portal's _Settings -> Plug-ins_ dialog.
4. That's it. You should now have the plugin's actions available to you in Touch Portal.

### Configure
Several settings are available in the _Touch Portal_ _Settings_ window (select _Plug-ins_ on the left, then
_TJoy Touch Portal Plugin_ from the drop-down menu). The options are as follows:

* `Default Device`: The name/number of the default device configured on your system which you would like to use.
  All the action and slider controls are set to use the "Default" device by default. If you mostly use one device,
  this prevents the need to select a device for each action/slider. The default device will also be connected automatically
  at startup.
  * You can enter just a number here, for example for `vJoy` you would specify a device ID of 1-16, or for one of the gamepad
    drivers it could be 1-4. If you have multiple drivers installed, it will look for a vJoy joystick first, then an ScpVBus XBox360 device, and finally
    a ViGEm XBox360.
    drivers.
  * You can also be more specific here by providing a device (driver) name as well as a number. For example: `vJoy 1` or `vXBox 4`.
    The device names are as follows (case insensitive): `vJoy` (vJoy driver), `vXBox` (ScpVBus driver), `vgeXBox` and `vgeDS4` (ViGEm driver).
  * Enter zero to disable the default device feature. In this case you will need to select a specific device to use for every
    action/slider you configure.

* `Auto-Connect Device On Action/Slider Event`: When enabled (`1`), activating an action (button) or moving a slider which
  controls a non-connected device will automatically try connecting to it. Set to `0` to disable.

* `Position State Report Update Interval`: The plugin can optionally send the current joystick axis and button values as TP
  States.  Enter how often to send these updates, in milliseconds, or enter zero to disable them.

* `Report Raw Axis Values`: When reporting is enabled (above), the axis values are by default sent as percentages.
  Enable this option to get the actual raw values instead (eg. for vJoy axis it's 0-32767).

* `Buttons To Report`: vJoy can have up to 128 buttons configured.  That's a lot of states to send over if you don't
  need them all. Here you can specify the maximum number of buttons to report (eg. `8` to get buttons 1-8),
  or a range of buttons to report for, for example `32 - 64` to get only those buttons states sent over. Enter zero
  to not send any button states at all.


## Known Issues

* **Please note that support for detecting when a slider has been released (is no longer being touched) is spotty at the moment.**
This is very useful in a joystick-like control, but due to some vague language on the Touch Portal API reference site,
it turned out this feature maybe wasn't even meant to exist in TP.  It does, however, work most of the time
if you pause for a fraction of a second before releasing the slider.
  * I have a request in with the TP authors to improve this detection so it is more reliable, since it is vital for things
like "self-centering" joystick axes. Please help by also requesting this feature on the Touch Portal Discord server or via other support means.


## Update Notifications

The latest version of this software is always published on the GitHub [Releases](https://github.com/mpaperno/TJoy/releases) page.

You have several options for getting **automatically notified** about new releases:
* **If you have a GitHub account**, just open the _Watch_ menu of this repo in the top right of this page, then go to  _Custom_ and select the
_Releases_ option, then hit _Apply_ button.
* If you use **Discord**, subscribe to notifications on my server channel [#tjoy-plugin](https://discord.gg/nJ7w9g2Wrr).
* **If you already use an RSS/Atom feed reader**, just subscribe to the [feed URL](https://github.com/mpaperno/TJoy/releases.atom).
* **Use an RSS/Atom feed notification service**, either one specific for GitHub or a generic one, such as
(a list of services I found, I haven't necessarily tried nor do I endorse any of these):
  * https://blogtrottr.com/  (generic RSS feed notifications, no account required, use the [feed URL](https://github.com/mpaperno/MSFSTouchPortalPlugin/releases.atom))
  * https://coderelease.io/  (no account required)
  * https://newreleases.io/
  * https://gitpunch.com/

## Usage

### Actions & Sliders

#### Virtual Joystick Device (VJD)

There is an Action and a Slider for each type of joystick control (except there are no sliders for joystick buttons).

The first option in all actions/sliders is to select a Joystick/Gamepad Device to use, or the "Default" one as mentioned in the
configuration notes above.  The list of devices should be populated with what is available on your system. For example if you have 4 vJoy
joysticks configured, they will each show up as a choice (vJoy 1, vJoy 2, etc). If you have one of the gamepad drivers installed, there will
be 4 gameapd devices to choose from.

After that you will choose a button/axis/DPad for the action/slider to work on.  This list is populated based on the device you selected,
and in some cases which actual controls are configured for that device (eg. vJoy devices can be configured with any number of axes, buttons, or POV hats).
For gamepads you will always see all the available controls with appropriate names (eg. axis Lx or Ry, buttons A/X/LB, etc). Note that the gamepad
D-Pad directions are also available as individual buttons.

* `Button Action` - Select the button number or name to activate from the available choices.
  You also select the action, one of `Click`, `Down`, or `Up`. `Click` will issue a button press followed automatically by
  a release after 50ms.
  * To have it act as a "natural" button, which is pressed while you hold it and released when you let go,
    use this action in the `On Hold` button area and select the `Hold` choice for the button action (the "Up" action will
    happen automatically whenever you release the button).  This also works for quick "clicks" (you don't have to hold the button),
    since the `On Hold` action fires on a quick touch as well.

* `D-Pad / D-POV Hat Action/Slider` -  Controls a 4-way (vJoy) or 8-way (gamepad) D-Pad/Hat switch.
  * The Action is very similar to the button action, except you also select the hat direction to activate from a list
    (using 4 or 8 compass directions, plus "Center"). And like buttons you can select the action to perform,
    `Click`, `Down`, or `Up`. The D-Pad/hat returns to the center position when all other directions are released.
    * Like joystick buttons, for a "natural" button feel use this action in the `On Hold` button setup with the `Down` action.
      The POV will return to center when you release the TP button.
    * For vJoy, if your device is configured with a "Continuous" type POV, this action will still work to control the 4 available directions.
  * A Slider can be set up to control either the North/South buttons (`Y` axis), the East/West buttons (`X` axis), or go all the
    way around the 4/8 directions, starting with either North/South and then going East/West (`YX` axis) or vice versa, East/West first
    and then North/South (`XY` axis). The POV center position is always in the middle of the slider.
    * For example a vertical slider set up for the `Y` axis would press the `North` button when pushed up from center and would press the
      `South` POV direction when moved down from center. A horizontal slider set up for the `XY` axis would progressively press the
      `East->South->West->North` buttons when moved to the right of center, and would reverse that when moved left of center, i.e.
      `West->South->East->North`.

* `Joystick Axis (Action/Slider)` -  Controls any of the available joystick/gamepad axes (this includes the "slider" triggers on gamepads).
  At the basic level, an axis has a value range between 0 and 100
  which you can set with either actions or sliders.  The value is simply the percentage of the full range of travel available on the axis,
  with zero at the bottom/left and 100 at the top/right. Axis controls also introduce the concept of "reset position" which is where you
  would like the axis value to return to when you release the button/slider controlling that axis. It's like a spring on a joystick axis,
  except you have full control of it, including being able to disable that feature altogether.
  * The Action basically lets you move an axis to any predetermined position in the 0-100 range. When used in the `On Hold` button action, you can set where
    the axis returns to when you release the button (any of the presets like `Center`/`Min`/`Max`/etc, or a custom value). You may use fractional
    amounts for the value and reset fields, to allow for very precise axis positioning (more so than with sliders).
    * Note that you can use calculations in the action's axis value (and custom reset) fields. Basic math operators are supported, `+`, `-`, `*`, `/`, `%` (modulo)
      as well as grouping with parenthesis. Of special note is that one could use variables in these fields as a basis for calculations, such
      as any local variable or numeric state from a plugin. For example, using the current `RZ` axis' value as the basis to move the axis
      by 10% steps:<br/>
      `${value:us.wdg.max.tpp.tjoy.state.vJoy.1.axis.RZ} + 10`
  * The Slider type of course moves a selected axis as one would expect. Things to note here is that you can reverse the movement direction
    (so sliding up/right will decrease the joystick axis value instead of increasing it), and that you can select the range of the axis
    you wish to move within. The default range is the full range of the axis, 0 - 100%, but you can also achieve more precise control over a smaller
    range of axis movement, or you could also _extend_ the range which would give faster control of an axis with less precision.
      * The Touch Portal sliders only move in whole steps in the 0-100 range, whereas the virtual joystick axes actually
        have much finer resolution (valid values for vJoy are 0 - 32,767).  By limiting the overall range the slider is controlling, you can get much
        finer adjustments.  For example if you limit travel within the 25% - 75% range, you just doubled the control resolution of that axis. You could then,
        for instance, calibrate the game or system using this smaller range (so it appears as 100% travel to the game).
      * Conversely, you could specify a larger movement range here as well. For example using a range of `0% to 200%` will double the movement speed
        along the axis, and also allow the axis to "wrap" back to its starting point and continue up to maximum range a second time (if you don't want the
        wrapping effect then you need to calibrate  that axis in your game or Windows, same as above).

* `Continuous 360° POV Hat (Action/Slider)` - Controls any of the 4 possible vJoy 360 degree "continuous" POV hats which move from center in any direction.
  The operation is nearly identical to the Axis controls described above, with the main difference being that "center" on a POV hat is a neutral position,
  when no other input is being given.
  * Again the Action options here are the same as for Axis controls where you can just set whichever value you wish the POV to move to when the action
    button is pressed or held.  The only difference is that there is a special value of `-1` which means the center/neutral hat position.  Same as with
    Axis controls, when used in `On Hold` button setup you can set where the POV hat returns to once the button is released (typically to center/neutral).
    * This action can also control a D-Pad/D-POV hat if that is what the device has. This is mainly for convenience with vJoy devices so either type of POV
      hat can be controlled with the same action.
  * The Sliders also work the same way as axes and you can again reverse the movement direction or set a smaller range of control for more precise movement.
    The difference here is that a slider will return to it's center point when a POV hat is in the neutral position. Currently there is no way to specify which
    point of the slider is "neutral" and no way to explicitly center a POV hat with a slider except using the "reset to center" option which works upon
    release of the slider.

#### Plugin

* `Virtual Joystick Device Actions` - Controls the connection to devices being used as well as some other options:
  * `Toggle`, `Connect` or `Disconnect` a specific device.
  * `Reset` all the device controls to neutral/default values (center axes, release buttons, etc).
  * `Refresh Report` to manually request a position state report (see States).
  * `Force Unplug` a gamepad device. If it happens that a virtual gamepad doesn't get properly removed/freed (either by _TJoy_ itself or another application)
    this is a way to force its removal and will allow _TJoy_ (or another application) to use that device again.
* `Set Slider Position` - Sets any TP slider(s) which are connected to the specified VJD and axis/POV to a specified position (0-100).
  This is intended to compensate for the lack of any built-in way in Touch Portal to visually set a slider position to reflect some value.
  * It does _not_ affect the actual joystick axis value, only the visual slider(s) position.
  * Could be used with any State or TP Value to show external feedback on an axis. For example to reflect a simulated vehicle throttle position when it is moved
    externally (not via the Touch Portal slider).
  * The position value must evaluate to numeric in the range of 0 to 100. Basic math operators (`+`, `-`, `*`, `/`, `%`, parenthesis) can be included in the position value.

### States

#### Dynamic States
These are only sent if enabled in the plugin's Settings. See notes for `Position State Update Interval` configuration, above.

`<Device>` represents a device name and number, eg. "vJoy 1" or "vXbox 3". `<N>` represents a number or a name of the control, eg. "1" or "A" for a button,
"Y" or "Ly" for an axis, etc.
* `<Device> - Button <N>` - `0` for off and `1` for on (pressed)
* `<Device> - Axis <N>` - A value in the range of 0 - 100 reflecting axis position, or a "raw" range (0 - 32,767 for vJoy) if so configured in the plugin's settings.
* `<Device> - Continuous Hat <N>` - A value in the range of 0 - 100 reflecting POV hat axis position or `-1` indicating neutral state. Or, a "raw" range
  (-1 through 35,900 for vJoy) if so configured in the plugin's settings.
* `<Device> - Discrete Hat <N>` - This will be a string representing a compass D-Pad/POV direction, one of `North`, `East`, `South`, `West` for 4-way hats (vJoy)
  and adding `NorthEast`, `SouthEast`, `SouthWest` and `NorthWest` for 8-way gamepad D-Pads.

#### Static States
* `Gamepad <N> LED Number` - This is the "player number" assigned by the Windows system to every plugged-in gamepad. This is not necessarily the same as the
  device number you use to connect/control the gamepad (eg. gamepad "1" may get any of the 4 player numbers assigned to it, somewhat at random).
  Each of the 4 gamepads sends one of these states. The possible values are 0 (off) and 1-4 when connected.
* `ID of the last connected/disconnected VJD` - There are 2 states which are used to trigger the Connected/Disconnected events (see below).
  They briefly show the name of the last device which connected or disconnected, but are then quickly cleared so they not very useful in and of themselves.
  Use the provided events instead to determine connection status.

### Events
* `Virtual Joystick Device Connected` - Allows choosing a device from a list and is triggered when the selected device connects.
* `Virtual Joystick Device Disconnected` - Triggered when the selected device disconnects.


## Monitoring Joystick Inputs

When setting up your joystick controls it can of course be very useful to see the actual result in real time. `vJoy` comes with a basic monitoring application
(_vJoy Monitor_) which is suitable for this task. There are other similar tools available online.  You can also use the Windows gaming devices control panel
feature to check the inputs, however be aware that this only updates the display when the windows has focus (it's easy to assume your inputs aren't working
while it's actually just the status display that's not updating). For gamepads, there are several online checkers and a few apps available (search for example "gamepad test online").

## Troubleshooting (Log File)

The plugin keeps a log file while running. This log file is in the plugin's installation folder, which will be in the Touch Portal data directory:<br/>
`C:\Users\<User Name>\AppData\Roaming\TouchPortal\plugins\TJoy-TouchPortal-Plugin\logs`<br/>
where `<User Name>` is your current Windows user name.
By default all warnings and errors will be logged, as well as some basic information about the Touch Portal connection and the vJoy device being used (if any).
This log is the first place to look if you suspect something isn't working correctly. For example it will show you if you're trying to use a joystick control
(button/axis/hat) which isn't set up in vJoy.

**This logged information will be vital in trying to track down any issues with the plugin code or functionality.** Please locate and consult your log file
before seeking support with the plugin, since I (or others) will very likely request to see it. The logs should not contain any sensitive or personal information,
(although it's always good to check before posting anything online... and unless you consider how your vJoy device is set up personal or sensitive, I guess!).

The log files are automatically rotated every day, and by default only the last 7 days are kept and the older ones deleted.

The logging levels (information/warning/debug) and log retention period can be set in the<br/>
`C:\Users\<User Name>\AppData\Roaming\TouchPortal\plugins\TJoy-TouchPortal-Plugin\dist\appSettings.json` file. But note that this file will get overwritten
if the plugin is updated or reinstalled.

## Bugs and Support

I've only tested this whole thing in very limited conditions so far (my main Windows 10 PC).
Your mileage may vary, as they say!  But I'm happy to help figure out any problems and improve the plugin.
Especially if you provide the log file...  ;-)   (see above)

Open an [Issue](https://github.com/mpaperno/TJoy/issues) here on GitHub, start a
[Discussion](https://github.com/mpaperno/TJoy/discussions) or ping me on my [Discord server](https://discord.gg/424r5M8cKy).

Please provide as much detail as possible. Did I mention log files already? I like log files.


## Credits

This project is written, tested, and documented by myself, Maxim (Max) Paperno.<br/>
https://github.com/mpaperno/

Uses a modified version of [TouchPortalSDK for C#](https://github.com/oddbear/TouchPortalSDK)
which is included in this repository and also [published here](https://github.com/mpaperno/TouchPortalSDK).
It is used under the MIT license.

Uses a heavily modified version of [vGen](https://github.com/shauleiz/vGen) API layer for vJoy and vXBox devices,
which is included in this repository (and is expanded to handle ViGEm devices as well).
It is Copyright (c) 2017 Shaul Eizikovich and is used under the MIT license.

Uses a slightly modified and custom built version of the v2.1.9.1 vJoy SDK from https://github.com/jshafer817/vJoy
which is Copyright (c) 2017 Shaul Eizikovich and is used under the MIT license.

Uses a rebuilt (but unchanged) version of the [XOutput API library](https://github.com/shauleiz/ScpVBus/tree/master/XOutput)
which is Copyright (c) Benjamin Höglinger, Shaul Eizikovich and used under the GPL v3 license.

Includes (though currently unused) 2 versions (forks) of the vJoy Interface SDK library DLLs,
v 2.2.1 from https://github.com/njz3/vJoy and v2.1.9 from https://github.com/jshafer817/vJoy
which are Copyright (c) 2017 Shaul Eizikovich and are used under the MIT license.

Joystick and gamepad images from 
[all-free-download.com](https://all-free-download.com/free-vector/download/game_consoles_vectors_59146.html), 
"free for non commercial use only," and used under CC-BY license (as far as I can tell).


## Copyright, License, and Disclaimer

TJoy Project<br/>
Copyright Maxim Paperno, all rights reserved.

This program and associated files may be used under the terms of the GNU
General Public License as published by the Free Software Foundation,
either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

A copy of the GNU General Public License is included in this repository
and is also available at <http://www.gnu.org/licenses/>.

This project may also use 3rd-party Open Source software under the terms
of their respective licenses. The copyright notice above does not apply
to any 3rd-party components used within.
