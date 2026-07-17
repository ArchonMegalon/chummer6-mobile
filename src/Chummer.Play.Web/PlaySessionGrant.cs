using Chummer.Play.Core.Application;
using Microsoft.AspNetCore.Http;

namespace Chummer.Play.Web;

/// <summary>
/// Server-authorized context for the private mobile turn-companion document.
/// The public install routes never construct this record from query parameters.
/// </summary>
public sealed record PlaySessionGrant(
    string GrantId,
    string SessionId,
    PlaySurfaceRole Role,
    string? DeviceId);

public static class PlaySessionGrantPolicy
{
    public const string GrantIdHeader = "X-Chummer-Play-Grant-Id";
    public const string SessionIdHeader = "X-Chummer-Play-Grant-Session-Id";
    public const string RoleHeader = "X-Chummer-Play-Grant-Role";
    public const string DeviceIdHeader = "X-Chummer-Play-Grant-Device-Id";

    internal static readonly object HttpContextItemKey = new();

    public static bool TryResolve(
        HttpContext context,
        out PlaySessionGrant? grant,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(context);

        grant = null;
        if (!PlayWebApplication.IsTrustedPlayApiRequest(context))
        {
            error = "trusted_edge_required";
            return false;
        }

        string grantId = context.Request.Headers[GrantIdHeader].ToString().Trim();
        string sessionId = context.Request.Headers[SessionIdHeader].ToString().Trim();
        string roleValue = context.Request.Headers[RoleHeader].ToString().Trim();
        string deviceId = context.Request.Headers[DeviceIdHeader].ToString().Trim();

        if (!IsBoundedIdentifier(grantId, minimumLength: 16, maximumLength: 160))
        {
            error = "invalid_grant_id";
            return false;
        }

        if (!IsBoundedIdentifier(sessionId, minimumLength: 1, maximumLength: 120))
        {
            error = "invalid_session_binding";
            return false;
        }

        if (!Enum.TryParse(roleValue, ignoreCase: true, out PlaySurfaceRole role)
            || !Enum.IsDefined(role))
        {
            error = "invalid_role_binding";
            return false;
        }

        if (deviceId.Length > 0 && !IsBoundedIdentifier(deviceId, minimumLength: 1, maximumLength: 120))
        {
            error = "invalid_device_binding";
            return false;
        }

        grant = new PlaySessionGrant(
            grantId,
            sessionId,
            role,
            deviceId.Length == 0 ? null : deviceId);
        error = string.Empty;
        return true;
    }

    public static PlaySessionGrant? ResolveCurrent(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        return accessor.HttpContext?.Items[HttpContextItemKey] as PlaySessionGrant;
    }

    private static bool IsBoundedIdentifier(string value, int minimumLength, int maximumLength)
        => value.Length >= minimumLength
           && value.Length <= maximumLength
           && value.All(static character =>
               char.IsAsciiLetterOrDigit(character)
               || character is '-' or '_' or '.' or ':');
}
