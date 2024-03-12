namespace Shared.Serverbound;

public class LoginResult(LoginResult.Result loggedIn, long accountSpace) {
    public readonly Result LoggedIn = loggedIn;
    public readonly long AccountSpace = accountSpace;

    public static byte[] Encode(LoginResult r) => [ (byte)r.LoggedIn, ..BitConverter.GetBytes(r.AccountSpace) ];
    public static LoginResult Decode(byte[] d) => new((Result)d[0], BitConverter.ToInt64(d, 1));
    
    public enum Result : byte {
        Success,
        InvalidToken,
        SessionAlive
    }
}