using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ClientErrorAllowanceTests
{
    private static readonly DateTimeOffset _start = new(2026, 10, 3, 22, 0, 0, TimeSpan.Zero);

    private readonly ClientErrorAllowance _allowance = new();

    [Fact]
    public void Admit_WithinTheAllowance_AdmitsEveryReport()
    {
        // When
        var admissions = Enumerable.Range(0, ClientErrorAllowance.ReportsPerWindow).Select(_ => _allowance.Admit(_start)).ToList();

        // Then
        Assert.All(admissions, admission => Assert.Equal(ClientErrorAdmission.Admitted, admission));
    }

    [Fact]
    public void Admit_BeyondTheAllowance_DropsAndTellsOnlyOnce()
    {
        // Given
        AdmitAll(_start);

        // When
        var first = _allowance.Admit(_start);
        var second = _allowance.Admit(_start.AddSeconds(1));

        // Then
        Assert.Equal((ClientErrorAdmission.FirstDropped, ClientErrorAdmission.Dropped), (first, second));
    }

    [Fact]
    public void Admit_AfterTheWindow_AdmitsAgain()
    {
        // Given
        AdmitAll(_start);
        _allowance.Admit(_start);

        // When
        var admission = _allowance.Admit(_start + ClientErrorAllowance.Window);

        // Then
        Assert.Equal(ClientErrorAdmission.Admitted, admission);
    }

    [Fact]
    public void Admit_FloodingAgainInALaterWindow_NeverTellsTwice()
    {
        // Given
        AdmitAll(_start);
        _allowance.Admit(_start);
        var later = _start + ClientErrorAllowance.Window;
        AdmitAll(later);

        // When
        var admission = _allowance.Admit(later);

        // Then
        Assert.Equal(ClientErrorAdmission.Dropped, admission);
    }

    private void AdmitAll(DateTimeOffset at)
    {
        for (var i = 0; i < ClientErrorAllowance.ReportsPerWindow; i++)
        {
            Assert.Equal(ClientErrorAdmission.Admitted, _allowance.Admit(at));
        }
    }
}
