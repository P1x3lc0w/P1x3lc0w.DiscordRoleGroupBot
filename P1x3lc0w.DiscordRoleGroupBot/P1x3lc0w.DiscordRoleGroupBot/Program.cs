using System.Threading;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            _ = new Bot().StartBot();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}