using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Common
{
    public static class TranslationResolver
    {
        public static T? Resolve<T>(IEnumerable<T> translations, LanguageCode requested, Func<T, LanguageCode> languageSelector)
        {
            var list = translations as IReadOnlyCollection<T> ?? translations.ToList();

            return list.FirstOrDefault(t => languageSelector(t) == requested)
                ?? list.FirstOrDefault(t => languageSelector(t) == LanguageCode.AZ)
                ?? list.FirstOrDefault(t => languageSelector(t) == LanguageCode.EN)
                ?? list.FirstOrDefault();
        }
    }

}
