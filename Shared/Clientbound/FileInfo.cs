using System.Text;

namespace Shared.Clientbound;

public class FileInfo(string fileName) {
    public readonly string FileName = fileName;

    public static byte[] Encode(FileInfo i) => Encoding.UTF8.GetBytes(i.FileName);
    public static FileInfo Decode(byte[] d) => new(Encoding.UTF8.GetString(d));
}