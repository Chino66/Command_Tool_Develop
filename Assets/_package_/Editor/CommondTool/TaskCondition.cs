using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CommandTool
{
    public class TaskCondition
    {
        #region Static

        public static async Task<bool> WaitUntilCondition(TaskCondition condition, int millisecondsDelay = 100)
        {
            while (condition.Value == false)
            {
                await Task.Delay(millisecondsDelay);
            }

            return condition.Value;
        }

        public static async Task<bool> WaitUntilCondition(TaskCondition condition)
        {
            await WaitUntilCondition(condition, 100);

            return condition.Value;
        }

        public static async Task<bool> WaitUntil(TaskCondition condition)
        {
            var task = Task.Delay(condition.Timeout, condition.TokenSource.Token)
                .ContinueWith(tsk => tsk.Exception == default);

            try
            {
                await task;
            }
            catch (OperationCanceledException e)
            {
                Debug.LogError(e);
            }
            finally
            {
                condition.TokenSource?.Dispose();
            }

            return condition.Value;
        }

        #endregion

        public bool Value;

        public bool IsRunning;

        public int Timeout = 10000;

        public CancellationTokenSource TokenSource;

        public TaskCondition()
        {
            Value = false;
        }

        public TaskCondition(bool value)
        {
            Value = value;
        }

        public void Start()
        {
            IsRunning = true;
            TokenSource = new CancellationTokenSource();
        }

        public void Complete()
        {
            IsRunning = false;
            TokenSource?.Cancel();
        }
    }
}