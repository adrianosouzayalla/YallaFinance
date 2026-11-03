using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Sockets;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Transport;

internal sealed class FinancialHttpEvidenceHandler(
    string directory,
    HttpMessageHandler innerHandler,
    bool allowRepeatedWrites = false) : DelegatingHandler(innerHandler)
{
    public string DirectoryPath => directory;
    private int requestSequence;
    private readonly HashSet<string> writeEndpoints = new(StringComparer.Ordinal);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        if (request.Method != HttpMethod.Get && !allowRepeatedWrites)
        {
            lock (writeEndpoints)
                if (!writeEndpoints.Add(request.Method.Method + " " + request.RequestUri?.AbsoluteUri))
                    throw new IOException("A write to this endpoint was already attempted in this POC execution.");
        }
        var sequence = Interlocked.Increment(ref requestSequence);
        var suffix = sequence == 1 ? "" : "." + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // CreateNew also prevents accidentally replaying a POC whose evidence already exists.
        await using (var file = new FileStream(
            Path.Combine(directory, "request" + suffix + ".json"),
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            await JsonSerializer.SerializeAsync(
                file,
                new
                {
                    capturedAtUtc = DateTimeOffset.UtcNow,
                    traceId = Activity.Current?.TraceId.ToString(),
                    method = request.Method.Method,
                    url = request.RequestUri?.AbsoluteUri,
                    apiToken = "[REDACTED]",
                    contentType = request.Content?.Headers.ContentType?.ToString(),
                    body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)
                },
                cancellationToken: cancellationToken);
        }
        HttpResponseMessage response;
        var elapsed = Stopwatch.StartNew();
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            try
            {
                await using var file = new FileStream(
                    Path.Combine(directory, "error" + suffix + ".json"),
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);
                await JsonSerializer.SerializeAsync(
                    file,
                    new
                    {
                        capturedAtUtc = DateTimeOffset.UtcNow,
                        traceId = Activity.Current?.TraceId.ToString(),
                        method = request.Method.Method,
                        url = request.RequestUri?.AbsoluteUri,
                        durationMs = elapsed.ElapsedMilliseconds,
                        exceptionType = exception.GetType().FullName,
                        innerExceptionType = exception.InnerException?.GetType().FullName,
                        exception.HResult,
                        transportError = (exception as HttpRequestException)?.HttpRequestError.ToString(),
                        socketError = (exception.InnerException as SocketException)?.SocketErrorCode.ToString(),
                        cancellationRequested = cancellationToken.IsCancellationRequested
                    },
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception captureError) when (captureError is IOException or UnauthorizedAccessException) { }
            throw;
        }
        try
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await using var file = new FileStream(
                Path.Combine(directory, "response" + suffix + ".json"),
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await JsonSerializer.SerializeAsync(file, new
            {
                capturedAtUtc = DateTimeOffset.UtcNow,
                traceId = Activity.Current?.TraceId.ToString(),
                statusCode = (int)response.StatusCode,
                retryAfter = response.Headers.RetryAfter?.ToString(),
                reasonPhrase = response.ReasonPhrase,
                contentType = response.Content.Headers.ContentType?.ToString(),
                body = Encoding.UTF8.GetString(bytes),
                bodyBase64 = Convert.ToBase64String(bytes)
            }, cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            response.Dispose();
            throw new HttpRequestException(
                "Failed to record response evidence after the request was sent.",
                exception);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
