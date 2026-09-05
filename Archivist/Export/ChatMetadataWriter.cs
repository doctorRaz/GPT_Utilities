using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using dRz.GPT_Utilities.Archivist.Files;
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

        string content = _fileSystem.ReadAllText(filePath);
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

        var model = new Dictionary<string, object?>();

        if (metadata.Title is not null)
            model["title"] = metadata.Title;
        if (metadata.HasAliases)
            model["aliases"] = metadata.Aliases;
        if (metadata.HasTags)
            model["tags"] = metadata.Tags;

        model["create_time"] = metadata.CreateTimeText ??
            metadata.CreateTime.ToString("O", CultureInfo.InvariantCulture);

        if (metadata.HasUpdateTime && metadata.UpdateTime is not null)
            model["update_time"] = metadata.UpdateTimeText ??
                metadata.UpdateTime.Value.ToString("O", CultureInfo.InvariantCulture);

        if (metadata.DateExport is not null)
            model["date_export"] = metadata.DateExport;
        if (metadata.ChatLink is not null)
            model["chat_link"] = metadata.ChatLink;
        if (metadata.ConversationId is Guid conversationId)
            model["conversation_id"] = conversationId.ToString();

        return serializer.Serialize(model).TrimEnd('\r', '\n') + Environment.NewLine;
    }
}
