using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace FuseBox.Ai;

internal static class ConfiguratorAiTools
{
    public static JsonArray CreateAll()
    {
        return new JsonArray
        {
            Function(
                "replace_project",
                "Replace the visible local configurator project with a complete project proposal. Use this when the user asks to create a project from scratch or describes a complete new project. Preserve unspecified project-level settings from the current snapshot and use C16/30 mA defaults for new consumer lines when protection values are omitted.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["project"] = ProjectSchema(),
                    },
                    new[] { "project" }
                )
            ),
            Function(
                "set_project_name",
                "Change only the project name.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["projectName"] = StringSchema(120),
                    },
                    new[] { "projectName" }
                )
            ),
            Function(
                "set_primary_settings",
                "Change one or more visible primary settings. Only include fields explicitly requested or safely preserved from the current snapshot.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["phasesCount"] = IntegerEnum(1, 3),
                        ["mainAmperage"] = IntegerEnum(25, 32, 63),
                        ["shieldWidth"] = IntegerEnum(12, 16, 24),
                        ["voltageStandard"] = IntegerEnum(220, 230),
                    },
                    ["minProperties"] = 1,
                    ["additionalProperties"] = false,
                }
            ),
            Function(
                "set_panel_devices",
                "Change one or more visible panel device toggles.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = PanelDeviceProperties(),
                    ["minProperties"] = 1,
                    ["additionalProperties"] = false,
                }
            ),
            Function(
                "add_floor",
                "Add a floor to the current project.",
                ObjectSchema(
                    new JsonObject { ["name"] = StringSchema(120) },
                    new[] { "name" }
                )
            ),
            Function(
                "rename_floor",
                "Rename an existing floor. Use the exact floorId from the current snapshot.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["floorId"] = StringSchema(200),
                        ["name"] = StringSchema(120),
                    },
                    new[] { "floorId", "name" }
                )
            ),
            Function(
                "remove_floor",
                "Remove an existing floor. Use the exact floorId from the current snapshot.",
                ObjectSchema(
                    new JsonObject { ["floorId"] = StringSchema(200) },
                    new[] { "floorId" }
                )
            ),
            Function(
                "add_room",
                "Add a room to an existing floor. Use the exact floorId from the current snapshot.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["floorId"] = StringSchema(200),
                        ["name"] = StringSchema(120),
                    },
                    new[] { "floorId", "name" }
                )
            ),
            Function(
                "rename_room",
                "Rename an existing room. Use the exact floorId and roomId from the current snapshot.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["floorId"] = StringSchema(200),
                        ["roomId"] = StringSchema(200),
                        ["name"] = StringSchema(120),
                    },
                    new[] { "floorId", "roomId", "name" }
                )
            ),
            Function(
                "remove_room",
                "Remove an existing room. Use the exact floorId and roomId from the current snapshot.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["floorId"] = StringSchema(200),
                        ["roomId"] = StringSchema(200),
                    },
                    new[] { "floorId", "roomId" }
                )
            ),
            Function(
                "replace_room_consumers",
                "Replace the complete consumer list for one room. Use this for adding, editing or removing consumers in an existing room. For a new consumer, use breakerAmperage=16 and rcdMilliAmps=30 when the user did not explicitly provide them.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["floorId"] = StringSchema(200),
                        ["roomId"] = StringSchema(200),
                        ["consumers"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = ConsumerSchema(),
                        },
                    },
                    new[] { "floorId", "roomId", "consumers" }
                )
            ),
        };
    }

    private static JsonObject Function(
        string name,
        string description,
        JsonObject parameters)
    {
        return new JsonObject
        {
            ["type"] = "function",
            ["name"] = name,
            ["description"] = description,
            ["parameters"] = parameters,
            ["strict"] = false,
        };
    }

    private static JsonObject ObjectSchema(
        JsonObject properties,
        IEnumerable<string> required)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = ToJsonArray(required),
            ["additionalProperties"] = false,
        };
    }

    private static JsonObject StringSchema(int maxLength)
    {
        return new JsonObject
        {
            ["type"] = "string",
            ["minLength"] = 1,
            ["maxLength"] = maxLength,
        };
    }

    private static JsonObject IntegerEnum(params int[] values)
    {
        return new JsonObject
        {
            ["type"] = "integer",
            ["enum"] = new JsonArray(
                values
                    .Select(value => (JsonNode?)JsonValue.Create(value))
                    .ToArray()
            ),
        };
    }

    private static JsonArray StringArray(params string[] values)
    {
        return new JsonArray(
            values
                .Select(value => (JsonNode?)JsonValue.Create(value))
                .ToArray()
        );
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        return new JsonArray(
            values
                .Select(value => (JsonNode?)JsonValue.Create(value))
                .ToArray()
        );
    }

    private static JsonObject NullableStringSchema(int maxLength)
    {
        return new JsonObject
        {
            ["type"] = StringArray("string", "null"),
            ["maxLength"] = maxLength,
        };
    }

    private static JsonObject NullableNumberSchema()
    {
        return new JsonObject
        {
            ["type"] = StringArray("number", "null"),
            ["minimum"] = 0,
        };
    }

    private static JsonObject PanelDeviceProperties()
    {
        return new JsonObject
        {
            ["mainBreaker"] = new JsonObject { ["type"] = "boolean" },
            ["main3PN"] = new JsonObject { ["type"] = "boolean" },
            ["surgeProtection"] = new JsonObject { ["type"] = "boolean" },
            ["loadSwitch2P"] = new JsonObject { ["type"] = "boolean" },
            ["modularContactor"] = new JsonObject { ["type"] = "boolean" },
            ["railMeter"] = new JsonObject { ["type"] = "boolean" },
            ["fireRcd"] = new JsonObject { ["type"] = "boolean" },
            ["voltageRelay"] = new JsonObject { ["type"] = "boolean" },
            ["railSocket"] = new JsonObject { ["type"] = "boolean" },
            ["nonDisconnectableLine"] = new JsonObject { ["type"] = "boolean" },
            ["crossModule"] = new JsonObject { ["type"] = "boolean" },
        };
    }

    private static JsonObject ConsumerSchema()
    {
        return ObjectSchema(
            new JsonObject
            {
                ["name"] = StringSchema(160),
                ["catalogTypeId"] = NullableStringSchema(64),
                ["powerSource"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = StringArray("estimated", "manual"),
                },
                ["powerWatts"] = NullableNumberSchema(),
                ["breakerAmperage"] = IntegerEnum(2, 4, 6, 10, 16, 20, 25, 32),
                ["rcdMilliAmps"] = IntegerEnum(10, 30),
            },
            new[]
            {
                "name",
                "catalogTypeId",
                "powerSource",
                "powerWatts",
                "breakerAmperage",
                "rcdMilliAmps",
            }
        );
    }

    private static JsonObject RoomSchema()
    {
        return ObjectSchema(
            new JsonObject
            {
                ["name"] = StringSchema(120),
                ["consumers"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = ConsumerSchema(),
                },
            },
            new[] { "name", "consumers" }
        );
    }

    private static JsonObject FloorSchema()
    {
        return ObjectSchema(
            new JsonObject
            {
                ["name"] = StringSchema(120),
                ["rooms"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = RoomSchema(),
                },
            },
            new[] { "name", "rooms" }
        );
    }

    private static JsonObject ProjectSchema()
    {
        return ObjectSchema(
            new JsonObject
            {
                ["projectName"] = StringSchema(120),
                ["primarySettings"] = ObjectSchema(
                    new JsonObject
                    {
                        ["phasesCount"] = IntegerEnum(1, 3),
                        ["mainAmperage"] = IntegerEnum(25, 32, 63),
                        ["shieldWidth"] = IntegerEnum(12, 16, 24),
                        ["voltageStandard"] = IntegerEnum(220, 230),
                    },
                    new[]
                    {
                        "phasesCount",
                        "mainAmperage",
                        "shieldWidth",
                        "voltageStandard",
                    }
                ),
                ["panelDevices"] = ObjectSchema(
                    PanelDeviceProperties(),
                    new[]
                    {
                        "mainBreaker",
                        "main3PN",
                        "surgeProtection",
                        "loadSwitch2P",
                        "modularContactor",
                        "railMeter",
                        "fireRcd",
                        "voltageRelay",
                        "railSocket",
                        "nonDisconnectableLine",
                        "crossModule",
                    }
                ),
                ["floors"] = new JsonObject
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = FloorSchema(),
                },
                ["assumptions"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = StringSchema(500),
                },
            },
            new[]
            {
                "projectName",
                "primarySettings",
                "panelDevices",
                "floors",
                "assumptions",
            }
        );
    }
}
