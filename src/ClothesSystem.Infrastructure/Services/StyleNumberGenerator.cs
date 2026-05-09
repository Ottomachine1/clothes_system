using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Services;
using ClothesSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Infrastructure.Services;

public class StyleNumberGenerator : IStyleNumberGenerator
{
    private const string AutoStyleNumberPrefix = "DYYK";
    private const int MonthlyStyleSequenceStart = 10000;
    private const int MaxRetryCount = 3;

    private readonly IApplicationDbContext _dbContext;

    public StyleNumberGenerator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateNextStyleNumberAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var period = now.ToString("yyMM");
        var monthlyPrefix = $"{AutoStyleNumberPrefix}-{period}";

        for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var sequence = await _dbContext.StyleNumberSequences
                    .SingleOrDefaultAsync(item => item.Period == period, cancellationToken);

                if (sequence == null)
                {
                    sequence = new StyleNumberSequence
                    {
                        Period = period,
                        LastSequence = await GetLegacyMonthlyMaxSequenceAsync(monthlyPrefix, cancellationToken)
                    };

                    _dbContext.StyleNumberSequences.Add(sequence);
                }

                sequence.LastSequence++;
                sequence.UpdatedAtUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return $"{monthlyPrefix}{sequence.LastSequence:00000}";
            }
            catch (DbUpdateException) when (attempt < MaxRetryCount)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to generate a new style number after multiple attempts.");
    }

    private async Task<int> GetLegacyMonthlyMaxSequenceAsync(string monthlyPrefix, CancellationToken cancellationToken)
    {
        var matchedStyleNumbers = await _dbContext.ClothingItems
            .AsNoTracking()
            .Where(item => item.StyleNumber.StartsWith(monthlyPrefix))
            .Select(item => item.StyleNumber)
            .ToListAsync(cancellationToken);

        return matchedStyleNumbers
            .Select(styleNumber => TryParseMonthlySequence(styleNumber, monthlyPrefix))
            .Where(sequence => sequence.HasValue)
            .Select(sequence => sequence!.Value)
            .DefaultIfEmpty(MonthlyStyleSequenceStart - 1)
            .Max();
    }

    private static int? TryParseMonthlySequence(string styleNumber, string monthlyPrefix)
    {
        if (!styleNumber.StartsWith(monthlyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = styleNumber[monthlyPrefix.Length..];
        return suffix.Length == 5 && int.TryParse(suffix, out var sequence)
            ? sequence
            : null;
    }
}
