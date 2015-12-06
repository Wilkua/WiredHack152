
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 11/30/2015 21:34:52
-- Generated from EDMX file: C:\Users\badwolf\Source\Workspaces\bensworkspace\WiredHack2015\WiredHack2015\WiredHack2015\DataModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [WiredHack];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[WiredHackModelStoreContainer].[loadDealer]', 'U') IS NOT NULL
    DROP TABLE [WiredHackModelStoreContainer].[loadDealer];
GO
IF OBJECT_ID(N'[WiredHackModelStoreContainer].[stgDealer]', 'U') IS NOT NULL
    DROP TABLE [WiredHackModelStoreContainer].[stgDealer];
GO
IF OBJECT_ID(N'[WiredHackModelStoreContainer].[tblBrand]', 'U') IS NOT NULL
    DROP TABLE [WiredHackModelStoreContainer].[tblBrand];
GO
IF OBJECT_ID(N'[WiredHackModelStoreContainer].[tblCity]', 'U') IS NOT NULL
    DROP TABLE [WiredHackModelStoreContainer].[tblCity];
GO
IF OBJECT_ID(N'[WiredHackModelStoreContainer].[tblState]', 'U') IS NOT NULL
    DROP TABLE [WiredHackModelStoreContainer].[tblState];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'loadDealers'
CREATE TABLE [dbo].[loadDealers] (
    [BrandName] nvarchar(50)  NULL,
    [DealerName] nvarchar(50)  NULL,
    [SignedOn] datetime  NULL,
    [DealerCode] nvarchar(50)  NULL,
    [ManfRegionCode] nvarchar(50)  NULL,
    [Address1] nvarchar(50)  NULL,
    [Address2] nvarchar(50)  NULL,
    [City] nvarchar(50)  NULL,
    [State] nchar(2)  NULL,
    [PostalCode] nvarchar(9)  NULL,
    [Lat] decimal(10,6)  NULL,
    [Long] decimal(10,6)  NULL,
    [id] int IDENTITY(1,1) NOT NULL
);
GO

-- Creating table 'tblBrands'
CREATE TABLE [dbo].[tblBrands] (
    [BrandId] int IDENTITY(1,1) NOT NULL,
    [BrandName] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'tblCities'
CREATE TABLE [dbo].[tblCities] (
    [CityId] int IDENTITY(1,1) NOT NULL,
    [City] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'tblStates'
CREATE TABLE [dbo].[tblStates] (
    [StateId] int IDENTITY(1,1) NOT NULL,
    [State] nchar(2)  NOT NULL
);
GO

-- Creating table 'stgDealers'
CREATE TABLE [dbo].[stgDealers] (
    [BrandName] varchar(50)  NULL,
    [DealerName] varchar(50)  NULL,
    [SignedOn] datetime  NULL,
    [DealerCode] varchar(50)  NULL,
    [ManfRegionCode] varchar(50)  NULL,
    [Address1] varchar(50)  NULL,
    [Address2] varchar(50)  NULL,
    [City] varchar(50)  NULL,
    [State] char(2)  NULL,
    [PostalCode] varchar(9)  NULL,
    [Lat] float  NULL,
    [Long] float  NULL,
    [id] int IDENTITY(1,1) NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [id] in table 'loadDealers'
ALTER TABLE [dbo].[loadDealers]
ADD CONSTRAINT [PK_loadDealers]
    PRIMARY KEY CLUSTERED ([id] ASC);
GO

-- Creating primary key on [BrandId], [BrandName] in table 'tblBrands'
ALTER TABLE [dbo].[tblBrands]
ADD CONSTRAINT [PK_tblBrands]
    PRIMARY KEY CLUSTERED ([BrandId], [BrandName] ASC);
GO

-- Creating primary key on [CityId], [City] in table 'tblCities'
ALTER TABLE [dbo].[tblCities]
ADD CONSTRAINT [PK_tblCities]
    PRIMARY KEY CLUSTERED ([CityId], [City] ASC);
GO

-- Creating primary key on [StateId], [State] in table 'tblStates'
ALTER TABLE [dbo].[tblStates]
ADD CONSTRAINT [PK_tblStates]
    PRIMARY KEY CLUSTERED ([StateId], [State] ASC);
GO

-- Creating primary key on [id] in table 'stgDealers'
ALTER TABLE [dbo].[stgDealers]
ADD CONSTRAINT [PK_stgDealers]
    PRIMARY KEY CLUSTERED ([id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------