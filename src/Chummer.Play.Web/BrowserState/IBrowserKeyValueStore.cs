namespace Chummer.Play.Web.BrowserState;

public interface IBrowserKeyValueStore
{
    Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default);
}
