using PartyGame.Contracts.Packs;
using PartyGame.Engine.Packs;

namespace PartyGame.Engine.Tests.Packs;

public sealed class PackMediaTests
{
    private static readonly MediaPath[] _media =
        [new("images/drapeau-japon.png"), new("monuments/tour-eiffel.jpg"), new("son.webp")];

    [Fact]
    public void Draw_SeveralMedia_GivesEachOneADistinctUrlSafeIdentifier()
    {
        // When
        var media = PackMedia.Draw(_media, new Random(42));

        // Then
        Assert.Equal(_media.Select(m => m.Value).Order(StringComparer.Ordinal), media.Files.Values.Select(m => m.Value).Order(StringComparer.Ordinal));
        Assert.Equal(_media.Length, media.Files.Keys.Select(id => id.Value).Distinct(StringComparer.Ordinal).Count());
        Assert.All(media.Files.Keys, id => Assert.Matches("^[A-Za-z0-9_-]{22}$", id.Value));
    }

    [Fact]
    public void Draw_AnyMedia_GivesIdentifiersThatRevealNothingOfTheFile()
    {
        // When
        var media = PackMedia.Draw(_media, new Random(42));

        // Then: neither the name of the file, nor its folder, nor its extension
        string[] parts = ["images", "drapeau", "japon", "monuments", "tour", "eiffel", "son", "png", "jpg", "webp"];
        Assert.All(media.Files.Keys, id => Assert.All(parts, part => Assert.DoesNotContain(part, id.Value, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Draw_SameSeed_GivesTheSameIdentifiers()
    {
        // When
        var first = PackMedia.Draw(_media, new Random(7));
        var second = PackMedia.Draw(_media, new Random(7));

        // Then: a replayed game serves its media under the same URLs
        Assert.Equal(first.Files.OrderBy(f => f.Key.Value, StringComparer.Ordinal), second.Files.OrderBy(f => f.Key.Value, StringComparer.Ordinal));
    }

    [Fact]
    public void Find_IdentifierOfAFile_ReturnsItsPath()
    {
        // Given
        var media = PackMedia.Draw(_media, new Random(42));
        var (id, path) = media.Files.First();

        // When
        var found = media.Find(id);

        // Then
        Assert.Equal(path, found);
    }

    [Theory]
    [InlineData("inconnu")]
    [InlineData("images/drapeau-japon.png")]
    [InlineData("drapeau-japon.png")]
    [InlineData("")]
    public void Find_UnknownIdentifierOrPath_ReturnsNothing(string id)
    {
        // Given
        var media = PackMedia.Draw(_media, new Random(42));

        // When
        var found = media.Find(new MediaId(id));

        // Then
        Assert.Null(found);
    }

    [Fact]
    public void Find_NoGameStarted_ReturnsNothing()
    {
        // When
        var found = PackMedia.Empty.Find(new MediaId("inconnu"));

        // Then
        Assert.Null(found);
    }

    [Fact]
    public void UrlOf_MediaOfThePack_IsTheMediaPrefixFollowedByItsIdentifier()
    {
        // Given
        var media = PackMedia.Draw(_media, new Random(42));
        var id = media.Files.Single(f => f.Value == _media[1]).Key;

        // When
        var url = media.UrlOf(_media[1]);

        // Then
        Assert.Equal($"/media/{id.Value}", url);
    }

    [Fact]
    public void UrlOf_MediaNotInThePack_Throws()
    {
        // Given
        var media = PackMedia.Draw(_media, new Random(42));

        // When
        var url = () => media.UrlOf(new MediaPath("images/absente.png"));

        // Then: the loading lists every media of a pack, so this is a bug
        Assert.Throws<InvalidOperationException>(url);
    }
}
