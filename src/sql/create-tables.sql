USE [updb]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[update_packages]
(
    [id]                [int] IDENTITY(1,1) NOT NULL,  -- The update package's ID.
    [file_name]         [nvarchar](1024) NOT NULL,        -- The update package's file name.
    [file_hash_sha1]    [binary](20) NOT NULL             -- The update package's file hash that computed with SHA1.
)
ON [PRIMARY]
WITH (DATA_COMPRESSION = PAGE)
GO

CREATE TABLE [dbo].[update_modules]
(
    [id]                [int] IDENTITY(1,1) NOT NULL,  -- The update module's ID.
    [file_name]         [nvarchar](1024) NOT NULL,        -- The update module's file name.
    [file_hash_sha1]    [binary](20) NOT NULL             -- The update module's file hash that computed with SHA1.
)
ON [PRIMARY]
WITH (DATA_COMPRESSION = PAGE)
GO
