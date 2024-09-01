using PilotsDeck.Simulator;
using PilotsDeck.Simulator.MSFS;
using System;

namespace PilotsDeck.Actions.Simple
{
    public class ActionCommand
    {
        public string AddressOn { get; set; } = "";
        public SimCommandType CommandType { get; set; } = SimCommandType.MACRO;
        public bool IsValueType { get { return SimCommand.IsValidValueCommand(AddressOn, DoNotRequestBvar, CommandType); } }
        public bool IsHoldable { get { return SimCommand.IsVjoyClearSet(AddressOn, CommandType) || HoldSwitch; } }
        public bool DoNotRequestBvar { get; set; } = true;
        public string AddressOff { get; set; } = "";
        public ValueState State { get; protected set; } = new ValueState(null, "0", "0");
        public bool Compares { get { return State?.Compares() == true; } }
        public bool HoldSwitch { get; set; } = false;
        public bool ToggleSwitch { get; set; } = false;
        public string AddressMonitor { get; set; } = "";
        public bool ResetSwitch { get; set; } = false;
        public int ResetDelay
        {
            get
            {
                if (ResetSwitch)
                    return App.Configuration.VariableResetDelay;
                else
                    return 0;
            }
        }
        public TimeSpan LongPressTime { get; set; } = TimeSpan.FromMilliseconds(App.Configuration.LongPressTime);
        public bool UseCommandDelay { get; set; } = false;

        public int CommandDelay
        {
            get
            {
                if (UseCommandDelay)
                    return App.Configuration.CommandDelay;
                else
                    return 0;
            }
        }

        public string ToggleAddress
        {
            get
            {
                if (ToggleSwitch)
                {
                    if (State?.ComparesOff() == true)
                        return AddressOff;
                    else
                        return AddressOn;
                }
                else
                    return AddressOn;
            }
        }

        public SimCommand GetSimCommand(string context, int ticks, bool keyUp = true)
        {
            ticks = ticks == 0 ? 1 : ticks;
            SimCommand simCommand = new()
            {
                Type = CommandType
            };

            if (ToggleSwitch)
            {
                simCommand.Address = ToggleAddress;
            }
            else if (HoldSwitch)
            {
                if (IsValueType)
                {
                    simCommand.Address = AddressOn;
                    simCommand.Value = State.GetSwitchValue(ticks, keyUp);
                }
                else
                    simCommand.Address = keyUp ? AddressOff : AddressOn;
            }
            else
            {
                simCommand.Address = AddressOn;
                if (IsValueType)
                    simCommand.Value = State.GetSwitchValue(ticks, keyUp);
            }

            if (ResetSwitch && IsValueType)
            {
                simCommand.IsValueReset = true;
                simCommand.ResetValue = State.OffState;
                simCommand.ResetDelay = ResetDelay;
            }

            bool isCalcCode = false;
            if (ToolsMSFS.IsCalculatorTemplate(simCommand.Type, simCommand.Address))
            {
                simCommand.Address = ToolsMSFS.BuildCalculatorCode(simCommand.Address, ticks);
                isCalcCode = true;
            }

            if (CommandType == SimCommandType.BVAR)
                simCommand.DoNotRequest = DoNotRequestBvar;

            simCommand.Context = context;
            simCommand.Ticks = isCalcCode || State.IsCode ? 1 : Math.Abs(ticks);
            simCommand.IsUp = keyUp;
            if (SimCommand.CommandTypeUsesDelay(CommandType, DoNotRequestBvar) && UseCommandDelay)
            {
                simCommand.CommandDelay = CommandDelay;
            }

            Logger.Verbose($"Created Command - Ticks: {simCommand.Ticks}");
            return simCommand;
        }

        public static bool GetHoldState(SettingsModelSimple settingsModel, SimCommandType type, bool donotrequest)
        {
            return settingsModel.HoldSwitch && (SimCommand.IsHoldableValue(type, donotrequest) || SimCommand.IsHoldableCommand(type, donotrequest));
        }

        public static bool GetToggleState(SettingsModelSimple settingsModel, SimCommandType type, bool donotrequest)
        {
            return settingsModel.ToggleSwitch && SimCommand.IsToggleable(type, donotrequest);
        }

        public static bool GetResetState(SettingsModelSimple settingsModel, SimCommandType type, bool donotrequest)
        {
            return settingsModel.UseLvarReset && SimCommand.IsResetableValue(type, donotrequest);
        }

        public static ActionCommand CreateMain(SettingsModelSimple settingsModel, RessourceStore store)
        {
            if (string.IsNullOrWhiteSpace(settingsModel.AddressAction))
                return null;

            ActionCommand command = new()
            {
                AddressOn = settingsModel.AddressAction,
                CommandType = settingsModel.ActionType,
                AddressOff = settingsModel.AddressActionOff,
                HoldSwitch = GetHoldState(settingsModel, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                ToggleSwitch = GetToggleState(settingsModel, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                AddressMonitor = settingsModel.AddressMonitor,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.Switch, settingsModel.AddressAction), settingsModel.SwitchOnState, settingsModel.SwitchOffState, command);
            else if (command.ToggleSwitch)
                command.State = new(store.AddVariable(VariableID.Monitor, settingsModel.AddressMonitor), settingsModel.SwitchOnState, settingsModel.SwitchOffState, command);

            return command;
        }

        public static ActionCommand CreateGuard(SettingsModelSimple settingsModel, RessourceStore store)
        {
            if (!settingsModel.IsGuarded || string.IsNullOrWhiteSpace(settingsModel.AddressActionGuard))
                return null;

            ActionCommand command = new()
            {
                AddressOn = settingsModel.AddressActionGuard,
                CommandType = settingsModel.ActionTypeGuard,
                AddressOff = settingsModel.AddressActionGuardOff,
                HoldSwitch = GetHoldState(settingsModel, settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar),
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.GuardCmd, settingsModel.AddressActionGuard), settingsModel.SwitchOnStateGuard, settingsModel.SwitchOffStateGuard, command);

            return command;
        }

        public static ActionCommand CreateLong(SettingsModelSimple settingsModel, RessourceStore store)
        {
            if (!settingsModel.HasLongPress || string.IsNullOrWhiteSpace(settingsModel.AddressActionLong))
                return null;

            ActionCommand command = new()
            {
                AddressOn = settingsModel.AddressActionLong,
                CommandType = settingsModel.ActionTypeLong,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeLong, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.SwitchLong, settingsModel.AddressActionLong), settingsModel.SwitchOnStateLong, settingsModel.SwitchOffStateLong, command);

            return command;
        }

        public static ActionCommand CreateTouch(SettingsModelSimple settingsModel, RessourceStore store)
        {
            if (string.IsNullOrWhiteSpace(settingsModel.AddressActionTouch))
                return null;

            ActionCommand command = new()
            {
                AddressOn = settingsModel.AddressActionTouch,
                CommandType = settingsModel.ActionTypeTouch,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeTouch, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.SwitchTouch, settingsModel.AddressActionTouch), settingsModel.SwitchOnStateTouch, settingsModel.SwitchOffStateTouch, command);

            return command;
        }

        public static ActionCommand CreateLeft(SettingsModelSimple settingsModel, RessourceStore store)
        {
            if (string.IsNullOrWhiteSpace(settingsModel.AddressActionLeft))
                return null;

            ActionCommand command = new()
            {
                AddressOn = settingsModel.AddressActionLeft,
                CommandType = settingsModel.ActionTypeLeft,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeLeft, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.SwitchLeft, settingsModel.AddressActionLeft), settingsModel.SwitchOnStateLeft, settingsModel.SwitchOffStateLeft, command);

            return command;
        }

        public static ActionCommand CreateRight(SettingsModelSimple settingsModel, RessourceStore store)
        {
            if (string.IsNullOrWhiteSpace(settingsModel.AddressActionRight))
                return null;

            ActionCommand command = new()
            {
                AddressOn = settingsModel.AddressActionRight,
                CommandType = settingsModel.ActionTypeRight,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeRight, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.SwitchRight, settingsModel.AddressActionRight), settingsModel.SwitchOnStateRight, settingsModel.SwitchOffStateRight, command);

            return command;
        }
    }
}
