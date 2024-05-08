/*
 * Author: Cael Gonçalves
 * 
 * This is a Simple FTP client/server implementation, as in the Simple FTP protocol defined by RFC913
 * The initial idea was to use the actual FTP implementation, but this is just a practice program, so 
 * this feels like the most achievable one
 * 
 */

using System;
using CommandLine;

namespace SimpleFTP;

internal class Program
{
    static void Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<ServerOptions, ClientOptions>(args);

        result.WithParsed<ServerOptions>(options =>
        {
           Server.SimpleFtpServer server = new Server.SimpleFtpServer(options.startingDirectory, options.Port);
        })
        .WithParsed<ClientOptions>(options =>
        {
            Client.SimpleFtpClient.StartClient(options.ServerIp, options.Port).Wait();
        });
    }
}
