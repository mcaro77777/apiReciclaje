using Reciclame.Api.Services;

namespace Reciclame.Api.Tests;

public sealed class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Hash_CanBeVerifiedWithOriginalPassword()
    {
        var hash = _service.Hash("Reciclame123!");

        Assert.True(_service.Verify("Reciclame123!", hash));
    }

    [Fact]
    public void Verify_RejectsWrongOrMalformedPasswords()
    {
        var hash = _service.Hash("Reciclame123!");

        Assert.False(_service.Verify("incorrecta", hash));
        Assert.False(_service.Verify("Reciclame123!", "invalid-hash"));
    }

    [Fact]
    public void Hash_UsesANewSaltForEveryPassword()
    {
        var first = _service.Hash("Reciclame123!");
        var second = _service.Hash("Reciclame123!");

        Assert.NotEqual(first, second);
    }
}
