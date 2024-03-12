using Shared.Clientbound;
using Shared.Serverbound;
using UltoLibraryNew.Network.Apps.Packets;
using FileInfo = Shared.Clientbound.FileInfo;

namespace Shared;

public static class PacketInitializer {
    public static void Initialize() {
        PacketDictionary.RegisterPacket(0, Login.Encode, Login.Decode);
        PacketDictionary.RegisterPacket(1, FileInfo.Encode, FileInfo.Decode);
        PacketDictionary.RegisterPacket(2, FileActionRequest.Encode, FileActionRequest.Decode);
        PacketDictionary.RegisterPacket(3, FilesListRequest.Encode, FilesListRequest.Decode);

        PacketDictionary.RegisterPacket(100, LoginResult.Encode, LoginResult.Decode);
        PacketDictionary.RegisterPacket(101, FilesList.Encode, FilesList.Decode);
        PacketDictionary.RegisterPacket(102, _ => [], _ => new FileEnd());
        
        PacketDictionary.RegisterPacket(200, FileDataPart.Encode, FileDataPart.Decode);
    }
}