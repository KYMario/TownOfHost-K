using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace TownOfHost
{
    class LateTask
    {
        public string name;
        public float timer;
        public Action action;
        public bool? NoLog;
        public bool IsStop;
        public bool Isruned;
        public static List<LateTask> Tasks = new();
        public bool Run(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0)
            {
                try
                {
                    Isruned = true;
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex}", name == "" ? $"LateTaskRun" : $"Run{name}");
                }
                return true;
            }
            return false;
        }
        public void CallStop()
        {
            this.IsStop = true;
        }
        public LateTask(Action action, float time, [CallerMemberName] string name = "", bool? NoLog = false)
        {
            this.action = action;
            this.timer = time;
            this.name = name;
            this.NoLog = NoLog;
            this.IsStop = false;
            if (time <= 0)//0s以下ならその場で処理したまえ。
            {
                try { action(); }
                catch (Exception ex) { Logger.Error($"{ex}", "LateTask0sError"); }
                return;
            }
            Tasks.Add(this);
            if (name != "" && NoLog == false)
                Logger.Info("\"" + name + "\" is created", "LateTask");
        }
        public static void Update(float deltaTime)
        {
            var TasksToRemove = new List<LateTask>();
            for (int i = 0; i < Tasks.Count; i++)
            {
                var task = Tasks[i];
                try
                {
                    if (task.Run(deltaTime))
                    {
                        if (task.name != "" && task.NoLog is false or null)
                            Logger.Info($"\"{task.name}\"{(task.NoLog is null ? "is end" : "is finished")}", "LateTask");
                        TasksToRemove.Add(task);
                        continue;
                    }
                    if (task.IsStop)
                    {
                        if (task.name != "" && task.NoLog is not true)
                            Logger.Info($"\"{task.name}\"isStoped.", "LateTask");
                        TasksToRemove.Add(task);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex.GetType()}: {ex.Message}  in \"{task.name}\"\n{ex.StackTrace}", "LateTask.Error", false);
                    TasksToRemove.Add(task);
                }
            }
            TasksToRemove.ForEach(task => Tasks.Remove(task));
        }
    }
}