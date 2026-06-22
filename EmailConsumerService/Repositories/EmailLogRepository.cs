using EmailConsumerService.Configuration;
using Microsoft.Data.SqlClient;

namespace EmailConsumerService.Repositories;

public class EmailLogRepository(DatabaseOptions options) : IEmailLogRepository
{
    private const string UpdateSendGridIdSql =
        "UPDATE tblEmailLog SET SendGridId = @SendGridId WHERE EmailLogId = @EmailLogId;";

    public async Task<int> UpdateSendGridIdAsync(
        int emailLogId,
        string sendGridId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString must be configured.");
        }

        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(UpdateSendGridIdSql, connection);
        command.Parameters.Add(new SqlParameter("@SendGridId", System.Data.SqlDbType.NVarChar)
        {
            Value = sendGridId
        });
        command.Parameters.Add(new SqlParameter("@EmailLogId", System.Data.SqlDbType.Int)
        {
            Value = emailLogId
        });

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
