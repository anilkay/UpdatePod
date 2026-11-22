namespace UpdatePod.Domain.Utils;

public static class HttpClientUtils
{
    /// <summary>
    /// Creates a linked token source that is cancelled when <paramref name="cancellationToken"/>
    /// is cancelled or after <paramref name="timeout"/>. The caller owns and must dispose it.
    /// </summary>
    public static CancellationTokenSource CreateTimeoutTokenSource(CancellationToken cancellationToken, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            cts.CancelAfter(timeout);
        }
        catch
        {
            // CancelAfter throws for timeouts beyond the timer range; dispose the
            // linked source so its registration on the input token is released.
            cts.Dispose();
            throw;
        }

        return cts;
    }
}
