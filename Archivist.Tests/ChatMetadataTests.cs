using System;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests
{
    public sealed class ChatMetadataTests
    {
        [Fact]
        public void ConversationId_ParsesGuidFromChatLink()
        {
            ChatMetadata metadata = new()
            {
                ChatLink = "https://chatgpt.com/c/11111111-1111-1111-1111-111111111111/"
            };

            Assert.Equal(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                metadata.ConversationId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("https://example.com/c/11111111-1111-1111-1111-111111111111")]
        [InlineData("https://chatgpt.com/c/not-a-guid")]
        public void ConversationId_ReturnsNull_WhenChatLinkIsMissingOrInvalid(
            string? chatLink)
        {
            ChatMetadata metadata = new()
            {
                ChatLink = chatLink
            };

            Assert.Null(metadata.ConversationId);
        }
    }
}
