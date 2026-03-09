namespace BlutdruckErfassungApp.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default);
}
