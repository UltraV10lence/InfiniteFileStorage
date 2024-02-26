using DSharpPlus.Entities;
using Shared;
using Shared.Clientbound;
using Shared.Serverbound;
using UltoLibraryNew.Network.Apps;
using UltoLibraryNew.Network.Apps.Tcp;
using FileInfo = Shared.Clientbound.FileInfo;

namespace IFSServer;

public class TcpReceiver {
    public readonly TcpNetServer Server = new("192.168.0.100", 24865);
    public readonly Dictionary<NetConnection, ClientSession> Sessions = new();

    public TcpReceiver() {
        PacketInitializer.Initialize();
        Server.OnConnect += c => {
            c.OnDisconnect += _ => Sessions.Remove(c);

            c.OnPacketReceive += p => {
                try {
                    PacketReceive(c, p);
                } catch (Exception e) {
                    Console.WriteLine(c.RemoteIp + ": " + e);
                }
            };
        };
    }

    private void PacketReceive(NetConnection c, object p) {
        if (p is Login login) {
            var acc = Program.GetAccount(login.Token);
            
            if (acc == null) {
                c.SendPacket(LoginResult.InvalidToken);
                c.Disconnect(DisconnectReason.Disconnect);
                return;
            }

            if (Sessions.Values.Any(s => s.Token.SequenceEqual(login.Token))) {
                c.SendPacket(LoginResult.SessionAlive);
                c.Disconnect(DisconnectReason.Disconnect);
                return;
            }
            
            Sessions.Add(c, new ClientSession(acc.UserToken));
            c.SendPacket(LoginResult.Success);
            return;
        }
        
        var session = Sessions[c];
        var account = Program.GetAccount(session.Token)!;
        switch (p) {
            case FileDataPart part:
                if (account.CapacityBytes >= 0) {
                    var used = account.UsedBytes + part.Data.Length;
                    if (used > account.CapacityBytes) return;
                }
                
                account.UsedBytes += part.Data.Length;
                AppendData(session, part.Data).Wait();
                return;
            case FileInfo info:
                if (session.LastUploaded == null || session.BufferIndex == 0) return;

                var counted = GetFileParts(session.LastUploaded.Value).GetAwaiter().GetResult();
                Program.FilesTable.Add(new ServerUserFile {
                    UserToken = session.Token,
                    Name = info.FileName,
                    Size = counted,
                    LastMessageId = session.LastUploaded.Value
                });

                Console.WriteLine($"Added new uploaded file entry. (\"{info.FileName}\", Id: {session.LastUploaded.Value}, Size: {counted})");
                break;
            case FilesListRequest:
                var token = session.Token;
                var files = Program.FilesTable.Entries
                    .Where(f => f.UserToken.SequenceEqual(token)).ToArray();

                var response = new UserFile[files.Length];

                for (var i = 0; i < files.Length; i++) {
                    response[i] = new UserFile {
                        Name = files[i].Name,
                        Id = files[i].FileId,
                        Size = files[i].Size
                    };
                }
                
                c.SendPacket(new FilesList(response));
                break;
            case FileActionRequest request:
                var file = Program.FilesTable.Entries
                    .Find(f => f.FileId == request.FileId);

                if (file == null || !file.UserToken.SequenceEqual(session.Token)) return;

                var msg = Program.FilesChannel.GetMessageAsync(file.LastMessageId).GetAwaiter().GetResult();

                switch (request.Action) {
                    case FileAction.Delete:
                        while (true) {
                            msg.DeleteAsync();
                            if (msg.Reference == null) break;
                            msg = Program.FilesChannel.GetMessageAsync(msg.Reference.Message.Id).GetAwaiter().GetResult();
                        }
                        break;
                    case FileAction.Read:
                        while (true) {
                            Console.WriteLine(msg.Attachments[0].Url + ", " + msg.Attachments[0].ProxyUrl);
                            
                            if (msg.Reference == null) break;
                            msg = Program.FilesChannel.GetMessageAsync(msg.Reference.Message.Id).GetAwaiter().GetResult();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
        }
    }

    public async Task AppendData(ClientSession session, byte[] data) {
        if (session.BufferIndex + data.Length <= ClientSession.BlockSize) {
            session.Buffer.Write(data);
            session.BufferIndex += data.Length;
            return;
        }

        while (session.BufferIndex + data.Length > ClientSession.BlockSize) {
            var toUse = ClientSession.BlockSize - session.BufferIndex;
            session.Buffer.Write(data.AsSpan()[..toUse]);
            session.BufferIndex += toUse;
            data = data[toUse..];
            session.LastUploaded = await UploadBuffer(session);
        }
        
        session.Buffer.Write(data);
        session.BufferIndex += data.Length;
    }

    public async Task<ulong> UploadBuffer(ClientSession session) {
        session.Buffer.Seek(0, SeekOrigin.Begin);
        session.Buffer.SetLength(session.BufferIndex); // установить размер отправляемым данным
        
        var builder = new DiscordMessageBuilder().AddFile("file_slice.txt", session.Buffer, AddFileOptions.None);
        if (session.LastUploaded != null) builder.WithReply(session.LastUploaded);
        
        var msg = await Program.FilesChannel.SendMessageAsync(builder); // отправить часть файла в канал с файлами
        
        session.Buffer.Seek(0, SeekOrigin.Begin); // сбросить буффер
        session.BufferIndex = 0;
        return msg.Id;
    }
    
    public static async Task<long> GetFileParts(ulong lastId) {
        var sum = 0L;
        
        var msg = await Program.FilesChannel.GetMessageAsync(lastId);
        sum += msg.Attachments[0].FileSize;

        while (msg.Reference != null) {
            msg = msg.Reference.Message;
            sum += msg.Attachments[0].FileSize;
        }

        return sum;
    }
    
    public void Run() {
        Server.Bind();
        Server.CloseTask.Wait();
    }
}