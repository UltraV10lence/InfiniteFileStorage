namespace Shared.Serverbound;

public enum LoginResult : byte {
    Success,
    InvalidToken,
    SessionAlive
}