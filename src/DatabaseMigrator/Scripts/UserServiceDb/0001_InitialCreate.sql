-- UserServiceDb — Initial schema
-- Mirrors the EF Core 'InitialCreate' migration (20260620120607_InitialCreate).
-- Running this script via DbUp in CI replaces the need for 'dotnet ef database update'.

CREATE TABLE [Users] (
    [Id]           uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Email]        nvarchar(256)    NOT NULL,
    [PasswordHash] nvarchar(max)    NOT NULL,
    [FirstName]    nvarchar(100)    NOT NULL,
    [LastName]     nvarchar(100)    NOT NULL,
    [CreatedAt]    datetime2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

-- ── EF Core migration history ─────────────────────────────────────────────────
-- Insert a record so that EF Core sees this migration as already applied
-- and skips it when the service starts with db.Database.Migrate() in Development.

IF OBJECT_ID('[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId]    nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = '20260620120607_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260620120607_InitialCreate', '10.0.9');
END;
