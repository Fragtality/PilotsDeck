using CFIT.AppLogger;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PilotsDeck.Plugin
{
    public enum StatisticID
    {
        PLUGIN_RECEIVE = 0,
        PLUGIN_REFRESH,
        SIM_PROCESS,
        SIM_COMMANDS,
        SD_RECEIVE,
        SD_TRANSMIT
    }

    public static class StatisticManager
    {
        public static TimeSpan WatchSpan { get; set; } = TimeSpan.FromMilliseconds(App.Configuration.IntervalUnusedRessources);
        public static ConcurrentDictionary<StatisticID, StatisticTracker> Tracker { get; set; } = [];
        public static DateTime LastTick { get; set; } = DateTime.Now;

        public static int Actions { get { return App.PluginController.ActionManager.Count; } }
        public static long Redraws { get { return App.PluginController.ActionManager.Redraws; } }
        public static int Variables { get { return App.PluginController.VariableManager.ManagedVariables.Count; } }
        public static int Images { get { return App.PluginController.ImageManager.Count; } }
        public static int Scripts { get { return App.PluginController.ScriptManager.Count; } }
        public static int ScriptsGlobal { get { return App.PluginController.ScriptManager.CountGlobal; } }
        public static int ScriptsImage { get { return App.PluginController.ScriptManager.CountImages; } }

        public static StatisticTracker AddTracker(StatisticID id)
        {
            StatisticTracker tracker = new(id); 
            Tracker.TryAdd(id, tracker);
            return tracker;
        }

        public static void StartTrack(StatisticID id)
        {
            Tracker[id]?.StartTrack();
        }

        public static void EndTrack(StatisticID id)
        {
            Tracker[id]?.EndTrack();
        }

        public static void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
                App.StatisticTimer?.Stop();

            foreach (var tracker in Tracker.Values)
                tracker.PrintStatistics();
            LastTick = DateTime.Now;
        }

        public static void PrintRessourceStatistics()
        {
            if (Redraws == 0 && Actions == 0 && Variables == 0 && Images == 0 && App.PluginController.ScriptManager.CountTotal == 0)
                return;

            Logger.Debug($"STATISTICS FOR {string.Format("{0,16}", "RESSOURCES")} - Actions: {Actions} | Redraws: {Redraws} | Variables: {Variables} | Images: {Images} | Scripts: {Scripts} | Scripts Global: {ScriptsGlobal} | Scripts Image: {ScriptsImage}");
            App.PluginController.ActionManager.Redraws = 0;
        }
    }

    public class StatisticTracker(StatisticID id)
    {
        public StatisticID ID { get; protected set; } = id;
        public Stopwatch Stopwatch { get; protected set; } = new Stopwatch();
        public long Ticks { get; protected set; } = 0;
        public double Elapsed { get; protected set; } = 0;
        public double Average { get { return Elapsed / Ticks; } }
        public double MinValue { get; protected set; } = double.MaxValue;
        public double MaxValue { get; protected set; } = double.MinValue;
        public DateTime PeriodStart { get; protected set; } = DateTime.MaxValue;

        public void StartTrack()
        {
            if (Ticks == 0)
                PeriodStart = DateTime.Now;

            Stopwatch.Restart();
        }

        public void EndTrack()
        {
            Stopwatch.Stop();
            double elapsed = Stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed > MaxValue)
                MaxValue = elapsed;
            if (elapsed < MinValue)
                MinValue = elapsed;
            
            Elapsed += elapsed;
            Ticks++;
        }

        public void Reset()
        {
            Stopwatch.Reset();
            PeriodStart = DateTime.MaxValue;
            Elapsed = 0;
            Ticks = 0;
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;
        }

        public void PrintStatistics()
        {
            double average = Average;
            double min = MinValue;
            double max = MaxValue;
            if (double.IsNaN(average))
            {
                average = 0;
                min = 0;
                max = 0;
            }
            if (Ticks > 0)
                Logger.Debug($"STATISTICS FOR {string.Format("{0,16}", ID)} - Ticks: {Ticks:0000} | Average: {average:F3}ms | Minimum: {min:F3}ms | Maximum: {max:F3}ms | Total: {Elapsed/1000.0:F2}s / {App.Configuration.IntervalUnusedRessources/1000.0:F0}s");
            Reset();
        }

        public string GetStatistics()
        {
            try
            {
                double average = Average;
                double min = MinValue;
                double max = MaxValue;
                if (double.IsNaN(average))
                {
                    average = 0;
                    min = 0;
                    max = 0;
                }
                return string.Format("{0,-16} Ticks\t{1:0000}\tAverage {2:000.000}ms\tMinimum {3:000.000}ms\tMaximum {4:000.000}ms\tTime {5:00.00}/{6:00}s", ID.ToString().Replace("_",""), Ticks, average, min, max, Elapsed/1000.0, App.Configuration.IntervalUnusedRessources / 1000.0);
            }
            catch
            {
                return "Unavailable";
            }
        }
    }
}
