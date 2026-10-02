using System.ComponentModel.DataAnnotations;
using System.Globalization;
using PartyGame.Contracts;

namespace PartyGame.Content;

/// <summary>
/// Checks the simple constraints declared on the descriptor types, the same attributes that the JSON Schema of the packs
/// is generated from (ADR 0004).
/// </summary>
internal static class Constraints
{
    /// <summary>
    /// Checks a value against a constraint of its property.
    /// </summary>
    /// <returns>The problem, or <see langword="null"/> when the value meets the constraint.</returns>
    /// <exception cref="InvalidOperationException">
    /// A descriptor type declares a constraint this check does not translate into a problem: a bug, to fix here.
    /// </exception>
    public static PackProblem? Check(ValidationAttribute constraint, object? value, string path)
    {
        if (constraint.IsValid(value))
        {
            return null;
        }

        var lengthCode = value is string ? PackProblemCode.PackTextLengthOutOfRange : PackProblemCode.PackItemCountOutOfRange;
        return constraint switch
        {
            RangeAttribute range => Problem(PackProblemCode.PackValueOutOfRange, path, range.Minimum, range.Maximum),
            StringLengthAttribute length => Problem(PackProblemCode.PackTextLengthOutOfRange, path, length.MinimumLength, length.MaximumLength),
            LengthAttribute length => Problem(lengthCode, path, length.MinimumLength, length.MaximumLength),
            MinLengthAttribute length => Problem(lengthCode, path, length.Length, max: null),
            MaxLengthAttribute length => Problem(lengthCode, path, min: null, length.Length),
            _ => throw new InvalidOperationException($"Constraint {constraint.GetType().Name} has no pack problem."),
        };
    }

    private static PackProblem Problem(PackProblemCode code, string path, object? min, object? max)
    {
        var parameters = new List<(string, string)>();
        if (min is not null)
        {
            parameters.Add(("min", Convert.ToString(min, CultureInfo.InvariantCulture)!));
        }

        if (max is not null)
        {
            parameters.Add(("max", Convert.ToString(max, CultureInfo.InvariantCulture)!));
        }

        return Problems.InDescriptor(code, path, [.. parameters]);
    }
}
