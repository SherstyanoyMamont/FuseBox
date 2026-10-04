using System.Text.Json.Serialization;

namespace FuseBox.App.SchemaEditor;

/// <summary>
/// Persistent metadata for one project's schema editor document.
/// Generated projects do not duplicate the generator graph here; DocumentJson
/// is populated only while the active schema mode is Customized.
/// </summary>
public sealed class SchemaEditorState
{
    public int ProjectId { get; set; }

    [JsonIgnore]
    public global::FuseBox.Project Project { get; set; } = null!;

    public string Mode { get; set; } = SchemaModes.Generated;
    public int Revision { get; set; } = 1;
    public string? DocumentJson { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
