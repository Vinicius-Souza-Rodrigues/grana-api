using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class UsuarioTest
{
    private readonly Notificacao _notificacao = new();

    private static string GerarHash(string senha) => $"hash:{senha}";

    [Fact]
    public void CriarUsuarioValido()
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", "maria@email.com", "senhaForte123", GerarHash, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.Equal("Maria Silva", usuario.Nome);
        Assert.Equal("maria@email.com", usuario.Email);
        Assert.Equal("hash:senhaForte123", usuario.SenhaHash);
        Assert.NotEqual(default, usuario.CriadoEm);
    }

    [Fact]
    public void EmailESalvoEmMinusculasESemEspacos()
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", "  Maria@Email.COM ", "senhaForte123", GerarHash, _notificacao);

        Assert.Equal("maria@email.com", usuario.Email);
    }

    [Fact]
    public void NomeVazioESenha123GeramOsDoisErros()
    {
        var usuario = Usuario.CriacaoUsuario("", "maria@email.com", "123", GerarHash, _notificacao);

        Assert.Null(usuario);
        Assert.Equal(2, _notificacao.Notificacoes.Count);
        Assert.Contains("O nome passado é inválido, não pode ser nulo.", _notificacao.Notificacoes);
        Assert.Contains("A senha deve ter no mínimo 8 caracteres, com pelo menos uma letra e um número.", _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData("Ma")]
    [InlineData("   Ma   ")]
    public void NomeComMenosDeTresCaracteres(string nome)
    {
        var usuario = Usuario.CriacaoUsuario(nome, "maria@email.com", "senhaForte123", GerarHash, _notificacao);

        Assert.Null(usuario);
        Assert.Contains("O nome não pode ter menos que 3 caracteres ou mais que 100.", _notificacao.Notificacoes);
    }

    [Fact]
    public void NomeComMaisDeCemCaracteres()
    {
        var usuario = Usuario.CriacaoUsuario(new string('a', 101), "maria@email.com", "senhaForte123", GerarHash, _notificacao);

        Assert.Null(usuario);
        Assert.Contains("O nome não pode ter menos que 3 caracteres ou mais que 100.", _notificacao.Notificacoes);
    }

    [Fact]
    public void NomeComCemCaracteresEValido()
    {
        var usuario = Usuario.CriacaoUsuario(new string('a', 100), "maria@email.com", "senhaForte123", GerarHash, _notificacao);

        Assert.NotNull(usuario);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("maria")]
    [InlineData("maria@")]
    [InlineData("maria@email")]
    [InlineData("@email.com")]
    [InlineData("maria silva@email.com")]
    [InlineData("maria@@email.com")]
    public void EmailInvalido(string email)
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", email, "senhaForte123", GerarHash, _notificacao);

        Assert.Null(usuario);
        Assert.Contains("O e-mail passado é inválido.", _notificacao.Notificacoes);
    }

    [Fact]
    public void EmailMaiorQueOLimite()
    {
        var email = $"{new string('a', 250)}@email.com";

        var usuario = Usuario.CriacaoUsuario("Maria Silva", email, "senhaForte123", GerarHash, _notificacao);

        Assert.Null(usuario);
        Assert.Contains("O e-mail passado é inválido.", _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void SenhaInvalida(string senha)
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", "maria@email.com", senha, GerarHash, _notificacao);

        Assert.Null(usuario);
        Assert.Single(_notificacao.Notificacoes);
        Assert.Contains("A senha deve ter no mínimo 8 caracteres, com pelo menos uma letra e um número.", _notificacao.Notificacoes);
    }

    [Fact]
    public void SenhaComOitoCaracteresLetraENumeroEValida()
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", "maria@email.com", "abcdefg1", GerarHash, _notificacao);

        Assert.NotNull(usuario);
    }

    [Fact]
    public void CredenciaisCorretas()
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", "maria@email.com", "senhaForte123", GerarHash, new Notificacao());

        Usuario.ValidarCredenciais(usuario, "senhaForte123", (hash, senha) => hash == GerarHash(senha), _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
    }

    [Fact]
    public void SenhaErradaEUsuarioInexistenteTemAMesmaMensagem()
    {
        var usuario = Usuario.CriacaoUsuario("Maria Silva", "maria@email.com", "senhaForte123", GerarHash, new Notificacao());
        var notificacaoUsuarioInexistente = new Notificacao();

        Usuario.ValidarCredenciais(usuario, "senhaErrada123", (hash, senha) => hash == GerarHash(senha), _notificacao);
        Usuario.ValidarCredenciais(null, "senhaForte123", (hash, senha) => hash == GerarHash(senha), notificacaoUsuarioInexistente);

        Assert.Equal(["E-mail ou senha inválidos."], _notificacao.Notificacoes);
        Assert.Equal(_notificacao.Notificacoes, notificacaoUsuarioInexistente.Notificacoes);
        Assert.Equal(TipoNotificacaoEnum.NaoAutorizado, _notificacao.Tipo);
        Assert.Equal(TipoNotificacaoEnum.NaoAutorizado, notificacaoUsuarioInexistente.Tipo);
    }

    [Fact]
    public void UsuarioInexistenteTambemPassaPelaVerificacaoDeSenha()
    {
        var verificou = false;

        Usuario.ValidarCredenciais(null, "senhaForte123", (hash, senha) =>
        {
            verificou = true;
            return false;
        }, _notificacao);

        Assert.True(verificou);
    }

    [Fact]
    public void HashNaoEGeradoQuandoHaErros()
    {
        var hashGerado = false;

        Usuario.CriacaoUsuario("", "maria@email.com", "senhaForte123", senha =>
        {
            hashGerado = true;
            return senha;
        }, _notificacao);

        Assert.False(hashGerado);
    }
}
