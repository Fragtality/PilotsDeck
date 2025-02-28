using CFIT.AppLogger;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ProfileManager
{
    public class InstallerTaskPanel : StackPanel
    {
        protected List<TaskView> TaskViewList { get; set; } = [];
        protected DispatcherTimer TimerRefreshList { get; set; }
        protected int LastCount { get; set; } = 0;
        protected Action ActionTasksCompleted { get; set; } = null;
        protected Action ActionTaskFailed { get; set; } = null;
		public bool DontStopOnError { get; set; } = false;

        public InstallerTaskPanel()
        {
            TimerRefreshList = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            TimerRefreshList.Tick += TimerRefreshListTick;
        }

        public void Activate(Action actionCompleted, Action actionFailed)
        {
            ActionTasksCompleted = actionCompleted;
            ActionTaskFailed = actionFailed;
            
            TaskStore.Clear();
            LastCount = 0;
            
            foreach (var view in TaskViewList)
                view.Disable(true);
            TaskViewList.Clear();
            
            Children.Clear();
            TimerRefreshList.Start();
        }

        public void Deactivate(bool clearTaskList = false, bool keepLast = false)
        {
            TimerRefreshList.Stop();
            ActionTasksCompleted = null;
            ActionTaskFailed = null;

            if (clearTaskList)
            {
                if (!keepLast)
                {
                    TaskStore.Clear();
                    LastCount = 0;

                    foreach (var view in TaskViewList)
                        view.Disable(true);
                    TaskViewList.Clear();
                    Children.Clear();
                }
                else
                {
                    var last = TaskViewList.LastOrDefault()?.Model;
                    TaskViewList.Clear();
                    Children.Clear();
                    if (last != null)
                        AddTaskView(last);
                }
            }            
        }

        protected void TimerRefreshListTick(object sender, EventArgs e)
        {
            try
            {
                if (TaskStore.Count > LastCount)
                {
                    var list = TaskStore.List;
                    int index = LastCount;
                    while (index < list.Count)
                    {
                        Logger.Debug($"Adding Task '{list[index].Title}'");
                        AddTaskView(list[index]);
                        index++;
                    }
                }
                LastCount = TaskStore.Count;

                if (TaskViewList.Any(v => v.Model.State == TaskState.ERROR))
                {
                    if (!DontStopOnError)
                    {
                        Logger.Debug($"Stopping Timers (Task with ERROR)");
                        TimerRefreshList.Stop();
                    }

                    ActionTaskFailed?.Invoke();
                }

                if (TaskViewList.All(v => v.Model.State == TaskState.COMPLETED))
                {
                    Logger.Debug($"Stopping Timers (Tasks COMPLETED)");
                    TimerRefreshList.Stop();

                    ActionTasksCompleted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void AddTaskView(TaskModel task)
        {
            TaskView component = new(task, false);
            component.SetMinWidth(384);
            Children.Add(component);
            TaskViewList.Add(component);
        }
    }
}
