using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace CommandTool
{
    public delegate void ProcessOutput(string result);

    public delegate void CmdOutput(Queue<string> msgs);

    public class ProcessProxy
    {
        private class TaskCondition
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

        /// <summary>
        /// 一条命令结束的标记
        /// </summary>
        public const string CommandReturnFlag = "#command return#";

        /// <summary>
        /// 每执行一条语句,都要附加执行这条语句,这条语句用于输出一句话:"#command return#"
        /// 这句话用于判断前一条命令是否执行完成,已获取完整的输出
        /// 因为cmd不能知道什么时候执行完成,所以通过这句话可以判断上一条命令是否执行完成
        /// 只有上一条命令完成,才能执行这一条命令
        /// </summary>
        private const string CommandReturnCMD = "echo " + CommandReturnFlag;

        private Process _process;

        private ProcessOutput _processOutput;

        private CmdOutput _currentCallback;

        /// <summary>
        /// 返回的消息是否需要包含执行的命令
        /// </summary>
        private bool _fullInfo = true;

        /// <summary>
        /// 命令执行完成的返回消息队列
        /// </summary>
        private Queue<string> _returnMsgs;

        /// <summary>
        /// 调试模式,显示接收到的所有消息
        /// </summary>
        private bool _debugMode = false;

        private TaskCondition _condition;

        public ProcessProxy()
        {
            _process = new Process();

            _process.StartInfo.FileName = "cmd.exe";
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;

            _condition = new TaskCondition();

            _returnMsgs = new Queue<string>();
            RegisterProcessOutput(MessageHandle);
        }

        private void MessageHandle(string msg)
        {
            if (_debugMode)
            {
                Debug.Log($"[debug mode]{msg}");
            }

            if (msg.Equals(ProcessProxy.CommandReturnFlag))
            {
                var msgs = new Queue<string>();
                var command = _returnMsgs.Dequeue();

                if (_fullInfo)
                {
                    msgs.Enqueue(command);
                }

                while (_returnMsgs.Count > 0)
                {
                    var line = _returnMsgs.Dequeue();
                    if (!line.Contains(ProcessProxy.CommandReturnFlag))
                    {
                        msgs.Enqueue(line);
                    }
                }

                // 如果没有如何返回值,则默认添加一个"\n"换行符
                if (msgs.Count == 0)
                {
                    msgs.Enqueue("\n");
                }

                _currentCallback?.Invoke(new Queue<string>(msgs));

                if (_condition.IsRunning)
                {
                    _condition.Complete();
                }
            }
            else if (string.IsNullOrEmpty(msg)
                     || msg == ""
                     || msg == " "
                     || msg.Equals("\n\n")
                     || msg.Equals("\n")
                     || msg.Equals("\t\t")
                     || msg.Equals("\t")
                     || msg.Equals("\r\r")
                     || msg.Equals("\r")
                     || msg.Contains("(c) 2019 Microsoft Corporation")
                     || msg.Contains("@ echo off"))
            {
            }
            else
            {
                _returnMsgs.Enqueue(msg);
            }
        }

        public void SetDebugMode(bool value)
        {
            _debugMode = value;
        }

        public void RegisterProcessOutput(ProcessOutput func)
        {
            if (func != null)
            {
                _processOutput += func;
            }
        }

        public void UnregisterProcessOutput(ProcessOutput func)
        {
            if (_processOutput != null && func != null)
            {
                _processOutput -= func;
            }
        }

        public void Start()
        {
            _process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            _process.EnableRaisingEvents = true;
            _process.Start();
            _process.BeginOutputReadLine();
            Run("@ echo off");
        }

        public void Run(string cmd, CmdOutput callback = null, bool fullInfo = true)
        {
            _returnMsgs.Clear();

            _currentCallback = callback;
            _fullInfo = fullInfo;

            _process.StandardInput.WriteLine(cmd);
            _process.StandardInput.WriteLine(CommandReturnCMD);
        }

        public async Task<bool> RunAsync(string cmd, CmdOutput callback = null, bool fullInfo = true,
            int timeout = 10000)
        {
            if (_condition.IsRunning)
            {
                // 目前只支持1个命令的异步执行
                return false;
            }

            _condition.Start();
            _condition.Timeout = timeout;

            _returnMsgs.Clear();
            _currentCallback = callback;
            _fullInfo = fullInfo;
            _process.StandardInput.WriteLine(cmd);
            _process.StandardInput.WriteLine(CommandReturnCMD);

            return await TaskCondition.WaitUntil(_condition);
        }

        public void Close()
        {
            Run("exit");
            _process.Close();
            _processOutput = null;
            _process = null;
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                _processOutput?.Invoke(e.Data);
            }
        }
    }
}