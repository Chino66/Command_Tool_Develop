using System.Diagnostics;
using System.Threading.Tasks;

namespace CommandTool
{
    public static class ProcessUtil
    {
        public static void Run(string fileName, string arguments, CommandCallback callback)
        {
            var process = new ProcessProxy(fileName, arguments, callback);
            process.Start();
        }

        public static async Task RunAsync(string fileName, string arguments, CommandCallback callback)
        {
            await Task.Run(() =>
            {
                var process = new ProcessProxy(fileName, arguments, callback);
                process.Start();
            });
        }
    }
}