using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FuseBox.Ai;

internal static class ConfiguratorAiCommandFactory
{
    private static readonly HashSet<int> PhaseCounts = new() { 1, 3 };
    private static readonly HashSet<int> MainAmperages = new() { 25, 32, 63 };
    private static readonly HashSet<int> ShieldWidths = new() { 12, 16, 24 };
    private static readonly HashSet<int> VoltageStandards = new() { 220, 230 };
    private static readonly HashSet<int> BreakerAmperages = new() { 2, 4, 6, 10, 16, 20, 25, 32 };
    private static readonly HashSet<int> RcdMilliAmps = new() { 10, 30 };

    private static readonly string[] PanelDeviceKeys =
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
    };

    public static JsonObject Create(
        string functionName,
        JsonObject arguments)
    {
        return functionName switch
        {
            "replace_project" => ReplaceProject(arguments),
            "set_project_name" => new JsonObject
            {
                ["operation"] = "set_project_name",
                ["projectName"] = RequiredString(arguments, "projectName", 120),
            },
            "set_primary_settings" => PrimarySettings(arguments),
            "set_panel_devices" => PanelDevices(arguments),
            "add_floor" => new JsonObject
            {
                ["operation"] = "add_floor",
                ["name"] = RequiredString(arguments, "name", 120),
            },
            "rename_floor" => new JsonObject
            {
                ["operation"] = "rename_floor",
                ["floorId"] = RequiredString(arguments, "floorId", 200),
                ["name"] = RequiredString(arguments, "name", 120),
            },
            "remove_floor" => new JsonObject
            {
                ["operation"] = "remove_floor",
                ["floorId"] = RequiredString(arguments, "floorId", 200),
            },
            "add_room" => new JsonObject
            {
                ["operation"] = "add_room",
                ["floorId"] = RequiredString(arguments, "floorId", 200),
                ["name"] = RequiredString(arguments, "name", 120),
            },
            "rename_room" => new JsonObject
            {
                ["operation"] = "rename_room",
                ["floorId"] = RequiredString(arguments, "floorId", 200),
                ["roomId"] = RequiredString(arguments, "roomId", 200),
                ["name"] = RequiredString(arguments, "name", 120),
            },
            "remove_room" => new JsonObject
            {
                ["operation"] = "remove_room",
                ["floorId"] = RequiredString(arguments, "floorId", 200),
                ["roomId"] = RequiredString(arguments, "roomId", 200),
            },
            "replace_room_consumers" => ReplaceRoomConsumers(arguments),
            _ => throw new InvalidOperationException(
                $"Unknown configurator tool: {functionName}."
            ),
        };
    }

    private static JsonObject ReplaceProject(JsonObject arguments)
    {
        var project = RequiredObject(arguments, "project");
        ValidateProject(project);

        var normalizedProject = (JsonObject)project.DeepClone();
        normalizedProject["contractVersion"] = 1;

        return new JsonObject
        {
            ["operation"] = "replace_project",
            ["project"] = normalizedProject,
        };
    }

    private static JsonObject PrimarySettings(JsonObject arguments)
    {
        var values = new JsonObject();

        CopyOptionalInteger(arguments, values, "phasesCount", PhaseCounts);
        CopyOptionalInteger(arguments, values, "mainAmperage", MainAmperages);
        CopyOptionalInteger(arguments, values, "shieldWidth", ShieldWidths);
        CopyOptionalInteger(arguments, values, "voltageStandard", VoltageStandards);

        if (values.Count == 0)
        {
            throw new InvalidOperationException(
                "set_primary_settings must contain at least one valid setting."
            );
        }

        return new JsonObject
        {
            ["operation"] = "set_primary_settings",
            ["values"] = values,
        };
    }

    private static JsonObject PanelDevices(JsonObject arguments)
    {
        var values = new JsonObject();

        foreach (var key in PanelDeviceKeys)
        {
            if (arguments[key] is null)
            {
                continue;
            }

            values[key] = RequiredBoolean(arguments, key);
        }

        if (values.Count == 0)
        {
            throw new InvalidOperationException(
                "set_panel_devices must contain at least one device value."
            );
        }

        return new JsonObject
        {
            ["operation"] = "set_panel_devices",
            ["values"] = values,
        };
    }

    private static JsonObject ReplaceRoomConsumers(JsonObject arguments)
    {
        var consumers = RequiredArray(arguments, "consumers");
        ValidateConsumers(consumers);

        return new JsonObject
        {
            ["operation"] = "replace_room_consumers",
            ["floorId"] = RequiredString(arguments, "floorId", 200),
            ["roomId"] = RequiredString(arguments, "roomId", 200),
            ["consumers"] = consumers.DeepClone(),
        };
    }

    private static void ValidateProject(JsonObject project)
    {
        NormalizeAssumptions(project);

        RequiredString(project, "projectName", 120);

        var primary = RequiredObject(project, "primarySettings");
        RequiredInteger(primary, "phasesCount", PhaseCounts);
        RequiredInteger(primary, "mainAmperage", MainAmperages);
        RequiredInteger(primary, "shieldWidth", ShieldWidths);
        RequiredInteger(primary, "voltageStandard", VoltageStandards);

        var devices = RequiredObject(project, "panelDevices");
        foreach (var key in PanelDeviceKeys)
        {
            RequiredBoolean(devices, key);
        }

        var floors = RequiredArray(project, "floors");
        if (floors.Count == 0)
        {
            throw new InvalidOperationException(
                "replace_project requires at least one floor."
            );
        }

        foreach (var floorNode in floors)
        {
            var floor = AsObject(floorNode, "floor");
            RequiredString(floor, "name", 120);

            var rooms = RequiredArray(floor, "rooms");
            foreach (var roomNode in rooms)
            {
                var room = AsObject(roomNode, "room");
                RequiredString(room, "name", 120);
                ValidateConsumers(RequiredArray(room, "consumers"));
            }
        }

        var assumptions = RequiredArray(project, "assumptions");
        foreach (var assumption in assumptions)
        {
            if (assumption is not JsonValue value ||
                !value.TryGetValue<string>(out var text) ||
                string.IsNullOrWhiteSpace(text) ||
                text.Length > 500)
            {
                throw new InvalidOperationException(
                    "Project assumptions must be non-empty strings up to 500 characters."
                );
            }
        }
    }

    private static void NormalizeAssumptions(JsonObject project)
    {
        var node = project["assumptions"];

        if (node is null)
        {
            project["assumptions"] = new JsonArray();
            return;
        }

        if (node is JsonArray array)
        {
            var normalized = new JsonArray();

            foreach (var item in array)
            {
                if (item is not JsonValue value ||
                    !value.TryGetValue<string>(out var text))
                {
                    continue;
                }

                text = text.Trim();

                if (text.Length == 0)
                {
                    continue;
                }

                normalized.Add(text.Length <= 500
                    ? text
                    : text[..500]);
            }

            project["assumptions"] = normalized;
            return;
        }

        if (node is JsonValue singleValue &&
            singleValue.TryGetValue<string>(out var singleText))
        {
            singleText = singleText.Trim();

            project["assumptions"] =
                singleText.Length == 0
                    ? new JsonArray()
                    : new JsonArray(
                        singleText.Length <= 500
                            ? singleText
                            : singleText[..500]
                    );

            return;
        }

        // assumptions is metadata only. Invalid metadata must never block
        // an otherwise valid configurator command.
        project["assumptions"] = new JsonArray();
    }

    private static void ValidateConsumers(JsonArray consumers)
    {
        foreach (var consumerNode in consumers)
        {
            var consumer = AsObject(consumerNode, "consumer");
            RequiredString(consumer, "name", 160);

            string? catalogTypeId = OptionalString(
                consumer,
                "catalogTypeId",
                64
            );

            var powerSource = RequiredString(
                consumer,
                "powerSource",
                20
            );

            if (powerSource is not ("estimated" or "manual"))
            {
                throw new InvalidOperationException(
                    "Consumer powerSource must be estimated or manual."
                );
            }

            if (powerSource == "estimated" &&
                string.IsNullOrWhiteSpace(catalogTypeId))
            {
                throw new InvalidOperationException(
                    "Estimated consumers require catalogTypeId."
                );
            }

            if (powerSource == "manual")
            {
                RequiredNonNegativeNumber(consumer, "powerWatts");
            }

            RequiredInteger(
                consumer,
                "breakerAmperage",
                BreakerAmperages
            );

            RequiredInteger(
                consumer,
                "rcdMilliAmps",
                RcdMilliAmps
            );
        }
    }

    private static void CopyOptionalInteger(
        JsonObject source,
        JsonObject target,
        string key,
        HashSet<int> allowed)
    {
        if (source[key] is null)
        {
            return;
        }

        target[key] = RequiredInteger(source, key, allowed);
    }

    private static string RequiredString(
        JsonObject value,
        string key,
        int maxLength)
    {
        if (value[key] is not JsonValue node ||
            !node.TryGetValue<string>(out var text))
        {
            throw new InvalidOperationException(
                $"{key} must be a string."
            );
        }

        text = text.Trim();

        if (text.Length == 0 || text.Length > maxLength)
        {
            throw new InvalidOperationException(
                $"{key} must contain 1 to {maxLength} characters."
            );
        }

        return text;
    }

    private static string? OptionalString(
        JsonObject value,
        string key,
        int maxLength)
    {
        var node = value[key];

        if (node is null ||
            (node is JsonValue nullValue &&
             nullValue.TryGetValue<object?>(out var raw) &&
             raw is null))
        {
            return null;
        }

        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var text))
        {
            throw new InvalidOperationException(
                $"{key} must be a string or null."
            );
        }

        text = text.Trim();

        if (text.Length > maxLength)
        {
            throw new InvalidOperationException(
                $"{key} must contain at most {maxLength} characters."
            );
        }

        return text.Length == 0 ? null : text;
    }

    private static bool RequiredBoolean(
        JsonObject value,
        string key)
    {
        if (value[key] is not JsonValue node ||
            !node.TryGetValue<bool>(out var result))
        {
            throw new InvalidOperationException(
                $"{key} must be a boolean."
            );
        }

        return result;
    }

    private static int RequiredInteger(
        JsonObject value,
        string key,
        HashSet<int> allowed)
    {
        if (value[key] is not JsonValue node ||
            !node.TryGetValue<int>(out var result) ||
            !allowed.Contains(result))
        {
            throw new InvalidOperationException(
                $"{key} has an unsupported value."
            );
        }

        return result;
    }

    private static double RequiredNonNegativeNumber(
        JsonObject value,
        string key)
    {
        if (value[key] is not JsonValue node ||
            !node.TryGetValue<double>(out var result) ||
            !double.IsFinite(result) ||
            result < 0)
        {
            throw new InvalidOperationException(
                $"{key} must be a finite non-negative number."
            );
        }

        return result;
    }

    private static JsonObject RequiredObject(
        JsonObject value,
        string key)
        => AsObject(value[key], key);

    private static JsonObject AsObject(
        JsonNode? value,
        string key)
        => value as JsonObject
           ?? throw new InvalidOperationException(
               $"{key} must be an object."
           );

    private static JsonArray RequiredArray(
        JsonObject value,
        string key)
        => value[key] as JsonArray
           ?? throw new InvalidOperationException(
               $"{key} must be an array."
           );
}
