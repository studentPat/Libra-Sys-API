using Dapper;
using LibraSys.Api.Configuration;
using LibraSys.Api.Data;
using Microsoft.Extensions.Options;

namespace LibraSys.Api.Features.Auth;

public sealed class AuthRepository(IDbConnectionFactory connectionFactory, IOptions<DatabaseOptions> options) : IAuthRepository
{
    private readonly string _encryptionKey = options.Value.EncryptionKey;

    public async Task<RegisteredMember?> RegisterMemberAsync(
        RegisterRequest request, string passwordHash, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string roleSql = "SELECT role_id FROM roles WHERE role_name = 'Member';";
            var roleId = await connection.QuerySingleOrDefaultAsync<long?>(
                new CommandDefinition(roleSql, transaction: transaction, cancellationToken: cancellationToken));
            if (roleId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string userSql = """
                INSERT INTO users (role_id, username, password_hash, status)
                VALUES (@RoleId, @Username, @PasswordHash, 'active');
                SELECT LAST_INSERT_ID();
                """;
            var userId = await connection.QuerySingleAsync<long>(new CommandDefinition(userSql,
                new { RoleId = roleId.Value, request.Username, PasswordHash = passwordHash },
                transaction: transaction, cancellationToken: cancellationToken));

            const string memberSql = """
                INSERT INTO members (user_id, first_name, last_name, email, contact_info_ciphertext, member_status)
                VALUES (@UserId, @FirstName, @LastName, @Email, AES_ENCRYPT(@ContactInfo, UNHEX(SHA2(@EncryptionKey, 256))), 'active');
                SELECT LAST_INSERT_ID();
                """;
            var memberId = await connection.QuerySingleAsync<long>(new CommandDefinition(memberSql,
                new { UserId = userId, request.FirstName, request.LastName, request.Email, request.ContactInfo, EncryptionKey = _encryptionKey },
                transaction: transaction, cancellationToken: cancellationToken));

            const string auditSql = """
                INSERT INTO transaction_logs (user_id, action, entity_type, entity_id, details)
                VALUES (@UserId, 'register_member', 'member', @MemberId,
                        JSON_OBJECT('username', @Username));
                """;
            await connection.ExecuteAsync(new CommandDefinition(auditSql,
                new { UserId = userId, MemberId = memberId, request.Username },
                transaction: transaction, cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return new RegisteredMember(memberId, request.Username);
        }
        catch (MySqlConnector.MySqlException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AuthUser?> FindActiveUserAsync(
        string username, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CAST(u.user_id AS SIGNED) AS UserId,
                   u.username AS Username,
                   u.password_hash AS PasswordHash,
                   r.role_name AS RoleName,
                   CAST(u.failed_login_count AS SIGNED) AS FailedLoginCount,
                   u.locked_until AS LockedUntil
            FROM users u
            INNER JOIN roles r ON r.role_id = u.role_id
            WHERE u.username = @Username
              AND u.status = 'active';
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(
            new CommandDefinition(sql, new { Username = username },
                cancellationToken: cancellationToken));
    }

    public async Task RecordFailedLoginAsync(
        long userId, int failedLoginCount, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE users
            SET failed_login_count = @FailedLoginCount,
                locked_until = CASE
                    WHEN @FailedLoginCount >= 5
                    THEN DATE_ADD(CURRENT_TIMESTAMP, INTERVAL 15 MINUTE)
                    ELSE locked_until
                END
            WHERE user_id = @UserId
              AND status = 'active';
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId, FailedLoginCount = failedLoginCount },
            cancellationToken: cancellationToken));
    }

    public async Task ResetFailedLoginsAsync(
        long userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE users
            SET failed_login_count = 0,
                locked_until = NULL
            WHERE user_id = @UserId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId }, cancellationToken: cancellationToken));
    }
}

