namespace Shared;

public class FileDataPart(byte[] data) {
    public readonly byte[] Data = data;

    public static FileDataPart Decode(byte[] data) => new(data);
    public static byte[] Encode(FileDataPart fd) => fd.Data;
}