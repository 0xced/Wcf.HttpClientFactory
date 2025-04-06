namespace Wcf.HttpClientFactory;

/// <summary>
/// Defines a factory for creating <typeparamref name="TContract"/> instances.
/// </summary>
/// <typeparam name="TContract">The WCF service contract interface.</typeparam>
public interface IContractFactory<TContract>
{
    /// <summary>
    /// Creates a new <typeparamref name="TContract"/> instance.
    /// </summary>
    /// <returns>A new <typeparamref name="TContract"/> instance.</returns>
    TContract CreateContract();

    /// <summary>
    /// Creates a new <typeparamref name="TContract"/> instance in an async context.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
    /// <returns>A task containing the created <typeparamref name="TContract"/> that represents the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
    Task<TContract> CreateContractAsync(CancellationToken cancellationToken = default);
}