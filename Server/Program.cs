using System.Net.Sockets;
using System.Net;
using System.Text;
using Server;

namespace Assignment3
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 5000;
            var server = new EchoServer(port);
            server.Run();
        }
    }
}