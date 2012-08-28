USE [master];
IF not EXISTS(SELECT * FROM sys.sysdatabases where name='ConcurrencyTest')
begin
   CREATE DATABASE [ConcurrencyTest];
end
GO
USE [ConcurrencyTest];
create table [Down]
(
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [UserName] [nvarchar](50) NOT NULL,
    [CreateTime] [datetime] NOT NULL default(GetDATE())
)
GO