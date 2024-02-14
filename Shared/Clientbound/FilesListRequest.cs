using UltoLibraryNew.Network.Apps.Packets;

namespace Shared.Clientbound;

public class FilesListRequest : PacketNoEncryption {
    public static byte[] Encode(FilesListRequest req) => [ ];
    public static FilesListRequest Decode(byte[] dat) => new();
}