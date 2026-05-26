-- ===== LIVE CLASS FEATURE (Phase 4) =====
-- Add LiveClassId column to Lessons table
ALTER TABLE [dbo].[Lessons] ADD [LiveClassId] [int] NULL;
GO

-- Create LiveClasses table
IF OBJECT_ID('dbo.LiveClasses', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.LiveClasses
	(
		Id INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
		CourseId INT NOT NULL,
		LessonId INT NULL,
		Title NVARCHAR(200) NOT NULL,
		Description NVARCHAR(2000) NULL,
		ScheduledDateTime DATETIME2 NOT NULL,
		DurationMinutes INT NULL,
		MaxParticipants INT NULL,
		ZoomMeetingId NVARCHAR(100) NULL,
		JoinUrl NVARCHAR(MAX) NULL,
		StartUrl NVARCHAR(MAX) NULL,
		RecordingUrl NVARCHAR(MAX) NULL,
		RecordingDurationSeconds BIGINT NULL,
		Status NVARCHAR(20) NOT NULL DEFAULT 'Scheduled',
		IsRecordingEnabled BIT NOT NULL DEFAULT 1,
		IsRecordingPublic BIT NOT NULL DEFAULT 1,
		ActualStartTime DATETIME2 NULL,
		ActualEndTime DATETIME2 NULL,
		InstructorId INT NOT NULL,
		CreatedAt DATETIME2 NOT NULL,
		UpdatedAt DATETIME2 NULL,
		CONSTRAINT FK_LiveClasses_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES dbo.Courses(Id) ON DELETE CASCADE,
		CONSTRAINT FK_LiveClasses_AspNetUsers_InstructorId FOREIGN KEY (InstructorId) REFERENCES dbo.AspNetUsers(Id),
		CONSTRAINT FK_LiveClasses_Lessons_LessonId FOREIGN KEY (LessonId) REFERENCES dbo.Lessons(Id) ON DELETE SET NULL
	);
END
GO

-- Create LiveClassAttendances table
IF OBJECT_ID('dbo.LiveClassAttendances', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.LiveClassAttendances
	(
		Id INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
		LiveClassId INT NOT NULL,
		UserId INT NOT NULL,
		JoinedAt DATETIME2 NULL,
		LeftAt DATETIME2 NULL,
		DurationMinutes INT NULL,
		Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
		CreatedAt DATETIME2 NOT NULL,
		UpdatedAt DATETIME2 NULL,
		CONSTRAINT FK_LiveClassAttendances_LiveClasses_LiveClassId FOREIGN KEY (LiveClassId) REFERENCES dbo.LiveClasses(Id) ON DELETE CASCADE,
		CONSTRAINT FK_LiveClassAttendances_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
	);
END
GO

-- Create LiveClassRecordings table
IF OBJECT_ID('dbo.LiveClassRecordings', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.LiveClassRecordings
	(
		Id INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
		LiveClassId INT NOT NULL,
		ExternalRecordingId NVARCHAR(100) NULL,
		RecordingUrl NVARCHAR(MAX) NOT NULL,
		DownloadUrl NVARCHAR(MAX) NULL,
		DurationSeconds BIGINT NULL,
		FileSizeBytes BIGINT NULL,
		Format NVARCHAR(20) NULL,
		IsPublic BIT NOT NULL DEFAULT 1,
		IsDownloadable BIT NOT NULL DEFAULT 0,
		ExpiresAt DATETIME2 NULL,
		Provider NVARCHAR(50) NULL DEFAULT 'Zoom',
		TranscriptUrl NVARCHAR(MAX) NULL,
		ThumbnailUrl NVARCHAR(MAX) NULL,
		Status NVARCHAR(20) NOT NULL DEFAULT 'Processing',
		AvailableAt DATETIME2 NULL,
		CreatedAt DATETIME2 NOT NULL,
		UpdatedAt DATETIME2 NULL,
		CONSTRAINT FK_LiveClassRecordings_LiveClasses_LiveClassId FOREIGN KEY (LiveClassId) REFERENCES dbo.LiveClasses(Id) ON DELETE CASCADE
	);
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Lessons_LiveClassId' AND object_id = OBJECT_ID('dbo.Lessons'))
	CREATE INDEX IX_Lessons_LiveClassId ON dbo.Lessons(LiveClassId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LiveClasses_CourseId' AND object_id = OBJECT_ID('dbo.LiveClasses'))
	CREATE INDEX IX_LiveClasses_CourseId ON dbo.LiveClasses(CourseId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LiveClasses_InstructorId' AND object_id = OBJECT_ID('dbo.LiveClasses'))
	CREATE INDEX IX_LiveClasses_InstructorId ON dbo.LiveClasses(InstructorId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LiveClasses_ScheduledDateTime' AND object_id = OBJECT_ID('dbo.LiveClasses'))
	CREATE INDEX IX_LiveClasses_ScheduledDateTime ON dbo.LiveClasses(ScheduledDateTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LiveClassAttendances_LiveClassId_UserId' AND object_id = OBJECT_ID('dbo.LiveClassAttendances'))
	CREATE UNIQUE INDEX IX_LiveClassAttendances_LiveClassId_UserId ON dbo.LiveClassAttendances(LiveClassId, UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LiveClassRecordings_LiveClassId' AND object_id = OBJECT_ID('dbo.LiveClassRecordings'))
	CREATE INDEX IX_LiveClassRecordings_LiveClassId ON dbo.LiveClassRecordings(LiveClassId);
GO

PRINT 'Live Class Feature tables created successfully!';
