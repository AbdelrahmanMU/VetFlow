namespace VetFlow.Domain.Organization;

/// <summary>
/// What a membership permits (BR-ORG-006). A closed set, continuing BD-PRD-003's ruling that
/// the clinic has exactly two roles. <b>No role is enumerated without a use that exists</b> —
/// the same discipline BR-INV-065 applies to movement types.
/// </summary>
public enum MembershipRole
{
    /// <summary>The veterinary doctor who owns the clinic and its business decisions.</summary>
    Owner = 1,

    /// <summary>The cashier/assistant handling day-to-day sales and recording.</summary>
    Cashier = 2,
}
