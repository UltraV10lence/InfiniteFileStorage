using Shared;
using Shared.Clientbound;
using Shared.Serverbound;
using UltoLibraryNew;
using UltoLibraryNew.Network.Apps.Tcp;
using FileInfo = Shared.Clientbound.FileInfo;

namespace IFS;

public class ServerConnection {
    public readonly TcpNetClient Client = new("127.0.0.1", 24865);
    public UserFile[]? Files;
    
    public void Connect() {
        Client.OnPacketReceive += p => {
            switch (p) {
                case LoginResult r:
                    Console.WriteLine((byte)r.LoggedIn);
                    if (r.LoggedIn == LoginResult.Result.Success) Console.WriteLine(r.AccountSpace); 
                    break;
                case FilesList f:
                    Files = f.Files;

                    foreach (var file in Files) {
                        Console.WriteLine($"{file.Id} {file.Size} {file.Name}");
                    }
                    break;
            }
        };
        Client.Connect();
    }

    public void Login(string token) {
        var realToken = UltoBytes.FromHexStr(token);
        Client.SendPacket(new Login(realToken));
    }

    public void FilesList() {
        Client.SendPacket(new FilesListRequest());
    }

    public void UploadFile(string path) {
        var file = File.OpenRead(path.Replace("\"", ""));
        var buffer = new byte[1024 * 1024];
        int length;
        while ((length = file.Read(buffer)) > 0) {
            Client.SendPacket(new FileDataPart(buffer[..length]));
        }
        Client.SendPacket(new FileInfo(Path.GetFileName(file.Name)));
        Console.WriteLine('c');
    }

    public void DeleteFile(long id) {
        Client.SendPacket(new FileActionRequest(id, FileAction.Delete));
    }

    public void DownloadFile(long id, string pathToSave) {
        pathToSave = Path.GetFullPath(pathToSave.Replace("\"", ""));
        using var file = File.OpenWrite(pathToSave);
        var tcs = new TaskCompletionSource();
        
        Client.OnPacketReceive += AppendData;
        Client.SendPacket(new FileActionRequest(id, FileAction.Download));
        
        tcs.Task.Wait();
        Client.OnPacketReceive -= AppendData;
        return;

        void AppendData(object packet) {
            switch (packet) {
                case FileDataPart dp:
                    file.Write(dp.Data);
                    break;
                case FileEnd:
                    tcs.SetResult();
                    break;
            }
        }
    }
}