﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProfileManager
{
    public enum TaskState
    {
        ACTIVE = 1,
        WAITING = 2,
        ERROR = 3,
        COMPLETED = 4
    }

    public class InstallerTask
    {
        public static readonly Queue<InstallerTask> TaskQueue = [];
        public static InstallerTask CurrentTask { get; protected set; } = new("", "");

        public string Title { get; set; } = "";
        public List<string> ListMessages { get; protected set; } = [];
        public string Message { get { return ListMessages.LastOrDefault(); } set { ListMessages.Add(value); } }
        public TaskState State { get; set; } = TaskState.ACTIVE;
        public string Hyperlink { get; set; } = "";
        public string HyperlinkURL { get; set; } = "";
        public string HyperLinkArg { get; set; } = "";
        public bool SetCompletedOnUrl { get; set; } = false;
        public bool SetUrlBold { get; set; } = false;
        public int SetUrlFontSize { get; set; } = -1;
        public Func<bool> HyperlinkOnClick { get; set; } = null;
		public bool IsUrlCallback { get { return HyperlinkURL == callbackUrl; } }
        public static readonly string callbackUrl = "action://callback";
        public bool DisableLinkAfterClick { get; set; } = true;																		 																															   
        public bool DisplayOnlyLastCompleted { get; set; } = true;
        public bool DisplayOnlyLastError { get; set; } = true;

        public InstallerTask(string title, string message)
        {
            Title = title;
            if (!string.IsNullOrEmpty(message))
                ListMessages.Add(message);
        }

        public static InstallerTask AddTask(string title, string message)
        {
            try
            {
                CurrentTask = new InstallerTask(title, message);
                TaskQueue.Enqueue(CurrentTask);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return CurrentTask;
        }

		public void SetCallbackUrl()
        {
            HyperlinkURL = callbackUrl;
        }														   
        public void ReplaceLastMessage(string message)
        {
            if (ListMessages.Count > 0)
                ListMessages.RemoveAt(ListMessages.Count - 1);
            ListMessages.Add(message);
        }

        public static void SetCurrentState(string message, TaskState state = (TaskState)(-1))
        {
            CurrentTask.SetState(message, state);
        }

        public void SetState(string message, TaskState state = (TaskState)(-1))
        {
            Message = message;

            if (state != (TaskState)(-1))
                State = state;
        }
		
		public void SetSuccess(string message)
        {
            Message = message;
            State = TaskState.COMPLETED;
        }

        public void SetError(Exception ex, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            Logger.WriteLogException(ex, classFile, classMethod);
            Message = $"{ex.GetType()}: {ex.Message}";
            State = TaskState.ERROR;
        }

        public void SetError(string message, LogLevel level = LogLevel.Error, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            Logger.WriteLog(level, message, classFile, classMethod);
            Message = message;
            State = TaskState.ERROR;
        }

        public void MessageLog(string message, LogLevel level = LogLevel.Debug, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            Logger.WriteLog(level, message, classFile, classMethod);
            Message = message;
        }
    }
}
