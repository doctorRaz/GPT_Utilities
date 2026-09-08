namespace dRz.GPT_Utilities.Archivist.Localization
{
    /// <summary>Методы склонения русских существительных по числу.</summary>
    public static class RussianPluralExtensions
    {
        /// <summary>
        /// Возвращает соответствующую числу форму существительного.
        /// </summary>
        /// <param name="number">Число, для которого выбирается форма.</param>
        /// <param name="forms">Набор форм существительного.</param>
        /// <param name="onlyWord">Если <see langword="true"/>, возвращается только слово без числа.</param>
        /// <returns>Строка с числом и существительным либо только существительное.</returns>
        public static string Of(this int number, RussianPluralForms forms, bool onlyWord = false)
        {
            int value = Math.Abs(number);
            string word;

            if (value % 100 is >= 11 and <= 19)
                word = forms.Many;
            else
            {
                word = (value % 10) switch
                {
                    1 => forms.One,
                    2 or 3 or 4 => forms.Few,
                    _ => forms.Many
                };
            }

            return onlyWord ? word : $"{number} {word}";
        }
    }
}