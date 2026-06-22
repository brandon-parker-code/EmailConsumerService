namespace EmailConsumerService.Repositories;

public interface IEmailLogRepository
{
    /// <summary>
    /// Updates the SendGridId column of the tblEmailLog record identified by <paramref name="emailLogId"/>.
    /// </summary>
    /// <returns>The number of rows affected.</returns>
    Task<int> UpdateSendGridIdAsync(
        int emailLogId,
        string sendGridId,
        CancellationToken cancellationToken = default);
}
