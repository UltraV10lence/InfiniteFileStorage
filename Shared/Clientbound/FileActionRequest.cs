namespace Shared.Clientbound;

public class FileActionRequest(long fileId, FileAction action) {
    public readonly long FileId = fileId;
    public readonly FileAction Action = action;

    public static byte[] Encode(FileActionRequest i) => [ (byte) i.Action, ..BitConverter.GetBytes(i.FileId) ];
    public static FileActionRequest Decode(byte[] d) => new(BitConverter.ToInt64(d, 1), (FileAction)d[0]);
}

public enum FileAction : byte {
    Delete,
    Read
}