using System.Threading.Tasks;

namespace CommandTool
{
    public static class Command
    {
        private static CmdProxy _proxy;

        public static CmdProxy Proxy
        {
            get
            {
                if (_proxy == null)
                {
                    _proxy = new CmdProxy();
                }

                return _proxy;
            }
        }

        static Command()
        {
            Proxy.Start();
        }

        public static void Run(string cmd, CommandCallback callback, bool debug = false)
        {
            Proxy.Run(cmd, callback, debug);
        }

        public static async Task<bool> RunAsync(string cmd, CommandCallback callback, bool debug = false, int timeout = 10000)
        {
            return await Proxy.RunAsync(cmd, callback, debug, timeout);
        }
    }
}