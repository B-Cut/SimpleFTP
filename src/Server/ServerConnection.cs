using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using SimpleFTP.src;

namespace SimpleFTP.Server
{
    /// <summary>
    /// Defines the transfer type to be used by the server during file transfer
    /// </summary>
    

    /// <summary>
    /// This class represents a active connection to the server and it's properties
    /// </summary>
  
    // We implement IDisposable since TcpClient and NetworkStream implement it as well
    internal class ServerConnection : IDisposable
    {


        private byte[] buffer;
        private readonly string serverFolder;
        private string workingDirectory;
        private TcpClient tcpClient;
        private NetworkStream stream;
        private TransferType type;
        private User user;
        private bool useAccountAndPassword;
        private bool disposedValue;
        private bool _running = true;
        private string remoteHostName = "";

        /// <summary>
        /// Current working directory
        /// </summary>
        public string WorkingDirectory { get { return workingDirectory; } set { workingDirectory = value; } }
        /// <summary>
        /// Network stream currently connected
        /// </summary>
        public NetworkStream Stream { get { return stream; } set { stream = value; } }
        /// <summary>
        /// Specifies current transfer type
        /// </summary>
        public TransferType Type { get { return type; } set { type = value; } }
        /// <summary>
        /// Stores information about the current user
        /// </summary>
        public User CurrentUser { get { return user;  } }
        /// <summary>
        /// Stores the original folder. Server should not go above this.
        /// </summary>
        public string ServerFolder { get { return serverFolder;  } }

        public ServerConnection(byte[] buffer, string workingDirectory, TcpClient client, bool useAccountAndPassword = true)
        {
            this.buffer = buffer;
            serverFolder = Path.GetFullPath(workingDirectory);
            this.workingDirectory = serverFolder;
            this.useAccountAndPassword = useAccountAndPassword;
            stream = client.GetStream();
            tcpClient = client;
            user = new User(useAccountAndPassword);
            type = TransferType.BINARY; // Default from the specification
        }

        public bool usingAccountAndPassword() { return useAccountAndPassword; }

        /// <summary>
        /// Handles communication with remote TCP client. Disposes of itself when done.
        /// </summary>
        /// <returns></returns>
        public async Task Execute()
        {
            remoteHostName = tcpClient.Client.RemoteEndPoint!.ToString()!;
            try
            {
                Console.WriteLine($"Connection to {remoteHostName} successful, sending positive to client");
                var message = Encoding.ASCII.GetBytes($"+{Dns.GetHostName()} SFTP Service");
                await stream.WriteAsync(message, 0, message.Length);


                while (_running)
                {
                    string command = await ReceiveMessage();

                    await CommandHandler.ParseCommand(command, this);
                }
            } catch (IOException)
            {
                Console.WriteLine($"{remoteHostName}: Connection was forcibly closed by client");
            }
            finally
            {
                Console.WriteLine($"{remoteHostName}: Ending connection");
                Close();
            }
        }
        public async Task<string> ReceiveMessage()
        {
            if (stream == null)
            {
                throw new InvalidOperationException();
            }

            int received = await stream.ReadAsync(buffer, 0, buffer.Length);
            string command = Encoding.ASCII.GetString(buffer, 0, received);
            Console.WriteLine($"{remoteHostName}: Received command {command}");
            return command;
        }

        public async Task SendMessage(string message)
        {
            if (stream == null)
            {
                throw new InvalidOperationException();
            }


            // Strings are always null terminated
            var convertedMessage = Encoding.ASCII.GetBytes(message + "\0");

            try
            {
                await stream.WriteAsync(convertedMessage, 0, convertedMessage.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // I don't think this is the proper place to do this, but anywhere else feels like needless repetition
                    if (_running)
                    {
                        _running = false;
                        Task.Delay(500).Wait(); // Just in case
                    }

                    stream.Close();
                    tcpClient.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null

                disposedValue = true;
            }
        }

        /// <summary>
        /// Closes underlying TCP client and stream and disposes of this instance
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
