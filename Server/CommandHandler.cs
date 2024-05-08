using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP.Server
{
    /// <summary>
    /// This class encapsulates the functions needed to handle
    /// the different messages received by the server
    /// </summary>
    internal static class CommandHandler
    {
        private static async Task SendMessage(NetworkStream stream, string message)
        {
            var convertedMessage = Encoding.ASCII.GetBytes(message);

            try
            {
                await stream.WriteAsync(convertedMessage, 0, convertedMessage.Length); 
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task HandleType(SimpleFtpServerState state, string[] splitCommand)
        {
            if (splitCommand.Length < 2)
            {
                await SendMessage(state.Stream, "-Insufficient arguments");
                return;
            }

            switch (splitCommand[1].ToUpper())
            {
                case "A": 
                    state.Type = TransferType.ASCII;
                    await SendMessage(state.Stream, "+Using Ascii mode");
                    Console.WriteLine("Now using ASCII mode");
                    break;
                case "B":
                    state.Type = TransferType.BINARY;
                    await SendMessage(state.Stream, "+Using Binary mode");
                    Console.WriteLine("Now using Binary mode");
                    break;
                case "C":
                    state.Type = TransferType.CONTINUOUS;
                    await SendMessage(state.Stream, "+Using Continuous mode");
                    Console.WriteLine("Now using Continuous mode");
                    break;
                default:
                    await SendMessage(state.Stream, "-Type not valid");
                    break;
            }
        }
    }
}
