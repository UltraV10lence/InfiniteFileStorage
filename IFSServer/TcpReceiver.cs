using DSharpPlus.Entities;
using Shared;
using Shared.Clientbound;
using Shared.Serverbound;
using UltoLibraryNew;
using UltoLibraryNew.Network.Apps;
using UltoLibraryNew.Network.Apps.Tcp;
using FileInfo = Shared.Clientbound.FileInfo;

namespace IFSServer;

public class TcpReceiver {
    public readonly TcpNetServer Server = new("127.0.0.1", 24865);
    public readonly Dictionary<NetConnection, ClientSession> Sessions = new();

    public TcpReceiver() {
        PacketInitializer.Initialize();
        Server.OnConnect += c => {
            c.OnDisconnect += _ => Sessions.Remove(c);

            c.OnPacketReceive += p => {
                try {
                    PacketReceive(c, p).Wait();
                } catch (Exception e) {
                    Console.WriteLine(c.RemoteIp + ": " + e);
                }
            };
        };
    }

    private async Task PacketReceive(NetConnection c, object p) {
        if (p is Login login) {
            var acc = Program.GetAccount(login.Token);
            
            if (acc == null) {
                c.SendPacket(new LoginResult(LoginResult.Result.InvalidToken, 0));
                c.Disconnect(DisconnectReason.Disconnect);
                return;
            }

            if (Sessions.Values.Any(s => s.Token.SequenceEqual(login.Token))) {
                c.SendPacket(new LoginResult(LoginResult.Result.SessionAlive, 0));
                c.Disconnect(DisconnectReason.Disconnect);
                return;
            }
            
            Sessions.Add(c, new ClientSession(acc.UserToken));
            c.SendPacket(new LoginResult(LoginResult.Result.Success, acc.CapacityBytes));
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
                await AppendData(session, part.Data);
                return;
            case FileInfo info:
                Console.WriteLine(info.FileName);
                if (session.BufferIndex > 0) session.LastUploaded = await UploadBuffer(session);
                if (session.LastUploaded == null) return;

                var counted = await GetFileParts(session.LastUploaded.Value);
                Program.FilesTable.Add(new ServerUserFile {
                    UserToken = session.Token,
                    Name = info.FileName,
                    Size = counted,
                    LastMessageId = session.LastUploaded.Value
                });

                Console.WriteLine($"Added new uploaded file entry. (\"{info.FileName}\", Id: {session.LastUploaded.Value}, Size: {counted})");
                session.LastUploaded = null;
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

                var msg = await Program.FilesChannel.GetMessageAsync(file.LastMessageId);

                switch (request.Action) {
                    case FileAction.Delete:
                        while (true) {
                            await msg.DeleteAsync();
                            if (msg.Reference == null) break;
                            msg = await Program.FilesChannel.GetMessageAsync(msg.Reference.Message.Id);
                        }

                        Program.FilesTable.Entries.RemoveAll(f => f.FileId == request.FileId);
                        break;
                    case FileAction.Download:
                        var packetBuffer = new byte[1024 * 1024];
                        while (true) {
                            var url = msg.Attachments[0].Url;

                            using (var client = new HttpClient()) {
                                var messageBuffer = await client.GetByteArrayAsync(url);
                                var dec = new MemoryStream(UltoBytes.DecryptAes(messageBuffer, session.Token));

                                int packetLength;
                                while ((packetLength = dec.Read(packetBuffer)) > 0) {
                                    c.SendPacket(new FileDataPart(packetBuffer[..packetLength]));
                                }
                            }
                            
                            if (msg.Reference == null) break;
                            msg = await Program.FilesChannel.GetMessageAsync(msg.Reference.Message.Id);
                        }
                        
                        c.SendPacket(new FileEnd());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
        }
    }

    public static async Task AppendData(ClientSession session, byte[] data) {
        if (session.BufferIndex + data.Length < ClientSession.BlockSize) {
            goto appendAll;
        }

        while (session.BufferIndex + data.Length >= ClientSession.BlockSize) {
            var toUse = Math.Min(ClientSession.BlockSize - session.BufferIndex, data.Length);
            session.Buffer.Write(data[..toUse]);
            session.BufferIndex += toUse;
            data = data[toUse..];
            session.LastUploaded = await UploadBuffer(session);
        }
        
        appendAll:
        session.Buffer.Write(data);
        session.BufferIndex += data.Length;
    }

    public static async Task<ulong> UploadBuffer(ClientSession session) {
        session.Buffer.Seek(0, SeekOrigin.Begin);
        session.Buffer.SetLength(session.BufferIndex); // установить размер отправляемым данным
        var encrypted = UltoBytes.EncryptAes(session.Buffer.ToArray(), session.Token); // зашифровать данные
        
        var builder = new DiscordMessageBuilder().AddFile("file_slice.txt", new MemoryStream(encrypted), AddFileOptions.None);
        if (session.LastUploaded != null) builder.WithReply(session.LastUploaded, true);
        
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
        Console.WriteLine("Server started");
        Server.CloseTask.Wait();
        Console.WriteLine("Server stopped");
    }
}