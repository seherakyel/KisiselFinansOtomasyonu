-- =====================================================
-- KİŞİSEL FİNANS OTOMASYONU - DEMO VERİLERİ
-- Bu script demo hesabı için örnek veriler ekler
-- Schema v3.0 ile uyumlu
-- =====================================================

USE FinansOtomasyonDb;

-- Önce mevcut demo verileri temizle (varsa)
SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM Transactions WHERE AccountId IN (SELECT Id FROM Accounts WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo'));
DELETE FROM Budgets WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM ScheduledTransactions WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM FinancialHealthHistory WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM Insights WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM AuditLogs WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM Accounts WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM Categories WHERE UserId = (SELECT Id FROM Users WHERE Username = 'demo');
DELETE FROM Users WHERE Username = 'demo';

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- 1. DEMO KULLANICI
-- Kullanıcı Adı: demo
-- Şifre: demo123 (SHA256 -> Base64)
-- =====================================================
INSERT INTO Users (Username, PasswordHash, FirstName, LastName, Email, CreatedAt, IsActive) VALUES
('demo', 'pmWkWSBCL51Bfkhn79xPuKBKHz//H6B+mY6G9/eieuM=', 'Demo', 'Kullanıcı', 'demo@example.com', NOW(), TRUE);

SET @UserId = LAST_INSERT_ID();

-- =====================================================
-- 2. HESAPLAR
-- =====================================================
INSERT INTO Accounts (UserId, AccountTypeId, AccountName, CurrencyCode, InitialBalance, CurrentBalance, CreditLimit, IsActive) VALUES
(@UserId, 1, 'Cüzdan', 'TRY', 0.00, 1250.00, 0.00, TRUE),
(@UserId, 2, 'Ziraat Bankası', 'TRY', 10000.00, 45780.50, 0.00, TRUE),
(@UserId, 2, 'Garanti BBVA', 'TRY', 5000.00, 12350.00, 0.00, TRUE),
(@UserId, 3, 'Bonus Kredi Kartı', 'TRY', 0.00, -3250.00, 25000.00, TRUE),
(@UserId, 4, 'Borsa Yatırım', 'TRY', 15000.00, 28500.00, 0.00, TRUE);

-- Hesap ID'lerini al
SET @NakitId = (SELECT Id FROM Accounts WHERE UserId = @UserId AND AccountName = 'Cüzdan');
SET @ZiraatId = (SELECT Id FROM Accounts WHERE UserId = @UserId AND AccountName = 'Ziraat Bankası');
SET @GarantiId = (SELECT Id FROM Accounts WHERE UserId = @UserId AND AccountName = 'Garanti BBVA');
SET @KrediKartiId = (SELECT Id FROM Accounts WHERE UserId = @UserId AND AccountName = 'Bonus Kredi Kartı');
SET @BorsaId = (SELECT Id FROM Accounts WHERE UserId = @UserId AND AccountName = 'Borsa Yatırım');

-- =====================================================
-- 3. KULLANICIYA ÖZEL KATEGORİLER
-- =====================================================
-- Gelir Kategorileri (Type = 1)
INSERT INTO Categories (UserId, ParentId, CategoryName, Type, IconIndex) VALUES
(@UserId, NULL, 'Maaş', 1, 1),
(@UserId, NULL, 'Freelance', 1, 2),
(@UserId, NULL, 'Yatırım Geliri', 1, 3),
(@UserId, NULL, 'Kira Geliri', 1, 4),
(@UserId, NULL, 'Diğer Gelir', 1, 5);

-- Gider Kategorileri (Type = 2)
INSERT INTO Categories (UserId, ParentId, CategoryName, Type, IconIndex) VALUES
(@UserId, NULL, 'Market', 2, 10),
(@UserId, NULL, 'Ulaşım', 2, 11),
(@UserId, NULL, 'Faturalar', 2, 12),
(@UserId, NULL, 'Eğlence', 2, 16),
(@UserId, NULL, 'Sağlık', 2, 14),
(@UserId, NULL, 'Giyim', 2, 17),
(@UserId, NULL, 'Yemek', 2, 18),
(@UserId, NULL, 'Eğitim', 2, 15),
(@UserId, NULL, 'Kira Gideri', 2, 13),
(@UserId, NULL, 'Diğer Gider', 2, 99);

-- Kategori ID'lerini al
SET @MaasId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Maaş');
SET @FreelanceId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Freelance');
SET @YatirimGelirId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Yatırım Geliri');
SET @KiraGelirId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Kira Geliri');
SET @DigerGelirId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Diğer Gelir');

SET @MarketId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Market');
SET @UlasimId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Ulaşım');
SET @FaturalarId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Faturalar');
SET @EglenceId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Eğlence');
SET @SaglikId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Sağlık');
SET @GiyimId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Giyim');
SET @YemekId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Yemek');
SET @EgitimId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Eğitim');
SET @KiraGiderId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Kira Gideri');
SET @DigerGiderId = (SELECT Id FROM Categories WHERE UserId = @UserId AND CategoryName = 'Diğer Gider');

-- =====================================================
-- 4. İŞLEMLER (Son 7 ay)
-- NOT: Trigger otomatik bakiye günceller!
-- =====================================================

-- İşlemleri eklemeden önce bakiyeleri sıfırlayalım (trigger'lar dolduracak)
UPDATE Accounts SET CurrentBalance = InitialBalance WHERE UserId = @UserId;

-- TEMMUZ 2025
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 30000.00, 'Temmuz Maaşı', '2025-07-01', '2025-07-01 09:00:00'),
(@ZiraatId, @FreelanceId, 1, 6000.00, 'Dashboard projesi', '2025-07-18', '2025-07-18 14:00:00'),
(@ZiraatId, @KiraGiderId, 2, 8000.00, 'Ev kirası', '2025-07-01', '2025-07-01 10:00:00'),
(@KrediKartiId, @MarketId, 2, 2300.00, 'Market alışverişi', '2025-07-05', '2025-07-05 19:00:00'),
(@ZiraatId, @FaturalarId, 2, 750.00, 'Elektrik faturası', '2025-07-10', '2025-07-10 11:00:00'),
(@ZiraatId, @FaturalarId, 2, 200.00, 'Su faturası', '2025-07-10', '2025-07-10 11:30:00'),
(@NakitId, @UlasimId, 2, 480.00, 'Akaryakıt', '2025-07-14', '2025-07-14 16:00:00'),
(@KrediKartiId, @EglenceId, 2, 350.00, 'Abonelikler', '2025-07-15', '2025-07-15 00:00:00'),
(@KrediKartiId, @YemekId, 2, 550.00, 'Dışarıda yemek', '2025-07-20', '2025-07-20 20:00:00'),
(@KrediKartiId, @SaglikId, 2, 1200.00, 'Gözlük', '2025-07-25', '2025-07-25 12:00:00');

-- AĞUSTOS 2025
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 32000.00, 'Ağustos Maaşı', '2025-08-01', '2025-08-01 09:00:00'),
(@GarantiId, @KiraGelirId, 1, 4500.00, 'Yazlık kira geliri', '2025-08-05', '2025-08-05 12:00:00'),
(@BorsaId, @YatirimGelirId, 1, 1800.00, 'Temettü geliri', '2025-08-20', '2025-08-20 10:00:00'),
(@ZiraatId, @KiraGiderId, 2, 8000.00, 'Ev kirası', '2025-08-01', '2025-08-01 10:00:00'),
(@KrediKartiId, @MarketId, 2, 2100.00, 'Market alışverişi', '2025-08-06', '2025-08-06 18:00:00'),
(@ZiraatId, @FaturalarId, 2, 680.00, 'Elektrik (klima)', '2025-08-10', '2025-08-10 11:00:00'),
(@NakitId, @UlasimId, 2, 800.00, 'Tatil yakıt masrafı', '2025-08-12', '2025-08-12 08:00:00'),
(@KrediKartiId, @EglenceId, 2, 4500.00, 'Tatil harcamaları', '2025-08-15', '2025-08-15 20:00:00'),
(@KrediKartiId, @YemekId, 2, 1200.00, 'Tatilde yemek', '2025-08-18', '2025-08-18 21:00:00'),
(@KrediKartiId, @GiyimId, 2, 800.00, 'Yazlık kıyafetler', '2025-08-22', '2025-08-22 15:00:00');

-- EYLÜL 2025
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 32000.00, 'Eylül Maaşı', '2025-09-01', '2025-09-01 09:00:00'),
(@GarantiId, @KiraGelirId, 1, 4500.00, 'Yazlık kira geliri', '2025-09-05', '2025-09-05 12:00:00'),
(@ZiraatId, @KiraGiderId, 2, 8000.00, 'Ev kirası', '2025-09-01', '2025-09-01 10:00:00'),
(@KrediKartiId, @MarketId, 2, 2600.00, 'Market alışverişi', '2025-09-08', '2025-09-08 17:00:00'),
(@ZiraatId, @FaturalarId, 2, 520.00, 'Elektrik faturası', '2025-09-10', '2025-09-10 11:00:00'),
(@KrediKartiId, @EgitimId, 2, 3500.00, 'Yıllık kurs ücreti', '2025-09-12', '2025-09-12 10:00:00'),
(@NakitId, @UlasimId, 2, 550.00, 'Akaryakıt', '2025-09-15', '2025-09-15 16:00:00'),
(@KrediKartiId, @EglenceId, 2, 350.00, 'Abonelikler', '2025-09-15', '2025-09-15 00:00:00'),
(@KrediKartiId, @YemekId, 2, 720.00, 'Restoranlar', '2025-09-20', '2025-09-20 21:00:00'),
(@KrediKartiId, @SaglikId, 2, 450.00, 'Sağlık kontrolü', '2025-09-25', '2025-09-25 09:00:00');

-- EKİM 2025
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 32000.00, 'Ekim Maaşı', '2025-10-01', '2025-10-01 09:00:00'),
(@ZiraatId, @FreelanceId, 1, 8000.00, 'E-ticaret projesi', '2025-10-10', '2025-10-10 16:00:00'),
(@BorsaId, @YatirimGelirId, 1, 2500.00, 'Hisse satışı karı', '2025-10-25', '2025-10-25 11:00:00'),
(@ZiraatId, @KiraGiderId, 2, 8500.00, 'Ev kirası', '2025-10-01', '2025-10-01 10:00:00'),
(@KrediKartiId, @MarketId, 2, 2200.00, 'Market alışverişi', '2025-10-05', '2025-10-05 18:00:00'),
(@ZiraatId, @FaturalarId, 2, 480.00, 'Elektrik faturası', '2025-10-10', '2025-10-10 11:00:00'),
(@ZiraatId, @FaturalarId, 2, 150.00, 'Su faturası', '2025-10-10', '2025-10-10 11:30:00'),
(@NakitId, @UlasimId, 2, 600.00, 'Akaryakıt', '2025-10-15', '2025-10-15 16:00:00'),
(@KrediKartiId, @GiyimId, 2, 2500.00, 'Sonbahar gardırobu', '2025-10-18', '2025-10-18 14:00:00'),
(@KrediKartiId, @EglenceId, 2, 350.00, 'Abonelikler', '2025-10-15', '2025-10-15 00:00:00'),
(@KrediKartiId, @YemekId, 2, 480.00, 'Dışarıda yemek', '2025-10-20', '2025-10-20 20:00:00'),
(@NakitId, @EglenceId, 2, 300.00, 'Konser bileti', '2025-10-28', '2025-10-28 19:00:00');

-- KASIM 2025
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 35000.00, 'Kasım Maaşı', '2025-11-01', '2025-11-01 09:00:00'),
(@GarantiId, @FreelanceId, 1, 3500.00, 'Mobil uygulama', '2025-11-20', '2025-11-20 15:00:00'),
(@ZiraatId, @KiraGiderId, 2, 8500.00, 'Ev kirası', '2025-11-01', '2025-11-01 10:00:00'),
(@KrediKartiId, @MarketId, 2, 2400.00, 'Market alışverişi', '2025-11-07', '2025-11-07 19:00:00'),
(@ZiraatId, @FaturalarId, 2, 650.00, 'Elektrik faturası', '2025-11-10', '2025-11-10 11:00:00'),
(@ZiraatId, @FaturalarId, 2, 180.00, 'Su faturası', '2025-11-10', '2025-11-10 11:30:00'),
(@NakitId, @UlasimId, 2, 450.00, 'Akaryakıt', '2025-11-12', '2025-11-12 17:00:00'),
(@KrediKartiId, @SaglikId, 2, 800.00, 'Diş tedavisi', '2025-11-15', '2025-11-15 14:00:00'),
(@KrediKartiId, @YemekId, 2, 650.00, 'Restoranlar', '2025-11-18', '2025-11-18 21:00:00'),
(@KrediKartiId, @EglenceId, 2, 350.00, 'Abonelikler', '2025-11-15', '2025-11-15 00:00:00'),
(@NakitId, @YemekId, 2, 220.00, 'Kahve', '2025-11-22', '2025-11-22 10:00:00'),
(@KrediKartiId, @EgitimId, 2, 1500.00, 'Online kurs', '2025-11-25', '2025-11-25 20:00:00');

-- ARALIK 2025
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 35000.00, 'Aralık Maaşı', '2025-12-01', '2025-12-01 09:00:00'),
(@ZiraatId, @FreelanceId, 1, 5000.00, 'Web sitesi projesi', '2025-12-15', '2025-12-15 14:30:00'),
(@BorsaId, @YatirimGelirId, 1, 1200.00, 'Temettü geliri', '2025-12-20', '2025-12-20 10:00:00'),
(@ZiraatId, @KiraGiderId, 2, 8500.00, 'Ev kirası', '2025-12-01', '2025-12-01 10:00:00'),
(@KrediKartiId, @MarketId, 2, 2800.00, 'Migros market alışverişi', '2025-12-05', '2025-12-05 18:30:00'),
(@KrediKartiId, @YemekId, 2, 450.00, 'Restoran', '2025-12-08', '2025-12-08 20:00:00'),
(@ZiraatId, @FaturalarId, 2, 850.00, 'Elektrik faturası', '2025-12-10', '2025-12-10 11:00:00'),
(@ZiraatId, @FaturalarId, 2, 320.00, 'Doğalgaz faturası', '2025-12-10', '2025-12-10 11:15:00'),
(@ZiraatId, @FaturalarId, 2, 250.00, 'İnternet faturası', '2025-12-12', '2025-12-12 09:00:00'),
(@NakitId, @UlasimId, 2, 500.00, 'Akaryakıt', '2025-12-14', '2025-12-14 16:00:00'),
(@KrediKartiId, @EglenceId, 2, 350.00, 'Netflix + Spotify', '2025-12-15', '2025-12-15 00:00:00'),
(@KrediKartiId, @GiyimId, 2, 1200.00, 'Kışlık mont', '2025-12-18', '2025-12-18 15:00:00'),
(@NakitId, @YemekId, 2, 180.00, 'Kahve ve atıştırmalık', '2025-12-20', '2025-12-20 14:00:00'),
(@KrediKartiId, @EglenceId, 2, 280.00, 'Sinema ve bowling', '2025-12-22', '2025-12-22 19:00:00'),
(@KrediKartiId, @MarketId, 2, 1500.00, 'Yılbaşı alışverişi', '2025-12-28', '2025-12-28 12:00:00'),
(@NakitId, @DigerGiderId, 2, 200.00, 'Hediyeler', '2025-12-30', '2025-12-30 16:00:00');

-- OCAK 2026 (Bu ay - güncel)
INSERT INTO Transactions (AccountId, CategoryId, TransactionType, Amount, Description, TransactionDate, CreatedAt) VALUES
(@ZiraatId, @MaasId, 1, 38000.00, 'Ocak Maaşı (Zam)', '2026-01-02', '2026-01-02 09:00:00'),
(@ZiraatId, @KiraGiderId, 2, 9000.00, 'Ev kirası', '2026-01-02', '2026-01-02 10:00:00'),
(@KrediKartiId, @MarketId, 2, 1800.00, 'Haftalık market', '2026-01-03', '2026-01-03 18:00:00');

-- =====================================================
-- 5. BÜTÇELER
-- =====================================================
INSERT INTO Budgets (UserId, CategoryId, AmountLimit, StartDate, EndDate) VALUES
(@UserId, @MarketId, 3500.00, '2026-01-01', '2026-01-31'),
(@UserId, @YemekId, 1500.00, '2026-01-01', '2026-01-31'),
(@UserId, @UlasimId, 800.00, '2026-01-01', '2026-01-31'),
(@UserId, @EglenceId, 1000.00, '2026-01-01', '2026-01-31'),
(@UserId, @GiyimId, 2000.00, '2026-01-01', '2026-01-31'),
(@UserId, @FaturalarId, 2000.00, '2026-01-01', '2026-01-31');

-- =====================================================
-- 6. PLANLI İŞLEMLER
-- =====================================================
INSERT INTO ScheduledTransactions (UserId, AccountId, CategoryId, Amount, Description, FrequencyType, DayOfMonth, NextExecutionDate, IsActive) VALUES
(@UserId, @ZiraatId, @MaasId, 38000.00, 'Aylık Maaş', 'Monthly', 1, '2026-02-01', TRUE),
(@UserId, @ZiraatId, @KiraGiderId, 9000.00, 'Ev Kirası', 'Monthly', 1, '2026-02-01', TRUE),
(@UserId, @ZiraatId, @FaturalarId, 250.00, 'İnternet Faturası', 'Monthly', 12, '2026-02-12', TRUE),
(@UserId, @KrediKartiId, @EglenceId, 350.00, 'Streaming Abonelikleri', 'Monthly', 15, '2026-02-15', TRUE);

-- =====================================================
-- 7. TASARRUF HEDEFLERİ
-- =====================================================
INSERT INTO SavingsGoals (UserId, Name, TargetAmount, CurrentAmount, StartDate, TargetDate, IsCompleted, CompletedDate) VALUES
(@UserId, '🚗 Yeni Araba', 500000.00, 125000.00, '2025-01-01', '2026-12-31', FALSE, NULL),
(@UserId, '🏠 Ev Peşinatı', 1000000.00, 350000.00, '2024-06-01', '2027-06-01', FALSE, NULL),
(@UserId, '✈️ Avrupa Turu', 80000.00, 45000.00, '2025-06-01', '2026-06-01', FALSE, NULL),
(@UserId, '💻 MacBook Pro', 85000.00, 60000.00, '2025-09-01', '2026-03-01', FALSE, NULL),
(@UserId, '📱 iPhone 16', 75000.00, 75000.00, '2025-08-01', '2025-12-01', TRUE, '2025-11-28');

-- =====================================================
-- 8. FİNANSAL SAĞLIK GEÇMİŞİ
-- =====================================================
INSERT INTO FinancialHealthHistory (UserId, Score, IncomeExpenseRatio, SavingsRate, BudgetAdherence, DebtRatio, CalculatedAt) VALUES
(@UserId, 72, 1.35, 18.50, 85.00, 12.30, '2025-07-01'),
(@UserId, 74, 1.42, 19.20, 88.00, 11.80, '2025-08-01'),
(@UserId, 71, 1.28, 15.50, 78.00, 14.20, '2025-09-01'),
(@UserId, 76, 1.55, 20.10, 90.00, 10.50, '2025-10-01'),
(@UserId, 78, 1.62, 22.00, 92.00, 9.80, '2025-11-01'),
(@UserId, 77, 1.58, 21.50, 89.00, 10.20, '2025-12-01'),
(@UserId, 79, 1.68, 23.00, 94.00, 9.50, '2026-01-01');

-- =====================================================
-- 9. AKILLI ÖNERİLER (INSIGHTS)
-- =====================================================
INSERT INTO Insights (UserId, InsightType, Title, Description, Severity, RelatedCategoryId, RelatedAmount, PercentageChange, IsRead, IsActive, CreatedAt) VALUES
(@UserId, 'SPENDING_INCREASE', '⚠️ Market Harcaması Yükseliyor', 'Bu ay market harcamalarınız geçen aya göre %15 arttı. Bütçenizi gözden geçirin.', 'WARNING', @MarketId, 2800.00, 15.00, FALSE, TRUE, NOW()),
(@UserId, 'SAVING_TIP', '💡 Tasarruf Önerisi', 'Streaming aboneliklerinizi birleştirerek ayda 150₺ tasarruf edebilirsiniz.', 'INFO', @EglenceId, 150.00, NULL, FALSE, TRUE, NOW()),
(@UserId, 'INCOME_INCREASE', '🎉 Harika İş!', 'Bu ay yatırım geliriniz %20 arttı. Aynı stratejiyi sürdürün!', 'SUCCESS', @YatirimGelirId, 1200.00, 20.00, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 5 DAY)),
(@UserId, 'DEBT_WARNING', '🔴 Kredi Kartı Borcu', 'Kredi kartı borcunuz 3.250₺. Faiz ödemelerinden kaçınmak için ödemeyi planlayın.', 'ALERT', NULL, 3250.00, NULL, FALSE, TRUE, NOW()),
(@UserId, 'BUDGET_WARNING', '💰 Acil Durum Fonu', 'Acil durum fonunuz 3 aylık giderleri karşılıyor. Hedef 6 ay olmalı.', 'INFO', NULL, NULL, NULL, TRUE, TRUE, DATE_SUB(NOW(), INTERVAL 10 DAY));

-- =====================================================
-- ÖZET BİLGİ
-- =====================================================
SELECT '╔════════════════════════════════════════════════════╗' AS '';
SELECT '║     DEMO VERİLERİ BAŞARIYLA EKLENDİ!              ║' AS '';
SELECT '╠════════════════════════════════════════════════════╣' AS '';
SELECT '║ Kullanıcı Adı : demo                              ║' AS '';
SELECT '║ Şifre         : demo123                           ║' AS '';
SELECT '╚════════════════════════════════════════════════════╝' AS '';

SELECT CONCAT('📊 Toplam Hesap     : ', COUNT(*)) AS '' FROM Accounts WHERE UserId = @UserId;
SELECT CONCAT('🏷️ Toplam Kategori  : ', COUNT(*)) AS '' FROM Categories WHERE UserId = @UserId;
SELECT CONCAT('💳 Toplam İşlem     : ', COUNT(*)) AS '' FROM Transactions WHERE AccountId IN (SELECT Id FROM Accounts WHERE UserId = @UserId);
SELECT CONCAT('🎯 Toplam Bütçe     : ', COUNT(*)) AS '' FROM Budgets WHERE UserId = @UserId;
SELECT CONCAT('💰 Toplam Varlık    : ₺', FORMAT(SUM(CurrentBalance), 2)) AS '' FROM Accounts WHERE UserId = @UserId AND CurrentBalance > 0;
