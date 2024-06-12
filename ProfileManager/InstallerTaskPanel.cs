using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ProfileManager
{
    public class InstallerTaskPanel : StackPanel
    {
        protected List<InstallerTaskView> TaskViewList { get; set; } = [];
        protected DispatcherTimer TimerRefreshList { get; set; }
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
                    TaskViewList.Clear();
                    Children.Clear();
                }
                else
                {
                    var last = TaskViewList.LastOrDefault()?.InstallerTask;
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
                foreach (var control in TaskViewList)
                    control.UpdateTaskView();

                while (InstallerTask.TaskQueue.Count > 0)
                {
                    if (InstallerTask.TaskQueue.TryDequeue(out var newTask))
                        AddTaskView(newTask);
                }

                if (TaskViewList.Any(v => v.InstallerTask.State == TaskState.ERROR))
                {
					if (!DontStopOnError)
                    {
						Logger.Log(LogLevel.Debug, $"Stopping Timers (Task with ERROR)");
						TimerRefreshList.Stop();
					}

                    ActionTaskFailed?.Invoke();
                }

                if (TaskViewList.All(v => v.InstallerTask.State == TaskState.COMPLETED))
                {
                    Logger.Log(LogLevel.Debug, $"Stopping Timers (Tasks COMPLETED)");
                    TimerRefreshList.Stop();

                    ActionTasksCompleted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void AddTaskView(InstallerTask task)
        {
            InstallerTaskView component = new(task);
            Children.Add(component);
            TaskViewList.Add(component);
        }
    }
}
