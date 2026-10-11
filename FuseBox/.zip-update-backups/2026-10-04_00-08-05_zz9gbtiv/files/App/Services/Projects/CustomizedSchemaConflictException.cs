namespace FuseBox.App.Services.Projects;

public sealed class CustomizedSchemaConflictException : InvalidOperationException
{
    public CustomizedSchemaConflictException()
        : base(
            "The project configuration cannot be changed while a customized " +
            "schema is active. Regenerate the schema explicitly first.")
    {
    }
}
