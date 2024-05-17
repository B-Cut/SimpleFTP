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
        // A dictionary of commands and their handler functions
        // All of them follow the same pattern, with the same arguments and return types, so doing this is easy
        // Doing this means not using a big switch case
        
        private static readonly Dictionary<string, Func<SimpleFtpServerState, string[], Task>> serverCommands = new Dictionary<string, Func<SimpleFtpServerState, string[], Task>>
        {
            { "USER", HandleUser },
            { "ACCT", HandleAcct },
            { "PASS", HandlePass },
            { "TYPE", HandleType },
            { "LIST", HandleList },
            { "CDIR", HandleCdir },
            { "KILL", HandleKill },
            { "NAME", HandleName },
            { "DONE", HandleDone },
            { "RETR", HandleRetr },
            { "STOR", HandleStor }
        };

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

            var option = splitCommand[0].ToUpper();

            if (!serverCommands.ContainsKey(option)) {
                await state.SendMessage("-Invalid command");
                return;
            }

            if (!(option == "USER" || option == "ACCT" || option == "PASS") && !state.CurrentUser.isUserLogged())
            {
                await state.SendMessage("-User not logged");
                return;
            }

            // All functions aside from DONE receive at least one argument
            if (splitCommand.Length < 2 && option is not "DONE")
            {
                await state.SendMessage($"-Insufficient arguments for command {option}");
                return;
            }

            // After we ensure the command exists in the command dictionary, we simply access it's entry and execute the function
            
            // I'm not sure if this is efficient, but it's more ellegant than a big switch case

            await serverCommands[option](state, splitCommand);
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
                // We use Skip(1) since 0 is the argument and 1 is the F/V switch
                var temp = joinPath(listDirectory, splitCommand.Skip(2).ToArray());

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

        private static async Task HandleCdir(SimpleFtpServerState state, string[] splitCommand)
        {
            // This task is a bit more involved since the server waits for another commands
            // So we will hand for a bit here.

            string path = "";

            if (splitCommand[1] == "..")
            {
                path = Directory.GetParent(state.WorkingDirectory).FullName;
            } else
            {
                path = joinPath(state.WorkingDirectory, splitCommand.Skip(1).ToArray());
            }

            if (path == ".")
            {
                await state.SendMessage($"-Can't connect to directory because: tried to change to current directory");
                return;
            }

            else if (!Directory.Exists(path))
            {
                await state.SendMessage($"-Can't connect to directory because: directory \"{path}\" doesn't exist");
                return;
            }
            else
            {
                if (state.CurrentUser.isUserLogged() || !state.usingAccountAndPassword())
                {
                    state.WorkingDirectory = path;
                    await state.SendMessage($"!Changed working dir to {state.WorkingDirectory}");
                } else
                {
                    await state.SendMessage("+directory ok, send account and password");
                    bool canChange = await cdirWaitForLogin(state);
                    if (canChange)
                    {
                        state.WorkingDirectory = path;
                        await state.SendMessage($"!Changed working dir to {state.WorkingDirectory}");
                    }
                }
            }
        }

        private static async Task<bool> cdirWaitForLogin(SimpleFtpServerState state)
        {
            // Ideally, this should call HandleAcct and HandlePass, but for now i'll do it like this
            while(true)
            {
                string command = await state.ReceiveMessage();

                var splitCommand = command.Split(' ');

                if(splitCommand[0].ToUpper() != "ACCT" && splitCommand[0].ToUpper() != "PASS")
                {
                    // if the command is not ACCT or PASS we assume the user does not want to continue this operation
                    // and execute the new command
                    await ParseCommand(command, state);
                    return false;
                }
                if(splitCommand.Length < 2)
                {
                    await state.SendMessage($"-Insufficient arguments for command: {splitCommand[0]}");
                    continue;
                }

                if (splitCommand[0].ToUpper() == "ACCT")
                {
                    if (state.CurrentUser.validateAccount(splitCommand[1]))
                    {
                        if (state.CurrentUser.hasValidAccount() && state.CurrentUser.hasValidPassword())
                        {
                            return true;
                        } else
                        {
                            await state.SendMessage("+account ok, send password");
                        }
                    } else
                    {
                        await state.SendMessage("-Invalid account");
                        return false;
                    }
                } else if (splitCommand[0].ToUpper() == "PASS")
                {
                    if (state.CurrentUser.validatePassword(splitCommand[1]))
                    {
                        if (state.CurrentUser.hasValidAccount() && state.CurrentUser.hasValidPassword())
                        {
                            return true;
                        }
                        else
                        {
                            await state.SendMessage("+password ok, send password");
                        }
                    } else
                    {
                        await state.SendMessage("-Invalid password");
                    }
                }
            }
        }

        public static async Task HandleKill(SimpleFtpServerState state, string[] splitCommand)
        {
            await state.SendMessage("Received the KILL command");
        }

        public static async Task HandleName(SimpleFtpServerState state, string[] splitCommand)
        {
            await state.SendMessage("Received the NAME command");
        }

        public static async Task HandleDone(SimpleFtpServerState state, string[] splitCommand)
        {
            await state.SendMessage("Received the DONE command");
        }

        public static async Task HandleRetr(SimpleFtpServerState state, string[] splitCommand)
        {
            await state.SendMessage("Received the RETR command");
        }

        public static async Task HandleStor(SimpleFtpServerState state, string[] splitCommand)
        {
            await state.SendMessage("Received the STOR command");
        }

        /// <summary>
        /// Small utility function that joins a path that cointaned spaces back into one thing
        /// </summary>
        /// <returns></returns>
        private static string joinPath(string basePath, string[] separatedParts)
        {
            string temp = "";
            foreach (string part in separatedParts)
            {
                temp += part + " ";
            }

            // Kind of wasteful but quick
            return Path.Combine(basePath, temp.Trim());
        }
    }
}
