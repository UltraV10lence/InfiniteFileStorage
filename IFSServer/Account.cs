using UltoLibraryNew.Databases;

namespace IFSServer;

public class Account {
    [IncludeInDatabase] public byte[] UserToken = [ ];
    [IncludeInDatabase] public long CapacityBytes = 1024 * 1024 * 1024 * 1L; // 1G - дефолт для акков
    [IncludeInDatabase] public long UsedBytes = 0;
}