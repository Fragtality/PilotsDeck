using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using System.Collections.Generic;

namespace Installer.Worker
{
    public class WorkerFsuipc7 : WorkerFsuipc7<Config>
    {
        public WorkerFsuipc7(Config config, Simulator sim) : base(config, sim)
        {

        }

        protected override bool RunCondition()
        {
            var simulators = Config.GetOption<List<Simulator>>(Config.OptionSearchSimulators);
            if (simulators == null)
                return false;
            return simulators.Contains(Fsuipc7Simulator) && Config?.GetOption<bool>(Config.OptionFsuipc7UseSecondary) == true;
        }
    }
}
