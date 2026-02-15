
-- Инициализация БД PostgreSQL для системы учёта ресурсов и задач (DataVault).
-- Создайте БД: CREATE DATABASE DataVaultDb; затем выполните скрипт в ней.

-- Удаление таблиц в порядке зависимостей (дочерние первыми)
DROP TABLE IF EXISTS "Remark" CASCADE;
DROP TABLE IF EXISTS "Verification" CASCADE;
DROP TABLE IF EXISTS "WorkTask" CASCADE;
DROP TABLE IF EXISTS "CategoryItem" CASCADE;
DROP TABLE IF EXISTS "ResourceTransaction" CASCADE;
DROP TABLE IF EXISTS "ResourceBalance" CASCADE;
DROP TABLE IF EXISTS "ActivityLog" CASCADE;
DROP TABLE IF EXISTS "TaskPhase" CASCADE;
DROP TABLE IF EXISTS "Category" CASCADE;
DROP TABLE IF EXISTS "Resource" CASCADE;
DROP TABLE IF EXISTS "Storage" CASCADE;
DROP TABLE IF EXISTS "Vendor" CASCADE;
DROP TABLE IF EXISTS "AppUser" CASCADE;
DROP TABLE IF EXISTS "AppRole" CASCADE;

-- Роли и пользователи
CREATE TABLE "AppRole" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL
);

CREATE TABLE "AppUser" (
    "Id" SERIAL PRIMARY KEY,
    "Login" VARCHAR(100) NOT NULL,
    "PasswordHash" VARCHAR(256) NOT NULL,
    "FullName" VARCHAR(200) NOT NULL,
    "RoleId" INT NOT NULL,
    CONSTRAINT "FK_AppUser_AppRole" FOREIGN KEY ("RoleId") REFERENCES "AppRole"("Id") ON DELETE NO ACTION
);
CREATE UNIQUE INDEX "IX_AppUser_Login" ON "AppUser"("Login");

-- Поставщики и ресурсы
CREATE TABLE "Vendor" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "ContactInfo" TEXT,
    "ReliabilityRating" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "AvgDeliveryDays" INT NOT NULL DEFAULT 0
);

CREATE TABLE "Resource" (
    "Id" SERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "ResourceKind" VARCHAR(50) NOT NULL,
    "Manufacturer" VARCHAR(200) NOT NULL DEFAULT '',
    "SpecsJson" TEXT,
    "UnitOfMeasure" VARCHAR(20) NOT NULL DEFAULT 'шт',
    "MinStock" INT NOT NULL DEFAULT 0,
    "MaxStock" INT NOT NULL DEFAULT 1000,
    "ExpiryDate" TIMESTAMP NULL,
    "ImageUrl" VARCHAR(500),
    "VendorId" INT NULL,
    CONSTRAINT "FK_Resource_Vendor" FOREIGN KEY ("VendorId") REFERENCES "Vendor"("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX "IX_Resource_Code" ON "Resource"("Code");

-- Хранилища и остатки
CREATE TABLE "Storage" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "StorageKind" VARCHAR(50) NOT NULL,
    "Capacity" INT NOT NULL DEFAULT 0,
    "TempRegime" VARCHAR(100)
);

CREATE TABLE "ResourceBalance" (
    "Id" SERIAL PRIMARY KEY,
    "ResourceId" INT NOT NULL,
    "StorageId" INT NOT NULL,
    "Quantity" INT NOT NULL DEFAULT 0,
    CONSTRAINT "FK_ResourceBalance_Resource" FOREIGN KEY ("ResourceId") REFERENCES "Resource"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ResourceBalance_Storage" FOREIGN KEY ("StorageId") REFERENCES "Storage"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_ResourceBalance_Resource_Storage" UNIQUE ("ResourceId", "StorageId")
);

CREATE TABLE "ResourceTransaction" (
    "Id" SERIAL PRIMARY KEY,
    "ResourceId" INT NOT NULL,
    "StorageId" INT NOT NULL,
    "TransactionType" VARCHAR(20) NOT NULL,
    "Quantity" INT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "Comment" VARCHAR(500),
    CONSTRAINT "FK_ResourceTransaction_Resource" FOREIGN KEY ("ResourceId") REFERENCES "Resource"("Id") ON DELETE NO ACTION,
    CONSTRAINT "FK_ResourceTransaction_Storage" FOREIGN KEY ("StorageId") REFERENCES "Storage"("Id") ON DELETE NO ACTION
);

-- Категории и состав
CREATE TABLE "Category" (
    "Id" SERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT
);

CREATE TABLE "CategoryItem" (
    "Id" SERIAL PRIMARY KEY,
    "CategoryId" INT NOT NULL,
    "ResourceId" INT NOT NULL,
    "Quantity" INT NOT NULL CHECK ("Quantity" > 0),
    CONSTRAINT "FK_CategoryItem_Category" FOREIGN KEY ("CategoryId") REFERENCES "Category"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CategoryItem_Resource" FOREIGN KEY ("ResourceId") REFERENCES "Resource"("Id") ON DELETE NO ACTION,
    CONSTRAINT "UQ_CategoryItem_Category_Resource" UNIQUE ("CategoryId", "ResourceId")
);

-- Фазы и задачи
CREATE TABLE "TaskPhase" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL
);

CREATE TABLE "WorkTask" (
    "Id" SERIAL PRIMARY KEY,
    "CategoryId" INT NOT NULL,
    "PhaseId" INT NOT NULL,
    "UserId" INT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "PlannedFinishAt" TIMESTAMP NULL,
    "ActualFinishAt" TIMESTAMP NULL,
    "Quantity" INT NOT NULL DEFAULT 1,
    "EstimatedMinutes" INT NOT NULL DEFAULT 0,
    "UnitCost" DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT "FK_WorkTask_Category" FOREIGN KEY ("CategoryId") REFERENCES "Category"("Id") ON DELETE NO ACTION,
    CONSTRAINT "FK_WorkTask_TaskPhase" FOREIGN KEY ("PhaseId") REFERENCES "TaskPhase"("Id") ON DELETE NO ACTION,
    CONSTRAINT "FK_WorkTask_AppUser" FOREIGN KEY ("UserId") REFERENCES "AppUser"("Id") ON DELETE SET NULL
);

-- Проверки и замечания
CREATE TABLE "Verification" (
    "Id" SERIAL PRIMARY KEY,
    "WorkTaskId" INT NOT NULL,
    "ProcedureName" VARCHAR(200) NOT NULL,
    "ResultValue" TEXT,
    "Passed" BOOLEAN NOT NULL DEFAULT FALSE,
    "VerifiedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "CertificateNumber" VARCHAR(100),
    CONSTRAINT "FK_Verification_WorkTask" FOREIGN KEY ("WorkTaskId") REFERENCES "WorkTask"("Id") ON DELETE CASCADE
);

CREATE TABLE "Remark" (
    "Id" SERIAL PRIMARY KEY,
    "WorkTaskId" INT NOT NULL,
    "RemarkType" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "RecordedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT "FK_Remark_WorkTask" FOREIGN KEY ("WorkTaskId") REFERENCES "WorkTask"("Id") ON DELETE CASCADE
);

-- Журнал активности
CREATE TABLE "ActivityLog" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INT NULL,
    "Action" VARCHAR(100) NOT NULL,
    "Entity" VARCHAR(100),
    "EntityId" INT,
    "Details" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT "FK_ActivityLog_AppUser" FOREIGN KEY ("UserId") REFERENCES "AppUser"("Id") ON DELETE SET NULL
);

-- Индексы
CREATE INDEX "IX_WorkTask_PhaseId" ON "WorkTask"("PhaseId");
CREATE INDEX "IX_WorkTask_CreatedAt" ON "WorkTask"("CreatedAt");
CREATE INDEX "IX_ResourceTransaction_CreatedAt" ON "ResourceTransaction"("CreatedAt");

-- Начальные данные: роли
INSERT INTO "AppRole" ("Id", "Name") VALUES
    (1, 'Администратор'),
    (2, 'Руководитель'),
    (3, 'Специалист'),
    (4, 'Кладовщик');
SELECT setval(pg_get_serial_sequence('"AppRole"', 'Id'), 4);

-- Фазы задач
INSERT INTO "TaskPhase" ("Id", "Name") VALUES
    (1, 'Новый'),
    (2, 'В работе'),
    (3, 'На проверке'),
    (4, 'Приостановлен'),
    (5, 'На доработке'),
    (6, 'Завершён');
SELECT setval(pg_get_serial_sequence('"TaskPhase"', 'Id'), 6);

-- Пользователь admin / password (BCrypt hash)
INSERT INTO "AppUser" ("Id", "Login", "PasswordHash", "FullName", "RoleId") VALUES
    (1, 'admin', '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Администратор', 1);
SELECT setval(pg_get_serial_sequence('"AppUser"', 'Id'), 1);

-- Категория по умолчанию
INSERT INTO "Category" ("Code", "Name", "Description") VALUES
    ('CAT-001', 'Основная категория', 'Пример категории');

-- Хранилище по умолчанию
INSERT INTO "Storage" ("Name", "StorageKind", "Capacity", "TempRegime") VALUES
    ('Основное хранилище', 'main', 10000, NULL);
