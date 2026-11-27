using Infraestructure.Seguranca;

namespace Tests.Infrastructure;

public sealed class HashSenhaTest
{
    private readonly HashSenha _hashSenha = new();

    [Fact]
    public void HashNaoContemASenhaEmTextoPuro()
    {
        var hash = _hashSenha.Gerar("senhaForte123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain("senhaForte123", hash);
    }

    [Fact]
    public void MesmaSenhaGeraHashesDiferentes()
    {
        Assert.NotEqual(_hashSenha.Gerar("senhaForte123"), _hashSenha.Gerar("senhaForte123"));
    }

    [Fact]
    public void VerificarSenhaCorreta()
    {
        var hash = _hashSenha.Gerar("senhaForte123");

        Assert.True(_hashSenha.Verificar(hash, "senhaForte123"));
    }

    [Theory]
    [InlineData("senhaErrada123")]
    [InlineData("SENHAFORTE123")]
    [InlineData("")]
    [InlineData(null)]
    public void VerificarSenhaErrada(string senha)
    {
        var hash = _hashSenha.Gerar("senhaForte123");

        Assert.False(_hashSenha.Verificar(hash, senha));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void VerificarSemHash(string hash)
    {
        Assert.False(_hashSenha.Verificar(hash, "senhaForte123"));
    }
}
