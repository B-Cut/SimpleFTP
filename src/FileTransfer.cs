using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP.src
{
    internal enum TransferType
    {
        ASCII,
        BINARY,
        CONTINUOUS
    }
    /// <summary>
    /// This class is responsible for handling the actual transfer of files
    /// </summary>
    internal class FileTransfer
    {
        public static async Task ReceiveFile(String fileName, String endDirectory, TransferType transferType, NetworkStream stream, ulong bytesToReceive)
        {
            // The specification differentiates between continuous and binary tranfer types
            // Since most systems nowadays use 8 bit bytes, their implementation will be the same here
            var tempFolder = Directory.CreateTempSubdirectory();
            var tempFilePath = Path.Combine(tempFolder.FullName, fileName);
            byte[] buffer = new byte[1024];

            if (transferType == TransferType.ASCII)
            {
                using (StreamWriter tempWriter = new StreamWriter(tempFilePath))
                {

                    while (bytesToReceive > 0)
                    {
                        int received = await stream.ReadAsync(buffer, 0, buffer.Length);
                        var message = Encoding.ASCII.GetString(buffer, 0, received);
                        await tempWriter.WriteAsync(message);
                        bytesToReceive -= (ulong)received;
                    }
                }   
            } else
            {
                using (FileStream tempStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    while (bytesToReceive > 0)
                    {
                        int received = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if ((ulong)received > bytesToReceive)
                        {
                            await tempStream.WriteAsync(buffer, 0, (int) bytesToReceive);
                            bytesToReceive = 0;
                        }
                        else
                        {
                            await tempStream.WriteAsync(buffer, 0, received);
                            bytesToReceive -= (ulong)received;
                        }
                        
                    }
                }
            }
            File.Move(tempFilePath, Path.Combine(endDirectory, fileName));
        }

        public static async Task SendFile(String filePath, TransferType transferType, NetworkStream stream)
        {
            if (transferType == TransferType.ASCII)
            {
                try
                {
                    var sr = new StreamReader(filePath);
                    int read = 1;
                    char[] buffer = new char[1024];
                    while (read > 0)
                    {
                        read = await sr.ReadAsync(buffer, 0, buffer.Length);
                        var toSend = Encoding.ASCII.GetBytes(buffer, 0, read);
                        await stream.WriteAsync(toSend, 0, toSend.Length);
                    }
                } catch
                {
                    throw;
                }
            }
            try
            {
                var file = File.OpenRead(filePath);
                int read = 1;
                byte[] buffer = new byte[1024];
                while (read > 0)
                {
                    read = await file.ReadAsync(buffer, 0, buffer.Length);
                    await stream.WriteAsync(buffer, 0, read);
                }
            } catch {
                throw;
            }
            return;
        }
    }
}
