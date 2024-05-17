using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP.Server
{
    /// <summary>
    /// Defines the transfer type to be used by the server during file transfer
    /// </summary>
    internal enum TransferType
    {
        ASCII,
        BINARY,
        CONTINUOUS
    }

    /// <summary>
    /// This class contains the current state of the server.
    /// </summary>
  
    // If we implement this, we can execute functions using the command handler
    internal class SimpleFtpServerState
    {
        private byte[] buffer;
        private readonly string serverFolder;
        private string workingDirectory;
        private NetworkStream? stream;
        private TransferType type;
        private User user;
        private bool useAccountAndPassword;

        /// <summary>
        /// Current working directory
        /// </summary>
        public string WorkingDirectory { get { return workingDirectory; } set { workingDirectory = value; } }
        /// <summary>
        /// Network stream currently connected
        /// </summary>
        public NetworkStream? Stream { get { return stream; } set { stream = value; } }
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

        public SimpleFtpServerState(byte[] buffer, string workingDirectory, bool useAccountAndPassword = true)
        {
            this.buffer = buffer;
            serverFolder = Path.GetFullPath(workingDirectory);
            this.workingDirectory = serverFolder;
            this.useAccountAndPassword = useAccountAndPassword;
            stream = null;
            user = new User(useAccountAndPassword);
            type = TransferType.BINARY; // Default from the specification
        }

        public bool usingAccountAndPassword() { return useAccountAndPassword; }

        // Not really right by OOP standarts, but useful. Might be moved to another class if state
        // gets too many functions/responsabilities

        public async Task<string> ReceiveMessage()
        {
            if (stream == null)
            {
                throw new InvalidOperationException();
            }

            int received = await stream.ReadAsync(buffer, 0, buffer.Length);
            string command = Encoding.ASCII.GetString(buffer, 0, received);
            Console.WriteLine($"Received command: {command}");
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
    }
}
