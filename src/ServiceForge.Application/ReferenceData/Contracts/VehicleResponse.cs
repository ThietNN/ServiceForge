namespace ServiceForge.Application.ReferenceData.Contracts;

public sealed record VehicleResponse(long Id, long CustomerId, string RegistrationNumber, string Make, string Model);
