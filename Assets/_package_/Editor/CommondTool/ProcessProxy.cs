using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CommandTool
{
    public delegate void ProcessOutput(string result);

    public delegate void CommandCallback(CommandContext ctx);

    public class CommandContext
    {
        public string FileName;
        public string Command;
        public Queue<string> Messages;
        public int ExitCode;

        public override string ToString()
        {
            var content = new StringBuilder();
            content.Append("FileName:").Append(FileName).Append("\n");
            content.Append("Command:").Append(Command).Append("\n");
            content.Append("Messages:\n");
            foreach (var message in Messages)
            {
                content.Append("  ").Append(message).Append("\n");
            }

            content.Append("ExitCode:").Append(ExitCode);

            return content.ToString();
        }
    }

    public class ProcessProxy
    {
        public Process Process => _process;

        public ProcessStartInfo ProcessStartInfo => _processStartInfo;

        /// <summary>
        /// 关于Process
        /// https://docs.microsoft.com/zh-cn/dotnet/api/system.diagnostics.process?view=net-5.0
        /// </summary>
        private Process _process;

        private ProcessStartInfo _processStartInfo;

        // private ProcessOutput _processOutput;

        private CommandCallback _callback;

        private string _fileName;
        private string _command;

        public ProcessProxy(string fileName, string arguments, CommandCallback callback = null)
        {
            _fileName = fileName;
            _command = $@"{arguments} & exit";
            _processStartInfo = new ProcessStartInfo(fileName, _command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            _callback = callback;
        }

        /// <summary>
        /// 在开始之前,可以通过ProcessStartInfo修改配置
        /// </summary>
        public void Start()
        {
            var output = new Queue<string>();

            _process = Process.Start(_processStartInfo);

            _process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                // _processOutput?.Invoke(e.Data);
                output.Enqueue(e.Data);
            };
            _process.BeginOutputReadLine();

            _process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                // _processOutput?.Invoke(e.Data);
                output.Enqueue(e.Data);
            };
            _process.BeginErrorReadLine();
            _process.WaitForExit();
            var exitCode = _process.ExitCode;
            _process.Close();

            var context = new CommandContext
            {
                FileName = _fileName,
                Command = _command,
                Messages = output,
                ExitCode = exitCode
            };

            _callback?.Invoke(context);
        }
    }
}