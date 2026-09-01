namespace dRz.GPT_Utilities.Archivist.CommandLine
{
    internal sealed class CommandLineOptionsValidator
    {
        public CommandLineOptions Validate(CommandLineOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.SourceDirectory))
            {
                throw new ArgumentException(
                    "Не указан каталог с ZIP-архивами.",
                    nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.DestinationDirectory))
            {
                throw new ArgumentException(
                    "Не указан каталог назначения.",
                    nameof(options));
            }

            string pattern = NormalizeZipPattern(
                options.ZipFilePattern);

            return new CommandLineOptions
            {
                SourceDirectory = options.SourceDirectory,
                DestinationDirectory = options.DestinationDirectory,
                ExtractAll = options.ExtractAll,
                ShowHelp = options.ShowHelp,
                ZipFilePattern = pattern
            };
        }

        /// <summary>
        /// Маска ZIP-файлов экспорта ChatGPT по умолчанию.
        /// </summary>
        private const string _defaultZipFilePattern = "*.zip";

        /// <summary>Determines whether [is valid zip file pattern] [the specified pattern].</summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns><c>true</c> if [is valid zip file pattern] [the specified pattern]; otherwise, <c>false</c>.</returns>
        private static bool ContainsInvalidZipPatternCharacters(string pattern)
        {
            char[] invalidCharacters =
                {
                   '\\',
                   '/',
                   ':',
                   '"',
                   '<',
                   '>',
                   '|'
               };

            return pattern.IndexOfAny(invalidCharacters) >= 0;
        }

        private static string NormalizeZipPattern(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return _defaultZipFilePattern;
            }

            pattern = pattern.Trim();

            if (ContainsInvalidZipPatternCharacters(pattern))
            {
                throw new ArgumentException(
                    $"Недопустимая маска ZIP-файлов: {pattern}",
                    nameof(pattern));
            }

            return pattern.EndsWith(
                ".zip",
                StringComparison.OrdinalIgnoreCase)
                ? pattern
                : $"{pattern}.zip";
        }
    }
}