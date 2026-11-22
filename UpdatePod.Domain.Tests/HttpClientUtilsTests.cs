using UpdatePod.Domain.Utils;

namespace UpdatePod.Domain.Tests;

public class HttpClientUtilsTests
{
    [Fact]
    public async Task CreateTimeoutTokenSource_CancelsAfterTimeout()
    {
        const int cancellationDelayMilliseconds = 100;
        using var cts = HttpClientUtils.CreateTimeoutTokenSource(
            CancellationToken.None, TimeSpan.FromMilliseconds(cancellationDelayMilliseconds));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.Delay(Timeout.InfiniteTimeSpan, cts.Token));
    }

    [Fact]
    public async Task CreateTimeoutTokenSource_CancelsWhenParentIsCancelled()
    {
        const int fallbackTimeoutMinutes = 1;
        using var parent = new CancellationTokenSource();
        using var cts = HttpClientUtils.CreateTimeoutTokenSource(
            parent.Token, TimeSpan.FromMinutes(fallbackTimeoutMinutes));

        parent.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.Delay(Timeout.InfiniteTimeSpan, cts.Token));
    }

    [Fact]
    public void CreateTimeoutTokenSource_ThrowsForNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var cts = HttpClientUtils.CreateTimeoutTokenSource(
                CancellationToken.None, TimeSpan.Zero);
        });
    }
}
