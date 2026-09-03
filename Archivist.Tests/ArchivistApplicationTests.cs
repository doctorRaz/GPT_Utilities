using System;
using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests;

public sealed class ArchivistApplicationTests
{
    [Test]
    public void Run_ReturnsSuccessExitCode_WhenProcessingSucceeds()
    {
        using TempDirectory source = new();
        using TempDirectory destination = new();
        StubProcessor processor = new(CreateResult(failed: 0, archiveFailed: 0));
        ArchivistApplication application = new(
            new CommandLineOptionsValidator(),
            processor,
            new LocalFileSystem());

        int exitCode = application.Run(new[]
        {
            "-s", source.Path,
            "-d", destination.Path
        });

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public void Run_ReturnsErrorExitCode_WhenMarkdownProcessingFails()
    {
        using TempDirectory source = new();
        using TempDirectory destination = new();
        StubProcessor processor = new(CreateResult(failed: 1, archiveFailed: 0));
        ArchivistApplication application = new(
            new CommandLineOptionsValidator(),
            processor,
            new LocalFileSystem());

        int exitCode = application.Run(new[]
        {
            "-s", source.Path,
            "-d", destination.Path
        });

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void Run_ReturnsErrorExitCode_WhenArchiveProcessingFails()
    {
        using TempDirectory source = new();
        using TempDirectory destination = new();
        StubProcessor processor = new(CreateResult(failed: 0, archiveFailed: 1));
        ArchivistApplication application = new(
            new CommandLineOptionsValidator(),
            processor,
            new LocalFileSystem());

        int exitCode = application.Run(new[]
        {
            "-s", source.Path,
            "-d", destination.Path
        });

        Assert.That(exitCode, Is.EqualTo(1));
    }

    private static ExportResult CreateResult(int failed, int archiveFailed) =>
        new(
            failed + archiveFailed,
            0,
            0,
            0,
            failed,
            archiveFailed,
            Array.Empty<ExportError>(),
            Array.Empty<ExportError>());

    private sealed class StubProcessor : IChatGptExportProcessor
    {
        private readonly ExportResult _result;

        public StubProcessor(ExportResult result)
        {
            _result = result;
        }

        public ExportResult Process(ExportRequest request) => _result;
    }
}
