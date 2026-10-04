namespace FuseBox.Ai;

public sealed class OpenAiConfiguratorOptions
{
    public const string SectionName = "OpenAI:Configurator";

    public string Model { get; set; } = "gpt-5.6-luna";
    public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
    public string? ApiKey { get; set; }
    public int MaxToolRounds { get; set; } = 6;
    public int MaxOutputTokens { get; set; } = 1400;
    public int DailyRequestLimit { get; set; } = 200;
    public int TranscriptionDailyRequestLimit { get; set; } = 200;
    public int MaxAttachmentFiles { get; set; } = 3;
    public int MaxAttachmentBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxAudioBytes { get; set; } = 10 * 1024 * 1024;
}
