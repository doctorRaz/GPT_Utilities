using System.IO;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests
{
    public sealed class FileNamerTests
    {
        [Theory]
        [InlineData("Моя_тема", "Моя тема")]
        [InlineData("Проверка___Staged__Diff", "Проверка Staged Diff")]
        [InlineData("  Тема  ", "Тема")]
        [InlineData("Тема\tс\tтабами", "Тема с табами")]
        [InlineData("Один пробел", "Один пробел")]
        public void Normalize_CollapsesUnderscoresAndWhitespace(
            string input,
            string expected)
        {
            Assert.Equal(expected, FileNamer.Normalize(input));
        }

        [Fact]
        public void GetUnique_ReturnsOriginalPath_WhenFileDoesNotExist()
        {
            using TempDirectory temp = new();
            string path = temp.Combine("Test.md");

            Assert.Equal(path, FileNamer.GetUnique(path));
        }

        [Fact]
        public void GetUnique_AppendsNumber_WhenFileExists()
        {
            using TempDirectory temp = new();
            string path = temp.Combine("Test.md");
            File.WriteAllText(path, "exists");

            string unique = FileNamer.GetUnique(path);

            Assert.Equal(temp.Combine("Test (1).md"), unique);
        }

        [Fact]
        public void GetUnique_SkipsOccupiedSuffixes()
        {
            using TempDirectory temp = new();
            File.WriteAllText(temp.Combine("Test.md"), "0");
            File.WriteAllText(temp.Combine("Test (1).md"), "1");
            File.WriteAllText(temp.Combine("Test (2).md"), "2");

            string unique = FileNamer.GetUnique(temp.Combine("Test.md"));

            Assert.Equal(temp.Combine("Test (3).md"), unique);
        }

        [Fact]
        public void GetUnique_Throws_WhenAllSuffixesAreTaken()
        {
            using TempDirectory temp = new();
            File.WriteAllText(temp.Combine("Test.md"), "0");

            for (int number = 1; number <= 100; number++)
            {
                File.WriteAllText(temp.Combine($"Test ({number}).md"), "x");
            }

            IOException ex = Assert.Throws<IOException>(
                () => FileNamer.GetUnique(temp.Combine("Test.md")));

            Assert.Contains("(100)", ex.Message);
        }
    }
}
