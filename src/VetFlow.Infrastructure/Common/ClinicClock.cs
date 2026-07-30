using Microsoft.Extensions.Options;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Common;

/// <summary>
/// The clinic local date (BR-INV-059, BR-INV-060): the current instant from the injected
/// <see cref="TimeProvider"/>, converted into the <b>one configured clinic time zone</b> and
/// reduced to a date. Never the UTC date, never the server's local date, never the caller's.
///
/// The time zone is resolved once at construction; an invalid id has already been rejected at
/// startup by the options validation, so there is no fallback path here — by design
/// (BR-INV-060: absence of configuration must not be answered with UTC).
/// </summary>
public sealed class ClinicClock : IClinicClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _clinicTimeZone;

    public ClinicClock(TimeProvider timeProvider, IOptions<ClinicTimeOptions> options)
    {
        _timeProvider = timeProvider;
        _clinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _clinicTimeZone).DateTime);
}
