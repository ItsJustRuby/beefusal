using System.Threading.Tasks;

namespace Beefusal
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var config = ConfigLoader.Load();
            BeefusalLog.InitializeSentry(config);
            return await Sync.Run(config);
        }
    }
}
