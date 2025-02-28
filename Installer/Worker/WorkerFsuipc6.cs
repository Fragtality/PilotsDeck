using CFIT.Installer.LibFunc;
using System.Collections.Generic;

namespace Installer.Worker
{
    public class WorkerFsuipc6 : CFIT.Installer.LibWorker.WorkerFsuipc6<Config>
    {
        public WorkerFsuipc6(Config config, Simulator sim) : base(config, sim)
        {

        }

        protected override bool RunCondition()
        {
            var simulators = Config.GetOption<List<Simulator>>(Config.OptionSearchSimulators);
            if (simulators == null)
                return false;
            return simulators.Contains(Fsuipc6Simulator);
        }
    }
}
