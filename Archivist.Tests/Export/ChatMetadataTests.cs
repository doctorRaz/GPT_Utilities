using dRz.GPT_Utilities.Archivist.Export;
using System;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    public sealed class ChatMetadataTests
    {
        /// <summary>Проверяет извлечение идентификатора разговора из корректной ссылки ChatGPT.</summary>
        [Test]
        public void ConversationId_ParsesGuidFromChatLink()
        {
            ChatMetadata metadata = new()
            {
                ChatLink = "https://chatgpt.com/c/11111111-1111-1111-1111-111111111111/"
            };

            Assert.That(
                metadata.ConversationId,
                Is.EqualTo(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        }

        /// <summary>Проверяет возврат null для отсутствующей или некорректной ссылки на разговор.</summary>
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("https://example.com/c/11111111-1111-1111-1111-111111111111")]
        [TestCase("https://chatgpt.com/c/not-a-guid")]
        public void ConversationId_ReturnsNull_WhenChatLinkIsMissingOrInvalid(
            string? chatLink)
        {
            ChatMetadata metadata = new()
            {
                ChatLink = chatLink
            };

            Assert.That(metadata.ConversationId, Is.Null);
        }
    }
}
