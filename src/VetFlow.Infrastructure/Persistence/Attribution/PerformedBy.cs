using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VetFlow.Infrastructure.Persistence.Attribution;

/// <summary>
/// Who performed an operation (REQ-IDN-008, AC-IDN-011, BR-INV-066 as amended 2026-08-02).
///
/// <b>Attribution is derived, not supplied.</b> The rule reads: every movement belongs to the
/// signed-in user, and that is read from the authenticated token's claims <i>exclusively</i>
/// (BR-IDN-004). A parameter on the writer would be a value a caller could get wrong — and the
/// free-text <c>ActorName</c> it supersedes was exactly that.
///
/// It is therefore a <b>shadow property stamped by an interceptor</b>, the same shape the tenant
/// and branch discriminators use and for the same reason (DEC-ORG-011): an inventory movement
/// <i>belongs to</i> a user; it does not <i>reason about</i> one, and no call site should have to
/// carry it.
///
/// <b><c>ActorName</c> is not touched.</b> Values recorded before authentication existed stay
/// readable exactly as they are — they are simply no longer the source of attribution.
/// </summary>
public static class PerformedBy
{
    /// <summary>The shadow property carrying the authenticated performer.</summary>
    public const string UserIdProperty = "PerformedByUserId";

    /// <summary>Annotation marking an entity as attributed, read by the interceptor and by tests.</summary>
    public const string Annotation = "VetFlow:AttributedToUser";

    /// <summary>
    /// Declares that every row of this entity records the authenticated user who wrote it.
    /// </summary>
    public static EntityTypeBuilder<T> AttributedToUser<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property<Guid>(UserIdProperty).IsRequired();
        builder.HasAnnotation(Annotation, true);
        return builder;
    }
}
