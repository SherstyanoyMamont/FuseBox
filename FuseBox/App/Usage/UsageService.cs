using FuseBox.App.DataBase;
using FuseBox.App.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Usage
{
    /// <summary>
    /// Commercial usage metering. This is intentionally separate from ASP.NET
    /// rate limiting: rate limits protect infrastructure; usage counters enforce
    /// plan entitlements such as Free/Pro daily AI allowance.
    /// </summary>
    public sealed class UsageService
    {
        public const string AiRequestMetric = "ai_request";

        private readonly AppDbContext _context;
        private readonly EntitlementService _entitlements;

        public UsageService(
            AppDbContext context,
            EntitlementService entitlements)
        {
            _context = context;
            _entitlements = entitlements;
        }

        public async Task<UsageSnapshot> GetAiUsageAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var limit = await _entitlements.GetLimitAsync(
                userId,
                EntitlementService.AiRequestsPerDayLimit,
                cancellationToken) ?? 0;

            var periodStartUtc = DateTime.UtcNow.Date;

            var used = await _context.UsageCounters
                .AsNoTracking()
                .Where(counter =>
                    counter.UserId == userId &&
                    counter.Metric == AiRequestMetric &&
                    counter.PeriodStartUtc == periodStartUtc)
                .Select(counter => counter.Count)
                .FirstOrDefaultAsync(cancellationToken);

            return BuildSnapshot(used, limit, periodStartUtc);
        }

        public async Task<UsageConsumeResult> TryConsumeAiRequestAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var limit = await _entitlements.GetLimitAsync(
                userId,
                EntitlementService.AiRequestsPerDayLimit,
                cancellationToken) ?? 0;

            var periodStartUtc = DateTime.UtcNow.Date;

            if (limit <= 0)
            {
                return new UsageConsumeResult(
                    false,
                    BuildSnapshot(0, limit, periodStartUtc));
            }

            // The composite primary key makes row creation idempotent. MySQL's
            // ON DUPLICATE KEY UPDATE avoids an application-level race when the
            // first requests of the day arrive concurrently.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO `UsageCounters`
                    (`UserId`, `Metric`, `PeriodStartUtc`, `Count`)
                VALUES
                    ({userId}, {AiRequestMetric}, {periodStartUtc}, 0)
                ON DUPLICATE KEY UPDATE `UserId` = `UserId`;
                """,
                cancellationToken);

            // This conditional UPDATE is the quota gate. The increment and the
            // Count < limit check happen atomically in MySQL, so concurrent
            // requests cannot both consume the final slot.
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE `UsageCounters`
                SET `Count` = `Count` + 1
                WHERE `UserId` = {userId}
                  AND `Metric` = {AiRequestMetric}
                  AND `PeriodStartUtc` = {periodStartUtc}
                  AND `Count` < {limit};
                """,
                cancellationToken);

            var used = await _context.UsageCounters
                .AsNoTracking()
                .Where(counter =>
                    counter.UserId == userId &&
                    counter.Metric == AiRequestMetric &&
                    counter.PeriodStartUtc == periodStartUtc)
                .Select(counter => counter.Count)
                .SingleAsync(cancellationToken);

            var snapshot = BuildSnapshot(used, limit, periodStartUtc);

            return new UsageConsumeResult(
                affected > 0,
                snapshot);
        }

        public async Task ResetAiUsageAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var periodStartUtc = DateTime.UtcNow.Date;

            await _context.UsageCounters
                .Where(counter =>
                    counter.UserId == userId &&
                    counter.Metric == AiRequestMetric &&
                    counter.PeriodStartUtc == periodStartUtc)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static UsageSnapshot BuildSnapshot(
            int used,
            int limit,
            DateTime periodStartUtc)
        {
            var safeLimit = Math.Max(0, limit);
            var safeUsed = Math.Max(0, used);

            return new UsageSnapshot(
                Used: safeUsed,
                Limit: safeLimit,
                Remaining: Math.Max(0, safeLimit - safeUsed),
                ResetAtUtc: DateTime.SpecifyKind(
                    periodStartUtc.AddDays(1),
                    DateTimeKind.Utc));
        }
    }
}
