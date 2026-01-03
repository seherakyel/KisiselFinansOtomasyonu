
DROP DATABASE IF EXISTS FinansOtomasyonDb;
CREATE DATABASE FinansOtomasyonDb 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_turkish_ci;

USE FinansOtomasyonDb;


CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(100),
    FirstName VARCHAR(50),
    LastName VARCHAR(50),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE,
    
    INDEX idx_username (Username),
    INDEX idx_email (Email)
) ENGINE=InnoDB;

CREATE TABLE AccountTypes (
    Id INT PRIMARY KEY,
    TypeName VARCHAR(50) NOT NULL
) ENGINE=InnoDB;


INSERT INTO AccountTypes (Id, TypeName) VALUES 
    (1, 'Nakit'),
    (2, 'Vadesiz Mevduat'),
    (3, 'Kredi Kartı'),
    (4, 'Yatırım Hesabı');


CREATE TABLE Accounts (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    AccountTypeId INT NOT NULL,
    AccountName VARCHAR(100) NOT NULL,
    CurrencyCode VARCHAR(3) DEFAULT 'TRY',
    InitialBalance DECIMAL(18,2) DEFAULT 0.00,
    CurrentBalance DECIMAL(18,2) DEFAULT 0.00,
    CreditLimit DECIMAL(18,2) DEFAULT 0.00,
    CutoffDay INT DEFAULT 0,
    IsActive BOOLEAN DEFAULT TRUE,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (AccountTypeId) REFERENCES AccountTypes(Id) ON DELETE RESTRICT,
    INDEX idx_user_accounts (UserId)
) ENGINE=InnoDB;


CREATE TABLE Categories (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NULL,
    ParentId INT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    Type TINYINT NOT NULL COMMENT '1: Gelir, 2: Gider',
    IconIndex INT DEFAULT 0,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    FOREIGN KEY (ParentId) REFERENCES Categories(Id) ON DELETE SET NULL,
    INDEX idx_user_categories (UserId),
    INDEX idx_category_type (Type)
) ENGINE=InnoDB;

INSERT INTO Categories (UserId, ParentId, CategoryName, Type, IconIndex) VALUES
    (NULL, NULL, 'Maaş', 1, 1),
    (NULL, NULL, 'Ek Gelir', 1, 2),
    (NULL, NULL, 'Yatırım Getirisi', 1, 3),
    (NULL, NULL, 'Hediye/Bağış', 1, 4),
    (NULL, NULL, 'Market/Gıda', 2, 10),
    (NULL, NULL, 'Ulaşım', 2, 11),
    (NULL, NULL, 'Faturalar', 2, 12),
    (NULL, NULL, 'Kira', 2, 13),
    (NULL, NULL, 'Sağlık', 2, 14),
    (NULL, NULL, 'Eğitim', 2, 15),
    (NULL, NULL, 'Eğlence', 2, 16),
    (NULL, NULL, 'Giyim', 2, 17),
    (NULL, NULL, 'Restoran/Kafe', 2, 18),
    (NULL, NULL, 'Diğer', 2, 99);


CREATE TABLE Transactions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    AccountId INT NOT NULL,
    CategoryId INT NULL,
    RelatedTransactionId INT NULL,
    TransactionDate DATETIME NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionType TINYINT NOT NULL COMMENT '1: Gelir, 2: Gider, 3: Transfer',
    Description VARCHAR(255),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE RESTRICT,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL,
    FOREIGN KEY (RelatedTransactionId) REFERENCES Transactions(Id) ON DELETE SET NULL,
    INDEX idx_account_transactions (AccountId),
    INDEX idx_transaction_date (TransactionDate),
    INDEX idx_transaction_type (TransactionType)
) ENGINE=InnoDB;


CREATE TABLE ScheduledTransactions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    AccountId INT NOT NULL,
    CategoryId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionType TINYINT NOT NULL COMMENT '1: Gelir, 2: Gider',
    Description VARCHAR(255),
    FrequencyType VARCHAR(20) NOT NULL COMMENT 'Daily, Weekly, Monthly, Yearly',
    DayOfMonth INT NULL,
    NextExecutionDate DATE NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE RESTRICT,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT,
    INDEX idx_user_scheduled (UserId),
    INDEX idx_next_execution (NextExecutionDate)
) ENGINE=InnoDB;


CREATE TABLE Budgets (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    CategoryId INT NOT NULL,
    AmountLimit DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT,
    INDEX idx_user_budgets (UserId),
    INDEX idx_budget_dates (StartDate, EndDate)
) ENGINE=InnoDB;


INSERT INTO Users (Username, PasswordHash, Email, FirstName, LastName) VALUES 
    ('demo', 'A6xnQhbz4Vx2HuGl4lXwZ5U2I8iziLRFnhP5eNfIRvQ=', 'demo@example.com', 'Demo', 'Kullanıcı');

INSERT INTO Accounts (UserId, AccountTypeId, AccountName, CurrencyCode, InitialBalance, CurrentBalance) VALUES
    (1, 1, 'Cüzdan', 'TRY', 500.00, 500.00),
    (1, 2, 'Ziraat Bankası', 'TRY', 5000.00, 5000.00),
    (1, 3, 'Kredi Kartı', 'TRY', 0.00, -1500.00);

SELECT 'Veritabanı başarıyla oluşturuldu!' AS Sonuc;
SELECT CONCAT('Toplam ', COUNT(*), ' kategori eklendi.') AS Kategoriler FROM Categories;
SELECT CONCAT('Test kullanıcısı: demo / 1234') AS TestKullanici;

