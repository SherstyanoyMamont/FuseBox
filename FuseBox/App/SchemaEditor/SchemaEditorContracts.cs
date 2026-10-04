using System.ComponentModel.DataAnnotations;
using FuseBox.App.Contracts.Common;

namespace FuseBox.App.SchemaEditor;

public sealed class SaveSchemaEditorRequest
{
    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; set; }

    [Required]
    public SchemaDocument? Schema { get; set; }
}

public sealed class RegenerateSchemaRequest
{
    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; set; }
}

public sealed class SchemaValidationError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ComponentId { get; init; }
    public string? ConnectionId { get; init; }
    public int? RowIndex { get; init; }
}

public sealed class SchemaValidationResponse
{
    public bool Valid => Errors.Count == 0;
    public IReadOnlyList<SchemaValidationError> Errors { get; init; } =
        Array.Empty<SchemaValidationError>();
}

public sealed class SchemaSaveValidationErrorResponse
{
    public string Code { get; init; } = ApiErrorCodes.ValidationFailed;
    public string Message { get; init; } = "Schema validation failed.";
    public bool Valid => false;
    public IReadOnlyList<SchemaValidationError> Errors { get; init; } =
        Array.Empty<SchemaValidationError>();
}

public enum SchemaEditorWriteStatus
{
    Success,
    NotFound,
    RevisionConflict,
    ValidationFailed
}

public sealed class SchemaEditorWriteResult
{
    public SchemaEditorWriteStatus Status { get; init; }
    public SchemaDocument? Document { get; init; }
    public SchemaValidationResponse? Validation { get; init; }
    public int? CurrentRevision { get; init; }
}
