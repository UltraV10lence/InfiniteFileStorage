namespace Shared.Clientbound;

public class Login(byte[] token) {
    public readonly byte[] Token = token;

    public static byte[] Encode(Login l) => l.Token;
    public static Login Decode(byte[] d) => new(d);
}