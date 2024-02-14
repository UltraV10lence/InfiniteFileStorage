using Shared;
using Shared.Clientbound;
using Shared.Serverbound;
using UltoLibraryNew;
using UltoLibraryNew.Network.Apps.Tcp;
using FileInfo = Shared.Clientbound.FileInfo;

namespace IFS;

public class ServerConnection {
    public TcpNetClient Client = null!;
    public UserFile[]? Files;
    
    
    public void Connect(byte[] token) {
        Client = new TcpNetClient("192.168.0.100", 24865);
        Client.OnConnect += () => {
            Client.SendPacket(new Login(token));
            Client.SendPacket(new FilesListRequest());
        };

        Client.OnPacketReceive += p => {
            switch (p) {
                case FilesList f:
                    Files = f.Files;
                    break;
            }
        };
        Client.Connect();
    }

    public void Login(string token) {
        var realToken = UltoBytes.FromHexStr(token);
        Client.SendPacket(new Login(realToken));
    }

    public void UploadFile(string path) {
        var file = File.OpenRead(path);
        var buffer = new byte[1024 * 1024];
        int length;
        while ((length = file.Read(buffer)) > 0) {
            Client.SendPacket(new FileDataPart(buffer[..length]));
        }
        Client.SendPacket(new FileInfo(Path.GetFileName(file.Name)));
    }

    public void DeleteFile(long id) {
        
    }
}