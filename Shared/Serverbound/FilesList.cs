namespace Shared.Serverbound;

public class FilesList(UserFile[] files) {
    public readonly UserFile[] Files = files;

    public static byte[] Encode(FilesList filesList) {
        using var data = new MemoryStream();
        using var writer = new BinaryWriter(data);

        writer.Write(filesList.Files.Length);
        foreach (var file in filesList.Files) {
            writer.Write(file.Name);
            writer.Write(file.Id);
            writer.Write(file.Size);
        }

        return data.ToArray();
    }
    public static FilesList Decode(byte[] data) {
        var reader = new BinaryReader(new MemoryStream(data));
        var files = new UserFile[reader.ReadInt32()];

        for (var i = 0; i < files.Length; i++) {
            files[i] = new UserFile {
                Name = reader.ReadString(),
                Id = reader.ReadInt64(),
                Size = reader.ReadInt64()
            };
        }

        return new FilesList(files);
    }
}