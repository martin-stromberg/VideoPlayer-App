#if DEBUG || DEV_API
namespace msTools.DeveloperApi;

/// <summary>
/// Handler-Schnittstelle der Developer-API: signatur eines Endpunkt-
/// Handlers, der eine eingelesene Anfrage in eine Antwort überführt.
/// Wird über <see cref="DevApiRouter.Map"/> registriert.
/// </summary>
/// <param name="request">Die eingehende Anfrage.</param>
/// <param name="cancellationToken">Abbruchtoken.</param>
/// <returns>Die zu versendende Antwort.</returns>
public delegate Task<DevApiResponse> DevApiEndpointHandler(DevApiRequest request, CancellationToken cancellationToken);
#endif
