using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using NextBand.Models;

namespace NextBand.Services;

public sealed class StorageService
{
    private const int PasswordIterations = 210_000;
    private const string DefaultConnectionString = "Server=tcp:192.168.68.111,1433;Database=NextBand;User Id=NextBandApp;Password=N3xtBand!2026#A9p;Encrypt=False;TrustServerCertificate=True;Connection Timeout=8;";
    private readonly string _connectionString = ResolveConnectionString();

    public async Task<AppDataModel> LoadAsync()
    {
        if (!IsConfigured)
        {
            return new AppDataModel();
        }

        await EnsureDatabaseAsync();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var user = await LoadPrimaryUserAsync(connection);
        if (user is null)
        {
            return new AppDataModel();
        }

        var userId = await GetUserIdByEmailAsync(connection, user.Email);
        return await LoadByUserAsync(connection, userId, user);
    }

    public async Task<AppDataModel?> LoadByLoginAsync(string email, string password)
    {
        EnsureConfigured();
        await EnsureDatabaseAsync();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, PasswordHash, PasswordSalt
            FROM Users
            WHERE LOWER(Email) = LOWER(@email);
            """;
        command.Parameters.AddWithValue("@email", email.Trim());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var userId = reader.GetInt32(0);
        var hash = reader.GetString(1);
        var salt = reader.GetString(2);
        if (!VerifyPassword(password, hash, salt))
        {
            return null;
        }

        await reader.DisposeAsync();
        var user = await LoadUserByIdAsync(connection, userId);
        return user is null ? null : await LoadByUserAsync(connection, userId, user);
    }

    public async Task SaveAsync(AppDataModel data)
    {
        EnsureConfigured();
        await EnsureDatabaseAsync();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        var sqlTransaction = (SqlTransaction)transaction;

        var userId = await SaveUserAsync(connection, sqlTransaction, data.User);
        await SaveProfileAsync(connection, sqlTransaction, userId, data);
        await SaveBandAsync(connection, sqlTransaction, userId, data.Band);
        await SaveConnectionsAsync(connection, sqlTransaction, userId, data.Connections);
        await SaveEmergencyAsync(connection, sqlTransaction, userId, data.EmergencyProfile);
        await SaveSettingsAsync(connection, sqlTransaction, userId, data.Band);

        await transaction.CommitAsync();
    }

    public Task DeleteLegacyLocalFileAsync()
    {
        return Task.CompletedTask;
    }

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    private static string ResolveConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("NEXTBAND_SQL_CONNECTION");
        return string.IsNullOrWhiteSpace(configured) ? DefaultConnectionString : configured;
    }

    private SqlConnection CreateConnection()
    {
        EnsureConfigured();
        return new SqlConnection(_connectionString);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Configure a variável de ambiente NEXTBAND_SQL_CONNECTION com a connection string do SQL Server compartilhado.");
        }
    }

    private async Task EnsureDatabaseAsync()
    {
        EnsureConfigured();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        foreach (var sql in Schema)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<AppDataModel> LoadByUserAsync(SqlConnection connection, int userId, UserModel user)
    {
        return new AppDataModel
        {
            User = user,
            Band = await LoadBandAsync(connection, userId),
            EmergencyProfile = await LoadEmergencyProfileAsync(connection, userId),
            Connections = await LoadConnectionsAsync(connection, userId)
        };
    }

    private async Task<UserModel?> LoadPrimaryUserAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 Id FROM Users ORDER BY Id DESC;";
        var id = await command.ExecuteScalarAsync();
        return id is null ? null : await LoadUserByIdAsync(connection, Convert.ToInt32(id));
    }

    private async Task<UserModel?> LoadUserByIdAsync(SqlConnection connection, int userId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.Username, u.FullName, u.Email, u.Phone, p.Instagram, p.LinkedIn,
                   p.Affiliation, p.Age, p.Biography
            FROM Users u
            LEFT JOIN UserProfiles p ON p.UserId = u.Id
            WHERE u.Id = @userId;
            """;
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserModel
        {
            UserName = ReadString(reader, 0),
            FullName = ReadString(reader, 1),
            Email = ReadString(reader, 2),
            Phone = ReadString(reader, 3),
            Instagram = ReadString(reader, 4),
            LinkedIn = ReadString(reader, 5),
            Affiliation = ReadString(reader, 6),
            Age = ReadString(reader, 7),
            Bio = ReadString(reader, 8),
            Password = string.Empty
        };
    }

    private async Task<int> GetUserIdByEmailAsync(SqlConnection connection, string email)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 Id FROM Users WHERE LOWER(Email) = LOWER(@email);";
        command.Parameters.AddWithValue("@email", email);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

    private async Task<int> SaveUserAsync(SqlConnection connection, SqlTransaction transaction, UserModel user)
    {
        var existingId = await GetUserIdByEmailAsync(connection, user.Email);
        if (existingId == 0)
        {
            var (hash, salt) = HashPassword(user.Password);
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO Users (Username, FullName, Email, Phone, PasswordHash, PasswordSalt, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES (@username, @fullName, @email, @phone, @hash, @salt, SYSUTCDATETIME(), SYSUTCDATETIME());
                """;
            insert.Parameters.AddWithValue("@username", user.UserName);
            insert.Parameters.AddWithValue("@fullName", user.FullName);
            insert.Parameters.AddWithValue("@email", user.Email);
            insert.Parameters.AddWithValue("@phone", user.Phone);
            insert.Parameters.AddWithValue("@hash", hash);
            insert.Parameters.AddWithValue("@salt", salt);
            return Convert.ToInt32(await insert.ExecuteScalarAsync());
        }

        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE Users
            SET Username = @username,
                FullName = @fullName,
                Email = @email,
                Phone = @phone,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @id;
            """;
        update.Parameters.AddWithValue("@id", existingId);
        update.Parameters.AddWithValue("@username", user.UserName);
        update.Parameters.AddWithValue("@fullName", user.FullName);
        update.Parameters.AddWithValue("@email", user.Email);
        update.Parameters.AddWithValue("@phone", user.Phone);
        await update.ExecuteNonQueryAsync();

        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            var (hash, salt) = HashPassword(user.Password);
            await using var passwordUpdate = connection.CreateCommand();
            passwordUpdate.Transaction = transaction;
            passwordUpdate.CommandText = "UPDATE Users SET PasswordHash = @hash, PasswordSalt = @salt WHERE Id = @id;";
            passwordUpdate.Parameters.AddWithValue("@id", existingId);
            passwordUpdate.Parameters.AddWithValue("@hash", hash);
            passwordUpdate.Parameters.AddWithValue("@salt", salt);
            await passwordUpdate.ExecuteNonQueryAsync();
        }

        return existingId;
    }

    private async Task SaveProfileAsync(SqlConnection connection, SqlTransaction transaction, int userId, AppDataModel data)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM UserProfiles WHERE UserId = @userId)
                UPDATE UserProfiles
                SET Instagram = @instagram, LinkedIn = @linkedIn, Affiliation = @affiliation,
                    Age = @age, Biography = @bio, IsChildModeEnabled = @childMode
                WHERE UserId = @userId;
            ELSE
                INSERT INTO UserProfiles (UserId, Instagram, LinkedIn, Affiliation, Age, Biography, IsChildModeEnabled)
                VALUES (@userId, @instagram, @linkedIn, @affiliation, @age, @bio, @childMode);
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@instagram", data.User.Instagram);
        command.Parameters.AddWithValue("@linkedIn", data.User.LinkedIn);
        command.Parameters.AddWithValue("@affiliation", data.User.Affiliation);
        command.Parameters.AddWithValue("@age", data.User.Age);
        command.Parameters.AddWithValue("@bio", data.User.Bio);
        command.Parameters.AddWithValue("@childMode", data.Band.ChildMode ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<BandDeviceModel> LoadBandAsync(SqlConnection connection, int userId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 1 DeviceName, IsConnected, OledText, LedEnabled, LedHexColor, QuickLinkType, QuickLinkValue
            FROM BandDevices
            WHERE UserId = @userId;
            """;
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new BandDeviceModel();
        }

        return new BandDeviceModel
        {
            DeviceName = ReadString(reader, 0),
            IsConnected = reader.GetInt32(1) == 1,
            OledText = ReadString(reader, 2),
            LedEnabled = reader.GetInt32(3) == 1,
            LedHexColor = ReadString(reader, 4),
            QuickLinkType = ReadString(reader, 5),
            QuickLink = ReadString(reader, 6)
        };
    }

    private async Task SaveBandAsync(SqlConnection connection, SqlTransaction transaction, int userId, BandDeviceModel band)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM BandDevices WHERE UserId = @userId)
                UPDATE BandDevices
                SET DeviceName = @deviceName, IsConnected = @connected, OledText = @oled,
                    LedEnabled = @ledEnabled, LedHexColor = @ledColor, QuickLinkType = @linkType,
                    QuickLinkValue = @linkValue, LastConnectionAt = @lastConnectionAt
                WHERE UserId = @userId;
            ELSE
                INSERT INTO BandDevices (UserId, DeviceName, IsConnected, OledText, LedEnabled, LedHexColor, QuickLinkType, QuickLinkValue, LastConnectionAt)
                VALUES (@userId, @deviceName, @connected, @oled, @ledEnabled, @ledColor, @linkType, @linkValue, @lastConnectionAt);
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@deviceName", band.DeviceName);
        command.Parameters.AddWithValue("@connected", band.IsConnected ? 1 : 0);
        command.Parameters.AddWithValue("@oled", band.OledText);
        command.Parameters.AddWithValue("@ledEnabled", band.LedEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@ledColor", band.LedHexColor);
        command.Parameters.AddWithValue("@linkType", band.QuickLinkType);
        command.Parameters.AddWithValue("@linkValue", band.QuickLink);
        command.Parameters.AddWithValue("@lastConnectionAt", band.IsConnected ? DateTimeOffset.UtcNow.ToString("O") : string.Empty);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<ObservableCollection<ConnectionModel>> LoadConnectionsAsync(SqlConnection connection, int userId)
    {
        var connections = new ObservableCollection<ConnectionModel>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ConnectedName, ConnectedUsername, ConnectedAgo, IsNew
            FROM Connections
            WHERE UserId = @userId
            ORDER BY Id;
            """;
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            connections.Add(new ConnectionModel
            {
                Name = ReadString(reader, 0),
                UserName = ReadString(reader, 1),
                ConnectedAgo = ReadString(reader, 2),
                IsNew = reader.GetInt32(3) == 1
            });
        }

        return connections;
    }

    private async Task SaveConnectionsAsync(SqlConnection connection, SqlTransaction transaction, int userId, ObservableCollection<ConnectionModel> connections)
    {
        await using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM Connections WHERE UserId = @userId;";
        delete.Parameters.AddWithValue("@userId", userId);
        await delete.ExecuteNonQueryAsync();

        foreach (var item in connections)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Connections (UserId, ConnectedName, ConnectedUsername, ConnectedAgo, ConnectedAt, IsNew)
                VALUES (@userId, @name, @username, @ago, SYSUTCDATETIME(), @isNew);
                """;
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@name", item.Name);
            command.Parameters.AddWithValue("@username", item.UserName);
            command.Parameters.AddWithValue("@ago", item.ConnectedAgo);
            command.Parameters.AddWithValue("@isNew", item.IsNew ? 1 : 0);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<EmergencyProfileModel> LoadEmergencyProfileAsync(SqlConnection connection, int userId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 1 Id, ChildName, Age, BloodType, Guardians, MainEmergencyPhone, Address,
                   Allergies, MedicalConditions, Disabilities, SpecialNeeds, Medications, EmergencyInstructions
            FROM EmergencyProfiles
            WHERE UserId = @userId;
            """;
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new EmergencyProfileModel();
        }

        var profileId = reader.GetInt32(0);
        var profile = new EmergencyProfileModel
        {
            ChildName = ReadString(reader, 1),
            Age = ReadString(reader, 2),
            BloodType = ReadString(reader, 3),
            Guardians = ReadString(reader, 4),
            MainPhone = ReadString(reader, 5),
            Address = ReadString(reader, 6),
            Allergies = ReadString(reader, 7),
            MedicalConditions = ReadString(reader, 8),
            Disabilities = ReadString(reader, 9),
            SpecialNeeds = ReadString(reader, 10),
            Medications = ReadString(reader, 11),
            EmergencyInstructions = ReadString(reader, 12)
        };

        await reader.DisposeAsync();
        profile.ExtraContacts = await LoadEmergencyContactsAsync(connection, profileId);
        profile.CustomInfos = await LoadCustomInfosAsync(connection, profileId);
        return profile;
    }

    private async Task SaveEmergencyAsync(SqlConnection connection, SqlTransaction transaction, int userId, EmergencyProfileModel profile)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM EmergencyProfiles WHERE UserId = @userId)
                UPDATE EmergencyProfiles
                SET ChildName = @childName, Age = @age, BloodType = @bloodType, Guardians = @guardians,
                    MainEmergencyPhone = @phone, Address = @address, Allergies = @allergies,
                    MedicalConditions = @medical, Disabilities = @disabilities, SpecialNeeds = @specialNeeds,
                    Medications = @medications, EmergencyInstructions = @instructions,
                    PublicSlug = @slug, UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @userId;
            ELSE
                INSERT INTO EmergencyProfiles (
                    UserId, ChildName, Age, BloodType, Guardians, MainEmergencyPhone, Address,
                    Allergies, MedicalConditions, Disabilities, SpecialNeeds, Medications, EmergencyInstructions,
                    PublicSlug, IsPublicEnabled, CreatedAt, UpdatedAt)
                VALUES (
                    @userId, @childName, @age, @bloodType, @guardians, @phone, @address,
                    @allergies, @medical, @disabilities, @specialNeeds, @medications, @instructions,
                    @slug, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@childName", profile.ChildName);
        command.Parameters.AddWithValue("@age", profile.Age);
        command.Parameters.AddWithValue("@bloodType", profile.BloodType);
        command.Parameters.AddWithValue("@guardians", profile.Guardians);
        command.Parameters.AddWithValue("@phone", profile.MainPhone);
        command.Parameters.AddWithValue("@address", profile.Address);
        command.Parameters.AddWithValue("@allergies", profile.Allergies);
        command.Parameters.AddWithValue("@medical", profile.MedicalConditions);
        command.Parameters.AddWithValue("@disabilities", profile.Disabilities);
        command.Parameters.AddWithValue("@specialNeeds", profile.SpecialNeeds);
        command.Parameters.AddWithValue("@medications", profile.Medications);
        command.Parameters.AddWithValue("@instructions", profile.EmergencyInstructions);
        command.Parameters.AddWithValue("@slug", Slug(profile.ChildName));
        await command.ExecuteNonQueryAsync();

        var profileId = await GetEmergencyProfileIdAsync(connection, transaction, userId);
        await SaveEmergencyContactsAsync(connection, transaction, profileId, profile.ExtraContacts);
        await SaveCustomInfosAsync(connection, transaction, profileId, profile.CustomInfos);
    }

    private async Task<int> GetEmergencyProfileIdAsync(SqlConnection connection, SqlTransaction transaction, int userId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT TOP 1 Id FROM EmergencyProfiles WHERE UserId = @userId;";
        command.Parameters.AddWithValue("@userId", userId);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

    private async Task<ObservableCollection<EmergencyContactModel>> LoadEmergencyContactsAsync(SqlConnection connection, int profileId)
    {
        var contacts = new ObservableCollection<EmergencyContactModel>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Relationship, Phone, Note FROM EmergencyContacts WHERE EmergencyProfileId = @profileId ORDER BY Id;";
        command.Parameters.AddWithValue("@profileId", profileId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            contacts.Add(new EmergencyContactModel
            {
                Name = ReadString(reader, 0),
                Relation = ReadString(reader, 1),
                Phone = ReadString(reader, 2),
                Note = ReadString(reader, 3)
            });
        }

        return contacts;
    }

    private async Task SaveEmergencyContactsAsync(SqlConnection connection, SqlTransaction transaction, int profileId, ObservableCollection<EmergencyContactModel> contacts)
    {
        await using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM EmergencyContacts WHERE EmergencyProfileId = @profileId;";
        delete.Parameters.AddWithValue("@profileId", profileId);
        await delete.ExecuteNonQueryAsync();

        foreach (var contact in contacts)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO EmergencyContacts (EmergencyProfileId, Name, Relationship, Phone, Note, CreatedAt)
                VALUES (@profileId, @name, @relationship, @phone, @note, SYSUTCDATETIME());
                """;
            command.Parameters.AddWithValue("@profileId", profileId);
            command.Parameters.AddWithValue("@name", contact.Name);
            command.Parameters.AddWithValue("@relationship", contact.Relation);
            command.Parameters.AddWithValue("@phone", contact.Phone);
            command.Parameters.AddWithValue("@note", contact.Note);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<ObservableCollection<CustomInfoModel>> LoadCustomInfosAsync(SqlConnection connection, int profileId)
    {
        var infos = new ObservableCollection<CustomInfoModel>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Title, Description FROM AdditionalInformation WHERE EmergencyProfileId = @profileId ORDER BY Id;";
        command.Parameters.AddWithValue("@profileId", profileId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            infos.Add(new CustomInfoModel
            {
                Title = ReadString(reader, 0),
                Description = ReadString(reader, 1)
            });
        }

        return infos;
    }

    private async Task SaveCustomInfosAsync(SqlConnection connection, SqlTransaction transaction, int profileId, ObservableCollection<CustomInfoModel> infos)
    {
        await using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM AdditionalInformation WHERE EmergencyProfileId = @profileId;";
        delete.Parameters.AddWithValue("@profileId", profileId);
        await delete.ExecuteNonQueryAsync();

        foreach (var info in infos)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO AdditionalInformation (EmergencyProfileId, Title, Description, CreatedAt)
                VALUES (@profileId, @title, @description, SYSUTCDATETIME());
                """;
            command.Parameters.AddWithValue("@profileId", profileId);
            command.Parameters.AddWithValue("@title", info.Title);
            command.Parameters.AddWithValue("@description", info.Description);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task SaveSettingsAsync(SqlConnection connection, SqlTransaction transaction, int userId, BandDeviceModel band)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM AppSettings WHERE UserId = @userId)
                UPDATE AppSettings
                SET NotificationsEnabled = @notifications, LocationEnabled = @location
                WHERE UserId = @userId;
            ELSE
                INSERT INTO AppSettings (UserId, NotificationsEnabled, LocationEnabled, Theme, Language)
                VALUES (@userId, @notifications, @location, 'Light', 'pt-BR');
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@notifications", band.NotificationsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@location", band.LocationEnabled ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    private static string ReadString(SqlDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, PasswordIterations, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var attemptedHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, PasswordIterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(attemptedHash, Convert.FromBase64String(storedHash));
    }

    private static string Slug(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Guid.NewGuid().ToString("N")
            : value.Trim().ToLowerInvariant().Replace(' ', '-');
    }

    private static readonly string[] Schema =
    [
        """
        IF OBJECT_ID('Users', 'U') IS NULL
        CREATE TABLE Users (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Username NVARCHAR(80) NOT NULL UNIQUE,
            FullName NVARCHAR(160) NULL,
            Email NVARCHAR(180) NOT NULL UNIQUE,
            Phone NVARCHAR(60) NULL,
            PasswordHash NVARCHAR(200) NOT NULL,
            PasswordSalt NVARCHAR(200) NOT NULL,
            AvatarPath NVARCHAR(500) NULL,
            CreatedAt DATETIME2 NOT NULL,
            UpdatedAt DATETIME2 NULL
        );
        """,
        """
        IF OBJECT_ID('UserProfiles', 'U') IS NULL
        CREATE TABLE UserProfiles (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NOT NULL UNIQUE,
            Instagram NVARCHAR(200) NULL,
            LinkedIn NVARCHAR(300) NULL,
            Affiliation NVARCHAR(200) NULL,
            Age NVARCHAR(40) NULL,
            Biography NVARCHAR(MAX) NULL,
            IsChildModeEnabled BIT DEFAULT 0,
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        );
        """,
        """
        IF OBJECT_ID('BandDevices', 'U') IS NULL
        CREATE TABLE BandDevices (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NOT NULL UNIQUE,
            DeviceName NVARCHAR(120) NULL,
            DeviceAddress NVARCHAR(160) NULL,
            IsConnected BIT DEFAULT 0,
            LastConnectionAt NVARCHAR(80) NULL,
            OledText NVARCHAR(80) NULL,
            LedEnabled BIT DEFAULT 0,
            LedHexColor NVARCHAR(20) NULL,
            QuickLinkType NVARCHAR(40) NULL,
            QuickLinkValue NVARCHAR(500) NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        );
        """,
        """
        IF OBJECT_ID('Connections', 'U') IS NULL
        CREATE TABLE Connections (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NOT NULL,
            ConnectedName NVARCHAR(160) NOT NULL,
            ConnectedUsername NVARCHAR(120) NULL,
            ConnectedProfileUrl NVARCHAR(500) NULL,
            ConnectedAt DATETIME2 NOT NULL,
            ConnectedAgo NVARCHAR(80) NULL,
            IsNew BIT DEFAULT 1,
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        );
        """,
        """
        IF OBJECT_ID('EmergencyProfiles', 'U') IS NULL
        CREATE TABLE EmergencyProfiles (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NOT NULL UNIQUE,
            ChildName NVARCHAR(160) NULL,
            Age NVARCHAR(40) NULL,
            BloodType NVARCHAR(10) NULL,
            Guardians NVARCHAR(MAX) NULL,
            MainEmergencyPhone NVARCHAR(80) NULL,
            Address NVARCHAR(MAX) NULL,
            Allergies NVARCHAR(MAX) NULL,
            MedicalConditions NVARCHAR(MAX) NULL,
            Disabilities NVARCHAR(MAX) NULL,
            SpecialNeeds NVARCHAR(MAX) NULL,
            Medications NVARCHAR(MAX) NULL,
            EmergencyInstructions NVARCHAR(MAX) NULL,
            PublicSlug NVARCHAR(220) UNIQUE,
            IsPublicEnabled BIT DEFAULT 1,
            CreatedAt DATETIME2 NOT NULL,
            UpdatedAt DATETIME2 NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        );
        """,
        """
        IF OBJECT_ID('EmergencyContacts', 'U') IS NULL
        CREATE TABLE EmergencyContacts (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            EmergencyProfileId INT NOT NULL,
            Name NVARCHAR(160) NOT NULL,
            Relationship NVARCHAR(120) NULL,
            Phone NVARCHAR(80) NOT NULL,
            Note NVARCHAR(MAX) NULL,
            CreatedAt DATETIME2 NOT NULL,
            FOREIGN KEY (EmergencyProfileId) REFERENCES EmergencyProfiles(Id)
        );
        """,
        """
        IF OBJECT_ID('AdditionalInformation', 'U') IS NULL
        CREATE TABLE AdditionalInformation (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            EmergencyProfileId INT NOT NULL,
            Title NVARCHAR(160) NOT NULL,
            Description NVARCHAR(MAX) NULL,
            CreatedAt DATETIME2 NOT NULL,
            FOREIGN KEY (EmergencyProfileId) REFERENCES EmergencyProfiles(Id)
        );
        """,
        """
        IF OBJECT_ID('AppSettings', 'U') IS NULL
        CREATE TABLE AppSettings (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NOT NULL UNIQUE,
            NotificationsEnabled BIT DEFAULT 1,
            LocationEnabled BIT DEFAULT 0,
            Theme NVARCHAR(40) DEFAULT 'Light',
            Language NVARCHAR(20) DEFAULT 'pt-BR',
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        );
        """
    ];
}
