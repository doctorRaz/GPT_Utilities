using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Записывает десериализованные метаданные обратно в YAML front matter Markdown-файла.
/// </summary>
internal interface IChatMetadataWriter
{
    void Write(string filePath, ChatMetadata metadata);
}

internal sealed class ChatMetadataWriter : IChatMetadataWriter
{
    private readonly IFileSystem _fileSystem;

    public ChatMetadataWriter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public void Write(string filePath, ChatMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        string content = string.Join(Environment.NewLine, _fileSystem.ReadLines(filePath));
        Match match = ChatMetadataReader.FrontMatterRegex.Match(content);
        if (!match.Success)
            throw new FormatException($"В файле отсутствует YAML front matter: {filePath}");

        string yaml = Serialize(metadata);
        string body = content[match.Length..];
        _fileSystem.WriteAllText(
            filePath,
            $"---{Environment.NewLine}{yaml}---{Environment.NewLine}{body}");
    }

    private static string Serialize(ChatMetadata metadata)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var model = new SerializableChatMetadata
        {
            Title = metadata.Title,
            Aliases = metadata.Aliases,
            Tags = metadata.Tags,
            CreateTime = metadata.CreateTime,
            UpdateTime = metadata.UpdateTime,
            DateExport = metadata.DateExport,
            ChatLink = metadata.ChatLink,
            ConversationId = metadata.ConversationId
        };

        return serializer.Serialize(model).TrimEnd('\r', '\n') + Environment.NewLine;
    }

    private sealed class SerializableChatMetadata
    {
        public string? Title { get; set; }
        public List<string?>? Aliases { get; set; }
        public List<string?>? Tags { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset? UpdateTime { get; set; }
        public string? DateExport { get; set; }
        public string? ChatLink { get; set; }
        [YamlMember(Alias = "conversation_ID")]
        public Guid? ConversationId { get; set; }
    }
}
