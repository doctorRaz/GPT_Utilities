using dRz.GPT_Utilities.Archivist.CommandLine;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserHelpTests
    {
        /// <summary>
        /// Тестирует возврат справки, когда аргументы не переданы.
        /// </summary>
        [Test]
        public void Parse_ReturnsShowHelp_WhenNoArgumentsProvided()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new string[] { });

            Assert.That(result.ShowHelp, Is.True);
            Assert.That(result.ExtractAll, Is.False);
            Assert.That(result.SourceDirectory, Is.Empty);
            Assert.That(result.DestinationDirectory, Is.Empty);
        }

        /// <summary>Проверяет показ справки по короткому параметру.</summary>
        [Test]
        public void Parse_ReturnsShowHelp_WhenHelpFlagShort()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-h" });

            Assert.That(result.ShowHelp, Is.True);
        }

        /// <summary>Проверяет показ справки по длинному параметру.</summary>
        [Test]
        public void Parse_ReturnsShowHelp_WhenHelpFlagLong()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--help" });

            Assert.That(result.ShowHelp, Is.True);
        }

        /// <summary>Проверяет показ справки по стандартному параметру Windows /?.</summary>
        [Test]
        public void Parse_ReturnsShowHelp_WhenHelpFlagWindows()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "/?" });

            Assert.That(result.ShowHelp, Is.True);
        }

        /// <summary>Проверяет регистронезависимый разбор параметра справки.</summary>
        [Test]
        public void Parse_IsCaseInsensitive_ForHelpFlag()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-H" });
            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--HELP" });
            CommandLineOptions result3 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--Help" });

            Assert.That(result1.ShowHelp, Is.True);
            Assert.That(result2.ShowHelp, Is.True);
            Assert.That(result3.ShowHelp, Is.True);
        }

        /// <summary>Проверяет немедленный возврат результата при обнаружении параметра справки.</summary>
        [Test]
        public void Parse_HelpFlagImmediatelyReturns()
        {
            // Help flag должен немедленно вернуть результат, остальные параметры игнорируются
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-h", "-s", "", "-d", "" });

            Assert.That(result.ShowHelp, Is.True);
        }
    }
}