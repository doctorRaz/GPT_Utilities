using System;
using System.IO;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    /// <summary>
    /// Интеграционные тесты чтения и записи метаданных на реальных Markdown fixture.
    /// Проверяют round-trip на файлах, близких к фактическим данным Convoviz/ChatGPT.
    /// </summary>
    [TestFixture]
    public sealed class ChatMetadataFixtureRoundTripTests
    {
        private const string ConvovizFixture =
            "Fixtures/ConvovizExport/Convoviz PR.md";

        private const string ChatExportFixture =
            "Fixtures/ChatExport/Convoviz PR #.md";

        private static readonly Guid ConvovizConversationId =
            Guid.Parse("6a99a2b3-862c-83eb-a079-f1749f595cbe");

        private static readonly Guid ChatExportConversationId =
            Guid.Parse("6a99a2b3-862c-83eb-a079-f1749f595cbe");

        [Test]
        public void ReadMetadata_ReadsConversationId_FromRealConvovizFixture()
        {
            string path = GetFixturePath(ConvovizFixture);

            ChatMetadata metadata =
                new ChatMetadataReader(new LocalFileSystem()).Read(path);

            Assert.That(metadata.Title, Is.EqualTo("Convoviz PR"));
            Assert.That(metadata.ChatLink,
                Is.EqualTo($"https://chatgpt.com/c/{ConvovizConversationId}"));
            Assert.That(metadata.ConversationId, Is.EqualTo(ConvovizConversationId));
        }

        [Test]
        public void ReadWriteRoundTrip_PreservesConversationId_OnRealConvovizFixture()
        {
            using TempDirectory temp = new();
            string source = GetFixturePath(ConvovizFixture);
            string destination = temp.Combine("Convoviz PR.md");
            File.Copy(source, destination);

            IFileSystem fileSystem = new LocalFileSystem();
            ChatMetadataReader reader = new(fileSystem);
            ChatMetadataWriter writer = new(fileSystem);

            ChatMetadata before = reader.Read(destination);
            writer.Write(destination, before);
            ChatMetadata after = reader.Read(destination);

            Assert.That(after.ConversationId, Is.EqualTo(ConvovizConversationId));
            Assert.That(after.ChatLink, Is.EqualTo(before.ChatLink));
            Assert.That(after.Title, Is.EqualTo(before.Title));
            Assert.That(after.CreateTime, Is.EqualTo(before.CreateTime));
            Assert.That(after.UpdateTime, Is.EqualTo(before.UpdateTime));
            Assert.That(File.ReadAllText(destination), Does.Contain(
                $"conversation_id: \"{ConvovizConversationId}\""));
        }

        [Test]
        public void ReadWriteRoundTrip_PreservesConversationId_OnRealChatExportFixture()
        {
            using TempDirectory temp = new();
            string source = GetFixturePath(ChatExportFixture);
            string destination = temp.Combine("Convoviz PR #.md");
            File.Copy(source, destination);

            IFileSystem fileSystem = new LocalFileSystem();
            ChatMetadataReader reader = new(fileSystem);
            ChatMetadataWriter writer = new(fileSystem);

            ChatMetadata before = reader.Read(destination);
            writer.Write(destination, before);
            ChatMetadata after = reader.Read(destination);

            Assert.That(after.ConversationId, Is.EqualTo(ChatExportConversationId));
            Assert.That(after.ChatLink, Is.EqualTo(before.ChatLink));
            Assert.That(after.Title, Is.EqualTo(before.Title));
            Assert.That(after.CreateTime, Is.EqualTo(before.CreateTime));
            Assert.That(after.UpdateTime, Is.EqualTo(before.UpdateTime));
            Assert.That(File.ReadAllText(destination), Does.Contain(
                $"conversation_id: \"{ChatExportConversationId}\""));
        }

        private static string GetFixturePath(string relativePath)
        {
            return Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
