using CFIT.AppLogger;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public class ConnectorNone
    {
        public static bool IsNoSimCommand(SimCommand command)
        {
            return command?.Type == SimCommandType.VJOYDRV || command?.Type == SimCommandType.INTERNAL || command?.Type == SimCommandType.LUAFUNC;
        }

        public static void Process()
        {
            try
            {
                var scriptValues = App.PluginController.VariableManager.VariableList.Where(v => v.Type == SimValueType.LUAFUNC && v.Registrations > 0);
                foreach (var variable in scriptValues)
                    Task.Run(() => { variable.SetValue(App.PluginController.ScriptManager.RunFunction(variable.Address, out _, false)); });

                var internalVars = App.PluginController.VariableManager.VariableList.Where(v => v.Type == SimValueType.INTERNAL && !v.IsSubscribed && v.Registrations > 0);
                foreach (var variable in internalVars)
                    variable.IsSubscribed = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static async Task<bool> RunCommand(SimCommand command)
        {
            if (command.Type == SimCommandType.VJOYDRV && command.Address.HasParameter)
            {
                return await RunVjoyToggle(command);
            }
            else if (command.Type == SimCommandType.VJOYDRV)
            {
                return await RunVjoyClearSet(command);
            }
            else if (command?.Type == SimCommandType.LUAFUNC)
            {
                _ = Task.Run(() => RunScript(command));
                return true; 
            }
            else if (command?.Type == SimCommandType.INTERNAL)
            {
                Logger.Verbose($"Is Internal Command");
                for (int i = 0; i < command.Ticks; i++)
                {
                    Logger.Verbose($"Running Tick {i}");
                    if (App.PluginController.VariableManager.TryGet(command.Address, out ManagedVariable variable))
                    {
                        Logger.Verbose($"Found Variable, set Value {command.Value}");
                        variable.SetValue(command.Value);
                    }
                }
                
                return true;
            }
            else
                return false;
        }

        protected static void RunScript(SimCommand command)
        {
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                App.PluginController.ScriptManager.RunFunction(command.Address, out bool hasError);
                if (hasError)
                    Logger.Warning($"Failed Script Execution for ({command})");
            }
        }

        protected async static Task<bool> RunVjoyToggle(SimCommand command)
        {
            Logger.Debug($"Running vJoy Driver Toggle '{command.Address}' (x{command.Ticks})");
            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                if (await VirtualJoystick.ToggleDriverButton(command.Address.Name))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(App.Configuration.VJoyMinimumPressed, App.CancellationToken);
                }
            }
            return i == success;
        }

        protected async static Task<bool> RunVjoyClearSet(SimCommand command)
        {
            Logger.Debug($"Running vJoy Clear/Set '{command.Address}' (x{command.Ticks}) (IsUp {command.IsUp})");
            if (command.Ticks == 1 && !command.EncoderAction)
            {
                return await VirtualJoystick.ClearSetDriverButton(command.Address.Name, command.IsDown);
            }
            else
            {
                int success = 0;
                int i;
                for (i = 0; i < command.Ticks; i++)
                {
                    if (await VirtualJoystick.ClearSetDriverButton(command.Address.Name, true))
                        success++;
                    if (await VirtualJoystick.ClearSetDriverButton(command.Address.Name, false))
                        success++;
                    if (i != command.Ticks)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }

                return i * 2 == success;
            }
        }
    }
}
