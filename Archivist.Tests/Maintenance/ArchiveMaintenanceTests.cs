using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Maintenance;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace dRz.GPT_Utilities.Archivist.Tests.Maintenance;

public sealed class ArchiveMaintenanceTests
{
    [Test]
    public void Run_NormalizesHashToSpace()
    {
        using TempDirectory temp = new();
        string month = temp.Combine("2026", "01-January");
        Directory.CreateDirectory(month);
        File.WriteAllText(Path.Combine(month, "A # B.md"), "content");

        _ = CreateMaintenance().Run(temp.Path);

        Assert.That(File.Exists(Path.Combine(month, "A B.md")), Is.True);
    }

    [Test]
    public void Run_NormalizesFiles_AndRebuildsIndexesFromDisk()
    {
        using TempDirectory temp = new();
        string month = temp.Combine("2026", "08-August");
        Directory.CreateDirectory(month);
        File.WriteAllText(Path.Combine(month, "A # B.md"), "a");
        File.WriteAllText(Path.Combine(month, "B_тест.md"), "b");
        File.WriteAllText(Path.Combine(month, "_index.md"), "stale-link.md");
        Directory.CreateDirectory(temp.Combine("2026", "09-September"));
        Directory.CreateDirectory(temp.Combine("2025", "12-December"));

        ArchiveMaintenanceResult result = CreateMaintenance().Run(temp.Path);

        Assert.That(result.RenamedFiles, Is.EqualTo(2));
        Assert.That(File.Exists(Path.Combine(month, "A B.md")), Is.True);
        Assert.That(File.Exists(Path.Combine(month, "B тест.md")), Is.True);
        string index = File.ReadAllText(Path.Combine(month, "_index.md"));
        Assert.That(index, Does.Contain("[A B](A%20B.md)"));
        Assert.That(index, Does.Contain("[B тест](B%20%D1%82%D0%B5%D1%81%D1%82.md)"));
        Assert.That(index, Does.Not.Contain("stale-link.md"));
        Assert.That(File.ReadAllText(temp.Combine("2026", "_index.md")), Does.Contain("08-August/_index.md"));
        Assert.That(File.ReadAllText(temp.Combine("_index.md")), Does.Contain("2026/_index.md"));
    }

    [Test]
    public void Run_DoesNotLoseFile_WhenNamesCollide()
    {
        using TempDirectory temp = new();
        string month = temp.Combine("2026", "01-January");
        Directory.CreateDirectory(month);
        File.WriteAllText(Path.Combine(month, "A # B.md"), "hash");
        File.WriteAllText(Path.Combine(month, "A B.md"), "plain");

        ArchiveMaintenanceResult result = CreateMaintenance().Run(temp.Path);

        Assert.That(Directory.GetFiles(month, "*.md").Count(path => Path.GetFileName(path) != "_index.md"), Is.EqualTo(2));
        Assert.That(File.ReadAllText(Path.Combine(month, "A B (1).md")), Is.EqualTo("hash"));
        Assert.That(File.ReadAllText(Path.Combine(month, "A B.md")), Is.EqualTo("plain"));
        Assert.That(result.Conflicts, Is.EqualTo(1));
    }

    [Test]
    public void Run_IsIdempotent()
    {
        using TempDirectory temp = new();
        string month = temp.Combine("2026", "01-January");
        Directory.CreateDirectory(month);
        File.WriteAllText(Path.Combine(month, "A # B.md"), "content");

        ArchiveMaintenance maintenance = CreateMaintenance();
        _ = maintenance.Run(temp.Path);
        ArchiveMaintenanceResult second = maintenance.Run(temp.Path);

        Assert.That(second.RenamedFiles, Is.EqualTo(0));
        Assert.That(second.Conflicts, Is.EqualTo(0));
        Assert.That(second.UpdatedIndexes, Is.EqualTo(0));
    }

    [Test]
    public void Parse_AcceptsMaintenanceWithoutImportDirectories()
    {
        CommandLineOptions options = CommandLineParser.Parse(new[] { "--maintenance", "C:\\Vault" });

        Assert.That(options.IsMaintenance, Is.True);
        Assert.That(options.MaintenanceDirectory, Is.EqualTo("C:\\Vault"));
    }

    private static ArchiveMaintenance CreateMaintenance() =>
        new(
            new LocalFileSystem(),
            new FileNameNormalizer(),
            new DirectoryIndexWriter(new LocalFileSystem()));
}
