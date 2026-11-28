using System.Text.RegularExpressions;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed partial class Usuario
{
    private Usuario()
    {
    }

    private Usuario(string nome, string email, string senhaHash)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        CriadoEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public const int TamanhoMinimoNome = 3;
    public const int TamanhoMaximoNome = 100;
    public const int TamanhoMaximoEmail = 254;
    public const int TamanhoMinimoSenha = 8;

    public static Usuario CriacaoUsuario(string nome, string email, string senha, Func<string, string> gerarHashSenha, Notificacao notificacao)
    {
        nome = nome?.Trim();
        email = NormalizarEmail(email);

        if (string.IsNullOrWhiteSpace(nome))
            notificacao.Adicionar("O nome passado é inválido, não pode ser nulo.");

        else if (nome.Length < TamanhoMinimoNome || nome.Length > TamanhoMaximoNome)
            notificacao.Adicionar("O nome não pode ter menos que 3 caracteres ou mais que 100.");

        if (string.IsNullOrWhiteSpace(email) || email.Length > TamanhoMaximoEmail || !FormatoEmail().IsMatch(email))
            notificacao.Adicionar("O e-mail passado é inválido.");

        if (!SenhaValida(senha))
            notificacao.Adicionar("A senha deve ter no mínimo 8 caracteres, com pelo menos uma letra e um número.");

        if (notificacao.TemNotificacoes)
            return null;

        return new Usuario(nome, email, gerarHashSenha(senha));
    }

    public static void ValidarCredenciais(Usuario usuario, string senha, Func<string, string, bool> verificarSenha, Notificacao notificacao)
    {
        var senhaConfere = verificarSenha(usuario?.SenhaHash, senha);

        if (usuario is null || !senhaConfere)
            notificacao.AdicionarNaoAutorizado("E-mail ou senha inválidos.");
    }

    public static string NormalizarEmail(string email) => email?.Trim().ToLowerInvariant();

    private static bool SenhaValida(string senha)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < TamanhoMinimoSenha)
            return false;

        return senha.Any(char.IsLetter) && senha.Any(char.IsDigit);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoEmail();
}
