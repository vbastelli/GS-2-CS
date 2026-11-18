namespace AguaNaRua.Tests;

internal sealed class FakeClock : TimeProvider
{
    public static readonly DateTimeOffset Instante = new(2026, 11, 18, 15, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Instante;
}
