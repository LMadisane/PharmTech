-- PharmTech evaluation dataset
-- Run once only against a fresh, dedicated evaluation database after migrations
-- and the normal application seeder have completed. Never run against pharmtech_db.

START TRANSACTION;

-- Dedicated fictitious facilities used by the evaluation.
INSERT INTO Facilities (Name, Address, ContactInfo)
SELECT 'Test Facility A', 'Evaluation data only', 'evaluation-a@pharmtech.invalid'
WHERE NOT EXISTS (SELECT 1 FROM Facilities WHERE Name = 'Test Facility A');

INSERT INTO Facilities (Name, Address, ContactInfo)
SELECT 'Test Facility B', 'Evaluation data only', 'evaluation-b@pharmtech.invalid'
WHERE NOT EXISTS (SELECT 1 FROM Facilities WHERE Name = 'Test Facility B');

SET @facilityA = (SELECT FacilityId FROM Facilities WHERE Name = 'Test Facility A' LIMIT 1);
SET @facilityB = (SELECT FacilityId FROM Facilities WHERE Name = 'Test Facility B' LIMIT 1);

-- The application seeder creates these role accounts with locally configured or
-- generated passwords. This script assigns them to the test facilities but never
-- stores a password.
SET @doctorA = (SELECT UserId FROM Users WHERE Email = 'charismadisane@pharmtech.com' LIMIT 1);
SET @doctorB = (SELECT UserId FROM Users WHERE Email = 'ernest.mwanza@pharmtech.com' LIMIT 1);
SET @pharmacistA = (SELECT UserId FROM Users WHERE Email = 'chebwa.hampongo@pharmtech.com' LIMIT 1);
SET @pharmacistB = (SELECT UserId FROM Users WHERE Email = 'priscillandhl@pharmtech.com' LIMIT 1);

UPDATE Users SET FacilityId = @facilityA WHERE UserId IN (@doctorA, @pharmacistA);
UPDATE Users SET FacilityId = @facilityB WHERE UserId IN (@doctorB, @pharmacistB);

-- Twenty non-login patient records. The password-hash field is required by the
-- schema, but patients do not authenticate and no usable password is stored.
INSERT INTO Users (Name, Email, PasswordHash, Role, IsActive, FacilityId)
SELECT
    CONCAT('Evaluation Patient ', n),
    CONCAT('eval.pt', LPAD(n, 3, '0'), '@pharmtech.invalid'),
    'NotForLogin',
    'Patient',
    1,
    CASE WHEN n <= 10 THEN @facilityA ELSE @facilityB END
FROM (
    SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
    UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10
    UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
    UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
) AS numbers
WHERE NOT EXISTS (
    SELECT 1
    FROM Users existingUser
    WHERE existingUser.Email = CONCAT('eval.pt', LPAD(numbers.n, 3, '0'), '@pharmtech.invalid')
);

-- Five medicines used in the spreadsheet and PharmTech baseline.
INSERT INTO Medicines (Name, DosageForm, BufferQty)
SELECT 'Amoxicillin', 'Capsule', 100
WHERE NOT EXISTS (SELECT 1 FROM Medicines WHERE Name = 'Amoxicillin');

INSERT INTO Medicines (Name, DosageForm, BufferQty)
SELECT 'Paracetamol', 'Tablet', 20
WHERE NOT EXISTS (SELECT 1 FROM Medicines WHERE Name = 'Paracetamol');

INSERT INTO Medicines (Name, DosageForm, BufferQty)
SELECT 'Ibuprofen', 'Tablet', 20
WHERE NOT EXISTS (SELECT 1 FROM Medicines WHERE Name = 'Ibuprofen');

INSERT INTO Medicines (Name, DosageForm, BufferQty)
SELECT 'Cetirizine', 'Tablet', 10
WHERE NOT EXISTS (SELECT 1 FROM Medicines WHERE Name = 'Cetirizine');

INSERT INTO Medicines (Name, DosageForm, BufferQty)
SELECT 'Metronidazole', 'Tablet', 10
WHERE NOT EXISTS (SELECT 1 FROM Medicines WHERE Name = 'Metronidazole');

SET @amoxicillin = (SELECT MedId FROM Medicines WHERE Name = 'Amoxicillin' LIMIT 1);
SET @paracetamol = (SELECT MedId FROM Medicines WHERE Name = 'Paracetamol' LIMIT 1);
SET @ibuprofen = (SELECT MedId FROM Medicines WHERE Name = 'Ibuprofen' LIMIT 1);
SET @cetirizine = (SELECT MedId FROM Medicines WHERE Name = 'Cetirizine' LIMIT 1);
SET @metronidazole = (SELECT MedId FROM Medicines WHERE Name = 'Metronidazole' LIMIT 1);

-- Fifteen batches. PAR-A-01 and EXP-P9-001 support the FEFO and expired-stock
-- cases. The two IB-B lots have matching expiry and received dates for the
-- final BatchId tie-breaker case.
INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @amoxicillin, @facilityA, 'AMX-A-01', 75, DATE_ADD(CURDATE(), INTERVAL 120 DAY), DATE_SUB(CURDATE(), INTERVAL 30 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'AMX-A-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @paracetamol, @facilityA, 'PAR-A-01', 40, DATE_ADD(CURDATE(), INTERVAL 10 DAY), DATE_SUB(CURDATE(), INTERVAL 60 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'PAR-A-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @paracetamol, @facilityA, 'PAR-A-02', 50, DATE_ADD(CURDATE(), INTERVAL 120 DAY), DATE_SUB(CURDATE(), INTERVAL 20 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'PAR-A-02');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @paracetamol, @facilityA, 'EXP-P9-001', 10, DATE_SUB(CURDATE(), INTERVAL 1 DAY), DATE_SUB(CURDATE(), INTERVAL 90 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'EXP-P9-001');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @cetirizine, @facilityA, 'CET-A-01', 30, DATE_ADD(CURDATE(), INTERVAL 180 DAY), DATE_SUB(CURDATE(), INTERVAL 25 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'CET-A-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @metronidazole, @facilityA, 'MET-A-01', 25, DATE_ADD(CURDATE(), INTERVAL 90 DAY), DATE_SUB(CURDATE(), INTERVAL 15 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'MET-A-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @amoxicillin, @facilityB, 'AMX-B-01', 50, DATE_ADD(CURDATE(), INTERVAL 100 DAY), DATE_SUB(CURDATE(), INTERVAL 35 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'AMX-B-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @amoxicillin, @facilityB, 'AMX-B-02', 20, DATE_ADD(CURDATE(), INTERVAL 200 DAY), DATE_SUB(CURDATE(), INTERVAL 10 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'AMX-B-02');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @paracetamol, @facilityB, 'PAR-B-01', 60, DATE_ADD(CURDATE(), INTERVAL 90 DAY), DATE_SUB(CURDATE(), INTERVAL 40 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'PAR-B-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @paracetamol, @facilityB, 'PAR-B-02', 20, DATE_ADD(CURDATE(), INTERVAL 150 DAY), DATE_SUB(CURDATE(), INTERVAL 12 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'PAR-B-02');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @ibuprofen, @facilityB, 'IB-B-01', 30, DATE_ADD(CURDATE(), INTERVAL 200 DAY), DATE_SUB(CURDATE(), INTERVAL 30 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'IB-B-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @ibuprofen, @facilityB, 'IB-B-TIE-01', 10, DATE_ADD(CURDATE(), INTERVAL 200 DAY), DATE_SUB(CURDATE(), INTERVAL 30 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'IB-B-TIE-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @cetirizine, @facilityB, 'CET-B-01', 25, DATE_ADD(CURDATE(), INTERVAL 180 DAY), DATE_SUB(CURDATE(), INTERVAL 20 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'CET-B-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @metronidazole, @facilityB, 'MET-B-01', 40, DATE_ADD(CURDATE(), INTERVAL 120 DAY), DATE_SUB(CURDATE(), INTERVAL 35 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'MET-B-01');

INSERT INTO MedicineBatches (MedId, FacilityId, LotNumber, Quantity, ExpiryDate, DateReceived, IsFlagged)
SELECT @metronidazole, @facilityB, 'MET-B-02', 10, DATE_ADD(CURDATE(), INTERVAL 200 DAY), DATE_SUB(CURDATE(), INTERVAL 10 DAY), 0
WHERE NOT EXISTS (SELECT 1 FROM MedicineBatches WHERE LotNumber = 'MET-B-02');

-- Inventory summaries match all batch quantities, including the expired batch.
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityA, @amoxicillin, 75 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityA AND MedId = @amoxicillin);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityA, @paracetamol, 100 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityA AND MedId = @paracetamol);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityA, @ibuprofen, 0 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityA AND MedId = @ibuprofen);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityA, @cetirizine, 30 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityA AND MedId = @cetirizine);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityA, @metronidazole, 25 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityA AND MedId = @metronidazole);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityB, @amoxicillin, 70 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityB AND MedId = @amoxicillin);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityB, @paracetamol, 80 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityB AND MedId = @paracetamol);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityB, @ibuprofen, 40 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityB AND MedId = @ibuprofen);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityB, @cetirizine, 25 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityB AND MedId = @cetirizine);
INSERT INTO InventoryItems (FacilityId, MedId, Quantity)
SELECT @facilityB, @metronidazole, 50 WHERE NOT EXISTS (SELECT 1 FROM InventoryItems WHERE FacilityId = @facilityB AND MedId = @metronidazole);

SET @patientA1 = (SELECT UserId FROM Users WHERE Email = 'eval.pt001@pharmtech.invalid' LIMIT 1);
SET @patientA2 = (SELECT UserId FROM Users WHERE Email = 'eval.pt002@pharmtech.invalid' LIMIT 1);
SET @patientB11 = (SELECT UserId FROM Users WHERE Email = 'eval.pt011@pharmtech.invalid' LIMIT 1);
SET @patientA3 = (SELECT UserId FROM Users WHERE Email = 'eval.pt003@pharmtech.invalid' LIMIT 1);
SET @patientA4 = (SELECT UserId FROM Users WHERE Email = 'eval.pt004@pharmtech.invalid' LIMIT 1);

-- Three pending prescriptions used by lookup, dispensing, FEFO, and facility-isolation cases.
INSERT INTO Prescriptions (PatientId, MedId, DosagePerDay, DurationDays, PrescribedAt, ReferenceCode, Status, FacilityId)
SELECT @patientA1, @paracetamol, 2, 5, CURDATE(), 'RX-BASE-001', 'Pending', @facilityA
WHERE NOT EXISTS (SELECT 1 FROM Prescriptions WHERE ReferenceCode = 'RX-BASE-001');

INSERT INTO Prescriptions (PatientId, MedId, DosagePerDay, DurationDays, PrescribedAt, ReferenceCode, Status, FacilityId)
SELECT @patientA2, @amoxicillin, 1, 5, CURDATE(), 'RX-BASE-002', 'Pending', @facilityA
WHERE NOT EXISTS (SELECT 1 FROM Prescriptions WHERE ReferenceCode = 'RX-BASE-002');

INSERT INTO Prescriptions (PatientId, MedId, DosagePerDay, DurationDays, PrescribedAt, ReferenceCode, Status, FacilityId)
SELECT @patientB11, @ibuprofen, 2, 3, CURDATE(), 'RX-BASE-003', 'Pending', @facilityB
WHERE NOT EXISTS (SELECT 1 FROM Prescriptions WHERE ReferenceCode = 'RX-BASE-003');

-- Two pending returns support the return workflow test cases.
INSERT INTO DrugReturns (MedId, FacilityId, Quantity, PatientId, ProcessedById, Reason, Status, IsRestockable, ProcessedAt)
SELECT @paracetamol, @facilityA, 2, @patientA3, NULL, 'Evaluation restockable return', 'Pending', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM DrugReturns WHERE Reason = 'Evaluation restockable return');

INSERT INTO DrugReturns (MedId, FacilityId, Quantity, PatientId, ProcessedById, Reason, Status, IsRestockable, ProcessedAt)
SELECT @paracetamol, @facilityA, 1, @patientA4, NULL, 'Evaluation non-restockable return', 'Pending', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM DrugReturns WHERE Reason = 'Evaluation non-restockable return');

COMMIT;

-- Expected baseline counts in a fresh evaluation database.
SELECT
    (SELECT COUNT(*) FROM Medicines WHERE Name IN ('Amoxicillin', 'Paracetamol', 'Ibuprofen', 'Cetirizine', 'Metronidazole')) AS Medicines,
    (SELECT COUNT(*) FROM Users WHERE Email LIKE 'eval.pt%@pharmtech.invalid') AS Patients,
    (SELECT COUNT(*) FROM MedicineBatches WHERE LotNumber IN ('AMX-A-01', 'PAR-A-01', 'PAR-A-02', 'EXP-P9-001', 'CET-A-01', 'MET-A-01', 'AMX-B-01', 'AMX-B-02', 'PAR-B-01', 'PAR-B-02', 'IB-B-01', 'IB-B-TIE-01', 'CET-B-01', 'MET-B-01', 'MET-B-02')) AS Batches,
    (SELECT COUNT(*) FROM Prescriptions WHERE ReferenceCode IN ('RX-BASE-001', 'RX-BASE-002', 'RX-BASE-003') AND Status = 'Pending') AS PendingPrescriptions,
    (SELECT COUNT(*) FROM DrugReturns WHERE Reason IN ('Evaluation restockable return', 'Evaluation non-restockable return') AND Status = 'Pending') AS PendingReturns;
