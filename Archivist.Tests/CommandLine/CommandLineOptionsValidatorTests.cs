using System;
using dRz.GPT_Utilities.Archivist.CommandLine;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine;

[TestFixture]
public sealed class CommandLineOptionsValidatorTests
{
    private readonly CommandLineOptionsValidator _validator =
        new CommandLineOptionsValidator();

    [Test]
    public void Validate_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        TestDelegate action = () => _validator.Validate(null!);

        Assert.That(
            action,
            Throws.TypeOf<ArgumentNullException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void Validate_ThrowsArgumentException_WhenSourceDirectoryIsMissing(
        string? sourceDirectory)
    {
        CommandLineOptions options = CreateOptions(
            sourceDirectory: sourceDirectory);

        Assert.That(
            () => _validator.Validate(options),
            Throws.TypeOf<ArgumentException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void Validate_ThrowsArgumentException_WhenDestinationDirectoryIsMissing(
        string? destinationDirectory)
    {
        CommandLineOptions options = CreateOptions(
            destinationDirectory: destinationDirectory);

        Assert.That(
            () => _validator.Validate(options),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Validate_UsesDefaultPattern_WhenPatternIsMissing()
    {
        CommandLineOptions options = CreateOptions(
            zipFilePattern: null);

        CommandLineOptions result = _validator.Validate(options);

        Assert.That(result.ZipFilePattern, Is.EqualTo("*.zip"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void Validate_UsesDefaultPattern_WhenPatternIsWhitespace(
        string pattern)
    {
        CommandLineOptions options = CreateOptions(
            zipFilePattern: pattern);

        CommandLineOptions result = _validator.Validate(options);

        Assert.That(result.ZipFilePattern, Is.EqualTo("*.zip"));
    }

    [TestCase("backup", "backup.zip")]
    [TestCase("backup*", "backup*.zip")]
    [TestCase("backup.zip", "backup.zip")]
    [TestCase("backup.ZIP", "backup.ZIP")]
    [TestCase("  backup  ", "backup.zip")]
    public void Validate_NormalizesZipFilePattern(
        string pattern,
        string expectedPattern)
    {
        CommandLineOptions options = CreateOptions(
            zipFilePattern: pattern);

        CommandLineOptions result = _validator.Validate(options);

        Assert.That(
            result.ZipFilePattern,
            Is.EqualTo(expectedPattern));
    }

    [TestCase('\\')]
    [TestCase('/')]
    [TestCase(':')]
    [TestCase('"')]
    [TestCase('<')]
    [TestCase('>')]
    [TestCase('|')]
    public void Validate_ThrowsArgumentException_WhenPatternContainsInvalidCharacter(
        char invalidCharacter)
    {
        CommandLineOptions options = CreateOptions(
            zipFilePattern: $"backup{invalidCharacter}file");

        Assert.That(
            () => _validator.Validate(options),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Validate_PreservesOtherOptions()
    {
        CommandLineOptions options = CreateOptions(
            sourceDirectory: "source",
            destinationDirectory: "destination",
            zipFilePattern: "backup",
            extractAll: true,
            showHelp: true);

        CommandLineOptions result = _validator.Validate(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceDirectory, Is.EqualTo("source"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("destination"));
            Assert.That(result.ZipFilePattern, Is.EqualTo("backup.zip"));
            Assert.That(result.ExtractAll, Is.True);
            Assert.That(result.ShowHelp, Is.True);
            Assert.That(result, Is.Not.SameAs(options));
        });
    }

    private static CommandLineOptions CreateOptions(
        string? sourceDirectory = "source",
        string? destinationDirectory = "destination",
        string? zipFilePattern = "*.zip",
        bool extractAll = false,
        bool showHelp = false)
    {
        return new CommandLineOptions
        {
            SourceDirectory = sourceDirectory!,
            DestinationDirectory = destinationDirectory!,
            ZipFilePattern = zipFilePattern!,
            ExtractAll = extractAll,
            ShowHelp = showHelp
        };
    }
}