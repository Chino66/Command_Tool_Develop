using System.Threading.Tasks;

namespace CommandTool
{
    public class CMD
    {
        public static void Run(string cmd, CommandCallback callback)
        {
            var proxy = new CmdProxy();
            proxy.Start();
            proxy.Run(cmd, callback);
            proxy.Close();
        }

        public static async Task RunAsync(string cmd, CommandCallback callback)
        {
            var proxy = new CmdProxy();
            proxy.Start();
            await proxy.RunAsync(cmd, callback);
            proxy.Close();
        }
    }
}