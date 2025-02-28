using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using System.Collections.Generic;

namespace Installer.Worker
{
    public class WorkerMobi : WorkerMobiModule<Config>
    {
        public WorkerMobi(Config config, Simulator sim) : base(config, sim)
        {

        }

        protected override bool RunCondition()
        {
            var simulators = Config.GetOption<List<Simulator>>(Config.OptionSearchSimulators);
            if (simulators == null)
                return false;
            return simulators.Contains(MobiSimulator);
        }
    }
}
