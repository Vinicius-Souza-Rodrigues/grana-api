using Domain.Models;

namespace Application.Interfaces;

public interface IGeradorToken
{
    TokenAcesso Gerar(Usuario usuario);
}
