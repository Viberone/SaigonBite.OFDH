IF DB_ID('SaigonBiteDB') IS NULL
    CREATE DATABASE SaigonBiteDB;
GO

USE SaigonBiteDB;
GO

DROP TABLE IF EXISTS Vouchers;
GO

CREATE TABLE Vouchers (
    VoucherId      INT IDENTITY(1,1) CONSTRAINT PK_Vouchers PRIMARY KEY,
    Code           NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Vouchers_Code UNIQUE,
    Type           NVARCHAR(12)  NOT NULL CONSTRAINT CK_Vouchers_Type CHECK (Type IN ('Fixed','Percentage')),
    Value          DECIMAL(18,2) NOT NULL CONSTRAINT CK_Vouchers_Value CHECK (Value > 0),
    MaxDiscount    DECIMAL(18,2) NULL     CONSTRAINT CK_Vouchers_Max CHECK (MaxDiscount IS NULL OR MaxDiscount > 0),
    MinOrderValue  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Vouchers_Min DEFAULT 0 CONSTRAINT CK_Vouchers_Min CHECK (MinOrderValue >= 0),
    ValidFrom      DATETIME2     NOT NULL,
    ValidTo        DATETIME2     NOT NULL,
    UsageLimit     INT           NOT NULL CONSTRAINT CK_Vouchers_Limit CHECK (UsageLimit > 0),
    UsedCount      INT           NOT NULL CONSTRAINT DF_Vouchers_Used DEFAULT 0,
    Status         NVARCHAR(12)  NOT NULL CONSTRAINT DF_Vouchers_Status DEFAULT 'Draft'
                   CONSTRAINT CK_Vouchers_Status CHECK (Status IN ('Draft','Active','Expired','Deactivated')),
    CONSTRAINT CK_Vouchers_Dates CHECK (ValidTo >= ValidFrom),
    CONSTRAINT CK_Vouchers_Percent CHECK (Type <> 'Percentage' OR Value <= 100),
    CONSTRAINT CK_Vouchers_UsedCount CHECK (UsedCount >= 0 AND UsedCount <= UsageLimit)
);
GO

INSERT INTO Vouchers (Code, Type, Value, MaxDiscount, MinOrderValue, ValidFrom, ValidTo, UsageLimit, Status)
VALUES ('WELCOME10', 'Fixed',      10000, NULL,  0,      '2026-11-01', '2026-12-31', 1000, 'Active'),
       ('SUMMER10',  'Percentage', 10,    20000, 100000, '2026-11-15', '2027-01-31', 500,  'Draft');
GO