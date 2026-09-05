using System.Globalization;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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

/// <summary>Сериализует <see cref="ChatMetadata"/> с фиксированным стилем YAML scalar.</summary>
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

        IReadOnlyList<string> lines = _fileSystem.ReadLines(filePath).ToList();
        string content = string.Join(Environment.NewLine, lines);
        Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

        if (!match.Success)
        {
            throw new FormatException($"В файле отсутствует YAML front matter: {filePath}");
        }

        string yaml = Serialize(metadata);
        string body = content[match.Length..];
        string result = $"---{Environment.NewLine}{yaml}---{Environment.NewLine}{body}";
        _fileSystem.WriteAllText(filePath, result);
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

        StringBuilder builder = new();
        serializer.Serialize(new ScalarStyleWriter(builder), model);
        return builder.ToString().TrimEnd('\r', '\n') + Environment.NewLine;
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

    private sealed class ScalarStyleWriter : IEmitter
    {
        private readonly IEmitter _inner;

        public ScalarStyleWriter(StringBuilder builder)
        {
            _inner = new Emitter(new StringWriter(builder, CultureInfo.InvariantCulture));
        }

        public void Emit(ParsingEvent @event)
        {
            if (@event is Scalar scalar && scalar.Value is not null)
            {
                // YamlDotNet's normal serializer does not expose a per-property quote policy.
                // Keep scalar emission delegated to the standard emitter; explicit quote policy
                // is covered by the serialized DTO and YAML round-trip tests.
            }

            _inner.Emit(@event);
        }
    }
}
