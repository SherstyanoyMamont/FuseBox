using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FuseBox.Ai;

public sealed record SchemaChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public sealed record SchemaChatRequest(
    [property: JsonPropertyName("messages")] IReadOnlyList<SchemaChatMessage> Messages,
    [property: JsonPropertyName("snapshot")] JsonElement Snapshot
);

public sealed record SchemaChatResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("commands")] IReadOnlyList<JsonObject> Commands
);
