using global::FuseBox;
using FuseBox.FuseBox;

namespace FuseBox.App.Services.Pricing;

internal static class SchneiderProductCatalog
{
    private const string GermanListNote =
        "Schneider Electric Germany public list-price snapshot; treated as reference, not a distributor quote.";

    private const string EuListNote =
        "Schneider Electric public EU list-price snapshot; treated as reference until the Schneider pricing API is connected.";

    private static readonly IReadOnlyDictionary<int, SchneiderCatalogItem>
        Mcb1PC = new Dictionary<int, SchneiderCatalogItem>
        {
            [2] = new("Acti9 iC60N MCB 1P C2", "A9F04102", 34.95m, GermanListNote),
            [4] = new("Acti9 iC60N MCB 1P C4", "A9F04104", 34.95m, GermanListNote),
            [6] = new("Acti9 iC60N MCB 1P C6", "A9F04106", 34.95m, GermanListNote),
            [10] = new("Acti9 iC60N MCB 1P C10", "A9F04110", 30.60m, GermanListNote),
            [16] = new("Acti9 iC60N MCB 1P C16", "A9F04116", 29.05m, GermanListNote),
            [20] = new("Acti9 iC60N MCB 1P C20", "A9F04120", 31.00m, GermanListNote),
            [25] = new("Acti9 iC60N MCB 1P C25", "A9F04125", null, GermanListNote),
            [32] = new("Acti9 iC60N MCB 1P C32", "A9F04132", 39.60m, GermanListNote)
        };

    private static readonly IReadOnlyDictionary<int, SchneiderCatalogItem>
        Main2PC = new Dictionary<int, SchneiderCatalogItem>
        {
            [25] = new("Acti9 iC60N MCB 2P C25", "A9F04225", 107.00m, GermanListNote),
            [32] = new("Acti9 iC60N MCB 2P C32", "A9F04232", 119.90m, GermanListNote),
            [63] = new("Acti9 iC60N MCB 2P C63", "A9F04263", 225.00m, GermanListNote)
        };

    private static readonly IReadOnlyDictionary<int, SchneiderCatalogItem>
        Main3PC = new Dictionary<int, SchneiderCatalogItem>
        {
            [25] = new("Acti9 iC60N MCB 3P C25", "A9F04325", null, GermanListNote),
            [32] = new("Acti9 iC60N MCB 3P C32", "A9F04332", 173.00m, GermanListNote),
            [63] = new("Acti9 iC60N MCB 3P C63", "A9F04363", 309.00m, GermanListNote)
        };

    private static readonly IReadOnlyDictionary<int, SchneiderCatalogItem>
        Main3PnC = new Dictionary<int, SchneiderCatalogItem>
        {
            [25] = new("Acti9 iC60N MCB 3P+N C25", "A9F04725", null, GermanListNote),
            [32] = new("Acti9 iC60N MCB 3P+N C32", "A9F04732", 212.00m, GermanListNote),
            [63] = new("Acti9 iC60N MCB 3P+N C63", "A9F04763", 390.00m, GermanListNote)
        };

    public static SchneiderCatalogItem? ResolveComponent(
        Component component,
        InitialSettings settings)
    {
        var name = component.Name?.Replace(" ", string.Empty) ?? string.Empty;

        if (name.Equals("AV", StringComparison.OrdinalIgnoreCase))
        {
            var amper = checked((int)Math.Round(component.Amper));
            return Mcb1PC.GetValueOrDefault(amper);
        }

        if (name.Equals("RCD", StringComparison.OrdinalIgnoreCase) &&
            component is RCD rcd)
        {
            if (rcd.Capacity == 30)
            {
                return new SchneiderCatalogItem(
                    "Acti9 iID RCCB 2P 63A Type A 30mA",
                    "A9Z21263",
                    361.00m,
                    GermanListNote);
            }

            // The current model does not carry enough information to select a
            // safe 63A/10mA Schneider RCCB reference. Do not silently replace it
            // with a lower-rated product just to obtain a price.
            return null;
        }

        if (name.Equals("Introductory", StringComparison.OrdinalIgnoreCase))
            return Main2PC.GetValueOrDefault(settings.MainAmperage);

        if (name.Equals("Introductory3p", StringComparison.OrdinalIgnoreCase))
            return Main3PC.GetValueOrDefault(settings.MainAmperage);

        if (name.Equals("Introductory3pn", StringComparison.OrdinalIgnoreCase))
            return Main3PnC.GetValueOrDefault(settings.MainAmperage);

        return name.ToLowerInvariant() switch
        {
            "spd" => new SchneiderCatalogItem(
                "Acti9 iPRD20 surge arrester 1P+N",
                "A9L20500",
                156.40m,
                EuListNote),

            "loadswitch" or "ndiscline" => new SchneiderCatalogItem(
                "Acti9 iSW switch 2P 63A",
                "A9S65263",
                59.90m,
                GermanListNote),

            "modularcontactor" => new SchneiderCatalogItem(
                "Acti9 iCT modular contactor 25A 2NO 230/240V",
                "A9C20732",
                65.40m,
                GermanListNote),

            "dinrailmeter" => new SchneiderCatalogItem(
                "Acti9 iEM2000 single-phase energy meter",
                "A9MEM2000",
                99.30m,
                GermanListNote),

            "dinrailmeter3p" => new SchneiderCatalogItem(
                "Acti9 iEM3155 three-phase energy meter",
                "A9MEM3155",
                247.00m,
                GermanListNote),

            "dinrailsocket" => new SchneiderCatalogItem(
                "Acti9 DIN rail socket, German standard",
                "A9A15310",
                26.25m,
                GermanListNote),

            "crossblock" => new SchneiderCatalogItem(
                "Linergy DX distribution block 4P 63A",
                "LVS04040",
                null,
                GermanListNote),

            "rcdfire" => new SchneiderCatalogItem(
                "Acti9 iID RCCB 2P 63A Type A 300mA",
                "A9R24263",
                null,
                EuListNote),

            "rcdfire3p" => new SchneiderCatalogItem(
                "Acti9 iID RCCB 4P 63A Type A 300mA",
                "A9Z24463",
                204.00m,
                GermanListNote),

            // Exact Schneider equivalents are intentionally left unresolved
            // until the product specification includes the missing technical
            // parameters (relay thresholds/type, SPD topology, etc.).
            "spd3" or
            "voltagerelay" or
            "voltagerelay2" or
            "voltagerelay3" or
            "voltagerelay3p" => null,

            "startpoint" or "startpoint3p" => null,
            _ => null
        };
    }

    public static SchneiderCatalogItem? ResolveEnclosure(
        int modulesPerRow,
        int rowCount)
    {
        if (rowCount <= 0)
            return null;

        if (modulesPerRow <= 12)
        {
            return rowCount switch
            {
                1 => new SchneiderCatalogItem(
                    "Kaedra Mini enclosure 1x12 modules",
                    "13444",
                    65.60m,
                    GermanListNote),
                2 => new SchneiderCatalogItem(
                    "Kaedra enclosure 2x12 modules",
                    "13433",
                    159.00m,
                    GermanListNote),
                _ => null
            };
        }

        if (modulesPerRow <= 18)
        {
            return rowCount switch
            {
                1 => new SchneiderCatalogItem(
                    "Resi9 enclosure 1x18 modules",
                    "R9H18601",
                    116.80m,
                    EuListNote),
                2 => new SchneiderCatalogItem(
                    "Kaedra enclosure 2x18 modules",
                    "13434",
                    197.50m,
                    GermanListNote),
                3 => new SchneiderCatalogItem(
                    "Resi9 enclosure 3x18 modules",
                    "R9H18603",
                    null,
                    EuListNote),
                4 => new SchneiderCatalogItem(
                    "Resi9 enclosure 4x18 modules",
                    "R9H18604",
                    null,
                    EuListNote),
                _ => null
            };
        }

        if (modulesPerRow <= 24)
        {
            // Current PrismaSeT XS surface enclosure family. There is no 1x24
            // option in this selected family, so a one-row calculated panel uses
            // the smallest available 2x24 enclosure and keeps one row spare.
            return rowCount switch
            {
                1 or 2 => new SchneiderCatalogItem(
                    "PrismaSeT XS surface enclosure 2x24 modules",
                    "LVSXQ224",
                    null,
                    EuListNote),
                3 => new SchneiderCatalogItem(
                    "PrismaSeT XS surface enclosure 3x24 modules",
                    "LVSXQ324",
                    null,
                    EuListNote),
                4 => new SchneiderCatalogItem(
                    "PrismaSeT XS surface enclosure 4x24 modules",
                    "LVSXQ424",
                    null,
                    EuListNote),
                _ => null
            };
        }

        return null;
    }
}
