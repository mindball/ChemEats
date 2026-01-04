namespace Shared.DTOs.Errors;

public sealed record ProblemDetailsDto(string? Title, string? Detail, int? Status, string? Type);