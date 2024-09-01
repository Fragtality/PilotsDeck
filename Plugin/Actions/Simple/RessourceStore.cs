using PilotsDeck.Resources;
using PilotsDeck.Resources.Images;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System.Collections.Concurrent;
using System.Linq;

namespace PilotsDeck.Actions.Simple
{
    public enum SwitchID
    {
        Switch = 2,
        SwitchLong = 3,
        SwitchLeft = 4,
        SwitchRight = 5,
        SwitchTouch = 6,
        GuardCmd = 19
    }

    public enum VariableID
    {
        Control = 0,
        Monitor = 1,
        Switch = 2,
        SwitchLong = 3,
        SwitchLeft = 4,
        SwitchRight = 5,
        SwitchTouch = 6,
        Indication = 14,
        GuardMon = 18,
        GuardCmd = 19,
        Top = Control,
        Bottom = 7,
        Active = Control,
        Standby = Bottom,
        Gauge = Control,
        GaugeColor = 8,
        GaugeFirst = Control,
        GaugeSecond = Bottom,
    };

    public enum ImageID
    {
        Background = 1,
        Indication = 4,
        On = 5,
        Off = 6,
        Top = 7,
        Bottom = 8,
        Swap = 9,
        Guard = 10,
        Map = 11,
        MapTop = 11,
        MapBot = 12,
        MapGuard = 13
    };

    public class RessourceStore
    {
        protected static VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        protected static ImageManager ImageManager { get { return App.PluginController.ImageManager; } }

        public ConcurrentDictionary<SwitchID, ActionCommand> ActionCommands { get; } = [];
        public ConcurrentDictionary<VariableID, ManagedVariable> Variables { get; } = [];
        public ConcurrentDictionary<string, ManagedVariable> DynamicVariables { get; } = [];
        public ConcurrentDictionary<VariableID, ValueState> States { get; } = [];
        public ConcurrentDictionary<ImageID, ManagedImage> Images { get; } = [];
        public ConcurrentDictionary<ImageID, string> ImageRefs { get; } = [];
        public ConcurrentDictionary<ImageID, ImageMapping> ImageMaps { get; } = [];

        public ActionCommand AddCommand(SwitchID id, SettingsModelSimple settingsModel)
        {
            ActionCommand command = null;

            switch (id)
            {
                case SwitchID.Switch:
                    command = ActionCommand.CreateMain(settingsModel, this);
                    break;
                case SwitchID.SwitchLong:
                    command = ActionCommand.CreateLong(settingsModel, this);
                    break;
                case SwitchID.SwitchLeft:
                    command = ActionCommand.CreateLeft(settingsModel, this);
                    break;
                case SwitchID.SwitchRight:
                    command = ActionCommand.CreateRight(settingsModel, this);
                    break;
                case SwitchID.SwitchTouch:
                    command = ActionCommand.CreateTouch(settingsModel, this);
                    break;
                case SwitchID.GuardCmd:
                    command = ActionCommand.CreateGuard(settingsModel, this);
                    if (settingsModel.IsGuarded)
                        AddState(VariableID.GuardMon, settingsModel.AddressGuardActive, settingsModel.GuardActiveValue, "", command);
                    break;
                default:
                    break;
            }
            if (command == null)
                Logger.Debug($"Command for ID {id} is null");
            else
            {
                Logger.Verbose($"Add Command for ID {id}");
                ActionCommands.TryAdd(id, command);
                if (command.CommandType == Simulator.SimCommandType.LUAFUNC)
                    App.PluginController.ScriptManager.RegisterScript(command.AddressOn);
                if (command.CommandType == Simulator.SimCommandType.LUAFUNC && !string.IsNullOrWhiteSpace(command.AddressOff))
                    App.PluginController.ScriptManager.RegisterScript(command.AddressOff);
            }

            return command;
        }

        public ActionCommand UpdateCommand(SwitchID id, SettingsModelSimple settingsModel)
        {
            ActionCommand command;

            RemoveCommand(id);
            command = AddCommand(id, settingsModel);

            return command;
        }

        public void RemoveCommand(SwitchID id)
        {
            if (ActionCommands.TryRemove(id, out ActionCommand command))
            {
                Logger.Verbose($"Remove Command for ID {id} ('{command.AddressOn}' - '{command.AddressOff}')");
                if (command.IsValueType)
                    RemoveVariable((VariableID)id);
                if (command.ToggleSwitch)
                    RemoveVariable(VariableID.Monitor);
                if (id == SwitchID.GuardCmd)
                    RemoveState(VariableID.GuardMon);

                if (command.CommandType == Simulator.SimCommandType.LUAFUNC)
                    App.PluginController.ScriptManager.DeregisterScript(command.AddressOn);
                if (command.CommandType == Simulator.SimCommandType.LUAFUNC && !string.IsNullOrWhiteSpace(command.AddressOff))
                    App.PluginController.ScriptManager.DeregisterScript(command.AddressOff);
            }
            else if (id == SwitchID.GuardCmd)
                RemoveState(VariableID.GuardMon);
        }

        public ActionCommand GetCommand(SwitchID id, out ActionCommand command)
        {
            command = GetCommand(id);
            return command;
        }

        public ActionCommand GetCommand(SwitchID id)
        {
            if (ActionCommands.TryGetValue(id, out ActionCommand command))
                return command;
            else
            {
                Logger.Debug($"No ActionCommand mapped to ID {id}");
                return null;
            }
        }

        public bool HasChanges()
        {
            if (Variables.Where(v => v.Value.IsChanged).Any())
            {
                return true;
            }
            else if (DynamicVariables.Where(v => v.Value.IsChanged).Any())
            {
                return true;
            }
            else
                return false;
        }

        public ValueState AddState(VariableID id, string address, string onState, string offState, ActionCommand parent = null)
        {
            var variable = AddVariable(id, address);
            ValueState state = null;

            if (variable != null)
            {
                state = new(variable, onState, offState, parent);
                Logger.Verbose($"Add State for ID {id} ('{address}')");
                States.TryAdd(id, state);
            }
            return state;
        }

        public ValueState AddState(VariableID id, string address, string onState, bool decodeBCD = false, string scalar = "1", string format = "")
        {
            var variable = AddVariable(id, address);
            ValueState state = null;

            if (variable != null)
            {
                state = new(variable, onState, decodeBCD, Conversion.ToDouble(scalar, 1), format);
                Logger.Verbose($"Add State for ID {id} ('{address}')");
                States.TryAdd(id, state);
            }
            return state;
        }

        public ValueState UpdateState(VariableID id, string address, string onState, string offState, ActionCommand parent = null)
        {
            var variable = UpdateVariable(id, address);
            Logger.Verbose($"Remove State for ID {id}");
            States.TryRemove(id, out _);

            ValueState state = null;
            if (variable != null)
            {
                state = new(variable, onState, offState, parent);
                Logger.Verbose($"Add State for ID {id} ('{address}')");
                States.TryAdd(id, state);
            }
            return state;
        }

        public ValueState UpdateState(VariableID id, string address, string onState, bool decodeBCD = false, string scalar = "1", string format = "")
        {
            var variable = UpdateVariable(id, address);
            Logger.Verbose($"Remove State for ID {id}");
            States.TryRemove(id, out _);

            ValueState state = null;
            if (variable != null)
            {
                state = new(variable, onState, decodeBCD, Conversion.ToDouble(scalar, 1), format);
                Logger.Verbose($"Add State for ID {id} ('{address}')");
                States.TryAdd(id, state);
            }
            return state;
        }

        public void RemoveState(VariableID id)
        {
            Logger.Verbose($"Remove State for ID {id}");
            if (States.TryRemove(id, out _))
                RemoveVariable(id);
        }

        public ValueState GetState(VariableID id)
        {
            if (States.TryGetValue(id, out ValueState state))
                return state;
            else
            {
                Logger.Error($"No ValueState mapped to ID {id}");
                return null;
            }
        }

        public ManagedVariable AddVariable(VariableID id, string address)
        {
            Logger.Verbose($"Add Variable for ID {id} ('{address}')");
            var variable = VariableManager.RegisterVariable(address);
            if (variable != null)
                Variables.TryAdd(id, variable);

            return variable;
        }

        public ManagedVariable UpdateVariable(VariableID id, string newAddress)
        {
            var variable = GetVariable(id);
            if (variable != null)
                RemoveVariable(id);

            variable = AddVariable(id, newAddress);

            return variable;
        }

        public void RemoveVariable(VariableID id)
        {
            Variables.TryRemove(id, out ManagedVariable variable);
            {
                Logger.Verbose($"Remove Variable for ID {id} ('{variable?.Address}')");
                VariableManager.DeregisterVariable(variable?.Address);
            }
        }

        public ManagedVariable GetVariable(VariableID id)
        {
            if (Variables.TryGetValue(id, out ManagedVariable variable))
                return variable;
            else
            {
                Logger.Warning($"No ManagedVariable mapped to ID {id}");
                return new VariableNumeric(VariableManager.ADDRESS_EMPTY, SimValueType.INTERNAL); ;
            }
        }

        public ManagedVariable AddDynamicVariable(string name)
        {
            Logger.Verbose($"Add DynamicVariable for Address {name}");
            if (VariableManager.TryGet(name, out ManagedVariable variable))
                DynamicVariables.TryAdd(name, variable);

            return variable;
        }

        public void RemoveDynamicVariable(string name)
        {
            Logger.Verbose($"Remove DynamicVariable for Address {name}");
            DynamicVariables.TryRemove(name, out _);
        }

        public ManagedImage AddImage(ImageID id, string file)
        {
            var image = ImageManager.RegisterImage(file);
            if (image != null)
            {
                Logger.Verbose($"Add Image for ID {id} (file: {file})");
                if (Images.TryAdd(id, image))
                    ImageRefs.TryAdd(id, file);
            }
            else
                Logger.Error($"No Image found for ID {id} (file: {file})");

            return image;
        }

        public ManagedImage UpdateImage(ImageID id, string newFile)
        {
            var image = GetImage(id);
            ImageRefs.TryGetValue(id, out string oldFile);
            if (image != null && oldFile != newFile)
                RemoveImage(id);

            if (oldFile != newFile)
                image = AddImage(id, newFile);

            return image;
        }

        public void RemoveImage(ImageID id)
        {
            if (Images.TryRemove(id, out ManagedImage image))
            {
                Logger.Verbose($"Remove Image for ID {id} (file: {image.BaseFile})");
                ImageManager.DeregisterImage(image.RequestedFile);
                ImageRefs.TryRemove(id, out _);
            }
        }

        public ManagedImage GetImage(ImageID id)
        {
            if (Images.TryGetValue(id, out ManagedImage image))
                return image;
            else
            {
                Logger.Warning($"No ManagedImage mapped to ID {id}");
                return ImageManager.DEFAULT_WAIT;
            }
        }

        public ImageMapping AddImageMap(ImageID id, string map)
        {
            if (string.IsNullOrWhiteSpace(map))
                return null;

            if (ImageMaps.ContainsKey(id))
            {
                Logger.Warning($"ImageMap for ID {id} already added ('{map}')");
                return null;
            }

            Logger.Verbose($"Add ImageMap for ID {id} ('{map}')");
            var mapping = new ImageMapping(map, this);
            ImageMaps.TryAdd(id, mapping);

            return mapping;
        }

        public ImageMapping UpdateImageMap(ImageID id, bool settingUseMap, string mapStr)
        {
            ImageMapping.RefUpdateHelper(ImageMaps, id, settingUseMap, mapStr, this);
            ImageMaps.TryGetValue(id, out ImageMapping mapping);
            return mapping;
        }

        public void RemoveImageMap(ImageID id)
        {
            if (ImageMaps.TryRemove(id, out ImageMapping mapping))
            {
                Logger.Verbose($"Remove ImageMap for ID {id} ('{mapping.MapString}')");
                mapping.DeregisterMap();
            }
        }

        public ImageMapping GetImageMap(ImageID id)
        {
            if (ImageMaps.TryGetValue(id, out ImageMapping mapping))
                return mapping;
            else
            {
                Logger.Warning($"No ImageMapping mapped to ID {id}");
                return null;
            }
        }
    }
}
