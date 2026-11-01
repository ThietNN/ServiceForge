using ServiceForge.Application.Appointments.Contracts;
using ServiceForge.Application.Common.Pagination;
using ServiceForge.Application.Validation;

namespace ServiceForge.UnitTests;

public sealed class ApplicationValidationTests
{
    [Fact]
    public void CreateAppointmentRequest_accepts_explicit_offset_whole_minute()
    {
        var validator = new CreateAppointmentRequestValidator();
        var request = new CreateAppointmentRequest(1, 2, 3, 4, "2030-01-02T09:00:00+07:00");

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("2030-01-02T09:00:30+07:00")]
    [InlineData("2030-01-02T09:00:00")]
    public void CreateAppointmentRequest_rejects_missing_or_non_minute_offset(string startTime)
    {
        var validator = new CreateAppointmentRequestValidator();
        var request = new CreateAppointmentRequest(1, 2, 3, 4, startTime);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void PaginationRequest_validator_enforces_approved_bounds()
    {
        var validator = new PaginationRequestValidator();

        Assert.True(validator.Validate(new PaginationRequest()).IsValid);
        Assert.False(validator.Validate(new PaginationRequest(0, 20)).IsValid);
        Assert.False(validator.Validate(new PaginationRequest(1, 101)).IsValid);
    }
}
