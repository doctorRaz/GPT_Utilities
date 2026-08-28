using System;

namespace dRz.GPT_Utilities.Archivist
{
    namespace dRz.GPT_Utilities.Archivist
    {
        public static class PluralExtensions
        {
            public static string Of(this int number, PluralForms forms)
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

                return $"{number} {word}";
            }
        }

        public readonly record struct PluralForms(
            string One,
            string Few,
            string Many);

        
    }
}
