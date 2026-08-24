using System.Data;
using System.Data.Common;
using System.Globalization;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IndependentApproval.Api.Application.Requests;

public sealed class RequestNumberGenerator(
    IndependentApprovalDbContext dbContext) : IRequestNumberGenerator
{
    private const int MaximumAttempts = 5;

    public async Task<string> GenerateAsync(
        Guid requestTypeVersionId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var definition = await dbContext.RequestTypeVersions
            .AsNoTracking()
            .Where(version => version.Id == requestTypeVersionId)
            .Select(version => new
            {
                version.RequestPrefix,
                version.Lifecycle,
                version.RequestType.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "The request-type version does not exist.");

        if (definition.IsArchived
            || definition.Lifecycle != RequestTypeVersionLifecycle.Published)
        {
            throw new InvalidOperationException(
                "Request numbers can only be generated for a published, non-archived request type.");
        }

        var normalizedPrefix = definition.RequestPrefix.Trim().ToUpperInvariant();
        var year = timestamp.UtcDateTime.Year;
        var sequence = await ReserveSequenceAsync(
            normalizedPrefix,
            year,
            cancellationToken);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{normalizedPrefix}-{year:D4}-{sequence:D6}");
    }

    private async Task<long> ReserveSequenceAsync(
        string normalizedPrefix,
        int year,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var currentTransaction = dbContext.Database.CurrentTransaction;

        if (currentTransaction is not null)
        {
            return await ReserveWithinTransactionAsync(
                connection,
                currentTransaction.GetDbTransaction(),
                normalizedPrefix,
                year,
                cancellationToken);
        }

        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
            {
                try
                {
                    return await ReserveSequenceAttemptAsync(
                        connection,
                        normalizedPrefix,
                        year,
                        cancellationToken);
                }
                catch (SqlException exception)
                    when (attempt < MaximumAttempts
                          && exception.Number is 1205 or 2601 or 2627)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(20 * attempt),
                        cancellationToken);
                }
            }

            throw new InvalidOperationException(
                "A request-number sequence could not be reserved.");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<long> ReserveSequenceAttemptAsync(
        DbConnection connection,
        string normalizedPrefix,
        int year,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var reserved = await ReserveWithinTransactionAsync(
            connection,
            transaction,
            normalizedPrefix,
            year,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return reserved;
    }

    private static async Task<long> ReserveWithinTransactionAsync(
        DbConnection connection,
        DbTransaction transaction,
        string normalizedPrefix,
        int year,
        CancellationToken cancellationToken)
    {
        var reserved = await TryIncrementExistingAsync(
            connection,
            transaction,
            normalizedPrefix,
            year,
            cancellationToken);

        if (reserved is null)
        {
            await InsertSequenceAsync(
                connection,
                transaction,
                normalizedPrefix,
                year,
                cancellationToken);
            reserved = RequestNumberFormat.FirstSequenceValue;
        }

        return reserved.Value;
    }

    private static async Task<long?> TryIncrementExistingAsync(
        DbConnection connection,
        DbTransaction transaction,
        string normalizedPrefix,
        int year,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 30;
        command.CommandText =
            """
            UPDATE [app].[RequestNumberSequences] WITH (UPDLOCK, HOLDLOCK)
            SET [NextValue] = [NextValue] + 1
            OUTPUT DELETED.[NextValue]
            WHERE [NormalizedPrefix] = @prefix AND [Year] = @year;
            """;
        AddParameters(command, normalizedPrefix, year);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull
            ? null
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task InsertSequenceAsync(
        DbConnection connection,
        DbTransaction transaction,
        string normalizedPrefix,
        int year,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 30;
        command.CommandText =
            """
            INSERT INTO [app].[RequestNumberSequences]
                ([NormalizedPrefix], [Year], [NextValue])
            VALUES
                (@prefix, @year, @nextValue);
            """;
        AddParameters(command, normalizedPrefix, year);
        command.Parameters.Add(new SqlParameter(
            "@nextValue",
            SqlDbType.BigInt)
        {
            Value = RequestNumberFormat.FirstSequenceValue + 1
        });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
        DbCommand command,
        string normalizedPrefix,
        int year)
    {
        command.Parameters.Add(new SqlParameter(
            "@prefix",
            SqlDbType.VarChar,
            30)
        {
            Value = normalizedPrefix
        });
        command.Parameters.Add(new SqlParameter(
            "@year",
            SqlDbType.Int)
        {
            Value = year
        });
    }
}
