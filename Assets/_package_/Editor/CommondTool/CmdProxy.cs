using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TaskKits;
using Debug = UnityEngine.Debug;

namespace CommandTool
{


    /// <summary>
    /// cmd.exe代理类
    /// 一个cmd窗口连续输入命令行操作
    /// </summary>
    public class CmdProxy
    {
        /// <summary>
        /// 命令结束的标记
        /// </summary>
        public const string COMMAND_RETURN = "#command return#";

        /// <summary>
        /// 每执行一条语句,都要附加执行这条语句,这条语句用于输出一句话:"#command return#"
        /// 这句话用于判断前一条命令是否执行完成,用于获取上一条命令的完整输出
        /// 因为cmd不能知道什么时候执行完成,所以通过这句话可以判断上一条命令是否执行完成
        /// 只有上一条命令完成,才能执行这一条命令
        /// </summary>
        private const string ECHO_COMMAND_RETURN = "echo " + COMMAND_RETURN;

        private const string ECHO_OFF = "@ echo off";


        /// <summary>
        /// 关于Process
        /// https://docs.microsoft.com/zh-cn/dotnet/api/system.diagnostics.process?view=net-5.0
        /// </summary>
        private Process _process;

        private ProcessOutput _processOutput;

        private CommandCallback _currentCallback;

        /// <summary>
        /// 命令执行完成的返回消息队列
        /// </summary>
        private readonly Queue<string> _returnMessages;

        /// <summary>
        /// 调试模式,显示接收到的所有消息
        /// </summary>
        private bool _debugMode = false;

        /// <summary>
        /// 是否开始处理消息
        /// </summary>
        private bool _handleMsg = false;

        private readonly TaskCondition _condition;

        public CmdProxy()
        {
            _process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };
            _condition = new TaskCondition();
            _returnMessages = new Queue<string>();
            RegisterProcessOutput(MessageHandle);
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
            _handleMsg = false;
            _process.StandardInput.WriteLine(ECHO_OFF);
        }

        public void Run(string cmd, CommandCallback callback = null, bool debug = false)
        {
            _returnMessages.Clear();
            _currentCallback = callback;
            _debugMode = debug;
            _process.StandardInput.WriteLine(cmd);
            _process.StandardInput.WriteLine(ECHO_COMMAND_RETURN);
        }

        public async Task<bool> RunAsync(string cmd,
            CommandCallback callback = null,
            bool debug = false,
            int timeout = 10000)
        {
            if (_condition.IsRunning)
            {
                // 目前只支持1个命令的异步执行
                return false;
            }

            _returnMessages.Clear();
            _currentCallback = callback;
            _debugMode = debug;
            _process.StandardInput.WriteLine(cmd);
            _process.StandardInput.WriteLine(ECHO_COMMAND_RETURN);

            return await _condition.WaitUntilComplete(timeout);
        }

        private void MessageHandle(string msg)
        {
            if (_debugMode)
            {
                Debug.Log($"[debug]{msg}");
            }

            if (!_handleMsg)
            {
                if (msg.EndsWith(ECHO_OFF))
                {
                    _handleMsg = true;
                }

                return;
            }

            if (msg.Equals(COMMAND_RETURN))
            {
                var messages = new Queue<string>();
                var command = _returnMessages.Dequeue();

                while (_returnMessages.Count > 0)
                {
                    var line = _returnMessages.Dequeue();
                    if (!line.Contains(CmdProxy.COMMAND_RETURN))
                    {
                        messages.Enqueue(line);
                    }
                }

                var ctx = new CommandContext
                {
                    Command = command,
                    Messages = new Queue<string>(messages)
                };

                _currentCallback?.Invoke(ctx);

                if (_condition.IsRunning)
                {
                    _condition.Complete();
                }
            }
            else
            {
                _returnMessages.Enqueue(msg);
            }
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