using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace CommandTool
{
    public delegate void ProcessOutput(string result);

    public delegate void CmdOutput(Queue<string> msgs);

    public class ProcessProxy
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


        private Process _process;

        private ProcessOutput _processOutput;

        private CmdOutput _currentCallback;

        /// <summary>
        /// 返回的消息是否需要包含执行的命令
        /// 用于调试
        /// </summary>
        // private bool _debugInfo = true;

        /// <summary>
        /// 命令执行完成的返回消息队列
        /// </summary>
        private Queue<string> _returnMsgs;

        /// <summary>
        /// 调试模式,显示接收到的所有消息
        /// </summary>
        private bool _debugMode = false;

        /// <summary>
        /// 是否开始处理消息
        /// </summary>
        private bool _handleMsg = false;

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
            Run(ECHO_OFF);
        }

        public void Run(string cmd, CmdOutput callback = null, bool debug = false)
        {
            _returnMsgs.Clear();

            _currentCallback = callback;
            _debugMode = debug;

            _process.StandardInput.WriteLine(cmd);
            _process.StandardInput.WriteLine(ECHO_COMMAND_RETURN);
        }

        public async Task<bool> RunAsync(string cmd,
            CmdOutput callback = null,
            bool debug = false,
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
            _debugMode = debug;
            _process.StandardInput.WriteLine(cmd);
            _process.StandardInput.WriteLine(ECHO_COMMAND_RETURN);

            return await TaskCondition.WaitUntil(_condition);
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
                var msgs = new Queue<string>();
                // var command = _returnMsgs.Dequeue();

                // if (_debugInfo)
                // {
                //     msgs.Enqueue(command);
                // }

                while (_returnMsgs.Count > 0)
                {
                    var line = _returnMsgs.Dequeue();
                    if (!line.Contains(ProcessProxy.COMMAND_RETURN))
                    {
                        msgs.Enqueue(line);
                    }
                }

                // 如果没有任何返回值,则默认添加一个"\n"换行符
                if (msgs.Count == 0)
                {
                    // msgs.Enqueue("\n");
                }

                _currentCallback?.Invoke(new Queue<string>(msgs));

                if (_condition.IsRunning)
                {
                    _condition.Complete();
                }
            }
            else
            {
                _returnMsgs.Enqueue(msg);
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