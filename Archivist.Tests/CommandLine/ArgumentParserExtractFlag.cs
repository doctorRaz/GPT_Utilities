using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserExtractFlag
    {
        /// <summary>Parses the parses extract all with short flag.</summary>
        [Fact]
        public void Parse_ParsesExtractAll_WithShortFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });

            Assert.True(result.ExtractAll);
        }

        /// <summary>Parses the parses extract all with long flag.</summary>
        [Fact]
        public void Parse_ParsesExtractAll_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.True(result.ExtractAll);
        }

        /// <summary>Parses the extract all default is false.</summary>
        [Fact]
        public void Parse_ExtractAllDefaultIsFalse()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.False(result.ExtractAll);
        }

        /// <summary>Parses the handles multiple extract all flags.</summary>
        [Fact]
        public void Parse_HandlesMultipleExtractAllFlags()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a", "-a", "-a" });

            Assert.True(result.ExtractAll);
        }
    }
}