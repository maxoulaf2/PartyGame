using System.Collections.Frozen;
using System.Text.RegularExpressions;
using PartyGame.Contracts;

namespace PartyGame.Server.Packs;

/// <summary>
/// The French message of each pack problem, for the <c>validate</c> command: the console has no client to translate the
/// codes. Written after the messages of the game master console (<c>client/src/shared/i18n/fr.ts</c>).
/// </summary>
internal static partial class PackProblemMessages
{
    // The message of a code whose parameters are bounds: both, a minimum only, or a maximum only.
    private sealed record Bounds(string Between, string AtLeast, string AtMost);

    internal static readonly FrozenDictionary<PackProblemCode, object> Messages = new Dictionary<PackProblemCode, object>
    {
        [PackProblemCode.PackJsonInvalid] = "JSON mal formé, ligne {line}, colonne {column} : vérifiez les virgules, guillemets et accolades autour.",
        [PackProblemCode.PackPropertyMissing] = "Propriété obligatoire absente : {property}",
        [PackProblemCode.PackPropertyUnknown] = "Propriété inconnue : {property} (faute de frappe ?)",
        [PackProblemCode.PackValueTypeInvalid] = "Valeur incorrecte : il faut {expected}.",
        [PackProblemCode.PackValueOutOfRange] = "Valeur hors limites : de {min} à {max}",
        [PackProblemCode.PackTextLengthOutOfRange] = new Bounds(
            "Longueur du texte incorrecte : de {min} à {max} caractères",
            "Texte trop court : longueur minimale {min}",
            "Texte trop long : longueur maximale {max}"),
        [PackProblemCode.PackItemCountOutOfRange] = new Bounds(
            "Nombre d'éléments incorrect : de {min} à {max}",
            "Pas assez d'éléments : au moins {min}",
            "Trop d'éléments : au plus {max}"),
        [PackProblemCode.PackRoundTypeUnknown] = "Type d'activité inconnu : « {type} »",
        [PackProblemCode.PackMediaPathInvalid] = "Chemin de média mal écrit : {media} (séparez les dossiers par /, sans caractère spécial)",
        [PackProblemCode.PackMediaOutsidePack] = "Média hors du dossier du pack : {media}",
        [PackProblemCode.PackMediaTypeUnsupported] = "Format de média non pris en charge : {media} (il faut {expected})",
        [PackProblemCode.PackMediaMissing] = "Média introuvable : {media}",
        [PackProblemCode.PackMediaCaseMismatch] = "Majuscules et minuscules différentes du fichier : {media} au lieu de {actual}",
        [PackProblemCode.PackMediaUnreadable] = "Fichier MP3 illisible : {media}",
        [PackProblemCode.PackAudioExcerptStartBeyondEnd] = "L'extrait commence à {start} s, après la fin du morceau ({duration} s)",
        [PackProblemCode.PackLoadFailed] = "Le pack n'a pas pu être chargé à cause d'une erreur inattendue, détaillée dans le journal du serveur.",
        [PackProblemCode.QuizCorrectChoiceMissing] = "Question sans bonne réponse : marquez une proposition avec \"correct\": true",
        [PackProblemCode.QuizCorrectChoiceDuplicated] = "Question avec plusieurs bonnes réponses : une seule proposition doit avoir \"correct\": true",
        [PackProblemCode.QuizChoiceDuplicated] = "Proposition en double : « {choice} »",
        [PackProblemCode.BlindTestPointsMissing] = "Aucun morceau ne rapporte de points : donnez des points au titre, ou à l'artiste d'au moins un morceau",
        [PackProblemCode.OpenQuestionAnswerLengthOutOfRange] = "Réponse vide ou trop longue : de 1 à {max} caractères (maxLength de la manche)",
        [PackProblemCode.OpenQuestionAnswerEmpty] = "Réponse « {answer} » vide une fois la casse, les accents, la ponctuation et l'article initial ignorés",
        [PackProblemCode.OpenQuestionAnswerDuplicated] = "Variante « {answer} » en double : elle revient à la réponse attendue ou à une autre variante",
        [PackProblemCode.OpenQuestionAnswerNotNumeric] = "Réponse « {answer} » non numérique : une question \"numeric\" n'accepte que des chiffres",
    }.ToFrozenDictionary();

    // What a value should be, inserted in place of the expected parameter.
    private static readonly FrozenDictionary<string, string> _valueTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["string"] = "un texte entre guillemets",
        ["integer"] = "un nombre entier",
        ["number"] = "un nombre",
        ["boolean"] = "true ou false",
        ["array"] = "une liste entre crochets [ ]",
        ["object"] = "un objet entre accolades { }",
        ["image"] = "une image .jpg, .jpeg, .png ou .webp",
        ["audio"] = "un fichier .mp3",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The message of a problem, with its parameters filled in. A bound of 0 is no bound, as in the console.
    /// </summary>
    public static string Describe(PackProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        var parameters = problem.Parameters;
        var template = Messages.GetValueOrDefault(problem.Code) switch
        {
            string text => text,
            Bounds bounds => (parameters.TryGetValue("min", out var min) && min != "0", parameters.ContainsKey("max")) switch
            {
                (true, true) => bounds.Between,
                (true, false) => bounds.AtLeast,
                _ => bounds.AtMost,
            },
            _ => problem.Code.ToString(),
        };

        return Placeholder().Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (!parameters.TryGetValue(name, out var value))
            {
                return match.Value;
            }

            return name == "expected" ? _valueTypes.GetValueOrDefault(value, value) : value;
        });
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();
}
