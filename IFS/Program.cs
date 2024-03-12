using Shared;
using UltoLibraryNew.Network.Apps.Tcp;

namespace IFS;

internal static class Program {
    private static void Main() {
        PacketInitializer.Initialize();
        var conn = new ServerConnection();
        conn.Client.OnConnect += Login;
        conn.Client.OnException += _ => Environment.Exit(2);
        conn.Client.OnDisconnect += _ => Environment.Exit(1);
        
        conn.Connect();
        conn.Client.CloseTask.Wait();
        return;

        void Login() {
            Task.Run(() => {
                var hexToken = Console.ReadLine();
                conn.Login(hexToken!);
                
                while (true) {
                    var cmd = Console.ReadLine();
                    if (cmd!.Length < 1) {
                        Console.WriteLine("Enter command");
                        break;
                    }

                    try {
                        switch (cmd[0]) {
                            case 'l':
                                conn.FilesList();
                                break;
                            case 'u':
                                if (cmd.Length < 3) {
                                    Console.WriteLine("Specify path");
                                    break;
                                }
                                var path = cmd[2..];
                                conn.UploadFile(path);
                                break;
                            case 'd':
                                if (cmd.Length < 3) {
                                    Console.WriteLine("Specify id");
                                    break;
                                }
                                var id = long.Parse(cmd[2..]);
                                conn.DeleteFile(id);
                                conn.FilesList();
                                break;
                            case 'g':
                                var s = cmd.Split(' ');
                                if (s.Length < 3) {
                                    Console.WriteLine("Specify id and name");
                                    break;
                                }
                                id = long.Parse(s[1]);
                                conn.DownloadFile(id, string.Join(' ', s[2..]));
                                break;
                            case 'e':
                                conn.Client.Disconnect(DisconnectReason.Disconnect);
                                break;
                        }
                    } catch {
                        Environment.Exit(3);
                    }
                }
            });
        }
    }
}