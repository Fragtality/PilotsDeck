# Changelog


## v0.9.1.0

### Plugin
- DEPRECATION NOTICE: Using RPN Code as Variable (aka 'C-Var') is now considered deprecated!
  - The Plugin still allows Usage - existing Actions continue to work
  - Adding C-Var(s) to an Action is still allowed, but the Plugin will mark them as 'invalid Syntax'
  - Consider using the Composite Action and/or Lua Scripts as Replacement
- Added:  for Global Scripts now works on *ALL* Variable Types (across all Simulators!)
  - RunEvent will trigger every Time the Value is *set*
  - So be aware that for polled/timed Types (e.g. FSUIPC or X-Plane) your callback Function will be called *a lot* (every ~50ms)
  - To get only notified when the Value has *changed*, use the new  Function
- Added: New 'Settings' View in the Developer UI (via Tray/Sys-Icon) to make selected Plugin Settings available in the UI
  - Intervals for Action Refresh and Sim Process can be adjusted
  - Common Sim Connection Settings like FSUIPC on MSFS or WebAPI on X-Plane
- Improved: Checks for running Simulator Binaries only query the System's Process List every 5s (to save CPU Cycles)
- Fixed: Tick/Info Icon for Inputs on Classic Actions - Contribution by @JKWTCN
- Fixed: XP-Commands for Hold Switches (command once not checked and using WebAPI) - Contribution by @maboehme
- Fixed: Some XP-Commands (and DataRefs) not enumerated through WebAPI when they registered 'late' - Contribution by @maboehme
- Fixed: Profile Switch Back not working
- Fixed: Syntax Check in classic Actions
- Fixed: Positions are linked after using Copy until Actions are reloaded (Composite Actions)
- Fixed: MSFS Variables sometimes getting stale (not updated anymore)
- Fixed: RunInterval changes on Global Scripts not applied on Script Reload
- Fixed: RunEvent on Global Scripts firing duplicated Events when Script was reloaded
- Fixed: Custom Log for Global Scripts not working after normal Sim/Session Start
- Fixed: API unregister Commands 'draining' Subscriptions on Variables not registered through API
- Changed: Rework of the Plugin and Library Code mixing up async and non async Code

### Installer
- Added: Support (in GUI) to Install the Plugin to "HotSpot StreamDock" - Contribution by @JKWTCN
  - PilotsDeck still is and will be primarily a Stream*Deck* Plugin
  - Advanced Features like Profile Mapping/Switching and Package Installation only work for StreamDeck Software
- Removed: The "--ignorestreamdeck" Commandline Option
- Fixed: not finding the StreamDeck Setup Path in Registry after SD 7.4.0
- Changed: Set StreamDeck Software 7.4.1 as recommended Target
- Changed: Set .NET 10.0.6 as Target

### Profile Manager
- Added: New Field 'removefiles' in package.json to remove outdated Files of the Package (Scripts and Images)
- Fixed: Profile Manager not installing Packages when passed through Commandline

### General
- New Release/Update Approach: there will be *only* Build from now on. No more Dev vs Release, it will always the "latest"
  - There will be a (Github) Release called "latest" always being updated when Changes are pushed
  - The PilotsDeck-Installer-latest.exe in the Project Files can still be used - it is the same Thing
  - Recent Changes are now tracked in CHANGES.md (which will also be added the Description of the "latest" Release)
  - When the Version increases, the Changes will be added to the CHANGELOG.md
  - The Installer will be scanned by VirusTotal every Time an Update is pushed - in Case you need a second Opinion on your Scanner saying its Malware ;)

<br/><br/>
