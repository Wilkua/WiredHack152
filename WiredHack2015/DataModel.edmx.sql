
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
/*
IF OBJECT_ID(N'[dbo].[loadDealer]', 'U') IS NOT NULL
    DROP TABLE [dbo].[loadDealer];
GO
*/
IF OBJECT_ID(N'[dbo].[stgDealer]', 'U') IS NOT NULL
    DROP TABLE [dbo].[stgDealer];
GO
IF OBJECT_ID(N'[dbo].[tblBrand]', 'U') IS NOT NULL
    DROP TABLE [dbo].[tblBrand];
GO
IF OBJECT_ID(N'[dbo].[tblCity]', 'U') IS NOT NULL
    DROP TABLE [dbo].[tblCity];
GO
IF OBJECT_ID(N'[dbo].[tblState]', 'U') IS NOT NULL
    DROP TABLE [dbo].[tblState];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

/*
-- Creating table 'loadDealers'
CREATE TABLE [dbo].[loadDealer] (
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
*/

-- Creating table 'tblBrands'
CREATE TABLE [dbo].[tblBrand] (
    [BrandId] int IDENTITY(1,1) NOT NULL,
    [BrandName] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'tblCities'
CREATE TABLE [dbo].[tblCity] (
    [CityId] int IDENTITY(1,1) NOT NULL,
    [City] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'tblStates'
CREATE TABLE [dbo].[tblState] (
    [StateId] int IDENTITY(1,1) NOT NULL,
    [State] nchar(2)  NOT NULL
);
GO

-- Creating table 'stgDealers'
CREATE TABLE [dbo].[stgDealer] (
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
    [Lat] float  NULL,
    [Long] float  NULL,
    [id] int IDENTITY(1,1) NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

/*
-- Creating primary key on [id] in table 'loadDealers'
ALTER TABLE [dbo].[loadDealers]
ADD CONSTRAINT [PK_loadDealers]
    PRIMARY KEY CLUSTERED ([id] ASC);
GO
*/

-- Creating primary key on [BrandId], [BrandName] in table 'tblBrands'
ALTER TABLE [dbo].[tblBrand]
ADD CONSTRAINT [PK_tblBrand]
    PRIMARY KEY CLUSTERED ([BrandId], [BrandName] ASC);
GO

-- Creating primary key on [CityId], [City] in table 'tblCities'
ALTER TABLE [dbo].[tblCity]
ADD CONSTRAINT [PK_tblCity]
    PRIMARY KEY CLUSTERED ([CityId], [City] ASC);
GO

-- Creating primary key on [StateId], [State] in table 'tblStates'
ALTER TABLE [dbo].[tblState]
ADD CONSTRAINT [PK_tblState]
    PRIMARY KEY CLUSTERED ([StateId], [State] ASC);
GO

-- Creating primary key on [id] in table 'stgDealers'
ALTER TABLE [dbo].[stgDealer]
ADD CONSTRAINT [PK_stgDealer]
    PRIMARY KEY CLUSTERED ([id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- --------------------------------------------------
-- Stored Procedures
-- --------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetMissingRecordsLatAndLong]
AS
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	 -- Insert statements for procedure here
	SELECT [Id],
		[Address1],
		[City],
		[State],
		[PostalCode]
		FROM [dbo].[stgDealer]
--		INNER JOIN [dbo].[tblCity] AS [C] ON [C].[CityId] = [SD].[City]
--		INNER JOIN [dbo].[tblState] AS [S] ON [S].[StateID] = [SD].[State]
		WHERE [Lat] IS NULL OR [Long] IS NULL;
GO

CREATE PROCEDURE [dbo].[UpdateLatandLong]
	@Id int,
	@lat float,
	@lng float
AS
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	 -- Insert statements for procedure here
	UPDATE [dbo].[stgDealer]
	SET [Lat] = @lat,
	[Long] = @lng
	WHERE [id] = @Id;
GO

CREATE FUNCTION [dbo].[CoordinateDistanceMiles]
(
	@Latitude1 float,
	@Longitude1 float,
	@Latitude2 float,
	@Longitude2 float
)
	RETURNS float
AS
	BEGIN
	-- CONSTANTS
	DECLARE @EarthRadiusInMiles float;
	SET @EarthRadiusInMiles = 3963.1
	DECLARE @PI  float;
	SET @PI = PI();
	-- RADIANS conversion
	DECLARE @lat1Radians FLOAT;
	DECLARE @long1Radians FLOAT;
	DECLARE @lat2Radians FLOAT;
	DECLARE @long2Radians FLOAT;
	SET @lat1Radians = @Latitude1 * @PI / 180;
	SET @long1Radians = @Longitude1 * @PI / 180;
	SET @lat2Radians = @Latitude2 * @PI / 180;
	SET @long2Radians = @Longitude2 * @PI / 180;
	RETURN Acos(
		Cos(@lat1Radians) * Cos(@long1Radians) * Cos(@lat2Radians) * Cos(@long2Radians) + 
		Cos(@lat1Radians) * Sin(@long1Radians) * Cos(@lat2Radians) * Sin(@long2Radians) + 
		Sin(@lat1Radians) * Sin(@lat2Radians)
	) * @EarthRadiusInMiles;
END
GO

CREATE PROCEDURE sp_getDealersByLatLong	@distance int, @lat float, @lng float
AS
	SELECT [Id],
		[Address1],
		[Address2],
		[City],
		[State],
		[PostalCode],
		[DealerCode],
		[DealerName],
		[Lat],
		[Long],
		[ManfRegionCode],
		[SignedOn],
		[BrandName]
	FROM [dbo].[stgDealer]
	WHERE [dbo].CoordinateDistanceMiles(@lat, @lng, [lat], [long]) < @distance
GO

CREATE PROCEDURE GetRecordsByBrand
	@Brand nvarchar(50)
AS
	SELECT [Id],
		[BrandName],
		[DealerName],
		[SignedOn],
		[DealerCode],
		[ManfRegionCode],
		[Address1],
		[Address2],
		[City],
		[State],
		[PostalCode],
		[Lat],
		[Long]
	FROM [dbo].[stgDealer]
	WHERE [BrandName] = @Brand;
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------