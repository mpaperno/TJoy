[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/mpaperno/TJoy?include_prereleases)](https://github.com/mpaperno/TJoy/releases)
[![Downloads](https://img.shields.io/github/downloads/mpaperno/TJoy/total.svg)](https://github.com/mpaperno/TJoy/releases)
[![Downloads of latest release](https://img.shields.io/github/downloads/mpaperno/TJoy/latest/total)](https://github.com/mpaperno/TJoy/releases/latest)
[![License](https://img.shields.io/badge/license-GPL3-blue.svg)](LICENSE)


# TJoy - Touch Joystick Virtual Controller Project

## TJoy Touch Portal Plugin

This project is currently implemented as a "plugin" for the [Touch Portal](https://www.touch-portal.com) macro launcher software,
designed for integrating with virtual gaming controllers. It currently fully supports the [vJoy](https://github.com/njz3/vJoy) virtual joystick driver,
with support for [ViGEm Bus](https://github.com/ViGEm/ViGEmBus) (XBox 360 and DualShock 4 controller emulation) in the works.

This project is in the beginning phase so please bear with me while I expand the documentation, refine features, or fix bugs.
Your testing and feedback is important, please help!

## Features

* Supports all controls provided by joystick driver:
  * `vJoy`: up to 16 axes, 4 continuous (360 degree) or 4-way POV hats, and 128 buttons.
* Use Touch Portal Actions and Sliders for all joystick controls (except joystick buttons currently do not have a slider input).
* Multiple options for each joystick control. Click, hold, or toggle buttons/hats, adjust axis range and precision,
  reverse axis direction, set up custom mixes, button sequences, and much more.
* Control multiple joystick buttons/axes/hats simultaneously (usually up to 10, depending on your touchscreen and device performance).
* Use calculations in action data fields, for example to control a joystick axis value based on the value of
  a variable or state, with basic math operators available (+, -, *, /, % (modulo)).
* Use multiple linked controls for the same axis, for example a "coarse" slider over the full range
  of an axis and a "fine" slider which controls a smaller range of the same axis. Linked controls are
  updated in real-time.
* Optional reporting of current joystick axis positions and button states at a configurable update frequency.
* Connect and disconnect to joystick driver on demand (via an action) and see current connection status (via a state).


## Examples

We need some... for now check what's in the [assets](https://github.com/mpaperno/TJoy/tree/main/assets) folder in this repo.


## Setup

### Requirements:
* [Touch Portal](https://www.touch-portal.com) Pro (paid version) for Windows, v3.0.6 or newer.
* **`vJoy` device driver**. You **must** install this separately otherwise this plugin can do nothing useful.
  * Currently the latest signed version I can find is [v2.2.1.1](https://github.com/njz3/vJoy/releases) from https://github.com/njz3/vJoy which works fine on my **Windows 10** 21H2.
  * For **Windows 11** it seems like [v2.1.9.1](https://github.com/jshafer817/vJoy/releases/tag/v2.1.9.1) from the original author works better. It may need a few tries to install.
* The latest version of this plugin: get the `TJoy-TouchPortal-Plugin-X.X.X.X.tpp` file from the latest release on the [Releases](https://github.com/mpaperno/TJoy/releases) page.

### Install:
1. The plugin is distributed and installed as a standard Touch Portal `.tpp` plugin file. If you know how to import a plugin,
just do that and skip to step 4. There is also a [short guide](https://www.touch-portal.com/blog/post/tutorials/import-plugin-guide.php) on the Touch Portal site.
2. Import the plugin:
    1. Start _Touch Portal_ (if not already running).
    2. Click the "gear" icon at the top and select "Import plugin..." from the menu.
    3. Browse to where you downloaded this plugin's `.tpp` file and select it.
3. Restart _Touch Portal_
    * When prompted by _Touch Portal_ to trust the plugin startup script, select "Yes" (the source code is public!).
4. Make sure `vJoy` driver is installed and at least one device is configured. If you are not familiar with vJoy setup,
  it's easy (there is a configuration app available once the driver is installed) but there are also many tutorials available online.
5. In Touch Portal, open the __Settings__ page (gear icon), navigate to the __Plug-ins__ page, then select `TJoy Touch Portal Plugin`
  from the dropdown list.  Enter the number of your configured `vJoy` device (typically `1`) in the `vJoy Device ID` field
  and hit the _Save_ button.
6. That's it for the configuration, now you're ready to create some joystick controls and use the plugin!

### Configure
Several settings are available in the _Touch Portal_ _Settings_ window (select _Plug-ins_ on the left, then
_TJoy _Touch Portal_ Plugin_ from the dropdown menu). The options are as follows:

* `vJoy Device ID`: The number of the `vJoy` device configured on your system which you would like to use. Enter zero
  to disable the vJoy connection entirely.

* `vJoy State Update Interval`: The plugin can optionally send the current joystick axis and button values as TP
States.  Enter how often to send these updates, in milliseconds, or enter zero to disable them.

* `Report Raw Axis Values`: When reporting is enabled (above), the axis values are by default sent as percentages.
Enable this option to get the actual raw values instead (eg. for vJoy axis it's 0-32767).

* `Buttons To Report`: vJoy can have up to 128 buttons configured.  That's a lot of states to send over if you don't
  need them all. Here you can specify the maximum number of buttons to report (eg. `8` to get buttons 1-8),
  or a range of buttons to report for, for example `32 - 64` to get only those buttons states sent over. Enter zero
  to not send any button states at all.


## Known Issues

**Please note that support for detecting when a slider has been released (is no longer being touched) is spotty at the moment.**
This is very useful in a joystick-like control, but due to some vague language on the Touch Portal API reference site,
it turned out this feature maybe wasn't even meant to exist in TP.  It does, however, work most of the time
if you pause for a fraction of a second before releasing the slider.

I have a request in with the TP authors to improve this detection so it is more reliable, since it is vital for things
like "self-centering" joystick axes. Please help by also requesting this feature on the Touch Portal Discord server or via other support means.


## Update Notifications

The latest version of this software is always published on the GitHub [Releases](https://github.com/mpaperno/TJoy/releases) page.
You have several options for getting **automatically notified** about new releases:
* **If you have a GitHub account**, just open the _Watch_ menu of this repo in the top right of this page, then go to  _Custom_ and select the
_Releases_ option, then hit _Apply_ button.
* **If you already use an RSS/Atom feed reader**, just subscribe to: https://github.com/mpaperno/TJoy/releases.atom
* **Use a RSS/Atom feed notification service**, either one specific for GitHub or a generic one, such as
(a list of services I found, I haven't necessarily tried nor do I endorse any of these):
  * https://blogtrottr.com/  (generic RSS feed notifications, no account required, see above for feed URL to use)
  * https://coderelease.io/  (no account required)
  * https://newreleases.io/
  * https://gitpunch.com/

I will also post update notices in the Touch Portal Discord server room [#tjoy-virtual-joystick](https://discord.com/channels/548426182698467339/949596705018511430)

## Usage

### Actions & Sliders

There is an Action and a Slider for each type of joystick control (except there are no sliders for joystick buttons).
In all cases the joystick control (axis/hat/button) being used must be configured in the vJoy driver. The axis and button
selections presented in this plugin's actions and sliders are not specific to your current device (there is no way to do that with Touch Portal
at this time). Also note that a vJoy device can be set up to have either discrete _or_ continuous POV hats, not both types at the same time.

#### vJoy

* `vJoy Button Action` - (Action only) Enter the button number to activate in the range of 1 through 128.
  You also select the action, one of `Click`, `Down`, or `Up`. `Click` will issue a button press followed automatically by
  a release after 50ms.
  * To have it act as a "natural" button, which is pressed while you hold it and released when you let go,
    use this action in the `On Hold` button area and select the `Hold` choice for the button action (the "Up" action will
    happen automatically whenever you release the button).  This also works for quick "clicks" (you don't have to hold the button),
    since the `On Hold` action fires on a quick touch as well.

* `vJoy 4-way D-Pad/POV Hat` - (Action/Slider) If you set up your vJoy device to have "directional" POV hats, use these actions/sliders
  to control them.
  * The Action is very similar to the button action, except you select the POV hat number (1-4) from a choice list, and then select
    the hat direction to use (`North`, `East`, `South`, `West` or `Center`). And like buttons you can select the action to perform,
    `Click`, `Down`, or `Up`. The POV hat returns to the center position when all other directions are released.
    * Like joystick buttons, for a "natural" button feel use this action in the `On Hold` button setup with the `Down` action.
      The POV will return to center when you release the TP button.
  * A Slider can can be set up to control either the North/South buttons (`Y` axis), the East/West buttons (`X` axis), or go all the
    way around the 4 directions, starting with either North/South and then going East/West (`YX` axis) or vice versa, East/West first
    and then North/South (`XY` axis). The POV center position is always in the middle of the slider.
    * For example a vertical slider set up for the `Y` axis would press the `North` button when pushed up from center and would press the
      `South` POV direction when moved down from center. A horizontal slider set up for the `XY` axis would progressively press the
      `East->South->West->North` buttons when moved to the right of center, and would reverse that when moved left of center, i.e.
      `West->South->East->North`.

* `vJoy Axis` - (Action/Slider) Controls any of the 16 available vJoy axes. At the basic level, an axis has a value range between 0 and 100
  which you can set with either actions or sliders.  The value is simply the percentage of the full range of travel available on the axis,
  with zero at the bottom/left and 100 at the top/right. Axis controls also introduce the concept of "reset position" which is where you
  would like the axis value to return to when you release the button/slider controlling that axis. It's like a spring on a joystick axis,
  except you have full control of it, including being able to disable that feature altogether.
  * The Action basically lets you move an axis to any predetermined position in the 0-100 range. When used in the `On Hold` button setup you can set where
    the axis returns to when you release the button (any of the presets like `Center`/`Min`/`Max`/etc, or a custom value). You may use fractional
    amounts for the value and reset fields, to allow for very precise axis positioning (more so than with sliders).
    * Note that you can use calculations in the action's axis value (and custom reset) fields. Basic math operators are supported, `+`, `-`, `*`, `/`, `%` (modulo)
      as well as grouping with parenthesis. Of special note is that one could use variables in these fields as a basis for calculations, such
      as any local variable or numeric state from a plugin. For example, using the current `RZ` axis' value as the basis to move the axis
      by 10% steps:<br/>
      `${value:us.wdg.max.tpp.tjoy.vJoy.state.axis.RZ} + 10`
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

* `vJoy Continous POV Hat` - (Action/Slider) Controls any of the 4 possible vJoy 360 degree "continuous" POV hats which move from center in any direction.
  The operation is nearly identical to the Axis controls described above, with the main difference being that "center" on a POV hat is a neutral position,
  when no other input is being given.
  * Again the Action options here are the same as for Axis controls where you can just set whichever value you wish the POV to move to when the action
    button is pressed or held.  The only difference is that there is a special value of `-1` which means the center/neutral hat position.  Same as with
    Axis controls, when used in `On Hold` button setup you can set where the POV hat returns to once the button is released (typically to center/neutral).
  * The Sliders also work the same way and you can again reverse the movement direction or set a smaller range of control for more precise movement.
    The difference here is that a slider will return to it's center point when a POV hat is in the neutral position. Currently there is no way to specify which
    point of the slider is "neutral" and no way to explicitly center a POV hat with a slider except using the "reset to center" option which works upon
    release of the slider.

#### Plugin

* `Control vJoy Driver Connection` - (Action only) Lastly there is a simple action to control connection to vJoy device being used. If you share the vJoy
  device with other controller ("feeder") software, this would be more convenient than disabling vJoy each time in this plugin's settings. You can `Toggle` the connection
  or explicitly set it to `On` or `Off`. Also see the related `ID of currently connected vJoy device` state, below, to determine status of the current connection.

### States

#### Static States
* `ID of currently connected vJoy device, 1-16 or 0 if none` - This will be `> 0` when a vJoy device is connected, and `= 0` (or `< 1`) when disconnected.
  When connected, this should match the vJoy device number configured in this plugin's settings.

#### Dynamic States
These are only sent if enabled in the plugin's Settings. See notes for `vJoy State Update Interval` configuration, above.
* `<Device> - Button <N>` - `0` for off and `1` for on (pressed)
* `<Device> - Axis <N>` - A value in the range of 0 - 100 reflecting axis position, or a "raw" range (0 - 32,767 for vJoy) if so configured in the plugin's settings.
* `<Device> - Coninuous POV <N>` - A value in the range of 0 - 100 reflecting POV hat axis position or `-1` indicating neutral state. Or, a "raw" range (-1 through 35,900 for vJoy) if so configured in the plugin's settings.
* `<Device> - Discrete POV <N>` - For vJoy this will be a number representing a direction, one of: `-1`: Center, `0`: North, `1`: East, `2`: South, `3`: West.


## Monitoring Joystick Inputs

When setting up your joystick controls it can of course be very useful to see the actual result in real time. `vJoy` comes with a basic monitoring application
(_vJoy Monitor_) which is suitable for this task. There are other similar tools available online.  You can also use the Windows gaming devices control panel
feature to check the inputs, however be aware that this only updates the display when the windows has focus (it's easy to assume your inputs aren't working
while it's actually just the status display that's not updating).

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

Open an [Issue](https://github.com/mpaperno/TJoy/issues) here on GitHub or start a
[Discussion](https://github.com/mpaperno/TJoy/discussions).
Please provide as much detail as possible. Did I mention log files already? I like log files.


## Credits

This project is written, tested, and documented by myself, Maxim (Max) Paperno.<br/>
https://github.com/mpaperno/

Uses a modified version of [TouchPortalSDK for C#](https://github.com/oddbear/TouchPortalSDK)
which is included in this repository and also [published here](https://github.com/mpaperno/TouchPortalSDK).
It is used under the MIT License.

Includes the vJoy Interface SDK library DLLs from https://github.com/njz3/vJoy
(forked from https://github.com/shauleiz/vJoy) which are
Copyright (c) 2017 Shaul Eizikovich and are used under the MIT license.

Joystick icon from the original vJoy Touch Portal plugin by Ivan Sørensen
(https://github.com/grawsom/vJoyTP), used under MIT licence.


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
