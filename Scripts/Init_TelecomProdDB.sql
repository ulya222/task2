-- Инициализация БД PostgreSQL для системы управления производственным циклом узлов связи.
-- Создайте базу telecomprod_db в pgAdmin, затем выполните скрипт.
DROP TABLE IF EXISTS "DefectRecord" CASCADE;
DROP TABLE IF EXISTS "QualityTest" CASCADE;
DROP TABLE IF EXISTS "ProductionOrder" CASCADE;
DROP TABLE IF EXISTS "BomItem" CASCADE;
DROP TABLE IF EXISTS "AssemblyUnit" CASCADE;
DROP TABLE IF EXISTS "StockMovement" CASCADE;
DROP TABLE IF EXISTS "StockBalance" CASCADE;
DROP TABLE IF EXISTS "Component" CASCADE;
DROP TABLE IF EXISTS "Warehouse" CASCADE;
DROP TABLE IF EXISTS "Supplier" CASCADE;
DROP TABLE IF EXISTS "OrderStatus" CASCADE;
DROP TABLE IF EXISTS "AuditLog" CASCADE;
DROP TABLE IF EXISTS "User" CASCADE;
DROP TABLE IF EXISTS "Role" CASCADE;

CREATE TABLE "Role" ("Id" SERIAL PRIMARY KEY, "Name" VARCHAR(50) NOT NULL UNIQUE);
CREATE TABLE "User" ("Id" SERIAL PRIMARY KEY, "Login" VARCHAR(100) NOT NULL UNIQUE, "PasswordHash" VARCHAR(256) NOT NULL, "FullName" VARCHAR(200) NOT NULL, "RoleId" INT NOT NULL REFERENCES "Role"("Id") ON DELETE RESTRICT);
CREATE TABLE "Supplier" ("Id" SERIAL PRIMARY KEY, "Name" VARCHAR(200) NOT NULL, "ContactInfo" TEXT, "ReliabilityRating" DECIMAL(5,2) NOT NULL DEFAULT 0, "AvgDeliveryDays" INT NOT NULL DEFAULT 0);
CREATE TABLE "Warehouse" ("Id" SERIAL PRIMARY KEY, "Name" VARCHAR(100) NOT NULL, "WarehouseType" VARCHAR(50) NOT NULL, "Capacity" INT NOT NULL DEFAULT 0, "TempRegime" VARCHAR(50));
CREATE TABLE "Component" (
    "Id" SERIAL PRIMARY KEY, "Code" VARCHAR(20) NOT NULL UNIQUE, "Name" VARCHAR(200) NOT NULL,
    "ComponentType" VARCHAR(50) NOT NULL, "Manufacturer" VARCHAR(200) NOT NULL, "TechSpecsJson" TEXT,
    "UnitOfMeasure" VARCHAR(20) NOT NULL DEFAULT 'шт', "MinStock" INT NOT NULL DEFAULT 0, "MaxStock" INT NOT NULL DEFAULT 1000,
    "ExpiryDate" TIMESTAMP, "ImageUrl" VARCHAR(500), "SupplierId" INT REFERENCES "Supplier"("Id") ON DELETE SET NULL
);
CREATE TABLE "StockBalance" ("Id" SERIAL PRIMARY KEY, "ComponentId" INT NOT NULL REFERENCES "Component"("Id") ON DELETE CASCADE, "WarehouseId" INT NOT NULL REFERENCES "Warehouse"("Id") ON DELETE CASCADE, "Quantity" INT NOT NULL DEFAULT 0, UNIQUE ("ComponentId", "WarehouseId"));
CREATE TABLE "StockMovement" ("Id" SERIAL PRIMARY KEY, "ComponentId" INT NOT NULL REFERENCES "Component"("Id") ON DELETE RESTRICT, "WarehouseId" INT NOT NULL REFERENCES "Warehouse"("Id") ON DELETE RESTRICT, "MovementType" VARCHAR(20) NOT NULL, "Quantity" INT NOT NULL, "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, "Comment" TEXT);
CREATE TABLE "AssemblyUnit" ("Id" SERIAL PRIMARY KEY, "Code" VARCHAR(50) NOT NULL, "Name" VARCHAR(200) NOT NULL, "Description" TEXT);
CREATE TABLE "BomItem" ("Id" SERIAL PRIMARY KEY, "AssemblyUnitId" INT NOT NULL REFERENCES "AssemblyUnit"("Id") ON DELETE CASCADE, "ComponentId" INT NOT NULL REFERENCES "Component"("Id") ON DELETE RESTRICT, "Quantity" INT NOT NULL CHECK ("Quantity" > 0), UNIQUE ("AssemblyUnitId", "ComponentId"));
CREATE TABLE "OrderStatus" ("Id" SERIAL PRIMARY KEY, "Name" VARCHAR(50) NOT NULL UNIQUE);
CREATE TABLE "ProductionOrder" (
    "Id" SERIAL PRIMARY KEY, "AssemblyUnitId" INT NOT NULL REFERENCES "AssemblyUnit"("Id") ON DELETE RESTRICT, "StatusId" INT NOT NULL REFERENCES "OrderStatus"("Id") ON DELETE RESTRICT,
    "UserId" INT REFERENCES "User"("Id") ON DELETE SET NULL, "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, "PlannedFinishAt" TIMESTAMP, "ActualFinishAt" TIMESTAMP,
    "Quantity" INT NOT NULL DEFAULT 1, "AssemblyTimeMinutes" INT NOT NULL DEFAULT 0, "UnitCost" DECIMAL(18,2) NOT NULL DEFAULT 0
);
CREATE TABLE "QualityTest" ("Id" SERIAL PRIMARY KEY, "ProductionOrderId" INT NOT NULL REFERENCES "ProductionOrder"("Id") ON DELETE CASCADE, "TestProcedure" VARCHAR(200) NOT NULL, "MeasurementResult" TEXT, "Passed" BOOLEAN NOT NULL DEFAULT false, "TestedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, "CertificateNumber" VARCHAR(100));
CREATE TABLE "DefectRecord" ("Id" SERIAL PRIMARY KEY, "ProductionOrderId" INT NOT NULL REFERENCES "ProductionOrder"("Id") ON DELETE CASCADE, "DefectType" VARCHAR(100) NOT NULL, "Description" TEXT, "RecordedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE "AuditLog" ("Id" SERIAL PRIMARY KEY, "UserId" INT REFERENCES "User"("Id") ON DELETE SET NULL, "Action" VARCHAR(100) NOT NULL, "Entity" VARCHAR(100), "EntityId" INT, "Details" TEXT, "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);

CREATE INDEX IF NOT EXISTS "IX_Component_Code" ON "Component"("Code");
CREATE INDEX IF NOT EXISTS "IX_Component_Name" ON "Component"("Name");
CREATE INDEX IF NOT EXISTS "IX_User_Login" ON "User"("Login");
CREATE INDEX IF NOT EXISTS "IX_ProductionOrder_StatusId" ON "ProductionOrder"("StatusId");
CREATE INDEX IF NOT EXISTS "IX_ProductionOrder_CreatedAt" ON "ProductionOrder"("CreatedAt");

-- Роли: Администратор, Начальник производства, Технолог, Кладовщик, Сборщик
INSERT INTO "Role" ("Name") VALUES ('Администратор'), ('Начальник производства'), ('Технолог'), ('Кладовщик'), ('Сборщик');

-- Статусы производственных заказов
INSERT INTO "OrderStatus" ("Name") VALUES ('Новый'), ('В планировании'), ('В производстве'), ('Сборка'), ('Тестирование'), ('Готов'), ('Брак');

-- Пользователи: логин admin, chief, tech, store, assembler / пароль по умолчанию password
-- Хеш BCrypt для "password": $2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW
INSERT INTO "User" ("Login", "PasswordHash", "FullName", "RoleId") VALUES
    ('admin', '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Администратор системы', 1),
    ('chief', '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Иванов П.С. Начальник производства', 2),
    ('tech', '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Петрова А.В. Технолог', 3),
    ('store', '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Сидоров К.Д. Кладовщик', 4),
    ('assembler', '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Козлов М.И. Сборщик', 5);

-- Поставщики
INSERT INTO "Supplier" ("Name", "ContactInfo", "ReliabilityRating", "AvgDeliveryDays") VALUES
    ('ТелекомСнаб ООО', 'тел. +7(495)111-22-33, email: supply@tcom.ru', 4.8, 7),
    ('Электроника Плюс', 'тел. +7(495)222-33-44, email: order@elplus.ru', 4.5, 14),
    ('Микрокомпоненты РФ', 'тел. +7(812)333-44-55', 4.2, 21),
    ('ОптикаТех', 'тел. +7(495)444-55-66', 4.9, 5),
    ('СвязьКомплект', 'тел. +7(495)555-66-77', 4.6, 10),
    ('РадиоМаркет', 'тел. +7(495)666-77-88', 4.0, 28),
    ('ЧипСервис', 'тел. +7(812)777-88-99', 4.7, 12);

-- Склады
INSERT INTO "Warehouse" ("Name", "WarehouseType", "Capacity", "TempRegime") VALUES
    ('Склад основной', 'main', 10000, NULL),
    ('Склад производственный', 'production', 5000, '+18..+25°C'),
    ('Склад электроники', 'main', 2000, '+15..+25°C, влажность 40-60%'),
    ('Брак', 'reject', 500, NULL);

-- 50+ компонентов для телекоммуникационного оборудования
INSERT INTO "Component" ("Code", "Name", "ComponentType", "Manufacturer", "TechSpecsJson", "UnitOfMeasure", "MinStock", "MaxStock", "SupplierId") VALUES
    ('ELEC-00001', 'Модуль WiFi 6E', 'active,electronic', 'Intel', '{"standard":"802.11ax","bands":"2.4/5/6GHz"}', 'шт', 20, 200, 1),
    ('ELEC-00002', 'Оптический трансивер SFP+', 'active,electronic', 'Cisco', '{"type":"10G-SR","wavelength":"850nm"}', 'шт', 50, 500, 4),
    ('ELEC-00003', 'Маршрутизатор SoC', 'active,electronic', 'Broadcom', '{"cores":4,"freq":"2.0GHz"}', 'шт', 30, 150, 2),
    ('ELEC-00004', 'PHY чип 1GbE', 'active,electronic', 'Marvell', '{"ports":4,"interface":"RGMII"}', 'шт', 100, 1000, 2),
    ('ELEC-00005', 'DDR4 модуль 8GB', 'passive,electronic', 'Samsung', '{"speed":"3200MHz","form":"SODIMM"}', 'шт', 40, 300, 1),
    ('ELEC-00006', 'eMMC 32GB', 'passive,electronic', 'Kingston', '{"interface":"eMMC5.1","speed":"400MB/s"}', 'шт', 60, 500, 2),
    ('ELEC-00007', 'Кварцевый резонатор 25MHz', 'passive,electronic', 'Abracon', '{"freq":"25MHz","tolerance":"50ppm"}', 'шт', 500, 5000, 6),
    ('ELEC-00008', 'Конденсатор керамический 100nF', 'passive,electronic', 'Murata', '{"voltage":"50V","package":"0805"}', 'шт', 5000, 50000, 3),
    ('ELEC-00009', 'Резистор 10k 0805', 'passive,electronic', 'Vishay', '{"tolerance":"1%","power":"125mW"}', 'шт', 10000, 100000, 3),
    ('ELEC-00010', 'Коннектор RJ45', 'passive,mechanical', 'Molex', '{"ports":1,"poe":true}', 'шт', 200, 2000, 1),
    ('ELEC-00011', 'Разъём SFP+ cage', 'passive,mechanical', 'Amphenol', '{"type":"SFP+","lanes":1}', 'шт', 100, 1000, 4),
    ('ELEC-00012', 'Разъём питания DC 5.5x2.1', 'passive,mechanical', 'CUI', '{"voltage":"12V","current":"2A"}', 'шт', 300, 3000, 5),
    ('ELEC-00013', 'Разъём USB-C', 'passive,mechanical', 'TE Connectivity', '{"version":"3.2","data":true}', 'шт', 150, 1500, 1),
    ('ELEC-00014', 'Светодиод индикации 3мм', 'passive,electronic', 'Kingbright', '{"color":"green","forward":"2V"}', 'шт', 1000, 10000, 6),
    ('ELEC-00015', 'Предохранитель 2A', 'passive,electronic', 'Littelfuse', '{"rating":"2A","voltage":"250V"}', 'шт', 500, 5000, 5),
    ('ELEC-00016', 'Дроссель 10µH', 'passive,electronic', 'Coilcraft', '{"current":"3A","DCR":"20mOhm"}', 'шт', 200, 2000, 2),
    ('ELEC-00017', 'Диод Шоттки SS34', 'passive,electronic', 'Diodes Inc', '{"voltage":"40V","current":"3A"}', 'шт', 1000, 10000, 3),
    ('ELEC-00018', 'Транзистор MOSFET', 'active,electronic', 'Infineon', '{"Vds":"60V","Rds":"5mOhm"}', 'шт', 300, 3000, 2),
    ('ELEC-00019', 'Стабилизатор 3.3V LDO', 'active,electronic', 'Texas Instruments', '{"current":"1A","dropout":"300mV"}', 'шт', 400, 4000, 2),
    ('ELEC-00020', 'DC-DC конвертер 12V->5V', 'active,electronic', 'Analog Devices', '{"efficiency":"95%","current":"3A"}', 'шт', 100, 1000, 1),
    ('ELEC-00021', 'Оптический кабель LC-LC 1м', 'passive,mechanical', 'CommScope', '{"type":"OM4","length":"1m"}', 'шт', 200, 2000, 4),
    ('ELEC-00022', 'Кабель Ethernet Cat6 1м', 'passive,mechanical', 'Belden', '{"category":6,"length":"1m"}', 'шт', 500, 5000, 5),
    ('ELEC-00023', 'Плата печатная 4 слоя', 'passive,mechanical', 'JLCPCB', '{"layers":4,"thickness":"1.6mm"}', 'шт', 50, 500, 7),
    ('ELEC-00024', 'Радиатор алюминиевый', 'passive,mechanical', 'Thermaltake', '{"thermal":"2.5W/K","height":"15mm"}', 'шт', 100, 1000, 1),
    ('ELEC-00025', 'Винт M3x6', 'passive,mechanical', 'ГОСТ', '{"thread":"M3","length":"6mm"}', 'шт', 5000, 50000, 5),
    ('ELEC-00026', 'Корпус металлический 1U', 'passive,mechanical', 'Schroff', '{"height":"1U","depth":"300mm"}', 'шт', 20, 200, 1),
    ('ELEC-00027', 'Блок питания 48V 2A', 'active,electronic', 'Mean Well', '{"output":"48V","power":"96W"}', 'шт', 30, 300, 5),
    ('ELEC-00028', 'Дисплей LCD 2.8"', 'active,electronic', 'Waveshare', '{"resolution":"240x320","interface":"SPI"}', 'шт', 20, 200, 2),
    ('ELEC-00029', 'Кнопка тактовая 6x6', 'passive,mechanical', 'E-Switch', '{"force":"160g","life":"100k"}', 'шт', 500, 5000, 6),
    ('ELEC-00030', 'Антенна WiFi 5dBi', 'passive,electronic', 'Taoglas', '{"gain":"5dBi","connector":"RP-SMA"}', 'шт', 100, 1000, 1),
    ('ELEC-00031', 'Патч-корд RJ45 0.5м', 'passive,mechanical', 'Hirose', '{"category":"6A","length":"0.5m"}', 'шт', 300, 3000, 5),
    ('ELEC-00032', 'Фильтр EMI', 'passive,electronic', 'TDK', '{"freq":"1MHz","current":"6A"}', 'шт', 150, 1500, 2),
    ('ELEC-00033', 'Термоинтерфейс', 'passive,mechanical', 'Arctic', '{"conductivity":"8.5W/mK"}', 'шт', 100, 1000, 1),
    ('ELEC-00034', 'Стойка 19" 1U', 'passive,mechanical', 'Rittal', '{"height":"1U","width":"19inch"}', 'шт', 10, 100, 1),
    ('ELEC-00035', 'Модуль управления питанием', 'active,electronic', 'Microchip', '{"channels":4,"i2c":true}', 'шт', 40, 400, 2),
    ('ELEC-00036', 'EEPROM 256KB', 'passive,electronic', 'Microchip', '{"interface":"I2C","address":"0x50"}', 'шт', 200, 2000, 2),
    ('ELEC-00037', 'Оптрон 4N35', 'passive,electronic', 'Vishay', '{"ctr":"100%","isolation":"5kV"}', 'шт', 300, 3000, 3),
    ('ELEC-00038', 'Индуктивность 22µH', 'passive,electronic', 'Würth', '{"current":"5A","saturation":"6A"}', 'шт', 250, 2500, 2),
    ('ELEC-00039', 'Кварц 40MHz', 'passive,electronic', 'Fox', '{"freq":"40MHz","package":"HC49"}', 'шт', 400, 4000, 6),
    ('ELEC-00040', 'Реле 5V 10A', 'active,electronic', 'Omron', '{"voltage":"5V","contacts":"1 Form C"}', 'шт', 80, 800, 5),
    ('ELEC-00041', 'Плата шасси', 'passive,mechanical', 'Локальное', '{"material":"aluminum","thickness":"2mm"}', 'шт', 30, 300, 1),
    ('ELEC-00042', 'Корпусная стойка', 'passive,mechanical', 'Legrand', '{"height":"42U","depth":"1000mm"}', 'шт', 5, 50, 1),
    ('ELEC-00043', 'Модуль PoE 48V', 'active,electronic', 'Linear Tech', '{"ports":4,"power":"30W"}', 'шт', 25, 250, 2),
    ('ELEC-00044', 'Flash 128MB SPI', 'passive,electronic', 'Winbond', '{"size":"128Mb","interface":"SPI"}', 'шт', 100, 1000, 2),
    ('ELEC-00045', 'Коннектор HDMI', 'passive,mechanical', 'Hirose', '{"version":"2.0","type":"A"}', 'шт', 50, 500, 1),
    ('ELEC-00046', 'Кабель питания C13', 'passive,mechanical', 'Schurter', '{"length":"1.8m","rating":"10A"}', 'шт', 200, 2000, 5),
    ('ELEC-00047', 'Клеммник 2 позиции', 'passive,mechanical', 'Phoenix', '{"pitch":"5.08mm","current":"12A"}', 'шт', 400, 4000, 5),
    ('ELEC-00048', 'Крышка корпуса', 'passive,mechanical', 'Локальное', '{"material":"steel","finish":"powder"}', 'шт', 50, 500, 1),
    ('ELEC-00049', 'Этикетка самоклеящаяся', 'passive,mechanical', 'Brady', '{"size":"50x25mm","material":"polyester"}', 'шт', 1000, 10000, 5),
    ('ELEC-00050', 'Уплотнитель EPDM', 'passive,mechanical', 'Silicone', '{"thickness":"2mm","width":"5mm"}', 'м', 100, 1000, 1);

-- Узлы сборки (AssemblyUnit)
INSERT INTO "AssemblyUnit" ("Code", "Name", "Description") VALUES
    ('UZL-001', 'Узел связи базовый', 'Базовый узел связи 1U с 4 портами Ethernet'),
    ('UZL-002', 'Узел связи расширенный', 'Расширенный узел с WiFi и SFP'),
    ('UZL-003', 'Шлюз телематики', 'Компактный шлюз для IoT');

-- BOM для UZL-001 (узла связи базового)
INSERT INTO "BomItem" ("AssemblyUnitId", "ComponentId", "Quantity")
SELECT 1, c."Id", CASE c."Code" WHEN 'ELEC-00003' THEN 1 WHEN 'ELEC-00004' THEN 4 WHEN 'ELEC-00005' THEN 1 WHEN 'ELEC-00006' THEN 1 WHEN 'ELEC-00007' THEN 2 WHEN 'ELEC-00008' THEN 50 WHEN 'ELEC-00010' THEN 4 WHEN 'ELEC-00019' THEN 2 WHEN 'ELEC-00023' THEN 1 WHEN 'ELEC-00026' THEN 1 WHEN 'ELEC-00027' THEN 1 ELSE 1 END
FROM "Component" c WHERE c."Code" IN ('ELEC-00003','ELEC-00004','ELEC-00005','ELEC-00006','ELEC-00007','ELEC-00008','ELEC-00010','ELEC-00019','ELEC-00023','ELEC-00026','ELEC-00027');

-- Начальные остатки на складе (основной склад)
INSERT INTO "StockBalance" ("ComponentId", "WarehouseId", "Quantity")
SELECT c."Id", 1, GREATEST(c."MinStock", c."MaxStock" / 2) FROM "Component" c;
