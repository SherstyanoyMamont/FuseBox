using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FuseBox.Ai;

public sealed record ConfiguratorChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public sealed record ConfiguratorChatRequest(
    [property: JsonPropertyName("messages")] IReadOnlyList<ConfiguratorChatMessage> Messages,
    [property: JsonPropertyName("snapshot")] JsonElement Snapshot
);

public sealed record ConfiguratorChatResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("commands")] IReadOnlyList<JsonObject> Commands
);

public sealed record ConfiguratorChatAttachment(
    string FileName,
    string ContentType,
    byte[] Data
);

public sealed record ConfiguratorTranscriptionResponse(
    [property: JsonPropertyName("text")] string Text
);
