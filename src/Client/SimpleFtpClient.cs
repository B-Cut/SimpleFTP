using SimpleFTP.src;
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
        static async Task<String> ReceiveMessage(NetworkStream stream, byte[] buffer)
        {
            int received = await stream.ReadAsync(buffer);
            var message = Encoding.ASCII.GetString(buffer, 0, received);
            return message;
        }

        public static async Task StartClient(string serverAddress, int port)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(serverAddress, AddressFamily.InterNetwork);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            TransferType transferType = TransferType.BINARY;
            var ipEndpoint = new IPEndPoint(ipAddress, port);

            using TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(ipEndpoint);
            using NetworkStream stream = tcpClient.GetStream();

            var buffer = new byte[1024];

            

            while (true) {
                {
                    var message = await ReceiveMessage(stream, buffer);
                    Console.WriteLine(message);

                    Console.Write("> ");
                    string toSend = Console.ReadLine()!;

                    await stream.WriteAsync(Encoding.ASCII.GetBytes(toSend, 0, toSend.Length), 0, toSend.Length);

                    
                    

                    if (toSend.Contains("done", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if(toSend.Contains("type a", StringComparison.OrdinalIgnoreCase) && message[0] == '+')
                    {
                        transferType = TransferType.ASCII;
                    }
                    if (toSend.Contains("type b", StringComparison.OrdinalIgnoreCase) && message[0] == '+')
                    {
                        transferType = TransferType.BINARY;
                    }
                    if (toSend.Contains("type c", StringComparison.OrdinalIgnoreCase) && message[0] == '+')
                    {
                        transferType = TransferType.CONTINUOUS;
                    }

                    if (toSend.Contains("retr", StringComparison.OrdinalIgnoreCase) && message[0] != '-')
                    {
                        message = await ReceiveMessage(stream, buffer);
                        ulong bytesToStore = ulong.Parse(message);
                        var fileName = toSend.Split(" ").Skip(1).Aggregate("", (accumulator, next) => accumulator= " " + next, result => result).Trim();
                        var temp = Encoding.ASCII.GetBytes("SEND");
                        await stream.WriteAsync(temp, 0, temp.Length);
                        await FileTransfer.ReceiveFile(fileName, Directory.GetCurrentDirectory(), transferType, stream, bytesToStore);
                    }
                }
            }
        }
    }
}
