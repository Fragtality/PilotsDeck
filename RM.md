# Pilot's Deck
Directly check & control the FlightSim from your StreamDeck!
<br/><br/>

## 1 - Introduction
PilotsDeck is a Plugin for Elegato's StreamDeck with the Ability to **trigger Cockpit-Controls** in different Ways and especially reading & **displaying a Control's State** on the StreamDeck as Text, Image, Bar or Arc. It is lean & mean, flexible, completely Open-Source and Free-to-Use. It does not do any fancy Stuff like a PFD - it does exactly what is needed to support smooth Flight Operations 😎<br/>

StreamDeck-wise it behaves like any other StreamDeck Plugin: it runs alongside other Plugins and you can Drag, Drop, Copy, Paste the Actions like any other Action in the StreamDeck Software between your Folders, Pages or even different StreamDecks. The Action Configuration is done through the standard "Property Inspector" of the StreamDeck UI and saved in the StreamDeck Profile. You can create, export and share Profiles with the Plugin's Actions to share their Configuration.<br/>
The Plugin supports different StreamDeck Models: **Mini**, **Standard**/15-Key, **XL**, **Mobile** and **Plus**. Other Models might work, but an indented Support for Non-Display Models is not planned. The Plugin runs only on **Windows**. There no Plans for Linux or macOS Support (the first is not supported by StreamDeck at all, both do not run or support all Sims and some essential .NET Libraries are only available on Windows).<br/>

Simulator-wise it supports all major Platforms on Windows - **MS Flight Simulator**, **X-Plane** and **Prepar3D**. For MS Platforms it connects through **FSUIPC** to the Simulator, for X-Plane it connects directly via **UDP** Sockets. All Variables and Commands these Connections allow are usable with the Plugin. You can can directly connect to another Sim without reconfiguring anything (but you can't run multiple Simulators in parallel).<br/>
Not all Variables and Commands require a registered Version of FSUIPC, but a registered (bought) Copy of FSUIPC is recommended to use the full Potential. If you only fly in X-Plane, FSUIPC is not needed at all.<br/>
It is designed for **advanced Sim-Users** which "know how to do Stuff": it does not make any unnecessary complicated Stuff, but doesn't have Features allowing to do anything without knowing anything 😅 If you know how to read Control-States for your Plane and how to trigger these Controls, you can quickly define an Action for that on the StreamDeck. If you don't: be eager to read & learn. 😉 I'll try to give some Background in the Readme, but you have to take it from there!<br/>

Predefined StreamDeck Profiles are available under [Integrations](Integrations/), but there are not much. Either your Plane is among these for direct Use or they can at least serve as Example:<br/>
<img src="img/ExampleLayout01.jpg" width="420"><br/>
<img src="img/ExampleFNX03.jpg" width="420"><br/>
<img src="img/ExampleFNX04.jpg" width="420">
<br/><br/>

### 1.1 - Supported Simulator Versions

| Simulator | Supported | Tested | Requirement |
| :-------------|:-------------:|:-----:|:-----|
| **Flight Simulator 2020** | **yes** | **yes** | FSUIPC 7 & MobiFlight WASM |
| Flight Simulator X | yes | no | FSUIPC4 |
| Flight Simulator 2004 | yes | no | FSUIPC 3 |
| **Prepar3D v5** | **yes** | **yes** | FSUIPC 6 |
| Prepar3D v4 | yes | no | FSUIPC 5/6 |
| Prepar3D v1-3 | yes | no | FSUIPC 4 |
| **X-Plane 12** | **yes** | **yes** | None - does not use XUIPC |
| X-Plane 11 | yes | yes | None - does not use XUIPC |
| X-Plane <=10 | yes | no | None - does not use XUIPC |

Supported is understood as "technical and basically supported by the Connection Method". Support in Terms of ensured Compatibility, Fixing Issues and giving Support exists only for the latest Version of the three Major Simulators: MSFS2020, X-Plane 12, P3D v5. I'm happy if it works for older Versions, but I won't make any Effort for them.
<br/><br/>

### 1.2 - Supported Sim-Commands & -Variables
Here a quick Overview of what you can send to the Simulator ("**Command**") or from what you can read Values from the Simulator ("**Variable**"). One of the Things which make the Plugin flexible: Variables can also be used as Commands. For Example to move a Cockpit-Control by writing a different Value to a Variable.<br/><br/>
How Commands and Variables are configured and the different Options how they can be executed is described under INSERTLINK.<br/>

| ID | Description | Command | Variable | Simulators               | 
| :---: | :------------ | :---: | :---: | :-------------------- | 
| **MACRO** | Execute any Macro known to FSUIPC | ✔️ | ✖️ | MSFS*, P3D, FSX |
| **SCRIPT** | Run any Lua-Code known to FSUIPC | ✔️ | ✖️ | MSFS, P3D, FSX |
| **CONTROL** | Send any SimEvent defined by its numerical ID (also known as FS-Controls, Control-Codes. Numerical Variant of a K-Variable/K-Event) | ✔️ | ✖️ | MSFS, P3D, FSX, FS9 |
| **LVAR** | Read from / Write to any L-Var ("Local Variable") | ✔️ | ✔️ | MSFS, P3D, FSX |
| **OFFSET** | Read from / Write to any FSUIPC Offset | ✔️ | ✔️ | MSFS, P3D, FSX, FS9 |
| **VJOY** | Toggle/Clear/Set a Button of a virtual Joystick from *FSUIPC* | ✔️ | ✖️ | MSFS, P3D, FSX |
| **VJOYDRV** | Toggle/Clear/Set a Button of a virtual Joystick from the known *vJoy Device Driver* (if installed) | ✔️ | ✖️ | ALL |
| **HVAR** | Activate any H-Variable in the Simulator | ✔️ | ✖️ | MSFS |
| **CALCULATOR** | Run any Calculator/Gauge Code in the Simulator | ✔️ | ✖️ | MSFS |
| **XPCMD** | Send any Command known to X-Plane | ✔️ | ✖️ | XP |
| **XPWREF** | Read from / Write to any X-Plane DataRef | ✔️ | ✔️ | XP |
| **AVAR** | Read from / Write to any Simulation Variable (also known as A-Var) | ✔️ | ✔️ | MSFS |

\* = MSFS does not support Mouse-Macros<br/>
:grey_exclamation: Please mind that the Command Types Script, Macro, Lvar and vJoy can only work with a **registered** Version of FSUIPC!<br/>
:grey_exclamation: Both **vJoy** Command Types are independent of each other and are two different Things! "VJOY" can only be assigned within FSUIPC (and not in the Simulator). The "VJOYDRV" can be assigned by anything which understands a Joystick Button (Simulator, FSUIPC, Addons, ...).
<br/><br/>

### 1.3 - Available StreamDeck Actions
All Actions work on the **Keypads** (the normal/square StreamDeck Buttons). The Dial/Touchpad (aka **Encoder**) on the SD+ is only supported by some Actions (the ones which make the most Sense).<br/><br/>
On Keypads you can assign **two** different Commands, based on how long you hold it: A **Short**/Normal and **Long** Press (>= 600ms). Only one of the available Actions can be put in StreamDeck Multi-Actions.<br/>
On Encoders you can assign **five** different Commands for each Interaction: **Left** Turn, **Right** Turn, **Touch** Tap and a **Short** & **Long** Press on the Dial. The Actions can be put in StreamDeck Encoder-Stacks, but will then lose their Short/Long Press Function.<br/><br/>
How these Actions can be configured and customized is described under INSERTLINK.<br/>

|  | Action Name | Keypad / Encoder | Description |
| :---: | :-------------- | :---------: | :----------- |
| <img src="img/DisplayXpndr.png" width="188"> | **Display Value** | Keypad | Display a Sim Variable as Number or Text (display "ON" instead of "1"). You can scale/round/format the Value as needed and customize the Font-Settings. Only for Display. |
| <img src="img/DisplaySwitchTcas.png" width="188"> | **Display Value with Switch** | Keypad / Encoder | Like before, but this Action also send Commands. |
| <img src="img/SimpleButton.png" width="188"> | **Simple Button** | Keypad | Can only send Commands and always shows the same (configurable) Image. Supported in StreamDeck Multi-Actions! |
| <img src="img/DynamicButtonLight.png" width="188"> | **Dynamic Button** | Keypad | This Action dynamically changes the displayed Image based on a Variable (in Addition to sending Commands). Different Values trigger different Images. |
| <img src="img/DisplaySwitchKorry.png" width="188"> | **Korry Button** | Keypad | Intended for Korry-Switches in the Cockpit: the Action displays two "Sub-Images" independently of each other. They are shown/hidden based on their own Variable, but will not change the Image based on the Value. Can be adapted to other Use-Cases. |
| <img src="img/ComRadio1.png" width="188"> | **COM Radio** | Keypad / Encoder | Intended for Com Frequencies: the Action shows two different Variables which can be independently scaled/rounded/formatted but share the same Font-Settings. Can be adapted to other Use-Cases. |
| <img src="img/GaugeBarRudder.png" width="188"> | **Display Gauge** | Keypad / Encoder | This Actions renders the Value dynamically on a Bar or Arc. Size, Color, Position and Font-Settings can be tweaked. It can optionally send Commands. |
| <img src="img/GaugeArcEng.png" width="188"> | **Display Gauge (Dual)** | Keypad | As before, but it renders two Values dynamically on the same Bar or Arc. |

<br/>

### 1.4 - Installation & Requirements
The best Way to Install and Update the Plugin is via the **Installer**: Download, Execute and click Install! It will check the Requirements, informs & links what it missing and installs the Plugin in the correct Space if the Requirements are met (the StreamDeck Software will be automatically stopped).<br/>
It is highly likely that you need to **Unblock/Exclude** the Installer & Plugin from BitDefender and other AV-/Security-Software. It's the number one Reason for "the Plugin is not working"-Issues.<br/>
If it still does not work right or at all, please check INSERTLINK.<br/><br/>
The Requirements for the Plugin to run:
- Windows **10** or **11** (updated)
- [**StreamDeck Software v6**](https://www.elgato.com/downloads)
- [**.NET 7**](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) x64 Runtime (Core + Desktop) installed & updated
- If not X-Plane only: The **latest** Release of the [**FSUIPC**](http://fsuipc.com/) Major Version specific to your Simulator (e.g. FSUIPC 7 for MSFS2020)
- If MSFS is installed: The **latest** Release of the WASM-Module from [**MobiFlight**](https://github.com/MobiFlight/MobiFlight-WASM-Module) installed in your Community-Folder
- Optional: If you want to use **VJOYDRV** Commands you need Version [v2.2.1.1](https://github.com/njz3/vJoy/releases/tag/v2.2.1.1) for Windows 10 and Version [2.1.9.1](https://github.com/jshafer817/vJoy/releases/tag/v2.1.9.1) for Windows 11. It is not checked by the Installer (since it is not a Requirement for the Plugin).



<br/><br/>
If you want to install it manually (not recommened), just put the Contents of the Zip in your Plugin-Folder here:

 > %appdata%\Elgato\StreamDeck\Plugins

When Updating manually, delete all Binaries/Libraries in the Plugin's Folder before unzipping.<br/>
If you don't know what that means, what to do and what Software Versions you have installed: Use the Installer!!! 😅<br/>
(Number 2 & 3 Reasons the Plugin is not working: Missing/outdated Requirements and wrong Location)

<br/><br/><br/>
## 2 - Action Configuration
Since the Plugin is very flexible, there is a "little" Learning Curve 😳 In this Chapter you'll find:
- The Options and Behavior common to all Actions.
- How Commands & Variables are defined and configured.
- How each Plugin Action can be configured and customized.

<br/>

### 2.1 - Common Syntax, Options and Behavior
Generally, most of the Configuration is defined in Text-Input-Fields. This is on Purpose so that Parts of the Configuration can quickly be changed and easily be copied between Actions.<br/><br/>

#### 2.1.1 - Common Syntax
If a Field requires multiple numeric Values - like Sizes, Positions and Ranges - the Values must be separated by a semicolon. For Example: `0; 10` or `9; 14; 54; 20`<br/><br/>
Mostly all Fields which accept numeric Values understand Float-Values also with either Decimal-Character independent of your System Locale (so `0,5` and `0.5` is the same for the Plugin). All Float-Values rendered by the Plugin will use a Decimal-Point **`.`** though!<br/><br/>
If a Field/Syntax requires multiple Parameters, the Parameters are separated by colon **`:`**<br/><br/>
If a Field/Syntax requires a Mapping or Assignment it is defined by an equal-Sign **`=`** . Some allow even simple Value-Comparisons like **`<=`**, **`>=`**, **`<`** or **`>`**.
- A single Comparison like **`>=5`** will be interpreted as "*if* the current Value is >= 5, *then* use/trigger this Option". Equality is automatically tested if just a Number is present (do not add the Equal-Sign for that).
- A Comparison in a Mapping can only be with Equality (`<=` or `>=`) and a Comparison like **`5>=X`** will be interpreted as "*if* 5 is >= the current Value, *then* use/display X".

<br/>

#### 2.1.2 - Common Options
**Decode BCD**: If the Value is a binary coded decimal (BCD), the Plugin can decode it for you with that Checkbox!<br/>

**Scalar**: Multiply the Value by that Number to scale it, if it needs to be converted. Defaults to 1.<br/>One Example would be "Pressure QNH as millibars" - it is delivered as multiple of 16 (e.g. 1013 = 16208). So we would scale it by "0.0625" (1/16) to have the right Value.<br/>

**Format**: Add leading Zeros, Round the Value and/or add Text to it. Syntax: `Zeros.Fraction:Text %s Text`
  - *Zeros*: Leading Zeros will be added so that the Value has always the same number of integral Digits. Optional, but can only be specified together with *Fraction*.
  - *Fraction*: Define to how many fractional Digits the Value should be rounded (can be zero to produce an Integer). If not specified, the Value is not rounded at all.
  - *Text*: The Value is inserted at the `%s` in the Text you specify here. E.g. to add Units to the Value with `%s kg` or a Percent-Sign with `%s %`. Or put something in Front of it. Every %s will be replaced by the Value. If Zeros/Digits are not specified, you don't need to add a colon.<br/>
  - *Examples*
    - `1`     One fractional Digit after the Decimal Point, a Value of 20.522534 will be displayed as `20.5`. Match for zero would be 0.0.
    - `2:1%s` Two fractional Digits and add an "1" before the Text - useful for the BCD encoded Frequencies!
    - `%s ft` Add "ft" after the Value, 300 will show as `300 ft`
    - `3.3`   The Value will have 3 Digits before and after the Decimal Point, a Value of 20.522534 will be displayed as `020.523`.

The Order in which these Options are applied: DecodeBCD -> Scale -> Round -> Format. If the Value is to be matched or mapped, it is done after Round and before Format.<br/>

**Inherit Font**: Actions which render Text will default to inherit the Font Settings configured in the Title Settings. When disabled, you can specify the Font, Size, Color and Style separately! Note that the Plugin can use **all Fonts** installed on your System, so it offers way more Choices then the default Title Settings allow.<br/>

<br/>

#### 2.1.3 - Display Behavior & Images
The Plugin has 4 different Main **States** that affect how and which Images is drawn:
- When no Simulator running it is in the **Idle** State: all Actions will have a static *Default Image* which resembles/previews their current Configuration. You don't need to have a Simulator running to do basic Configurations and directly see the visual Result!
- When a Simulator is running but not connected, ready or paused it is in the **Waiting** State: all Actions will show a hardcoded *Wait Image* (three white Dots on black Background). It can't do or show anything useful in that State, so just wait!
- When a Simulator Session is running normally it is in the **Ready** State: all Actions will be drawn according to their Configuration and current Value. All Actions are refreshed and redrawn every 200ms (5 fps) - but only if one of their Variables have changed!
- When a Simulator quits, crashes or does not react it is in the **Error** State: all Actions will show their configured *Error Image* until the Simulator is reconnected (goes back to Ready) or the Simulator's Executable terminates (goes back to Idle).
The *Default Image* is also mostly used as Background on which an Action renders its Contents in the Idle or Ready State.<br/><br/>

**Images** are selected from Dropdown Boxes which will have a small Preview of the selected Image besides them. The Plugin **includes a basic Set** of Images with a more "iconograpic" Look for better Readability instead of realistic Looks. But if you want a **different visual Style**, you can easily add & select own Images or Images from great **IconPacks** like from Guenseli or others! Just copy/save it to the Images Sub-Folder of the Plugin:

 > %appdata%\Elgato\StreamDeck\Plugins\com.extension.pilotsdeck.sdPlugin\Images

You don't have to restart the Plugin, every new Image will be selectable/usable when you open the next Property Inspector. When an Image is updated, it needs to be "uncached" before the new File is read - make sure it is not used or configured in any Action currently visible and wait 30s (or just restart the StreamDeck Software in that Case).<br/>
To be usable the Image needs to be in **PNG** Format and must be **"square"** - having the same Height and Width. It will be automatically scaled to fit (but not stretched on an Encoder). For Encoders the Image should be "rectangular" - having a higher Width than Height.<br/><br/>

But for optimal Visual Results it is recommended to provide an Image in specific and multiple **Sizes**. The Plugin will then automatically select the Image-Size which is optimal for the StreamDeck an Action is on. These are:
- The "Standard" Size of 72x72 Pixel, for Example called `myImage.png` - it will be used for the Mini, SD and Mobile.
- The "HQ" Size of 144x144 Pixel with a `@2x` Suffix: `myImage@2x.png` - it will be used for XL, Plus Keypads.
- The "Plus" Size of 200x100Pixel with a `@3x` Suffix: `myImage@3x.png` - it will be used for the Plus Encoders (Touch-Display). Make the Background transparent if possible/applicable.
- The Image will be selectable as `myImage` - the other Size-Variants are not visible in the UI.
- So if you add a "HQ" or "Plus" Image, you need to also add a File without Suffix!

It is only really needed if you have multiple StreamDeck Types in use. For Example, if you have only have a XL and all your Icons are already 144px: just copy over the Images!

You can also add new Images for the "Sub-Images" selectable in the **Korry Button** Action. These are stored in the `\korry` Sub-Folder of the above mentioned Path. These use the following Sizes (also PNG Format, preferrably with transparent Background):
- The "Standard" Size is 54x20 Pixel with no Suffix.
- The "HQ" Size is 108x40 Pixel with a `@2x` Suffix.

<br/>

Note that the Images itself are not stored in the StreamDeck Profile: if you want to **share** a Profile with someone, you need to provide the Images you used in that Profile. ❕ *IF* you have the Permission to share these Images - check and ask if you are allowed to do so! The Images which came with the Plugin don't need to be shared, just make sure to be at the same Plugin-Version.

<br/><br/>

### 2.2 - Defining Commands & Variables
One of PilotsDeck's Core-Concepts is: everthing has/is an Address. So whether it is a Variable to read (e.g. L-Var/DataRef/Offset) or a Command to send (e.g. Control, Script, Calculator-Code): it is identified by the Address. A Command is defined by its Type and Address in the UI. A Variable only by the Address.<br/>
Every Type needs a specific [Address Syntax](#221---address-syntax) to be used since some Commands/Variables require extra Parameter and some Commands can also be **sequenced**. That means the Plugin will send multiple Commands with just one Button-Press!<br/><br/>
The Property Inspector UI has a Syntax-Check build in for every Type except for Calculator: When the Syntax is correct, you see a little Check-Mark in the Input Field. Everything you enter will also checked more thourougly by the Plugin before it executes a Command (if possible/trackable).<br/>
If a Command could not be executed by any Reason (invalid Syntax, Sim not connected) the Keypad will show an yellow Alert Sign briefly on the Display. On an Encoder the Touch-Display will shortly flash in red in that Case. (The standard StreamDeck Mechanic how Actions can show an Error/Warning)
<br/><br/>

#### 2.2.1 - Address Syntax

| **MACRO** | Command | MSFS, P3D, FSX | `File:Macro(:Macro)*` |
| --- | --- | --- | --- |
- *File*: The Filename without Extension of the FSUIPC Macro-File.
- *Macro*: One or more Macros from that File to be executed in sequence.

*Examples*:
- `QW787_MAIN:QW_ENG1_START` - Run Macro *QW_ENG1_START* from Macro-File *QW787_MAIN.MCRO*.
- `FSLA3XX_MAIN:ACPACK1:ACPACK2` - Run Macro *ACPACK1* from Macro-File *FSLA3XX_MAIN.MCRO* and then *ACPACK2* from the same File.

<br/>

*Background*: Macro Files are a rather "rudimentary and legacy" Way to Script Actions in FSUIPC. In my Profiles I only use them in Prepar3D for "Mouse-Macros" which are able to Click Controls that can only be triggered with the Mouse (Mouse-Macros are not supported on MSFS). So if you don't know and have Macros, your Time is better invested to look at Lua or RPN :wink:<br/>
If you want to learn more about Macros, look at the "*FSUIPC7 for Advanced Users*" Document in your *My Documents\FSUIPC7* Folder (or \FSUIP6 for P3D)!
<br/><br/><br/>

| **SCRIPT** | Command | MSFS, P3D, FSX | `(Lua\|LuaToggle\|LuaSet\|LuaClear\|LuaValue):File(:Flag)*` |
| --- | --- | --- | --- |

- *Lua...*: The different Lua Controls defined by FSUIPC, you need to define exactly one.
- *File*: The Filename without Extension of the Lua-File known (!) to FSUIPC.
- *Flag*: Zero or more optional numeric Parameters to pass to the Lua-Script in sequence (for Toggle, Set, Clear or Value).

*Examples*:
- `Lua:Baro_Toggle` - Run Lua-Script *Baro_Toggle.lua*.
- `LuaToggle:FNX320_AUTO:12` - Toggle Flag *12* for Script *FNX320_AUTO.lua*.
- `LuaValue:Pilotsdeck_FSL:271:1` - Send Value *271* to Script *Pilotsdeck_FSL.lua* and then send Value *1* to the same Script.

<br/>

*Background*: The ability to run Lua Files is a Core-Feature of FSUIPC (at least for me!). Lua-Code is easier to write, understand and are more flexible than Things like Marcos or RPN for Calculator-Code. It greatly extends the Things you're able to do when pressing a Button on the StreamDeck - like automating GSX Calls or setting up your Aircraft from Cold & Dark for Example.<br/>
It also extends the Things you are able to read: you can run a Script in the Background which writes Values to the FSUIPC Custom Offset Range - for Example the combined State of both Landing Lights or the Contents of the Barometer Display. The Plugin can then read and display this Offset - with the Value which you generated/calculated in Lua.<br/>
If you want to learn more about Lua-Scripts, look at the "*FSUIPC Lua Plug-Ins*" and "*FSUIPC Lua Library*" Documents in your *My Documents\FSUIPC7* Folder (or \FSUIP6 for P3D)! Look at the Scripts that come with my Integrations or from other Users to understand how Lua can be used for different Things. Look at the *event.flag* and *event.param* Functions to understand how to use the LuaToggle and LuaValue Lua-Controls.<br/>

*Note for X-Plane Users*: You can achieve the same (and sometimes more) with the FlyWithLua Plugin for X-Plane! The Lua-Scripts there can define "Custom Commands" and "Custom DataRefs" which then can be used from the Plugin like any other X-Plane Command or DataRef.
<br/><br/><br/>

| **CONTROL** | Command | MSFS, P3D, FSX, FS9 | `Control=Parameter(:Parameter)*(:Control=Parameter(:Parameter)*)*` |
| --- | --- | --- | --- |
- *Control*: The numerical ID of the Control (aka Event-ID).
- *Parameter*: Zero or more optional Parameters for that Control.

*Examples*:
- `66168:65567` - Send Control *66168* and then send Control *65567*.
- `66587=72476:72478` - Send Control *66587* with Parameter *72476* and then Control *66587* with Parameter *72478*.
- `67195=3:67191=3:4` - Send Control *67195* with Parameter *3* and then Send Control *67191* with Parameter *3* and after that with Parameter *4*.

<br/>

*Background*: In Essence, these Control-Codes are the numerical ID of the standard SimEvents documented in the MSFS/P3D/FSX SDK. You can find their numerical Values in the "*Controls List...*" Text-File in your *My Documents\FSUIPC7* Folder (or \FSUIP6 for P3D)!<br/>
It is often used for Planes which are controlled via *Rotorbrake-Codes* like FSLabs: you need to send specific Parameters to the standard Rotor-Brake Control (hence the Name) to trigger a Cockpit-Control. Like in the second Example: That is how a LeftClick & Release is done on the Beacon-Switch (on FSL).<br/>
Note that these are the same as "K-Vars" or "Key-Events" - So you can achieve the same with using Calculator-Code and the textual ID of that Event! It's two different Sides of the same Coin :wink:
<br/><br/><br/>

| **LVAR** | Variable, Command | MSFS, P3D, FSX | `(L:)Name` |
| --- | --- | --- | --- |
- *Name*: The Name of the L-Var with or without preceding "L:".

*Examples*:
- `I_OH_FUEL_CENTER_1_U` - Read from the L-Var *I_OH_FUEL_CENTER_1_U*.

When used as **Command**, you need to specify the **On Value** and the **Off Value**. The Plugin will toggle between these two Values and writes them to the Variable.<br/>
In addition to writing plain Values, the Plugin can also do simple Operations like increasing/decreasing the Value or toggling the Value in a defined Sequence. Look under INSERTLINK for Details.<br/><br/>

*Background*: Local Variables (sometimes "Local Gauge Variables") are created and updated by the Plane. There are no standard L-Vars which could be used on every Plane. There is also no communality if and which L-Vars a specific Plane has. For some Planes it is the official Way to Read and Trigger Cockpit-Controls (e.g. Fenix, QualityWings). For some it is only for Read (e.g. PMDG). For some they exist, but are not really supported or usable sometimes (e.g. FSLabs).<br/>
But they are not only used by Planes. Some Addons like GSX also create and update L-Vars which can be used to interface with them! You can list all (up to 3066) currently used L-Vars in the **FSUIPC7 UI**.
<br/><br/><br/>

| **OFFSET** | Variable, Command | MSFS, P3D, FSX, FS9 | `(0x)Address:Size(:Type:Signedness\|BitNum)` |
| --- | --- | --- | --- |
- *Address*: The Address of the FSUIPC Offset as 4-Digit Hexadecimal Number, as documented in FSUIPC. The Hex Prefix "0x" is Optional.
- *Size*: The Size of this Offset in Bytes. A 1-digit (Decimal) Number.
- *Type*: Specify if the Offset is an Integer "**i**", Float/Double "**f**", Bit "**b**" or String "**s**". Defaults to ":i" for Integers if not specified.
- *Signedness*: Specify if the Offset is Signed "**s**" or Unsigned "**u**". Defaults to ":u" for Unsigned if not specified and only relevant for Integers.
- *BitNum*: Only for Offset-Type Bit, the Number/Index of the Bit to read from or write to.

*Examples*:
- `2118:8:f` - Read a *8* Byte long *float* Number from Address *2118* ("Turbine Engine 2 corrected N1").
- `3544:4:i:s` - Read a *4* Byte long *signed* *integer* Number from Address *3544* ("standby alitmeter in feet").
- `0x0ec6:2:i` - Read a *2* Byte long *unsigned* *integer* Number from Address *0EC6* ("Pressure QNH").
- `0x5408:10:s` - Read a *10* Byte long *String* from Address *5408*.
- `0x0D0C:2:b:1` - Read Bit *1* from the *2* Byte long Bitmask at Address *0D0C* (Nav Lights).

Before you use an Offset as **Command**, make sure that it is writeable (some are read-only)! When used as Command, you need to specify the **On Value** and the **Off Value**. The Plugin will toggle between these two Values and writes them to the Variable. Use only 1 or 0 for Bit-Offsets.<br/>
In addition to writing plain Values, the Plugin can also do simple Operations like increasing/decreasing the Value or toggling the Value in a defined Sequence. Look under INSERTLINK for Details.<br/><br/>

*Background*: These Offsets are the way how FSUIPC makes Simulator Variables (also known as A-Vars) accessible to outside Applications (like my Plugin for Example). FSUIPC sticks with that Concept for historical and compatibility Reasons. But not all Offsets are Simulator Variables: the Benefit of that System is that Applications can exchange Data through FSUIPC Offsets. For Example PMDG or Project Magenta use assigned Offset Ranges to share their Data. I use some of these Offset Ranges in my Integrations to exchange Data between Lua-Scripts and the Plugin.<br/>
You can find the **full List** of available/official Offsets in the "*FSUIPC Offsets Status*" Document in your *My Documents\FSUIPC7* Folder (or \FSUIP6 for P3D)!<br/>
Since most Offsets represent Simulator Variables, you can achieve the same Thing with the *AVAR* Type in MSFS. Not all Simulator Variables are exported by FSUIPC (like "FUELSYSTEM PUMP SWITCH" for Example) - in that Cases you need to read this Variable via *AVAR*.
<br/><br/><br/>

| **VJOY** | Command | MSFS, P3D, FSX | `Joystick:Button(:t)` |
| --- | --- | --- | --- |
- *Joystick*: The Number of the virtual Joystick to use, as documented in FSUIPC (Joystick 64 - 72).
- *Button*: The Number of the Button on that Joystick (Button 0 - 31).
- *Toggle*: The specified Button is handled as toggleable Button: The Plugin toggles the State of that Button between pressed and unpressed (it will remain in that State). Without this Option, the Keypad works like a normal Joystick-Button (it stays pressed as long as you press the Keypad).

*Examples*:
- `64:4` - The Keypad on the StreamDeck is recognized as Joystick *64*, Button *4* in FSUIPC.
- `72:2:t` - The Keypad on the StreamDeck will toggle Joystick *72*, Button *2* between pressed and unpressed in FSUIPC.

The additional **Long Press** Command will not be available when you use a non-toggling vJoy for the normal/short Press!<br/>
The VJOY Command can also be used on the **Encoders** and the Touch-Display. On these Interactions a non-toggle vJoy will be shortly pressed. A toggling vJoy will be toggled on every Interaction.<br/><br/>

*Background*: The virtual Joystick Facility of FSUIPC has nothing to do with the System Driver and can be used independently. So the Use-Cases are very narrow, but it can be a useful Feature. When you want to stick of doing your Mappings and Assignement mainly in the FSUIPC UI, you could use these vJoys to map the StreamDeck Keypads/Encoders.
<br/><br/><br/>

| **VJOYDRV** | Command | ALL | `Joystick:Button(:t)` |
| --- | --- | --- | --- |
- *Joystick*: The Number of the virtual Joystick to use, as you configured in vJoyConf (Joystick 1 - 16).
- *Button*: The Number of the Button on that Joystick (Button 1 - 128).
- *Toggle*: The specified Button is handled as toggleable Button: The Plugin toggles the State of that Button between pressed and unpressed (it will remain in that State). Without this Option, the Keypad works like a normal Joystick-Button (it stays pressed as long as you press the Keypad).

*Examples*:
- `1:2` - The Keypad on the StreamDeck will set/clear Button *2* on the virtual Joystick *1*.
- `2:3:t` - The Keypad on the StreamDeck will toggle Button *3* on the virtual Joystick *2* between pressed and unpressed.

The additional **Long Press** Command will not be available when you use a non-toggling vJoy for the normal/short Press!<br/>
The VJOYDRV Command can also be used on the **Encoders** and the Touch-Display. On these Interactions a non-toggle vJoy will be shortly pressed. A toggling vJoy will be toggled on every Interaction.<br/>
Note that this Command is also only executed when the Plugin is in the Ready State (connected to a Simulator) like all other Commands. So it is normal to see the Alert Sign on the StreamDeck an no Activity in the vJoy Monitor. If you want to use vJoys in other Games or Simulators, you can use the vJoy Plugin from [ashupp](https://github.com/ashupp/Streamdeck-vJoy)!<br/><br/>

*Background*: Using virtual Joysticks is really a great Feature and Solution for specific Use-Cases! For Example when you want to **press and hold** Cockpit-Controls from the StreamDeck (e.g. Fire-Test Buttons). Or when the Simulator has Commands which can only be triggered with a Joystick-Mapping (e.g. Custom Cameras in MSFS). It is especially useful in X-Plane to circumvent the API-Limitation that X-Plane Commands can only be send as "command_once".
<br/><br/><br/>

| **HVAR** | Command | MSFS | `Name(:Name)*` |
| --- | --- | --- | --- |
- *Name*: The Name of the H-Var with or without preceding "H:". You can activate multiple H-Vars in a Sequence if you separate them with a **:** Sign.

*Examples*
- `A32NX_EFIS_L_CHRONO_PUSHED` - Activate the H-Var named *A32NX_EFIS_L_CHRONO_PUSHED*.
- `A320_Neo_CDU_1_BTN_F:A320_Neo_CDU_1_BTN_L` - Activate H-Var *A320_Neo_CDU_1_BTN_F* first and then activate H-Var *A320_Neo_CDU_1_BTN_L*.

<br/>

*Background*: H-Vars are a new Simulation Variable Type that came with MSFS and work roughly similiar like K-Vars - they trigger an Event but can not be read. Note that you don't need to configure and use the Hvar-Files from FSUIPC for the Plugin. You can use any known and existing H-Var from the Plugin directly.
<br/><br/><br/>

| **CALCULATOR** | Command | MSFS | `RPN-Code \| $Name:Step(:Limit) \| $K:Name(:Parameter)` |
| --- | --- | --- | --- |
- *RPN-Code*: The Code in normal RPN Syntax you want to run with the execute_calculator_code Function.

For simple Tasks like increasing/decreasing a L-Var you can use the `$Name:Step(:Limit)` Template (the Plugin will build the right RPN-Code for that):
- *Name*: The Name of the L-Var with or without preceding "L:".
- *Step*: The positive/negative Number by which the Variable is increased/decreased. A Plus-Sing for a positive Number is optional.
- *Limit*: The Variable will not be increased/decreased beyond that optional Limit.

For simple Tasks like triggering a K-Var (Event) you can use the `$K:Name(:Parameter)` Template (the Plugin will build the right RPN-Code for that):
- *Name*: The Name of the K-Var to trigger, you have to prefix it with "K:".
- *Parameter*: The optional numeric Parameter to send.

*Examples*
- `(A:LIGHT POTENTIOMETER:13, percent over 100) 0.0 > if{ (A:LIGHT POTENTIOMETER:13, percent over 100) 0.1 - 100 * (>K:LIGHT_POTENTIOMETER_13_SET) }` - Example of RPN Code (Decreasing Cabin Lights in the Fenix Airbus).
- `$E_FCU_SPEED:+1` - Increase L-Var *E_FCU_SPEED* by *1*.
- `$A_ASP_VHF_1_VOLUME:-0.05:0` - Decrease L-Var *A_ASP_VHF_1_VOLUME* by *0.05* until it is *0*.
- `$K:FUELSYSTEM_PUMP_TOGGLE:2` - Trigger the Standard Event *FUELSYSTEM_PUMP_TOGGLE* with Parameter *2*.
- `$K:A32NX.FCU_SPD_DEC` - Trigger the Custom Event *A32NX.FCU_SPD_DEC*.

Templates have to start with the Dollar-Sign!<br/>
Note that there is no Syntax-Checking at all for Calculator Code (except for the Templates). Only use direct RPN-Code if you are used to it.<br/><br/>

*Background*: Calculator-Code or sometimes Gauge-Code are an internal Mechanic of the Simulator - very roughly spoken it is how the modeled Aircraft Panels work inside for Ages.<br/>
The Template for K-Vars is very useful when a Plane has **Custom Events** (e.g FBW, PMDG) since calculating their numerical ID is a bit complicated. With the Template you can directly use the Name. These are usually found in the SDK or API Reference for that Plane. The full List of **Standard Events** can be found in the Flight Simulator SDK under [Event IDs](https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm).<br/>
The Template for increasing/decreasing L-Vars is especially useful on the Right/Left Turn of the **Encoders**: The Plugin will incoporate the number of Ticks received by the StreamDeck Software in the Code it generates (instead of repeating it). That means that it is the "smoothest" Option to manipulate a L-Var with an Encoder!<br/>
Although RPN Syntax is very hard to understand (I still struggle 😵), it is a very powerful Feature. The Code has Access to all Variable-Types of the Simulator and can therefore do Things which Lua-Scripts can not do - like in the Example for the Fenix Cabin Lights!<br/>
If you really want to go down the Rabbit Hole of using direct RPN-Code, start in the Flight Simulator SDK under [Reversed Polish Notation](https://docs.flightsimulator.com/html/Additional_Information/Reverse_Polish_Notation.htm).
<br/><br/><br/>

| **XPCMD** | Command | XP | `Path(:Path)*` |
| --- | --- | --- | --- |
- *Path*: The Path to the Command as published. You can send multiple Commands in a Sequence if you separate them with a **:** Sign.

*Examples*
- `toliss_airbus/aircondcommands/Pack1Toggle` - Single Command
- `AirbusFBW/PopUpPFD1:AirbusFBW/PopUpND1` - Two Commands in Sequence

<br/>

*Background*: Every Command in X-Plane - whether it is from the Simulator, Plane or other Addons - has an unique Path. You can lookup these Paths in the UI or on the Internet like on [siminnovations.com](https://www.siminnovations.com/xplane/command/). Or you can use the DataRefTool Plugin to explore all Commands (including Custom) currently known to the Simulator.
<br/><br/><br/>

| **XPWREF** | Variable, Command | XP | `Path([index]\|:sNUM)` |
| --- | --- | --- | --- |
- *Path*: The Path to the DataRef as published.
- *\[index\]*: (Optional) The Index to access, if the DataRef is an Array.
- *:sNUM*: If the DataRef is a String, you need to add **:s** to the Path followed by the Number of Characters to read.

*Examples*
- `ckpt/lamp/74` - Reading a Value from a normal DataRef.
- `AirbusFBW/FuelOHPArray[2]` - Reading a Value (on Index *2*) from the Array under this Path.
- `FlyWithLua/TLS2PLD/fcuHdg:s8` - Reading a *String* of Length *8* from that Path.

Before you use a DataRef as **Command**, make sure that it is writeable (some are read-only)! When used as Command, you need to specify the **On Value** and the **Off Value**. The Plugin will toggle between these two Values and writes them to the Variable.<br/>
In addition to writing plain Values, the Plugin can also do simple Operations like increasing/decreasing the Value or toggling the Value in a defined Sequence. Look under INSERTLINK for Details.<br/><br/>

*Background*: Every Simulator Variable (called DataRef) in X-Plane - whether it is from the Simulator, Plane or other Addons - has an unique Path. DataRefs are Everthing and Everything is defined by DataRefs 😅<br/>
You can lookup these Paths in the X-Plane SDK under [Datarefs](https://developer.x-plane.com/datarefs/). Or you can use the DataRefTool Plugin to explore all DataRefs (including Custom) currently known to the Simulator.
<br/><br/><br/>

| **AVAR** | Variable, Command | MSFS | `(A:Name(:index), Unit)` |
| --- | --- | --- | --- |
- *Name*: The Name of the A-Var as published. You have to prefix it with **A:** and the whole Expression must be enclosed by Parenthesis **( )**.
- *:index*: (Optional) The Index to access, if the A-Var is a Map/Enum/Mask.
- *Unit*: The Unit of the A-Var as published separated by a **,** from the Name.

*Examples*
- `(A:FUEL RIGHT QUANTITY, Gallons)` - Reading the A-Var *FUEL RIGHT QUANTITY* using *Gallons* as Unit.
- `(A:FUELSYSTEM PUMP SWITCH:2, Bool)` - Reading Index *2* of the A-Var *FUELSYSTEM PUMP SWITCH* as *Bool* Value.
- `(A:LIGHT POTENTIOMETER:13, percent over 100)` - Reading Index *13* of the A-Var *LIGHT POTENTIOMETER* as *Percent over 100* Value (0.0 - 1.0).

Before you use an A-Var as **Command**, make sure that it is writeable (some are read-only)! When used as Command, you need to specify the **On Value** and the **Off Value**. The Plugin will toggle between these two Values and writes them to the Variable. Use only 1 or 0 for Booleans.<br/>
In addition to writing plain Values, the Plugin can also do simple Operations like increasing/decreasing the Value or toggling the Value in a defined Sequence. Look under INSERTLINK for Details.<br/><br/>

*Background*: A-Var (or SimVars) are the standard Simulator Variables defined by Flight Simulator (2020 and Predecessors). There are no "custom" A-Vars. Default Planes normally only use A-Vars but complex and externally simulated Planes use/update only a Fraction of them. MSFS still sticks to the legacy System of many different and weird Units which have to be passed for Access to a Variable. You can look up these Units in the Flight Simulator SDK under [Simulation Variable Units](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variable_Units.htm).<br/>
Since the Plugin can access all A-Vars, you can use Variables which are not exported as FSUIPC Offset without the Need of defining a myOffsets File for FSUIPC. Or even to completely circumvent FSUIPC Offsets at all if you prefer calling Things by their Name :wink:<br/>
A **full List** of all A-Vars with their according Unit (and if writable) can be found in the SDK under [Simulation Variables](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm).
<br/><br/><br/>

#### 2.2.2 - Command Options


<br/><br/><br/>
