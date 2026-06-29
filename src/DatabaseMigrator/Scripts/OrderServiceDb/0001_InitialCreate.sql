-- OrderServiceDb — Initial schema
-- Mirrors the EF Core 'InitialCreate' migration (20260620120851_InitialCreate).

CREATE TABLE [Orders] (
    [Id]          uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [UserId]      uniqueidentifier NOT NULL,
    [UserEmail]   nvarchar(max)    NOT NULL,
    [TotalAmount] decimal(18, 2)   NOT NULL,
    [Status]      nvarchar(max)    NOT NULL,
    [CreatedAt]   datetime2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
);

CREATE TABLE [OrderItems] (
    [Id]          uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [OrderId]     uniqueidentifier NOT NULL,
    [ProductId]   uniqueidentifier NOT NULL,
    [ProductName] nvarchar(max)    NOT NULL,
    [Quantity]    int              NOT NULL,
    [UnitPrice]   decimal(18, 2)   NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId]
        FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

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
    WHERE [MigrationId] = '20260620120851_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260620120851_InitialCreate', '10.0.9');
END;
