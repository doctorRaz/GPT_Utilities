using System;
using System.IO;
using System.Linq;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Export;

/// <summary>
/// NUnit-тесты выделенных компонентов архитектуры экспорта.
/// </summary>
[TestFixture]
public sealed class ExportArchitectureNUnitTests
{
    [Test]
    public void ArchiveSelector_ReturnsNewestArchive_WhenProcessAllIsFalse()
    {
        using TempDirectory source = new();
        string older = Path.Combine(source.Path, "older.zip");
        string newer = Path.Combine(source.Path, "newer.zip");
        File.WriteAllText(older, string.Empty);
        File.WriteAllText(newer, string.Empty);
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-2));
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);

        ExportRequest request = new(source.Path, source.Path, "*.zip", false);
        FileInfo[] result = new FileSystemArchiveSelector(new LocalFileSystem()).Select(request).ToArray();

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0].FullName, Is.EqualTo(newer));
    }

    [Test]
    public void PathBuilder_CreatesYearAndMonthDirectory()
    {
        using TempDirectory destination = new();
        ChatMetadata metadata = new()
        {
            CreateTime = new DateTimeOffset(2024, 3, 22, 14, 20, 15, TimeSpan.Zero)
        };

        string path = new ExportPathBuilder(new LocalFileSystem()).Build(
            destination.Path,
            metadata,
            "conversation.md");

        string expectedDirectory = Path.Combine(destination.Path, "2024", "03-March");
        Assert.That(path, Is.EqualTo(Path.Combine(expectedDirectory, "conversation.md")));
        Assert.That(Directory.Exists(expectedDirectory), Is.True);
    }
}
