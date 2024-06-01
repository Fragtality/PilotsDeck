// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0066:Switch-Anweisung in Ausdruck konvertieren")]
[assembly: SuppressMessage("Style", "IDE1006:Benennungsstile")]
[assembly: SuppressMessage("Usage", "CA2211:Nicht konstante Felder dürfen nicht sichtbar sein", Justification = "<Ausstehend>", Scope = "member", Target = "~F:PilotsDeck.vJoyManager.stateTable")]
[assembly: SuppressMessage("Style", "IDE0044:Modifizierer \"readonly\" hinzufügen")]
[assembly: SuppressMessage("Style", "IDE0074:Verbundzuweisung verwenden")]
[assembly: SuppressMessage("GeneratedRegex", "SYSLIB1045:In „GeneratedRegexAttribute“ konvertieren.")]
[assembly: SuppressMessage("Performance", "CA1854:Methode „IDictionary.TryGetValue(TKey, out TValue)“ bevorzugen", Justification = "<Ausstehend>", Scope = "member", Target = "~M:PilotsDeck.ValueManager.SetValue(System.Int32,PilotsDeck.IPCValue)")]
[assembly: SuppressMessage("Performance", "CA1822:Member als statisch markieren", Justification = "<Ausstehend>", Scope = "member", Target = "~M:PilotsDeck.MobiSimConnect.SimConnect_OnException(Microsoft.FlightSimulator.SimConnect.SimConnect,Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV_EXCEPTION)")]
[assembly: SuppressMessage("Performance", "CA1854:Methode „IDictionary.TryGetValue(TKey, out TValue)“ bevorzugen", Justification = "<Ausstehend>", Scope = "member", Target = "~M:PilotsDeck.ActionController.RegisterAction(System.String,PilotsDeck.IHandler)")]
[assembly: SuppressMessage("Performance", "CA1862:\"StringComparison\"-Methodenüberladungen verwenden, um Zeichenfolgenvergleiche ohne Beachtung der Groß-/Kleinschreibung durchzuführen")]
[assembly: SuppressMessage("Performance", "CA1822:Member als statisch markieren", Justification = "<Ausstehend>", Scope = "member", Target = "~M:PilotsDeck.ActionController.OnDidReceiveDeepLink(StreamDeckLib.Messages.StreamDeckEventPayload)")]
