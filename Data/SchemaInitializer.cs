using Microsoft.EntityFrameworkCore;

namespace ELearningWebsite.Data
{
    public static class SchemaInitializer
    {
        public static async Task EnsureMediaFilesTableAsync(ApplicationDbContext dbContext)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.MediaFolders', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MediaFolders](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(120) NOT NULL,
        [ParentFolderId] INT NULL,
        [OwnerUserId] INT NOT NULL,
        [CourseId] INT NULL,
        [Status] NVARCHAR(30) NOT NULL DEFAULT(N'Active'),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_MediaFolders] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

IF OBJECT_ID(N'dbo.MediaFiles', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MediaFiles](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OriginalFileName] NVARCHAR(260) NOT NULL,
        [BlobName] NVARCHAR(400) NOT NULL,
        [BlobPath] NVARCHAR(600) NOT NULL,
        [ContentType] NVARCHAR(120) NOT NULL,
        [SizeBytes] BIGINT NOT NULL,
        [OwnerUserId] INT NOT NULL,
        [CourseId] INT NULL,
        [FolderId] INT NULL,
        [Status] NVARCHAR(30) NOT NULL DEFAULT(N'Active'),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_MediaFiles] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END";

            await dbContext.Database.ExecuteSqlRawAsync(sql);

            const string indexSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaFiles_BlobName' AND object_id = OBJECT_ID(N'dbo.MediaFiles'))
    CREATE UNIQUE INDEX [IX_MediaFiles_BlobName] ON [dbo].[MediaFiles]([BlobName]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaFiles_OwnerUserId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.MediaFiles'))
    CREATE INDEX [IX_MediaFiles_OwnerUserId_CreatedAt] ON [dbo].[MediaFiles]([OwnerUserId], [CreatedAt]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaFiles_CourseId' AND object_id = OBJECT_ID(N'dbo.MediaFiles'))
    CREATE INDEX [IX_MediaFiles_CourseId] ON [dbo].[MediaFiles]([CourseId]);

IF COL_LENGTH('dbo.MediaFiles', 'FolderId') IS NULL
    ALTER TABLE [dbo].[MediaFiles] ADD [FolderId] INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaFiles_FolderId' AND object_id = OBJECT_ID(N'dbo.MediaFiles'))
    CREATE INDEX [IX_MediaFiles_FolderId] ON [dbo].[MediaFiles]([FolderId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaFolders_OwnerUserId_ParentFolderId_Name' AND object_id = OBJECT_ID(N'dbo.MediaFolders'))
    CREATE INDEX [IX_MediaFolders_OwnerUserId_ParentFolderId_Name] ON [dbo].[MediaFolders]([OwnerUserId], [ParentFolderId], [Name]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaFolders_ParentFolderId' AND object_id = OBJECT_ID(N'dbo.MediaFolders'))
    CREATE INDEX [IX_MediaFolders_ParentFolderId] ON [dbo].[MediaFolders]([ParentFolderId]);";

            await dbContext.Database.ExecuteSqlRawAsync(indexSql);
        }
    }
}