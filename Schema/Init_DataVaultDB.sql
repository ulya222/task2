-- Инициализация БД SQL Server для системы учёта ресурсов и задач (DataVault).
-- Выполняйте при подключении к экземпляру SQL Server или LocalDB (master не обязателен для LocalDB).

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'DataVaultDb')
    CREATE DATABASE DataVaultDb;
GO

USE DataVaultDb;
GO

-- Удаление таблиц в порядке зависимостей (дочерние первыми)
IF OBJECT_ID(N'dbo.Remark', N'U') IS NOT NULL DROP TABLE dbo.Remark;
IF OBJECT_ID(N'dbo.Verification', N'U') IS NOT NULL DROP TABLE dbo.Verification;
IF OBJECT_ID(N'dbo.WorkTask', N'U') IS NOT NULL DROP TABLE dbo.WorkTask;
IF OBJECT_ID(N'dbo.CategoryItem', N'U') IS NOT NULL DROP TABLE dbo.CategoryItem;
IF OBJECT_ID(N'dbo.ResourceTransaction', N'U') IS NOT NULL DROP TABLE dbo.ResourceTransaction;
IF OBJECT_ID(N'dbo.ResourceBalance', N'U') IS NOT NULL DROP TABLE dbo.ResourceBalance;
IF OBJECT_ID(N'dbo.ActivityLog', N'U') IS NOT NULL DROP TABLE dbo.ActivityLog;
IF OBJECT_ID(N'dbo.TaskPhase', N'U') IS NOT NULL DROP TABLE dbo.TaskPhase;
IF OBJECT_ID(N'dbo.Category', N'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID(N'dbo.Resource', N'U') IS NOT NULL DROP TABLE dbo.Resource;
IF OBJECT_ID(N'dbo.Storage', N'U') IS NOT NULL DROP TABLE dbo.Storage;
IF OBJECT_ID(N'dbo.Vendor', N'U') IS NOT NULL DROP TABLE dbo.Vendor;
IF OBJECT_ID(N'dbo.AppUser', N'U') IS NOT NULL DROP TABLE dbo.AppUser;
IF OBJECT_ID(N'dbo.AppRole', N'U') IS NOT NULL DROP TABLE dbo.AppRole;
GO

-- Роли и пользователи
CREATE TABLE dbo.AppRole (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE dbo.AppUser (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Login NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT FK_AppUser_AppRole FOREIGN KEY (RoleId) REFERENCES dbo.AppRole(Id) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX IX_AppUser_Login ON dbo.AppUser(Login);

-- Поставщики и ресурсы
CREATE TABLE dbo.Vendor (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    ContactInfo NVARCHAR(MAX),
    ReliabilityRating DECIMAL(5,2) NOT NULL DEFAULT 0,
    AvgDeliveryDays INT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Resource (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    ResourceKind NVARCHAR(50) NOT NULL,
    Manufacturer NVARCHAR(200) NOT NULL DEFAULT '',
    SpecsJson NVARCHAR(MAX),
    UnitOfMeasure NVARCHAR(20) NOT NULL DEFAULT N'шт',
    MinStock INT NOT NULL DEFAULT 0,
    MaxStock INT NOT NULL DEFAULT 1000,
    ExpiryDate DATETIME2 NULL,
    ImageUrl NVARCHAR(500),
    VendorId INT NULL,
    CONSTRAINT FK_Resource_Vendor FOREIGN KEY (VendorId) REFERENCES dbo.Vendor(Id) ON DELETE SET NULL
);
CREATE UNIQUE INDEX IX_Resource_Code ON dbo.Resource(Code);

-- Хранилища и остатки
CREATE TABLE dbo.Storage (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    StorageKind NVARCHAR(50) NOT NULL,
    Capacity INT NOT NULL DEFAULT 0,
    TempRegime NVARCHAR(100)
);

CREATE TABLE dbo.ResourceBalance (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ResourceId INT NOT NULL,
    StorageId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ResourceBalance_Resource FOREIGN KEY (ResourceId) REFERENCES dbo.Resource(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ResourceBalance_Storage FOREIGN KEY (StorageId) REFERENCES dbo.Storage(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_ResourceBalance_Resource_Storage UNIQUE (ResourceId, StorageId)
);

CREATE TABLE dbo.ResourceTransaction (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ResourceId INT NOT NULL,
    StorageId INT NOT NULL,
    TransactionType NVARCHAR(20) NOT NULL,
    Quantity INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Comment NVARCHAR(500),
    CONSTRAINT FK_ResourceTransaction_Resource FOREIGN KEY (ResourceId) REFERENCES dbo.Resource(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_ResourceTransaction_Storage FOREIGN KEY (StorageId) REFERENCES dbo.Storage(Id) ON DELETE NO ACTION
);

-- Категории и состав
CREATE TABLE dbo.Category (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX)
);

CREATE TABLE dbo.CategoryItem (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    ResourceId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    CONSTRAINT FK_CategoryItem_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CategoryItem_Resource FOREIGN KEY (ResourceId) REFERENCES dbo.Resource(Id) ON DELETE NO ACTION,
    CONSTRAINT UQ_CategoryItem_Category_Resource UNIQUE (CategoryId, ResourceId)
);

-- Фазы и задачи
CREATE TABLE dbo.TaskPhase (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE dbo.WorkTask (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    PhaseId INT NOT NULL,
    UserId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PlannedFinishAt DATETIME2 NULL,
    ActualFinishAt DATETIME2 NULL,
    Quantity INT NOT NULL DEFAULT 1,
    EstimatedMinutes INT NOT NULL DEFAULT 0,
    UnitCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_WorkTask_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_WorkTask_TaskPhase FOREIGN KEY (PhaseId) REFERENCES dbo.TaskPhase(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_WorkTask_AppUser FOREIGN KEY (UserId) REFERENCES dbo.AppUser(Id) ON DELETE SET NULL
);

-- Проверки и замечания
CREATE TABLE dbo.Verification (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    WorkTaskId INT NOT NULL,
    ProcedureName NVARCHAR(200) NOT NULL,
    ResultValue NVARCHAR(MAX),
    Passed BIT NOT NULL DEFAULT 0,
    VerifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CertificateNumber NVARCHAR(100),
    CONSTRAINT FK_Verification_WorkTask FOREIGN KEY (WorkTaskId) REFERENCES dbo.WorkTask(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.Remark (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    WorkTaskId INT NOT NULL,
    RemarkType NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Remark_WorkTask FOREIGN KEY (WorkTaskId) REFERENCES dbo.WorkTask(Id) ON DELETE CASCADE
);

-- Журнал активности
CREATE TABLE dbo.ActivityLog (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    Entity NVARCHAR(100),
    EntityId INT,
    Details NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ActivityLog_AppUser FOREIGN KEY (UserId) REFERENCES dbo.AppUser(Id) ON DELETE SET NULL
);

-- Индексы
CREATE INDEX IX_WorkTask_PhaseId ON dbo.WorkTask(PhaseId);
CREATE INDEX IX_WorkTask_CreatedAt ON dbo.WorkTask(CreatedAt);
CREATE INDEX IX_ResourceTransaction_CreatedAt ON dbo.ResourceTransaction(CreatedAt);
GO

-- Начальные данные: роли
SET IDENTITY_INSERT dbo.AppRole ON;
INSERT INTO dbo.AppRole (Id, Name) VALUES
    (1, N'Администратор'),
    (2, N'Руководитель'),
    (3, N'Специалист'),
    (4, N'Кладовщик');
SET IDENTITY_INSERT dbo.AppRole OFF;

-- Фазы задач
SET IDENTITY_INSERT dbo.TaskPhase ON;
INSERT INTO dbo.TaskPhase (Id, Name) VALUES
    (1, N'Новый'),
    (2, N'В работе'),
    (3, N'На проверке'),
    (4, N'Приостановлен'),
    (5, N'На доработке'),
    (6, N'Завершён');
SET IDENTITY_INSERT dbo.TaskPhase OFF;

-- Пользователь admin / password (BCrypt hash)
SET IDENTITY_INSERT dbo.AppUser ON;
INSERT INTO dbo.AppUser (Id, Login, PasswordHash, FullName, RoleId) VALUES
    (1, N'admin', N'$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', N'Администратор', 1);
SET IDENTITY_INSERT dbo.AppUser OFF;

-- Категория по умолчанию
INSERT INTO dbo.Category (Code, Name, Description) VALUES
    (N'CAT-001', N'Основная категория', N'Пример категории');

-- Хранилище по умолчанию
INSERT INTO dbo.Storage (Name, StorageKind, Capacity, TempRegime) VALUES
    (N'Основное хранилище', N'main', 10000, NULL);

GO
