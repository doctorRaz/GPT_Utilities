using System;
using System.IO;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    [TestFixture]
    public sealed class ConversationIdRoundTripTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 8, 24, 15, 23, 56, 473, TimeSpan.Zero);

        private static readonly Guid ExplicitConversationId =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static readonly Guid LinkConversationId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Test]
        public void ReadMetadata_ReadsConversationId_WhenChatLinkIsMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                conversation_id: "{ExplicitConversationId}"
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.EqualTo(ExplicitConversationId));
            Assert.That(metadata.ChatLink, Is.Null);
        }

        [Test]
        public void ReadMetadata_PrefersConversationId_WhenItConflictsWithChatLink()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                chat_link: https://chatgpt.com/c/{LinkConversationId}
                conversation_id: "{ExplicitConversationId}"
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.EqualTo(ExplicitConversationId));
            Assert.That(metadata.ChatLink, Is.EqualTo($"https://chatgpt.com/c/{LinkConversationId}"));
        }

        [Test]
        public void Write_SerializesConversationId_WhenChatLinkIsMissing()
        {
            using TempDirectory temp = new();
            string path = temp.Combine("conversation.md");
            File.WriteAllText(path, "---\ntitle: Test\n---\nBody\n");

            ChatMetadata metadata = new()
            {
                CreateTime = CreateTime,
                CreateTimeText = "2026-08-24T15:23:56.473Z",
                Title = "Test",
                ConversationId = ExplicitConversationId
            };

            new ChatMetadataWriter(new LocalFileSystem()).Write(path, metadata);

            string yaml = File.ReadAllText(path);
            Assert.That(yaml, Does.Contain($"conversation_id: \"{ExplicitConversationId}\""));
            Assert.That(yaml, Does.Not.Contain("chat_link:"));
        }

        [Test]
        public void ReadWriteRoundTrip_PreservesConversationId_WhenChatLinkIsMissing()
        {
            using TempDirectory temp = new();
            string path = temp.Combine("conversation.md");
            File.WriteAllText(path, $"""
                ---
                title: Test
                create_time: "2026-08-24T15:23:56.473Z"
                conversation_id: "{ExplicitConversationId}"
                ---

                # Body
                """);

            IFileSystem fileSystem = new LocalFileSystem();
            ChatMetadataReader reader = new(fileSystem);
            ChatMetadataWriter writer = new(fileSystem);

            ChatMetadata metadata = reader.Read(path);
            writer.Write(path, metadata);
            ChatMetadata roundTripped = reader.Read(path);

            Assert.That(roundTripped.ConversationId, Is.EqualTo(ExplicitConversationId));
            Assert.That(roundTripped.ChatLink, Is.Null);
        }
    }
}
