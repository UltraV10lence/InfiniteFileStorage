using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using UltoLibraryNew;
using UltoLibraryNew.Databases;
using Timer = System.Timers.Timer;

namespace IFSServer;

internal static class Program {
    public static Database Database = null!;
    public static DiscordClient DiscordClient = null!;
    public static DiscordGuild Guild = null!;
    public static readonly TcpReceiver Receiver = new();
    
    private static void Main() {
        Console.WriteLine("Loading bot...");
        InitializeBot().Wait();
        Console.WriteLine("Bot loaded.");
        
        Console.WriteLine("Loading database...");
        var initializedDatabase = InitializeDatabase();
        Database = initializedDatabase.db;
        if (initializedDatabase.isNew) SaveDatabase();
        Console.WriteLine("Database loaded.");

        var autosave = new Timer(TimeSpan.FromMinutes(5)); // Автосохранение базы данных каждые 5 минут
        autosave.Elapsed += (_, _) => SaveDatabase();
        autosave.Start();
        
        Receiver.Run();
    }

    private static async Task InitializeBot() {
        DiscordClient = new DiscordClient(new DiscordConfiguration {
            Token = DiscordBot.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents, // Может читать содержание сообщений (важно)
            MinimumLogLevel = LogLevel.None
        });

        await DiscordClient.ConnectAsync(status: UserStatus.DoNotDisturb);
        Guild = await DiscordClient.GetGuildAsync(DiscordBot.ServerId, true); // Подключается к серверу
    }

    private static (Database db, bool isNew) InitializeDatabase() {
        if (File.Exists("accounts.db")) return (Database.Load("accounts.db"), false); // Если БД есть, то она загружается

        var db = new Database();
        db.CreateTable<Account>("accounts"); // Создаётся таблица с аккаунтами
        db.CreateTable<ServerUserFile>("files"); // Создаётся таблица с файлами

        var acc = CreateAdminAccount(db); // Создаётся аккаунт админа
        Console.WriteLine("Admin account token: " + UltoBytes.ToHexStr(acc.UserToken));

        return (db, true);
    }

    public static Account CreateAdminAccount(Database? db = null) {
        db ??= Database;
        
        var token = UltoBytes.RandomSecure(32); // Создаётся токен аккаунта
        var account = new Account {
            UserToken = token,
            CapacityBytes = -1
        };
        db.GetTable<Account>("accounts")!.Add(account);
        return account;
    }

    public static Account? GetAccount(byte[] token) {
        return AccountsTable.Find(a => a.UserToken.SequenceEqual(token)); // По совпадению токена
    }

    public static void SaveDatabase() {
        Database.Save("accounts.db");
        Console.WriteLine("Database saved.");
    }
    
    public static DiscordChannel FilesChannel => Guild.GetChannel(DiscordBot.ChannelId);
    public static DatabaseTable<Account> AccountsTable => Database.GetTable<Account>("accounts")!;
    public static DatabaseTable<ServerUserFile> FilesTable => Database.GetTable<ServerUserFile>("files")!;
}