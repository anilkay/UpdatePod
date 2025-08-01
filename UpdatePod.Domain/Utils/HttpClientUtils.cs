namespace UpdatePod.Domain.Utils;

public static  class HttpClientUtils
{
    public static  CancellationToken GenerateCancellationTokenWithTimeout(CancellationToken cancellationToken, TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;
    }
}