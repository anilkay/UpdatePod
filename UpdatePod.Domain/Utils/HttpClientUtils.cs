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
        cts.CancelAfter(timeout);
        return cts;
    }
}
