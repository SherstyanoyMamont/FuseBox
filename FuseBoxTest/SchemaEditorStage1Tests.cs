using FuseBox;
using FuseBox.App.SchemaEditor;
using FuseBox.App.Services.Projects;
using Newtonsoft.Json.Linq;

namespace TestServices;

[TestFixture]
public class SchemaEditorStage1Tests
{
    [Test]
    public void CatalogLoadsEmbeddedResourceAndHasStableTerminals()
    {
        Assert.That(ComponentCatalog.Entries.Count, Is.GreaterThan(20));
        Assert.That(ComponentCatalog.Resolve("Empty Slot")!.Mounting, Is.EqualTo("spacer"));
        Assert.That(ComponentCatalog.Resolve("StartPoint3p")!.Slots, Is.Zero);
        Assert.That(ComponentCatalog.Resolve("RCD")!.Terminals.Select(t => t.Id),
            Is.EquivalentTo(new[] { "L_IN", "N_IN", "L_OUT", "N_OUT" }));
        Assert.That(ComponentCatalog.Resolve("AV")!.Terminals.Select(t => t.Id),
            Is.EquivalentTo(new[] { "L_IN", "L_OUT" }));
    }

    [Test]
    public void ReadMetadataDoesNotDependOnDatabaseIdsOrMutateGeneratorGraph()
    {
        var source = new Component("StartPoint", 0) { SerialNumber = 1, Id = 10 };
        var breaker = new Component("AV", 1) { SerialNumber = 25, Id = 11 };
        var padding = new EmptySlot(11) { Id = 12 };
        var project = new Project();
        project.FuseBox.ComponentGroups = new List<FuseBoxComponentGroup>
        {
            new() { Components = new List<Component> { padding, breaker, source } }
        };

        var first = JObject.Parse(ProjectResponseFactory.BuildSchemaJson(project));
        Assert.That((string?)first["SchemaMode"], Is.EqualTo("generated"));
        Assert.That((int?)first["SchemaRevision"], Is.EqualTo(1));
        var components = (JArray)first["ComponentGroups"]![0]!["Components"]!;
        Assert.That((int?)components[1]["SchemaId"], Is.EqualTo(25));
        Assert.That((string?)components[1]["EditorComponentId"], Is.EqualTo("generated:25"));
        Assert.That((int?)components[1]["SlotStart"], Is.Zero);
        Assert.That((int?)components[2]["SlotStart"], Is.EqualTo(1));
        Assert.That(components[2]["EditorComponentId"]!.Type, Is.EqualTo(JTokenType.Null));

        breaker.Id = 98765;
        var second = JObject.Parse(ProjectResponseFactory.BuildSchemaJson(project));
        Assert.That((string?)second["ComponentGroups"]![0]!["Components"]![1]!["EditorComponentId"],
            Is.EqualTo("generated:25"));
        Assert.That(breaker.SerialNumber, Is.EqualTo(25));
        Assert.That(project.FuseBox.ComponentGroups[0].Components[0], Is.SameAs(padding));
    }

    [Test]
    public void EditorPlacementRoundTripPreservesIdentity()
    {
        var component = new EditorComponent
        {
            Id = "generated:25", SchemaId = 25, CatalogTypeId = "fusebox.av",
            RowIndex = 0, SlotStart = 2, Slots = 1, LinkedConsumerIds = new() { 7, 8 }
        };
        component.RowIndex = 1;
        component.SlotStart = 7;
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(component);
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<EditorComponent>(json)!;
        Assert.That(restored.Id, Is.EqualTo("generated:25"));
        Assert.That(restored.SchemaId, Is.EqualTo(25));
        Assert.That(restored.RowIndex, Is.EqualTo(1));
        Assert.That(restored.SlotStart, Is.EqualTo(7));
        Assert.That(restored.LinkedConsumerIds, Is.EqualTo(new[] { 7, 8 }));
    }
}
