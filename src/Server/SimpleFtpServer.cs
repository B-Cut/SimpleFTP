using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleFTP.Server
{
    /// <summary>
    /// This class contains the logic for creating and operating the FTP server
    /// </summary>
    internal class SimpleFtpServer
    {
        // Port that the server listens to
        private readonly int _port;
        private TcpListener _listener;
        private string _workingDirectory;
        // Information about the server current state


        /// <summary>
        /// Creates a Simple FTP server on the specified <c>port</c> bound to the loopback address and uses the 
        /// given <c>workingDirectoryPath</c> as the initial working directory. If no <c>port</c> is defined, defaults to 115. 
        /// As a starting point, this server supports only a single connection at a time.
        /// </summary>1
        /// <param name="port"></param>
        /// <param name="workingDirectoryPath"></param>
        public SimpleFtpServer(string workingDirectoryPath, int port = 115)
        {
            _port = port;
            _workingDirectory = workingDirectoryPath;
            _listener = new TcpListener(IPAddress.Loopback, _port);
            StartServer().Wait();
        }

        // Actual server logic comes here
        private async Task StartServer()
        {
            try
            {
                // First we wait for a connection
                _listener.Start();
                Console.WriteLine("Server started...");
                Console.WriteLine($"Awaiting connection on {_listener.LocalEndpoint}...");
                while (true)
                {                  
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Stabilishing connection...");

                    // Creating a connection
                    var newConnection = new ServerConnection(new byte[1024], _workingDirectory, client);

                    // Now we create a thread to keep the connection alive and wait for the user messages

                    _ = Task.Factory.StartNew(newConnection.Execute, TaskCreationOptions.LongRunning);
                }         
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _listener.Stop();
            }
        }
    }
}
