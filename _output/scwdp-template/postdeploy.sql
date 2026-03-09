DECLARE @applicationId nvarchar(256)
SELECT TOP 1 @applicationId = [ApplicationId] FROM [aspnet_Applications] WHERE [ApplicationName] = 'sitecore'
IF NOT EXISTS (SELECT TOP 1 [RoleId] FROM [aspnet_Roles] WHERE [ApplicationId] = @applicationId AND [RoleName] = 'sitecore\PowerShell Extensions Remoting')
BEGIN
    INSERT INTO [aspnet_Roles] (ApplicationId, RoleId, RoleName, LoweredRoleName, Description)
    VALUES (@applicationId, NEWID(), 'sitecore\PowerShell Extensions Remoting', LOWER('sitecore\PowerShell Extensions Remoting'), NULL)
END
GO
IF EXISTS(SELECT * FROM   sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[base64_encode]')
AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT' ))
DROP FUNCTION[dbo].[base64_encode]
GO
CREATE FUNCTION[dbo].[base64_encode] (@data VARBINARY(MAX)) RETURNS VARCHAR(MAX)
BEGIN
RETURN(SELECT[text()] = @data FOR XML PATH('') )
END
GO
DECLARE @UnencodedSalt uniqueidentifier
DECLARE @Password nvarchar(128)
DECLARE @PasswordClear uniqueidentifier
DECLARE @PasswordSalt nvarchar(128)
SET @UnencodedSalt = NEWID()
SET @Password = NEWID()
SET @PasswordClear = NEWID()
SET @PasswordSalt = dbo.base64_encode(@UnencodedSalt)
SET @Password = dbo.base64_encode(HASHBYTES('SHA1', CAST(@PasswordClear as varbinary(MAX))))
DECLARE @applicationId nvarchar(256)
SELECT TOP 1 @applicationId = [ApplicationId] FROM [aspnet_Applications] WHERE [ApplicationName] = 'sitecore'
IF NOT EXISTS (SELECT TOP 1 [UserId] FROM [aspnet_Users] WHERE [ApplicationId] = @applicationId AND [UserName] = 'sitecore\PowerShellExtensionsAPI')
BEGIN
    INSERT INTO [aspnet_Users] (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
    VALUES (@applicationId, NEWID(), 'sitecore\PowerShellExtensionsAPI', LOWER('sitecore\PowerShellExtensionsAPI'), NULL, 0, GETUTCDATE())
END
DECLARE @UserId nvarchar(256)
SELECT TOP 1 @UserId =  [UserId] FROM
[aspnet_Users] WHERE[ApplicationId] = @applicationId AND[UserName] = 'sitecore\PowerShellExtensionsAPI'
DECLARE @Users nvarchar(256)
SELECT TOP 1 @Users = [RoleId] FROM[aspnet_Roles] WHERE[ApplicationId] = @applicationId AND[RoleName] = 'sitecore\PowerShell Extensions Remoting'
IF NOT EXISTS(SELECT TOP 1 * FROM[aspnet_UsersInRoles] WHERE[UserId] = @UserId AND[RoleId] = @Users)
BEGIN
INSERT INTO[aspnet_UsersInRoles](UserId, RoleId)
VALUES(@UserId, @Users)
END
IF NOT EXISTS(SELECT TOP 1 [UserId] FROM[aspnet_Membership] WHERE[ApplicationId] = @applicationId AND[UserId] = @UserId)
BEGIN
INSERT INTO[dbo].[aspnet_Membership]
([ApplicationId],[UserId],[Password],[PasswordFormat],[PasswordSalt],[MobilePIN],[Email],[LoweredEmail],[PasswordQuestion],[PasswordAnswer],[IsApproved],[IsLockedOut],[CreateDate],[LastLoginDate],[LastPasswordChangedDate],[LastLockoutDate],[FailedPasswordAttemptCount],[FailedPasswordAttemptWindowStart],[FailedPasswordAnswerAttemptCount],[FailedPasswordAnswerAttemptWindowStart],[Comment])
VALUES
(@applicationId, @UserId, @Password, 1, @PasswordSalt, NULL, 'no_reply@sitecore.net', 'no_reply@sitecore.net', NULL, NULL, 0, 0, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0, GETUTCDATE(), 0, GETUTCDATE(), NULL)
END
GO
DECLARE @applicationId nvarchar(256)
SELECT TOP 1 @applicationId = [ApplicationId] FROM[aspnet_Applications] WHERE[ApplicationName] = 'sitecore'
DECLARE @UserId nvarchar(256)
SELECT TOP 1 @UserId = [UserId] FROM[aspnet_Users] WHERE[ApplicationId] = @applicationId AND[UserName] = 'sitecore\PowerShellExtensionsAPI'
IF NOT EXISTS(SELECT TOP 1[UserId] FROM[aspnet_Profile] WHERE[UserId] = @UserId)
BEGIN
INSERT INTO[dbo].[aspnet_Profile]
([UserId],[PropertyNames],[PropertyValuesString],[PropertyValuesBinary],[LastUpdatedDate])
VALUES
(@UserId, 'IsAdministrator:S:0:5:','False', CAST('False' AS VARBINARY(MAX)), GETDATE())
END
GO
