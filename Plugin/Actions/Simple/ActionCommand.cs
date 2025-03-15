using CFIT.AppLogger;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.Simulator.MSFS;
using System;

namespace PilotsDeck.Actions.Simple
{
    public class ActionCommand
    {
        public ManagedAddress AddressOn { get; set; } = ManagedAddress.CreateEmptyCommand();
        public SimCommandType CommandType { get; set; } = SimCommandType.MACRO;
        public bool IsValueType { get { return SimCommand.IsValidValueCommand(AddressOn.Address, DoNotRequestBvar, CommandType); } }
        public bool IsHoldable { get { return SimCommand.IsVjoyClearSet(AddressOn) || HoldSwitch; } }
        public bool DoNotRequestBvar { get; set; } = true;
        public bool UseXpCommandOnce { get; set; } = false;
        public ManagedAddress AddressOff { get; set; } = ManagedAddress.CreateEmptyCommand();
        public ValueState State { get; protected set; } = new ValueState(null, "0", "0");
        public bool Compares { get { return State?.Compares() == true; } }
        public bool HoldSwitch { get; set; } = false;
        public bool ToggleSwitch { get; set; } = false;
        public ManagedAddress AddressMonitor { get; set; } = ManagedAddress.CreateEmpty();
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

        public ManagedAddress ToggleAddress
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

        public SimCommand GetSimCommand(string context, int ticks, bool encoderAction = false, bool keyUp = true)
        {
            ticks = ticks == 0 ? 1 : ticks;
            SimCommand simCommand = new()
            {
                Type = CommandType,
                EncoderAction = encoderAction,
            };

            if (ToggleSwitch)
            {
                simCommand.Address = ToggleAddress.Copy();
            }
            else if (HoldSwitch)
            {
                if (IsValueType)
                {
                    simCommand.Address = AddressOn.Copy();
                    simCommand.Value = State.GetSwitchValue(ticks, keyUp);
                }
                else
                    simCommand.Address = (keyUp ? AddressOff : AddressOn).Copy();
            }
            else
            {
                simCommand.Address = AddressOn.Copy();
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
            if (ToolsMSFS.IsCalculatorTemplate(simCommand.Type, simCommand.Address.Address))
            {
                simCommand.Address = new ManagedAddress(ToolsMSFS.BuildCalculatorCode(simCommand.Address.Address, ticks), SimCommandType.CALCULATOR, true);
                isCalcCode = true;
            }

            if (CommandType == SimCommandType.BVAR)
                simCommand.DoNotRequest = DoNotRequestBvar;

            simCommand.Context = context;
            simCommand.Ticks = isCalcCode || State.IsCode ? 1 : Math.Abs(ticks);
            simCommand.IsUp = (keyUp! && CommandType == SimCommandType.XPCMD && !UseXpCommandOnce) || keyUp;
            if (SimCommand.CommandTypeUsesDelay(CommandType, DoNotRequestBvar) && UseCommandDelay)
            {
                simCommand.CommandDelay = CommandDelay;
            }
            Logger.Debug($"Created Command from '{AddressOn}' - Ticks: {simCommand.Ticks}");
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
                AddressOn = new ManagedAddress(settingsModel.AddressAction, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                CommandType = settingsModel.ActionType,
                AddressOff = new ManagedAddress(settingsModel.AddressActionOff, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                HoldSwitch = GetHoldState(settingsModel, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                ToggleSwitch = GetToggleState(settingsModel, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                AddressMonitor = new ManagedAddress(settingsModel.AddressMonitor),
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionType, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
                UseXpCommandOnce = settingsModel.UseXpCommandOnce,
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
                AddressOn = new ManagedAddress(settingsModel.AddressActionGuard, settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar),
                CommandType = settingsModel.ActionTypeGuard,
                AddressOff = new ManagedAddress(settingsModel.AddressActionGuardOff, settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar),
                HoldSwitch = GetHoldState(settingsModel, settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar),
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
                UseXpCommandOnce = settingsModel.UseXpCommandOnce,
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
                AddressOn = new ManagedAddress(settingsModel.AddressActionLong, settingsModel.ActionTypeLong, settingsModel.DoNotRequestBvar),
                CommandType = settingsModel.ActionTypeLong,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeLong, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
                UseXpCommandOnce = true,
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
                AddressOn = new ManagedAddress(settingsModel.AddressActionTouch, settingsModel.ActionTypeTouch, settingsModel.DoNotRequestBvar),
                CommandType = settingsModel.ActionTypeTouch,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeTouch, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
                UseXpCommandOnce = true,
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
                AddressOn = new ManagedAddress(settingsModel.AddressActionLeft, settingsModel.ActionTypeLeft, settingsModel.DoNotRequestBvar),
                CommandType = settingsModel.ActionTypeLeft,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeLeft, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
                UseXpCommandOnce = true,
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
                AddressOn = new ManagedAddress(settingsModel.AddressActionRight, settingsModel.ActionTypeRight, settingsModel.DoNotRequestBvar),
                CommandType = settingsModel.ActionTypeRight,
                HoldSwitch = false,
                ToggleSwitch = false,
                ResetSwitch = GetResetState(settingsModel, settingsModel.ActionTypeRight, settingsModel.DoNotRequestBvar),
                UseCommandDelay = settingsModel.UseControlDelay,
                DoNotRequestBvar = settingsModel.DoNotRequestBvar,
                UseXpCommandOnce = true,
            };

            if (command.IsValueType)
                command.State = new(store.AddVariable(VariableID.SwitchRight, settingsModel.AddressActionRight), settingsModel.SwitchOnStateRight, settingsModel.SwitchOffStateRight, command);

            return command;
        }
    }
}
