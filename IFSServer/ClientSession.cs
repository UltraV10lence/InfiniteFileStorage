namespace IFSServer;

public class ClientSession(byte[] token) {
    public const int BlockSize = 1024 * 1024 * 24; // 24M (дс поддерживает отправку файлов до 25М, но лучше обезопаситься заранее для сохранности файлов)
    public readonly MemoryStream Buffer = new(BlockSize);
    public readonly byte[] Token = token;
    public int BufferIndex;
    public ulong? LastUploaded;
}