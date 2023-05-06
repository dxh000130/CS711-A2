using System.Net;
using System.Threading.Tasks;

namespace CS711_A2
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            int port = 8080;
            GameServer server = new GameServer(IPAddress.Any, port);
            await server.StartAsync();
        }
    }
}