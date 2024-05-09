using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP.Client
{
    /// <summary>
    /// This class contains the logic for initializing a FTP client
    /// </summary>
    internal class SimpleFtpClient
    {
        public static async Task StartClient(string serverAddress, int port)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(serverAddress, AddressFamily.InterNetwork);
            IPAddress ipAddress = ipHostInfo.AddressList[0];

            var ipEndpoint = new IPEndPoint(ipAddress, port);

            using TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(ipEndpoint);
            using NetworkStream stream = tcpClient.GetStream();

            var buffer = new byte[1024];
            int received = await stream.ReadAsync(buffer);

            var message = Encoding.ASCII.GetString(buffer, 0, received);
            Console.WriteLine(message);

            while (true)
            {
                Console.Write("> ");
                string toSend = Console.ReadLine();
                if (toSend != null)
                {
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(toSend, 0, toSend.Length), 0, toSend.Length);
                    received = await stream.ReadAsync(buffer);
                    message = Encoding.ASCII.GetString(buffer, 0, received);
                    Console.WriteLine(message);
                }
            }
        }
    }
}
