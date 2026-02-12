using System;
using System.Collections.Generic;
using System.Linq;
using Zxcvbn.Matcher.Matches;

namespace Zxcvbn
{
    /// <summary>
    /// Generates feedback based on the match results.
    /// </summary>
    internal static class Feedback
    {
        private static readonly FeedbackItem DefaultFeedback = new FeedbackItem
        {
            Warning = string.Empty,
            Suggestions = new[]
            {
                "Usa algunas palabras, evita frases comunes",
                "No necesitas símbolos, dígitos ni letras mayúsculas",
            },
        };

        /// <summary>
        /// Gets feedback based on the provided score and matches.
        /// </summary>
        /// <param name="score">The score to assess.</param>
        /// <param name="sequence">The sequence of matches to assess.</param>
        /// <returns>Any warnings and suggestiongs about the password matches.</returns>
        public static FeedbackItem GetFeedback(int score, IEnumerable<Match> sequence)
        {
            if (!sequence.Any())
                return DefaultFeedback;

            if (score > 2)
            {
                return new FeedbackItem
                {
                    Warning = string.Empty,
                    Suggestions = new List<string>(),
                };
            }

            var longestMatch = sequence.OrderBy(c => c.Token.Length).Last();

            var feedback = GetMatchFeedback(longestMatch, sequence.Count() == 1);
            var extraFeedback = "Agrega una o dos palabras más. Las palabras poco comunes son mejores.";

            if (feedback != null)
            {
                feedback.Suggestions.Insert(0, extraFeedback);
            }
            else
            {
                feedback = new FeedbackItem
                {
                    Warning = string.Empty,
                    Suggestions = new List<string> { extraFeedback },
                };
            }

            return feedback;
        }

        private static FeedbackItem GetDictionaryMatchFeedback(DictionaryMatch match, bool isSoleMatch)
        {
            var warning = string.Empty;

            if (match.DictionaryName == "passwords")
            {
                if (isSoleMatch && !match.L33t && !match.Reversed)
                {
                    if (match.Rank <= 10)
                        warning = "Esta es una de las 10 contraseñas más comunes";
                    else if (match.Rank <= 100)
                        warning = "Esta es una de las 100 contraseñas más comunes";
                    else
                        warning = "Esta es una contraseña muy común";
                }
                else if (match.GuessesLog10 <= 4)
                {
                    warning = "Esto es similar a una contraseña de uso común";
                }
            }
            else if ((match.DictionaryName == "english" || match.DictionaryName == "spanish") && isSoleMatch)
            {
                warning = "Una palabra por sí sola es fácil de adivinar";
            }
            else if (match.DictionaryName == "surnames" || match.DictionaryName == "male_names" || match.DictionaryName == "female_names")
            {
                if (isSoleMatch)
                    warning = "Nombres y apellidos por sí solos son fáciles de adivinar";
                else
                    warning = "Nombres y apellidos comunes son fáciles de adivinar";
            }

            var suggestions = new List<string>();
            var word = match.Token;
            if (char.IsUpper(word[0]))
                suggestions.Add("La capitalización no ayuda mucho");
            else if (word.All(c => char.IsUpper(c)) && word.ToLower() != word)
                suggestions.Add("Usar todo en mayúsculas es casi tan fácil de adivinar como todo en minúsculas");

            if (match.Reversed && match.Token.Length >= 4)
                suggestions.Add("Las palabras invertidas no son mucho más difíciles de adivinar");
            if (match.L33t)
                suggestions.Add("Sustituciones predecibles como '@' por 'a' no ayudan mucho");

            return new FeedbackItem
            {
                Suggestions = suggestions,
                Warning = warning,
            };
        }

        private static FeedbackItem GetMatchFeedback(Match match, bool isSoleMatch)
        {
            switch (match.Pattern)
            {
                case "dictionary":
                    return GetDictionaryMatchFeedback(match as DictionaryMatch, isSoleMatch);

                case "spatial":
                    return new FeedbackItem
                    {
                        Warning = (match as SpatialMatch).Turns == 1 ? "Filas rectas de teclas son fáciles de adivinar" : "Los patrones cortos de teclado son fáciles de adivinar",
                        Suggestions = new List<string>
                        {
                            "Usa un patrón de teclado más largo con más giros",
                        },
                    };

                case "repeat":
                    return new FeedbackItem
                    {
                        Warning = (match as RepeatMatch).BaseToken.Length == 1 ? "Repeticiones como 'aaa' son fáciles de adivinar" : "Repeticiones como 'abcabcabc' son sólo un poco más difíciles de adivinar que 'abc'",
                        Suggestions = new List<string>
                        {
                            "Evita palabras y caracteres repetidos",
                        },
                    };

                case "regex":
                    if ((match as RegexMatch).RegexName == "recent_year")
                    {
                        return new FeedbackItem
                        {
                            Warning = "Años recientes son fáciles de adivinar",
                            Suggestions = new List<string>
                            {
                                "Evita años recientes",
                                "Evita años que estén asociados contigo",
                            },
                        };
                    }

                    break;

                case "date":
                    return new FeedbackItem
                    {
                        Warning = "Las fechas suelen ser fáciles de adivinar",
                        Suggestions = new List<string>
                        {
                            "Evita fechas y años que estén asociados contigo",
                        },
                    };
            }

            return null;
        }
    }
}
