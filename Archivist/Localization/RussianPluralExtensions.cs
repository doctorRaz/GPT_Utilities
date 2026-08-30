namespace dRz.GPT_Utilities.Archivist.Localization
{
    /// <summary>
    /// русский язык Pluralization Extensions
    /// </summary>
    public static class RussianPluralExtensions
    {
        public static string Of(this int number, RussianPluralForms forms, bool onlyWord = false)
        {
            int value = Math.Abs(number);

            string word;

            if (value % 100 is >= 11 and <= 19)
            {
                word = forms.Many;
            }
            else
            {
                word = (value % 10) switch
                {
                    1 => forms.One,
                    2 or 3 or 4 => forms.Few,
                    _ => forms.Many
                };
            }
            if (onlyWord)
            {
                return $"{word}";
            }
            return $"{number} {word}";
        }
    }
}