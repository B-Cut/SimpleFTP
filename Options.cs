using System;
using CommandLine;

namespace SimpleFTP
{
    [Verb("server", HelpText = "Run as a server")]
    class ServerOptions
    {
        [Value(0, Required = true, MetaName = "Starting directory", HelpText = "The initial working directory of the server")]
        public string startingDirectory { get; set; }
        [Option('p', "port", HelpText = "Host port. Defaults to 21 if not specified", Default = 115)]
        public int Port { get; set; }
    }

    [Verb("client", HelpText = "Run as a client")]
    class ClientOptions
    {
        [Value(0, Required = true, MetaName = "Server Address", HelpText = "IP address of the FTP server")]
        public string ServerIp { get; set; }

        [Option('p', "port", HelpText = "Server port. Defaults to 21 if not specified", Default = 115)]
        public int Port { get; set; }
    }
}



