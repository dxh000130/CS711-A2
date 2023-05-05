using System.Threading.Tasks;

namespace CS711_A2
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            int port = 8080;
            GameServer server = new GameServer(port);
            await server.StartAsync();
        }
    }
}