using UltoLibraryNew.Databases;

namespace IFSServer;

public class ServerUserFile {
    [IncludeInDatabase] public long FileId = DateTime.Now.ToBinary();
    [IncludeInDatabase] public byte[] UserToken = [ ];
    [IncludeInDatabase] public string Name = string.Empty;
    [IncludeInDatabase] public long Size;
    [IncludeInDatabase] public ulong LastMessageId;
}