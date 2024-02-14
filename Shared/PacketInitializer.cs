using Shared.Clientbound;
using Shared.Serverbound;
using UltoLibraryNew.Network.Apps.Packets;
using FileInfo = Shared.Clientbound.FileInfo;

namespace Shared;

public static class PacketInitializer {
    public static void Initialize() {
        PacketDictionary.RegisterPacket(0, Login.Encode, Login.Decode);
        PacketDictionary.RegisterPacket(1, FileInfo.Encode, FileInfo.Decode);
        PacketDictionary.RegisterPacket(2, FileDataPart.Encode, FileDataPart.Decode);
        
        PacketDictionary.RegisterPacket(100, FilesList.Encode, FilesList.Decode);
    }
}