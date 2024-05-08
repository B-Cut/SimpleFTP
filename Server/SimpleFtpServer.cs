﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleFTP.Server
{
    internal enum TransferType
    {
        ASCII,
        BINARY,
        CONTINUOUS
    }

    /// <summary>
    /// This class contains the current state of the server.
    /// </summary>
    /// 

    // If we implement this, we can execute functions using the command handler
    internal class SimpleFtpServerState
    {
        private byte[] buffer;
        private string workingDirectory;
        private NetworkStream? stream;
        private TransferType type;

        public byte[] Buffer { get { return buffer; } }
        public string WorkingDirectory { get { return workingDirectory; } }
        public NetworkStream? Stream { get { return stream; } set { stream = value; } }
        public TransferType Type { get { return type; } set { type = value; } }

        public SimpleFtpServerState(byte[] buffer, string workingDirectory)
        {
            this.buffer = buffer;
            this.workingDirectory = workingDirectory;
            this.stream = null;
            this.type = TransferType.BINARY; // Default from the specification
        }
    }

    /// <summary>
    /// This class contains the logic for creating and operating the FTP server
    /// </summary>
    internal class SimpleFtpServer
    {
        // Port that the server listens to
        private readonly int _port;
        private TcpListener _listener;
        // Information about the server current state
        private SimpleFtpServerState _state;


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
            _state = new SimpleFtpServerState(new byte[1024], workingDirectoryPath);
            _listener = new TcpListener(IPAddress.Loopback, _port);
            StartServer().Wait();
        }

        // Separating the startup and the actual running of the server might make it more
        // error resistant
        private async Task RunServer()
        {

        }

        // Actual server logic comes here
        private async Task StartServer()
        {
            try
            {
                // First we wait for a connection
                _listener.Start();
                Console.WriteLine("Server Started...");
                Console.WriteLine($"Awaiting connection on {_listener.LocalEndpoint}...");
                using TcpClient handler = _listener.AcceptTcpClient();
                Console.WriteLine("Stabilishing a stream...");
                _state.Stream = handler.GetStream();

                // Not sure when i should send a negative connection yet
                Console.WriteLine("Connection sucessful, sending positive to client");
                var message = Encoding.ASCII.GetBytes($"+{Dns.GetHostName()} SFTP Service");
                await _state.Stream.WriteAsync(message, 0, message.Length);

                // Now we keep the connection alive and wait for the user messages

                // User authenthication will happen here
                while (true)
                {
                    int received = await _state.Stream.ReadAsync(_state.Buffer, 0, _state.Buffer.Length);
                    string command = Encoding.ASCII.GetString(_state.Buffer, 0, received);
                    Console.WriteLine($"Received command: {command}");
                    
                    await ParseCommand(command);
                }


            }
            catch (IOException e){
                Console.WriteLine("Connection was forcibly closed");
                // TODO: Make server persist through connection closing
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

        private async Task ParseCommand(string command)
        {
            string[] splitCommand = command.Split(" ");
            
            switch (splitCommand[0].ToUpper())
            {
                case "USER":
                    Console.WriteLine("Received the USER command");
                    break;
                case "ACCT":
                    Console.WriteLine("Received the ACCT command");
                    break;
                case "PASS":
                    Console.WriteLine("Received the PASS command");
                    break;
                case "TYPE":
                    await CommandHandler.HandleType(_state, splitCommand);
                    break;
                case "LIST":
                    Console.WriteLine("Received the LIST command");
                    break;
                case "CDIR":
                    Console.WriteLine("Received the CDIR command");
                    break;
                case "KILL":
                    Console.WriteLine("Received the KILL command");
                    break;
                case "NAME":
                    Console.WriteLine("Received the NAME command");
                    break;
                case "DONE":
                    Console.WriteLine("Received the DONE command");
                    break;
                case "RETR":
                    Console.WriteLine("Received the RETR command");
                    break;
                case "STOR":
                    Console.WriteLine("Received the STOR command");
                    break;
                default:
                    var message = Encoding.ASCII.GetBytes($"-Invalid command");
                    await _state.Stream.WriteAsync(message, 0, message.Length);
                    break;
            }
        }
    }
}
