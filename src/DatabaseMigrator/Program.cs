// DatabaseMigrator — runs DbUp SQL scripts against all three service databases.
//
// Usage (CLI args):
//   DatabaseMigrator --user <connStr> --product <connStr> --order <connStr>
//
// Usage (environment variables):
//   MIGRATE_USER_CS     — UserServiceDb connection string
//   MIGRATE_PRODUCT_CS  — ProductServiceDb connection string
//   MIGRATE_ORDER_CS    — OrderServiceDb connection string
//
// Exit codes: 0 = all databases up to date, 1 = one or more migrations failed.
//
// Scripts are embedded resources ordered by filename prefix (e.g. 0001_, 0002_).
// DbUp tracks which scripts have run in a SchemaVersions table — re-running is safe.

using DbUp;
using DbUp.Engine;

Console.WriteLine("=== MicroServiceDemo — DbUp Database Migrator ===");
Console.WriteLine();

var databases = new (string Name, string ScriptPrefix, string ConnectionString)[]
{
    ("UserServiceDb",    "UserServiceDb",    Resolve("--user",    args, "MIGRATE_USER_CS")),
    ("ProductServiceDb", "ProductServiceDb", Resolve("--product", args, "MIGRATE_PRODUCT_CS")),
    ("OrderServiceDb",   "OrderServiceDb",   Resolve("--order",   args, "MIGRATE_ORDER_CS")),
};

int exitCode = 0;

foreach (var (name, scriptPrefix, connectionString) in databases)
{
    Console.WriteLine($"── {name} ──────────────────────────────────────");

    try
    {
        // Create the database if it does not exist yet
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(Program).Assembly,
                // Only include scripts whose embedded resource name contains the database name,
                // e.g. "DatabaseMigrator.Scripts.UserServiceDb.0001_InitialCreate.sql"
                script => script.Contains($".Scripts.{scriptPrefix}."))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        if (!upgrader.IsUpgradeRequired())
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   Already up to date — nothing to run.");
            Console.ResetColor();
            Console.WriteLine();
            continue;
        }

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();

        if (result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   Migration succeeded.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"   Migration FAILED: {result.Error?.Message}");
            Console.ResetColor();
            exitCode = 1;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"   Error connecting to {name}: {ex.Message}");
        Console.ResetColor();
        exitCode = 1;
    }

    Console.WriteLine();
}

Console.WriteLine(exitCode == 0
    ? "All databases are up to date."
    : "One or more migrations failed — see errors above.");

return exitCode;

// ── helpers ──────────────────────────────────────────────────────────────────

static string Resolve(string cliFlag, string[] args, string envVar)
{
    // Check CLI flags first: --user "Server=..."
    int idx = Array.IndexOf(args, cliFlag);
    if (idx >= 0 && idx + 1 < args.Length)
        return args[idx + 1];

    // Fall back to environment variable
    return Environment.GetEnvironmentVariable(envVar)
        ?? throw new InvalidOperationException(
            $"Connection string not provided. " +
            $"Pass '{cliFlag} <connStr>' or set the {envVar} environment variable.");
}
