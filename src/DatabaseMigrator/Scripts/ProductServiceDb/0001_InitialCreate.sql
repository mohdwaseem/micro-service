-- ProductServiceDb — Initial schema
-- Mirrors the EF Core 'InitialCreate' migration (20260620120725_InitialCreate).

CREATE TABLE [Products] (
    [Id]            uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Name]          nvarchar(200)    NOT NULL,
    [Description]   nvarchar(max)    NOT NULL,
    [Price]         decimal(18, 2)   NOT NULL,
    [StockQuantity] int              NOT NULL,
    [Category]      nvarchar(max)    NOT NULL,
    [CreatedAt]     datetime2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

-- ── EF Core migration history ─────────────────────────────────────────────────

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
    WHERE [MigrationId] = '20260620120725_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260620120725_InitialCreate', '10.0.9');
END;
