using VetFlow.Application.Identity;
using VetFlow.Infrastructure.Identity;

namespace VetFlow.Api.Endpoints.Identity;

/// <summary>
/// Sign-in (REQ-IDN-002). The <b>only</b> anonymous endpoint in the system: every other business
/// endpoint requires an authenticated caller (REQ-IDN-006, BR-IDN-005).
///
/// There is no sign-out endpoint. With one access token and no refresh rotation (DEC-IDN-003)
/// and no server-side session, signing out is the client discarding its token — inventing a
/// server call that does nothing would misrepresent what actually happens.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(
                "/api/v1/auth/login",
                async (
                    SignInRequest request,
                    SignInCommandHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

                    // The tenant and branch are inside the token and are deliberately NOT echoed
                    // here: nothing the client holds as a value may ever name its own tenant
                    // (BR-IDN-004, ADR-0022 §12.5).
                    return Results.Ok(new
                    {
                        accessToken = result.AccessToken,
                        expiresAt = result.ExpiresAt,
                        displayName = result.DisplayName,
                    });
                })
            .AllowAnonymous();
    }
}

/// <summary>The sign-in request body (identity/requirements.md, API contract).</summary>
public sealed record SignInRequest
{
    public string? PhoneNumber { get; init; }

    public string? Password { get; init; }

    public SignInCommand ToCommand() => new()
    {
        PhoneNumber = PhoneNumber ?? string.Empty,
        Password = Password ?? string.Empty,
    };
}
