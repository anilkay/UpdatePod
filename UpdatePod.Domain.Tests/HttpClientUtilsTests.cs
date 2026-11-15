using UpdatePod.Domain.Utils;

namespace UpdatePod.Domain.Tests;

public class HttpClientUtilsTests
{
    [Fact]
    public async Task CreateTimeoutTokenSource_CancelsAfterTimeout()
    {
        using var cts = HttpClientUtils.CreateTimeoutTokenSource(
            CancellationToken.None, TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.Delay(Timeout.InfiniteTimeSpan, cts.Token));
    }

    [Fact]
    public async Task CreateTimeoutTokenSource_CancelsWhenParentIsCancelled()
    {
        using var parent = new CancellationTokenSource();
        using var cts = HttpClientUtils.CreateTimeoutTokenSource(
            parent.Token, TimeSpan.FromMinutes(1));

        parent.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.Delay(Timeout.InfiniteTimeSpan, cts.Token));
    }

    [Fact]
    public void CreateTimeoutTokenSource_ThrowsForNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HttpClientUtils.CreateTimeoutTokenSource(CancellationToken.None, TimeSpan.Zero));
    }
}
