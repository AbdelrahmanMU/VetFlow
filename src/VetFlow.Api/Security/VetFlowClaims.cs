namespace VetFlow.Api.Security;

/// <summary>
/// The claim types the access token carries (REQ-IDN-003). They are the <b>only</b> source of the
/// organizational scope — never a header, route value, query string or request body
/// (BR-IDN-004, ADR-0022 §12.5).
/// </summary>
public static class VetFlowClaims
{
    public const string TenantId = "vetflow:tenant_id";
    public const string BranchId = "vetflow:branch_id";
    public const string UserId = "vetflow:user_id";
    public const string DisplayName = "vetflow:display_name";
    public const string Role = "vetflow:role";
}
