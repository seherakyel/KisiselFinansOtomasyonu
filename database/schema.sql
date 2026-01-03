-- =====================================================
-- KİŞİSEL FİNANS OTOMASYON - VERİTABANI ŞEMASI
-- Versiyon: 3.0 (Trigger, Stored Procedure, Audit Log)
-- =====================================================

DROP DATABASE IF EXISTS FinansOtomasyonDb;
CREATE DATABASE FinansOtomasyonDb 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_turkish_ci;

USE FinansOtomasyonDb;

-- =====================================================
-- 1. KULLANICILAR TABLOSU
-- =====================================================
CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(100),
    FirstName VARCHAR(50),
    LastName VARCHAR(50),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE,
    
    INDEX idx_username (Username)
) ENGINE=InnoDB;

-- =====================================================
-- 2. HESAP TÜRLERİ
-- =====================================================
CREATE TABLE AccountTypes (
    Id INT PRIMARY KEY,
    TypeName VARCHAR(50) NOT NULL
) ENGINE=InnoDB;

INSERT INTO AccountTypes (Id, TypeName) VALUES 
    (1, 'Nakit'),
    (2, 'Vadesiz Mevduat'),
    (3, 'Kredi Kartı'),
    (4, 'Yatırım Hesabı');

-- =====================================================
-- 3. HESAPLAR TABLOSU
-- =====================================================
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

-- =====================================================
-- 4. KATEGORİLER
-- =====================================================
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

-- Varsayılan kategoriler
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

-- =====================================================
-- 5. İŞLEMLER (TRANSACTIONS)
-- =====================================================
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

-- =====================================================
-- 6. PLANLANMIŞ İŞLEMLER
-- =====================================================
CREATE TABLE ScheduledTransactions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    AccountId INT NOT NULL,
    CategoryId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Description VARCHAR(255),
    FrequencyType VARCHAR(20) NOT NULL,
    DayOfMonth INT NULL,
    NextExecutionDate DATE NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE RESTRICT,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT,
    INDEX idx_user_scheduled (UserId),
    INDEX idx_next_execution (NextExecutionDate)
) ENGINE=InnoDB;

-- =====================================================
-- 7. BÜTÇE HEDEFLERİ
-- =====================================================
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

-- =====================================================
-- 8. AUDIT LOG - TÜM DEĞİŞİKLİKLERİN KAYDI ⭐
-- =====================================================
CREATE TABLE AuditLogs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NULL,
    TableName VARCHAR(50) NOT NULL,
    RecordId INT NOT NULL,
    Action VARCHAR(20) NOT NULL COMMENT 'INSERT, UPDATE, DELETE',
    OldValues JSON NULL,
    NewValues JSON NULL,
    IpAddress VARCHAR(45) NULL,
    UserAgent VARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    INDEX idx_audit_table (TableName),
    INDEX idx_audit_date (CreatedAt),
    INDEX idx_audit_user (UserId)
) ENGINE=InnoDB;

-- =====================================================
-- 9. FİNANSAL SAĞLIK SKORU GEÇMİŞİ ⭐
-- =====================================================
CREATE TABLE FinancialHealthHistory (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Score INT NOT NULL COMMENT '0-100 arası skor',
    IncomeExpenseRatio DECIMAL(5,2),
    SavingsRate DECIMAL(5,2),
    BudgetAdherence DECIMAL(5,2),
    DebtRatio DECIMAL(5,2),
    CalculatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_health_user (UserId),
    INDEX idx_health_date (CalculatedAt)
) ENGINE=InnoDB;

-- =====================================================
-- 10. AKILLI İÇGÖRÜLER (INSIGHTS) ⭐
-- =====================================================
CREATE TABLE Insights (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    InsightType VARCHAR(50) NOT NULL COMMENT 'SPENDING_INCREASE, BUDGET_WARNING, SAVING_TIP, etc.',
    Title VARCHAR(200) NOT NULL,
    Description TEXT NOT NULL,
    Severity VARCHAR(20) DEFAULT 'INFO' COMMENT 'INFO, WARNING, ALERT, SUCCESS',
    RelatedCategoryId INT NULL,
    RelatedAmount DECIMAL(18,2) NULL,
    PercentageChange DECIMAL(8,2) NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt DATETIME NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RelatedCategoryId) REFERENCES Categories(Id) ON DELETE SET NULL,
    INDEX idx_insight_user (UserId),
    INDEX idx_insight_type (InsightType),
    INDEX idx_insight_active (IsActive, IsRead)
) ENGINE=InnoDB;

-- =====================================================
-- TRIGGER: İşlem Eklendiğinde Bakiye Güncelle ⭐
-- =====================================================
DELIMITER //

CREATE TRIGGER trg_transaction_insert_balance
AFTER INSERT ON Transactions
FOR EACH ROW
BEGIN
    -- Gelir: bakiyeyi artır
    IF NEW.TransactionType = 1 THEN
        UPDATE Accounts SET CurrentBalance = CurrentBalance + NEW.Amount WHERE Id = NEW.AccountId;
    -- Gider: bakiyeyi azalt
    ELSEIF NEW.TransactionType = 2 THEN
        UPDATE Accounts SET CurrentBalance = CurrentBalance - NEW.Amount WHERE Id = NEW.AccountId;
    -- Transfer çıkış: kaynak hesaptan düş
    ELSEIF NEW.TransactionType = 3 AND NEW.RelatedTransactionId IS NULL THEN
        UPDATE Accounts SET CurrentBalance = CurrentBalance - NEW.Amount WHERE Id = NEW.AccountId;
    -- Transfer giriş: hedef hesaba ekle
    ELSEIF NEW.TransactionType = 3 AND NEW.RelatedTransactionId IS NOT NULL THEN
        UPDATE Accounts SET CurrentBalance = CurrentBalance + NEW.Amount WHERE Id = NEW.AccountId;
    END IF;
END//

-- =====================================================
-- TRIGGER: İşlem Silindiğinde Bakiye Geri Al ⭐
-- =====================================================
CREATE TRIGGER trg_transaction_delete_balance
AFTER DELETE ON Transactions
FOR EACH ROW
BEGIN
    -- Gelir silinirse: bakiyeden düş
    IF OLD.TransactionType = 1 THEN
        UPDATE Accounts SET CurrentBalance = CurrentBalance - OLD.Amount WHERE Id = OLD.AccountId;
    -- Gider silinirse: bakiyeye ekle
    ELSEIF OLD.TransactionType = 2 THEN
        UPDATE Accounts SET CurrentBalance = CurrentBalance + OLD.Amount WHERE Id = OLD.AccountId;
    END IF;
END//

-- =====================================================
-- TRIGGER: Hesap Audit Log ⭐
-- =====================================================
CREATE TRIGGER trg_account_audit_insert
AFTER INSERT ON Accounts
FOR EACH ROW
BEGIN
    INSERT INTO AuditLogs (UserId, TableName, RecordId, Action, NewValues)
    VALUES (NEW.UserId, 'Accounts', NEW.Id, 'INSERT', 
        JSON_OBJECT('AccountName', NEW.AccountName, 'CurrentBalance', NEW.CurrentBalance));
END//

CREATE TRIGGER trg_account_audit_update
AFTER UPDATE ON Accounts
FOR EACH ROW
BEGIN
    INSERT INTO AuditLogs (UserId, TableName, RecordId, Action, OldValues, NewValues)
    VALUES (NEW.UserId, 'Accounts', NEW.Id, 'UPDATE',
        JSON_OBJECT('AccountName', OLD.AccountName, 'CurrentBalance', OLD.CurrentBalance),
        JSON_OBJECT('AccountName', NEW.AccountName, 'CurrentBalance', NEW.CurrentBalance));
END//

-- =====================================================
-- TRIGGER: İşlem Audit Log ⭐
-- =====================================================
CREATE TRIGGER trg_transaction_audit_insert
AFTER INSERT ON Transactions
FOR EACH ROW
BEGIN
    DECLARE v_user_id INT;
    SELECT UserId INTO v_user_id FROM Accounts WHERE Id = NEW.AccountId;
    
    INSERT INTO AuditLogs (UserId, TableName, RecordId, Action, NewValues)
    VALUES (v_user_id, 'Transactions', NEW.Id, 'INSERT',
        JSON_OBJECT('Amount', NEW.Amount, 'Type', NEW.TransactionType, 'Description', NEW.Description));
END//

-- =====================================================
-- STORED PROCEDURE: Aylık Özet Hesapla ⭐
-- =====================================================
CREATE PROCEDURE sp_GetMonthlySummary(
    IN p_user_id INT,
    IN p_year INT,
    IN p_month INT
)
BEGIN
    SELECT 
        COALESCE(SUM(CASE WHEN t.TransactionType = 1 THEN t.Amount ELSE 0 END), 0) AS TotalIncome,
        COALESCE(SUM(CASE WHEN t.TransactionType = 2 THEN t.Amount ELSE 0 END), 0) AS TotalExpense,
        COALESCE(SUM(CASE WHEN t.TransactionType = 1 THEN t.Amount ELSE 0 END), 0) - 
        COALESCE(SUM(CASE WHEN t.TransactionType = 2 THEN t.Amount ELSE 0 END), 0) AS NetBalance,
        COUNT(DISTINCT t.Id) AS TransactionCount,
        COUNT(DISTINCT t.CategoryId) AS CategoryCount
    FROM Transactions t
    INNER JOIN Accounts a ON t.AccountId = a.Id
    WHERE a.UserId = p_user_id
      AND YEAR(t.TransactionDate) = p_year
      AND MONTH(t.TransactionDate) = p_month;
END//

-- =====================================================
-- STORED PROCEDURE: Finansal Sağlık Skoru Hesapla ⭐
-- =====================================================
CREATE PROCEDURE sp_CalculateFinancialHealthScore(
    IN p_user_id INT,
    OUT p_score INT,
    OUT p_income_expense_ratio DECIMAL(5,2),
    OUT p_savings_rate DECIMAL(5,2),
    OUT p_budget_adherence DECIMAL(5,2)
)
BEGIN
    DECLARE v_total_income DECIMAL(18,2) DEFAULT 0;
    DECLARE v_total_expense DECIMAL(18,2) DEFAULT 0;
    DECLARE v_budget_total DECIMAL(18,2) DEFAULT 0;
    DECLARE v_budget_spent DECIMAL(18,2) DEFAULT 0;
    DECLARE v_score_income DECIMAL(5,2) DEFAULT 0;
    DECLARE v_score_savings DECIMAL(5,2) DEFAULT 0;
    DECLARE v_score_budget DECIMAL(5,2) DEFAULT 0;
    
    -- Son 30 günlük gelir
    SELECT COALESCE(SUM(t.Amount), 0) INTO v_total_income
    FROM Transactions t
    INNER JOIN Accounts a ON t.AccountId = a.Id
    WHERE a.UserId = p_user_id 
      AND t.TransactionType = 1
      AND t.TransactionDate >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
    
    -- Son 30 günlük gider
    SELECT COALESCE(SUM(t.Amount), 0) INTO v_total_expense
    FROM Transactions t
    INNER JOIN Accounts a ON t.AccountId = a.Id
    WHERE a.UserId = p_user_id 
      AND t.TransactionType = 2
      AND t.TransactionDate >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
    
    -- Gelir/Gider oranı skoru (max 40 puan)
    IF v_total_income > 0 THEN
        SET p_income_expense_ratio = (v_total_income - v_total_expense) / v_total_income * 100;
        SET v_score_income = LEAST(40, GREATEST(0, p_income_expense_ratio * 0.4));
    ELSE
        SET p_income_expense_ratio = 0;
        SET v_score_income = 0;
    END IF;
    
    -- Tasarruf oranı skoru (max 30 puan)
    IF v_total_income > 0 THEN
        SET p_savings_rate = (v_total_income - v_total_expense) / v_total_income * 100;
        SET v_score_savings = LEAST(30, GREATEST(0, p_savings_rate * 0.3));
    ELSE
        SET p_savings_rate = 0;
        SET v_score_savings = 0;
    END IF;
    
    -- Bütçe uyumu skoru (max 30 puan)
    SELECT COALESCE(SUM(b.AmountLimit), 0) INTO v_budget_total
    FROM Budgets b
    WHERE b.UserId = p_user_id AND CURDATE() BETWEEN b.StartDate AND b.EndDate;
    
    IF v_budget_total > 0 THEN
        -- Bütçe kategorilerindeki harcama
        SELECT COALESCE(SUM(t.Amount), 0) INTO v_budget_spent
        FROM Transactions t
        INNER JOIN Accounts a ON t.AccountId = a.Id
        INNER JOIN Budgets b ON t.CategoryId = b.CategoryId AND b.UserId = p_user_id
        WHERE a.UserId = p_user_id 
          AND t.TransactionType = 2
          AND t.TransactionDate >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
        
        SET p_budget_adherence = LEAST(100, (1 - (v_budget_spent / v_budget_total)) * 100);
        SET v_score_budget = LEAST(30, GREATEST(0, p_budget_adherence * 0.3));
    ELSE
        SET p_budget_adherence = 100;
        SET v_score_budget = 20; -- Bütçe yoksa orta puan
    END IF;
    
    -- Toplam skor
    SET p_score = ROUND(v_score_income + v_score_savings + v_score_budget);
    
    -- Skoru kaydet
    INSERT INTO FinancialHealthHistory (UserId, Score, IncomeExpenseRatio, SavingsRate, BudgetAdherence)
    VALUES (p_user_id, p_score, p_income_expense_ratio, p_savings_rate, p_budget_adherence);
END//

-- =====================================================
-- STORED PROCEDURE: Akıllı İçgörü Oluştur ⭐
-- =====================================================
CREATE PROCEDURE sp_GenerateInsights(IN p_user_id INT)
BEGIN
    DECLARE v_current_month_expense DECIMAL(18,2);
    DECLARE v_last_month_expense DECIMAL(18,2);
    DECLARE v_category_id INT;
    DECLARE v_category_name VARCHAR(100);
    DECLARE v_current_cat_expense DECIMAL(18,2);
    DECLARE v_last_cat_expense DECIMAL(18,2);
    DECLARE v_change_percent DECIMAL(8,2);
    DECLARE v_done INT DEFAULT FALSE;
    
    -- Kategori bazlı harcama analizi için cursor
    DECLARE cat_cursor CURSOR FOR
        SELECT DISTINCT c.Id, c.CategoryName
        FROM Transactions t
        INNER JOIN Accounts a ON t.AccountId = a.Id
        INNER JOIN Categories c ON t.CategoryId = c.Id
        WHERE a.UserId = p_user_id AND t.TransactionType = 2
          AND t.TransactionDate >= DATE_SUB(CURDATE(), INTERVAL 60 DAY);
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = TRUE;
    
    -- Eski içgörüleri temizle
    DELETE FROM Insights WHERE UserId = p_user_id AND CreatedAt < DATE_SUB(CURDATE(), INTERVAL 7 DAY);
    
    -- Genel harcama analizi
    SELECT COALESCE(SUM(t.Amount), 0) INTO v_current_month_expense
    FROM Transactions t
    INNER JOIN Accounts a ON t.AccountId = a.Id
    WHERE a.UserId = p_user_id AND t.TransactionType = 2
      AND YEAR(t.TransactionDate) = YEAR(CURDATE())
      AND MONTH(t.TransactionDate) = MONTH(CURDATE());
    
    SELECT COALESCE(SUM(t.Amount), 0) INTO v_last_month_expense
    FROM Transactions t
    INNER JOIN Accounts a ON t.AccountId = a.Id
    WHERE a.UserId = p_user_id AND t.TransactionType = 2
      AND YEAR(t.TransactionDate) = YEAR(DATE_SUB(CURDATE(), INTERVAL 1 MONTH))
      AND MONTH(t.TransactionDate) = MONTH(DATE_SUB(CURDATE(), INTERVAL 1 MONTH));
    
    -- Genel harcama artışı uyarısı
    IF v_last_month_expense > 0 AND v_current_month_expense > v_last_month_expense * 1.2 THEN
        SET v_change_percent = ((v_current_month_expense - v_last_month_expense) / v_last_month_expense) * 100;
        INSERT INTO Insights (UserId, InsightType, Title, Description, Severity, RelatedAmount, PercentageChange)
        VALUES (p_user_id, 'SPENDING_INCREASE', 
            CONCAT('Harcamalarınız %', ROUND(v_change_percent), ' arttı!'),
            CONCAT('Bu ay toplam ₺', FORMAT(v_current_month_expense, 2), ' harcadınız. Geçen aya göre ₺', 
                   FORMAT(v_current_month_expense - v_last_month_expense, 2), ' daha fazla.'),
            'WARNING', v_current_month_expense, v_change_percent);
    END IF;
    
    -- Kategori bazlı analiz
    OPEN cat_cursor;
    
    read_loop: LOOP
        FETCH cat_cursor INTO v_category_id, v_category_name;
        IF v_done THEN
            LEAVE read_loop;
        END IF;
        
        -- Bu ay kategori harcaması
        SELECT COALESCE(SUM(t.Amount), 0) INTO v_current_cat_expense
        FROM Transactions t
        INNER JOIN Accounts a ON t.AccountId = a.Id
        WHERE a.UserId = p_user_id AND t.TransactionType = 2
          AND t.CategoryId = v_category_id
          AND YEAR(t.TransactionDate) = YEAR(CURDATE())
          AND MONTH(t.TransactionDate) = MONTH(CURDATE());
        
        -- Geçen ay kategori harcaması
        SELECT COALESCE(SUM(t.Amount), 0) INTO v_last_cat_expense
        FROM Transactions t
        INNER JOIN Accounts a ON t.AccountId = a.Id
        WHERE a.UserId = p_user_id AND t.TransactionType = 2
          AND t.CategoryId = v_category_id
          AND YEAR(t.TransactionDate) = YEAR(DATE_SUB(CURDATE(), INTERVAL 1 MONTH))
          AND MONTH(t.TransactionDate) = MONTH(DATE_SUB(CURDATE(), INTERVAL 1 MONTH));
        
        -- %50'den fazla artış varsa uyar
        IF v_last_cat_expense > 100 AND v_current_cat_expense > v_last_cat_expense * 1.5 THEN
            SET v_change_percent = ((v_current_cat_expense - v_last_cat_expense) / v_last_cat_expense) * 100;
            INSERT INTO Insights (UserId, InsightType, Title, Description, Severity, RelatedCategoryId, RelatedAmount, PercentageChange)
            VALUES (p_user_id, 'CATEGORY_SPIKE',
                CONCAT(v_category_name, ' harcamanız %', ROUND(v_change_percent), ' arttı'),
                CONCAT('Bu ay ', v_category_name, ' kategorisinde ₺', FORMAT(v_current_cat_expense, 2), 
                       ' harcadınız. Geçen aya göre belirgin bir artış var.'),
                'ALERT', v_category_id, v_current_cat_expense, v_change_percent);
        END IF;
    END LOOP;
    
    CLOSE cat_cursor;
    
    -- Tasarruf önerisi
    IF v_current_month_expense > 0 THEN
        INSERT INTO Insights (UserId, InsightType, Title, Description, Severity)
        VALUES (p_user_id, 'SAVING_TIP',
            'Tasarruf İpucu 💡',
            CONCAT('Günlük ortalama ₺', FORMAT(v_current_month_expense / DAY(CURDATE()), 2), 
                   ' harcıyorsunuz. Küçük kesintiler büyük tasarruflara dönüşür!'),
            'INFO');
    END IF;
END//

-- =====================================================
-- STORED PROCEDURE: Bütçe Uyarıları Kontrol ⭐
-- =====================================================
CREATE PROCEDURE sp_CheckBudgetAlerts(IN p_user_id INT)
BEGIN
    -- Bütçe limiti yaklaşanlar için uyarı
    INSERT INTO Insights (UserId, InsightType, Title, Description, Severity, RelatedCategoryId, RelatedAmount, PercentageChange)
    SELECT 
        b.UserId,
        'BUDGET_WARNING',
        CONCAT(c.CategoryName, ' bütçeniz dolmak üzere!'),
        CONCAT('₺', FORMAT(b.AmountLimit, 2), ' limitinizin %', 
               ROUND((spent.total / b.AmountLimit) * 100), '''ini kullandınız.'),
        CASE 
            WHEN (spent.total / b.AmountLimit) >= 1 THEN 'ALERT'
            WHEN (spent.total / b.AmountLimit) >= 0.8 THEN 'WARNING'
            ELSE 'INFO'
        END,
        b.CategoryId,
        spent.total,
        (spent.total / b.AmountLimit) * 100
    FROM Budgets b
    INNER JOIN Categories c ON b.CategoryId = c.Id
    INNER JOIN (
        SELECT t.CategoryId, SUM(t.Amount) as total
        FROM Transactions t
        INNER JOIN Accounts a ON t.AccountId = a.Id
        WHERE a.UserId = p_user_id AND t.TransactionType = 2
          AND t.TransactionDate >= DATE_FORMAT(CURDATE(), '%Y-%m-01')
        GROUP BY t.CategoryId
    ) spent ON b.CategoryId = spent.CategoryId
    WHERE b.UserId = p_user_id
      AND CURDATE() BETWEEN b.StartDate AND b.EndDate
      AND (spent.total / b.AmountLimit) >= 0.8
      AND NOT EXISTS (
          SELECT 1 FROM Insights i 
          WHERE i.UserId = p_user_id 
            AND i.InsightType = 'BUDGET_WARNING' 
            AND i.RelatedCategoryId = b.CategoryId
            AND i.CreatedAt >= DATE_SUB(CURDATE(), INTERVAL 1 DAY)
      );
END//

DELIMITER ;

-- =====================================================
-- VIEW: Kullanıcı Finansal Özeti ⭐
-- =====================================================
CREATE VIEW vw_UserFinancialSummary AS
SELECT 
    u.Id AS UserId,
    u.Username,
    CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
    (SELECT COALESCE(SUM(CurrentBalance), 0) FROM Accounts WHERE UserId = u.Id AND IsActive = 1) AS TotalBalance,
    (SELECT COALESCE(SUM(Amount), 0) FROM Transactions t 
     INNER JOIN Accounts a ON t.AccountId = a.Id 
     WHERE a.UserId = u.Id AND t.TransactionType = 1 
       AND MONTH(t.TransactionDate) = MONTH(CURDATE())) AS MonthlyIncome,
    (SELECT COALESCE(SUM(Amount), 0) FROM Transactions t 
     INNER JOIN Accounts a ON t.AccountId = a.Id 
     WHERE a.UserId = u.Id AND t.TransactionType = 2 
       AND MONTH(t.TransactionDate) = MONTH(CURDATE())) AS MonthlyExpense,
    (SELECT COUNT(*) FROM Accounts WHERE UserId = u.Id AND IsActive = 1) AS AccountCount,
    (SELECT Score FROM FinancialHealthHistory WHERE UserId = u.Id ORDER BY CalculatedAt DESC LIMIT 1) AS HealthScore
FROM Users u
WHERE u.IsActive = 1;

-- =====================================================
-- VIEW: Kategori Harcama Analizi ⭐
-- =====================================================
CREATE VIEW vw_CategorySpendingAnalysis AS
SELECT 
    a.UserId,
    c.Id AS CategoryId,
    c.CategoryName,
    c.Type AS CategoryType,
    YEAR(t.TransactionDate) AS Year,
    MONTH(t.TransactionDate) AS Month,
    SUM(t.Amount) AS TotalAmount,
    COUNT(t.Id) AS TransactionCount,
    AVG(t.Amount) AS AvgAmount
FROM Transactions t
INNER JOIN Accounts a ON t.AccountId = a.Id
INNER JOIN Categories c ON t.CategoryId = c.Id
GROUP BY a.UserId, c.Id, c.CategoryName, c.Type, YEAR(t.TransactionDate), MONTH(t.TransactionDate);

-- =====================================================
-- ÖRNEK VERİ
-- =====================================================
INSERT INTO Users (Username, PasswordHash, Email, FirstName, LastName) VALUES 
    ('demo', 'A6xnQhbz4Vx2HuGl4lXwZ5U2I8iziLRFnhP5eNfIRvQ=', 'demo@example.com', 'Demo', 'Kullanıcı');

INSERT INTO Accounts (UserId, AccountTypeId, AccountName, CurrencyCode, InitialBalance, CurrentBalance) VALUES
    (1, 1, 'Cüzdan', 'TRY', 500.00, 500.00),
    (1, 2, 'Ziraat Bankası', 'TRY', 5000.00, 5000.00),
    (1, 3, 'Kredi Kartı', 'TRY', 0.00, 0.00);

-- =====================================================
SELECT '✅ Veritabanı başarıyla oluşturuldu!' AS Sonuc;
SELECT '📊 Trigger, Stored Procedure ve View''ler eklendi.' AS Bilgi;
SELECT '👤 Test kullanıcısı: demo / 1234' AS TestKullanici;
