using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Infraestructure.Seguranca;

public sealed class ConfiguracaoJwt
{
    public const string Secao = "Jwt";
    public const int TamanhoMinimoChave = 32;

    public string Chave { get; set; }
    public string Emissor { get; set; }
    public string Audiencia { get; set; }

    public bool EstaValida()
        => !string.IsNullOrWhiteSpace(Chave)
            && Encoding.UTF8.GetByteCount(Chave) >= TamanhoMinimoChave
            && !string.IsNullOrWhiteSpace(Emissor)
            && !string.IsNullOrWhiteSpace(Audiencia);

    public SymmetricSecurityKey ObterChaveAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}
