IF DB_ID('SaigonBiteDB') IS NULL
    CREATE DATABASE SaigonBiteDB;
GO

USE SaigonBiteDB;
GO

CREATE TABLE Vouchers (
    VoucherId       INT IDENTITY(1,1) CONSTRAINT PK_Vouchers PRIMARY KEY,
    Code            NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Vouchers_Code UNIQUE,
    DiscountAmount  DECIMAL(18,2) NOT NULL CONSTRAINT CK_Vouchers_Discount CHECK (DiscountAmount > 0),
    ValidFrom       DATETIME2     NOT NULL,
    ValidTo         DATETIME2     NOT NULL,
    IsActive        BIT           NOT NULL CONSTRAINT DF_Vouchers_IsActive DEFAULT 1,
    CONSTRAINT CK_Vouchers_Dates CHECK (ValidTo >= ValidFrom)
);
GO

INSERT INTO Vouchers (Code, DiscountAmount, ValidFrom, ValidTo)
VALUES ('WELCOME10', 10000, '2026-11-01', '2026-12-31');
GO