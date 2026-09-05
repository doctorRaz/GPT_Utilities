using System;
using System.IO;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Files
{
    public sealed class ConversationTitleIndexWriterTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        /// <summary>Проверяет выбор первого непустого alias вместо title и сохранение имени файла как цели ссылки.</summary>
        [Test]
        public void Refresh_UsesFirstNonEmptyAliasBeforeTitle_AndKeepsFileNameAsTarget()
        {
            using TempDirectory temp = new();
            string directory = temp.Combine("2026", "09-September");
            _ = Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "normalized-name.md");
            File.WriteAllText(file,
                "---\n" +
                $"create_time: {CreateTime:O}\n" +
                "title: Title\n" +
                "aliases:\n" +
                "  - \"\"\n" +
                "  - \"Original conversation name\"\n" +
                "---\n" +
                "body\n");

            ConversationTitleIndexWriter writer = CreateWriter();
            writer.Refresh(directory);

            string index = File.ReadAllText(Path.Combine(directory, "_index.md"));
            Assert.That(index, Does.Contain("- [Original conversation name](normalized-name.md)"));
            Assert.That(index, Does.Not.Contain("- [Title](normalized-name.md)"));
        }

        /// <summary>Проверяет использование title в индексе, если aliases отсутствуют.</summary>
        [Test]
        public void Refresh_UsesTitle_WhenAliasesAreMissing()
        {
            using TempDirectory temp = new();
            string directory = temp.Combine("2026", "09-September");
            _ = Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "conversation.md");
            File.WriteAllText(file,
                "---\n" +
                $"create_time: {CreateTime:O}\n" +
                "title: Conversation title\n" +
                "---\n" +
                "body\n");

            CreateWriter().Refresh(directory);

            string index = File.ReadAllText(Path.Combine(directory, "_index.md"));
            Assert.That(index, Does.Contain("- [Conversation title](conversation.md)"));
        }

        /// <summary>Проверяет использование имени файла, если title и aliases отсутствуют.</summary>
        [Test]
        public void Refresh_UsesFileName_WhenTitleAndAliasesAreMissing()
        {
            using TempDirectory temp = new();
            string directory = temp.Combine("2026", "09-September");
            _ = Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "fallback-name.md");
            File.WriteAllText(file,
                "---\n" +
                $"create_time: {CreateTime:O}\n" +
                "---\n" +
                "body\n");

            CreateWriter().Refresh(directory);

            string index = File.ReadAllText(Path.Combine(directory, "_index.md"));
            Assert.That(index, Does.Contain("- [fallback-name](fallback-name.md)"));
        }

        /// <summary>Проверяет использование имени файла, если YAML файла не удаётся прочитать.</summary>
        [Test]
        public void Refresh_UsesFileName_WhenYamlIsInvalid()
        {
            using TempDirectory temp = new();
            string directory = temp.Combine("2026", "09-September");
            _ = Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "invalid-yaml.md");
            File.WriteAllText(file,
                "---\n" +
                "create_time: [not valid\n" +
                "title: Ignored title\n" +
                "---\n" +
                "body\n");

            CreateWriter().Refresh(directory);

            string index = File.ReadAllText(Path.Combine(directory, "_index.md"));
            Assert.That(index, Does.Contain("- [invalid-yaml](invalid-yaml.md)"));
        }

        private static ConversationTitleIndexWriter CreateWriter()
        {
            return new ConversationTitleIndexWriter(
                new LocalFileSystem(),
                new ChatMetadataReader(new LocalFileSystem()));
        }
    }
}
