using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System.IO;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Files
{
    public sealed class FileNamerTests
    {
        private readonly IFileNameNormalizer _normalizer = new FileNameNormalizer();

        private static IUniqueFileNameProvider CreateProvider() =>
            new UniqueFileNameProvider(new LocalFileSystem());

        [TestCase("Моя_тема", "Моя тема")]
        [TestCase("Проверка___Staged__Diff", "Проверка Staged Diff")]
        [TestCase("  Тема  ", "Тема")]
        [TestCase("Тема\tс\tтабами", "Тема с табами")]
        [TestCase("Один пробел", "Один пробел")]
        public void Normalize_CollapsesUnderscoresAndWhitespace(
            string input,
            string expected)
        {
            Assert.That(_normalizer.Normalize(input), Is.EqualTo(expected));
        }

        [Test]
        public void GetUnique_ReturnsOriginalPath_WhenFileDoesNotExist()
        {
            using TempDirectory temp = new();
            string path = temp.Combine("Test.md");

            Assert.That(CreateProvider().GetUnique(path), Is.EqualTo(path));
        }

        [Test]
        public void GetUnique_AppendsNumber_WhenFileExists()
        {
            using TempDirectory temp = new();
            string path = temp.Combine("Test.md");
            File.WriteAllText(path, "exists");

            string unique = CreateProvider().GetUnique(path);

            Assert.That(unique, Is.EqualTo(temp.Combine("Test (1).md")));
        }

        [Test]
        public void GetUnique_SkipsOccupiedSuffixes()
        {
            using TempDirectory temp = new();
            File.WriteAllText(temp.Combine("Test.md"), "0");
            File.WriteAllText(temp.Combine("Test (1).md"), "1");
            File.WriteAllText(temp.Combine("Test (2).md"), "2");

            string unique = CreateProvider().GetUnique(temp.Combine("Test.md"));

            Assert.That(unique, Is.EqualTo(temp.Combine("Test (3).md")));
        }

        [Test]
        public void GetUnique_Throws_WhenAllSuffixesAreTaken()
        {
            using TempDirectory temp = new();
            File.WriteAllText(temp.Combine("Test.md"), "0");

            for (int number = 1; number <= 100; number++)
            {
                File.WriteAllText(temp.Combine($"Test ({number}).md"), "x");
            }

            IOException ex = Assert.Throws<IOException>(
                () => CreateProvider().GetUnique(temp.Combine("Test.md")));

            Assert.That(ex.Message, Contains.Substring("(100)"));
        }
    }
}
