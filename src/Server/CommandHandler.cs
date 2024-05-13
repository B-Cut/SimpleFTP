using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using SimpleFTP;

namespace SimpleFTP.Server
{
    /// <summary>
    /// This class encapsulates the functions needed to handle
    /// the different messages received by the server
    /// </summary>
    internal static class CommandHandler
    {
        // A dictionary of commands and function would be better
        // But i couldn't make it so it stored a method, so for now, the main parsing uses a big switch case


        /// <summary>
        /// Receives a string containing the received command and the state of the server.
        /// Starts execution of command if it is defined, else it sends a error message to client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static async Task ParseCommand(string command, SimpleFtpServerState state)
        {
            string[] splitCommand = command.Split(" ");

            // All functions aside from DONE receive at least one argument
            if (splitCommand.Length < 2 && splitCommand[0] is not "DONE")
            {
                await state.SendMessage($"-Insufficient arguments for command {splitCommand[0].ToUpper()}");
                return;
            }

            switch (splitCommand[0].ToUpper())
            {
                case "USER":
                    await HandleUser(state, splitCommand);
                    break;
                case "ACCT":
                    await HandleAcct(state, splitCommand);
                    break;
                case "PASS":
                    await HandlePass(state, splitCommand);
                    break;
                case "TYPE":
                    await HandleType(state, splitCommand);
                    break;
                case "LIST":
                    await HandleList(state, splitCommand);
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
                    await state.SendMessage("-Invalid command");
                    break;
            }
        }

        // This function deals with the user id property
        public static async Task HandleUser(SimpleFtpServerState state, string[] splitCommand)
        {
            string message;
            if (state.CurrentUser.validateUserId(splitCommand[1]))
            {
                if (state.CurrentUser.isUserLogged())
                {
                    message = $"!{state.CurrentUser.UserId} logged in";
                } else
                {
                    message = "+User-id valid, send account and password";

                }
            }
            else 
            {
                message = "-Invalid user, try again";
            }

            await state.SendMessage(message);
        }

        // This one with the account
        public static async Task HandleAcct(SimpleFtpServerState state, string[] splitCommand)
        {
            string message;

            // Verifies if account is valid
            if (state.CurrentUser.validateAccount(splitCommand[1]))
            {
                // Checks if user has enough info to be logged in 
                if (state.CurrentUser.isUserLogged())
                {
                    message = "!Account valid, logged in";
                } else
                {
                    message = "+Account valid, send password";
                }
            } else
            {
                message = "-Invalid account, try again";
            }

            await state.SendMessage(message);
        }

        // This one with the password
        public static async Task HandlePass(SimpleFtpServerState state, string[] splitCommand)
        {
            string message;

            if (state.CurrentUser.validatePassword(splitCommand[1]))
            {
                // Checks if user has enough info to be logged in 
                if (state.CurrentUser.isUserLogged())
                {
                    message = "!Logged in";
                }
                else
                {
                    message = "+Send account";
                }
            }
            else
            {
                message = "-Wrong password, try again";
            }

            await state.SendMessage(message);
        }

        public static async Task HandleType(SimpleFtpServerState state, string[] splitCommand)
        {
            switch (splitCommand[1].ToUpper())
            {
                case "A":
                    state.Type = TransferType.ASCII;
                    await state.SendMessage("+Using Ascii mode");
                    Console.WriteLine("Now using ASCII mode");
                    break;
                case "B":
                    state.Type = TransferType.BINARY;
                    await state.SendMessage("+Using Binary mode");
                    Console.WriteLine("Now using Binary mode");
                    break;
                case "C":
                    state.Type = TransferType.CONTINUOUS;
                    await state.SendMessage("+Using Continuous mode");
                    Console.WriteLine("Now using Continuous mode");
                    break;
                default:
                    await state.SendMessage("-Type not valid");
                    break;
            }
        }



        public static async Task HandleList(SimpleFtpServerState state, string[] splitCommand)
        {
            // TODO: As chamadas que se referem a Access Control são exclusivas do Windows. Separar chamadas para Windows e para unix
            string listDirectory = state.WorkingDirectory;

            // Directory path is not null
            if (splitCommand.Length >= 3)
            {
                var temp = splitCommand[2];
                for (int i = 3; i < splitCommand.Length; i++)
                {
                    temp += " " + splitCommand[i];
                }

                temp = Path.Combine(listDirectory, temp);
                if (Directory.Exists(temp))
                {
                    listDirectory = temp;
                }
                else
                {
                    await state.SendMessage($"-The directory {temp} does not exist in current path");
                    return;
                }
            }
            try
            {
                
                string message = $"+{listDirectory}\r\n";
                if (splitCommand[1].ToUpper() == "F")
                {
                    var dirContents = Directory.GetFileSystemEntries(listDirectory);
                    // Formatted shows only the name of the file/folder
                    foreach (var dir in dirContents)
                    {
                        string temp = dir.Replace(".\\", "");
                        if (temp.Contains(" "))
                        {
                            temp = $"\"{temp}\"";
                        }
                        temp += "\r\n";
                        message += temp;
                    }
                    message += "\0";

                    await state.SendMessage(message);
                    return;
                } else if (splitCommand[1].ToUpper() == "V")
                {
                    var directories = Directory.GetDirectories(listDirectory);
                    message += "TYPE \t CREATED AT\t\tLAST ACESSED\t\tOWNER \t GROUP\tNAME\r\n";

                    foreach ( var item in directories)
                    {   
                        var temp = "FOLDER \t ";
                        var dirInfo = new DirectoryInfo(item).GetAccessControl();
                        temp += Directory.GetCreationTime(item) + " \t";
                        temp += Directory.GetLastAccessTime(item) + " \t";
                        temp += dirInfo
                            .GetOwner(typeof(NTAccount))
                            .Value.Split(['/', '\\'])[1] + "\t ";
                        temp += dirInfo.GetGroup(typeof(NTAccount)).
                            Value.Split(['/', '\\'])[1] + "\t ";
                      
                        message += temp + item + "\r\n";
                    }


                    // TODO: Do both files and folders with no repetition
                    var files = Directory.GetFiles(listDirectory);
                    foreach(var file in files)
                    {
                        var temp = "FILE\t ";
                        var fileInfo = new FileInfo(file).GetAccessControl();
                        temp += File.GetCreationTime(file) + " \t";
                        temp += File.GetLastAccessTime(file) + " \t";
                        temp += fileInfo.
                            GetOwner(typeof(NTAccount))
                            .Value.Split(['/', '\\'])[1] + "\t ";
                        temp += fileInfo.GetGroup(typeof(NTAccount))
                            .Value.Split(['/', '\\'])[1] + "\t ";

                        message += temp + file + "\r\n";
                    }
                    message += "\0";
                    await state.SendMessage(message);
                    return;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                await state.SendMessage(Globals.GENERIC_ERROR_MESSAGE);
            }
            // Formatted list

            await state.SendMessage("-Invalid command. The format for this command is: LIST { F | V } directory path");
        }

    }
}
