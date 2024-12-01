using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Installer
{
    public class InstallerTaskPanel : StackPanel
    {
        protected List<InstallerTaskView> TaskViewList { get; set; } = new List<InstallerTaskView>();
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

        public void Deactivate(bool clearTaskList = false)
        {
            TimerRefreshList.Stop();
            ActionTasksCompleted = null;
            ActionTaskFailed = null;

            if (clearTaskList)
            {
                TaskViewList.Clear();
                Children.Clear();
            }            
        }

        public void HideCompleted()
        {
            foreach (var view in TaskViewList.Where(v => !v.DisplayMessagesInSummary && v.InstallerTask.State == TaskState.COMPLETED))
                view.Visibility = System.Windows.Visibility.Collapsed;
        }

        protected void TimerRefreshListTick(object sender, EventArgs e)
        {
            try
            {
                foreach (var control in TaskViewList)
                    control.UpdateTaskView();

                while (InstallerTask.TaskQueue.Count > 0)
                    AddTaskView(InstallerTask.TaskQueue.Dequeue());

                if (TaskViewList.Any(v => v.InstallerTask.State == TaskState.ERROR))
                {
                    if (!DontStopOnError)
                        TimerRefreshList.Stop();

                    ActionTaskFailed?.Invoke();
                }

                if (TaskViewList.All(v => v.InstallerTask.State == TaskState.COMPLETED))
                {
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
            InstallerTaskView component = new InstallerTaskView(task);
            Children.Add(component);
            TaskViewList.Add(component);
        }
    }
}
