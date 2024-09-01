using FSUIPC;
using PilotsDeck.Resources.Variables;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.FSUIPC
{
    public class FsuipcManager
    {
        protected VariableOffset OffsetInMenuFsx { get; set; } = null;
        public bool IsInMenuFsx { get { return (FsuipcMajor < 7 && OffsetInMenuFsx?.NumericValue != 0) || false; } }
        protected VariableOffset OffsetInMenu { get; set; } = null;
        public bool IsInMenu { get { return OffsetInMenu?.NumericValue != 0; } }
        protected VariableOffset OffsetIsPaused { get; set; } = null;
        public bool IsPaused { get { return OffsetIsPaused?.NumericValue != 0; } }
        protected VariableOffset OffsetAircraftPath { get; set; } = null;
        public string AircraftString { get { return OffsetAircraftPath?.Value; } }
        protected int FsuipcMajor { get; set; } = 0;
        public bool IsInitialized { get { return FsuipcMajor != 0 && OffsetInMenuFsx.IsConnected && OffsetInMenu.IsConnected && OffsetIsPaused.IsConnected && OffsetAircraftPath.IsConnected; } }

        public async Task<bool> Connect()
        {
            try
            {
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    if (!IsInitialized)
                    {
                        await Task.Delay(App.Configuration.FsuipcConnectDelay, App.CancellationToken);
                        CreateInternalOffsets();
                    }

                    var offsetVariables = App.PluginController.VariableManager.ManagedVariables.Where(v => v.Value is VariableOffset);
                    foreach (var variable in offsetVariables)
                        variable.Value.Connect();

                    FsuipcMajor = FSUIPCConnection.FSUIPCVersion.Major;
                    Logger.Information($"FSUIPC Version {FSUIPCConnection.FSUIPCVersion} connected.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to connect - Exception {ex.GetType().Name}");
                ResetInternalOffsets();
            }

            Process(true);

            return FSUIPCConnection.IsOpen;
        }

        public bool Disconnect()
        {
            try
            {
                if (IsInitialized)
                    ResetInternalOffsets();
                if (FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while closing FSUIPC: '{ex.GetType()}' - '{ex.Message}'");
            }

            return FSUIPCConnection.IsOpen;
        }

        protected void CreateInternalOffsets()
        {
            OffsetInMenu = new(AppConfiguration.IpcInMenuAddr);
            OffsetInMenuFsx = new(AppConfiguration.IpcInMenuFsxAddr);
            OffsetIsPaused = new(AppConfiguration.IpcIsPausedAddr);
            OffsetAircraftPath = new(AppConfiguration.IpcAircraftPathAddr);
        }

        protected void ResetInternalOffsets()
        {
            OffsetInMenuFsx?.Dispose();
            OffsetInMenuFsx = null;
            OffsetInMenu?.Dispose();
            OffsetInMenu = null;
            OffsetIsPaused?.Dispose();
            OffsetIsPaused = null;
            OffsetAircraftPath?.Dispose();
            OffsetAircraftPath = null;
            FsuipcMajor = 0;
        }

        public static bool Process(bool firstProcess = false)
        {
            if (FSUIPCConnection.IsOpen)
            {
                try
                {
                    var query = App.PluginController.VariableManager.VariableList.Where(v => !v.IsConnected && v.Registrations > 0);
                    if (query.Any())
                        query.ToList().ForEach(v => v.Connect());

                    FSUIPCConnection.Process(AppConfiguration.IpcGroupRead);

                    return true;
                }
                catch (Exception ex)
                {
                    if (!firstProcess)
                        Logger.Error($"Unable to process - Exception {ex.GetType().Name}");

                    return false;
                }
            }
            else
            {
                Logger.Warning($"Unable to process - Connection not opened");
                return false;
            }
        }
    }
}
