namespace Wcf.HttpClientFactory;

/// <summary>
/// Used to produce accurate error messages when only <see cref="ContractConfiguration{TContract}.ConfigureEndpointAsync"/> is overridden and the contract is created synchronously.
/// </summary>
internal enum ContractCreation
{
    /// <summary>
    /// The contract was created by dependency injection.
    /// </summary>
    DependencyInjection,

    /// <summary>
    /// The contract was created by calling the <see cref="IContractFactory{TContract}.CreateContract"/> method explicitly.
    /// </summary>
    FactoryMethod,
}