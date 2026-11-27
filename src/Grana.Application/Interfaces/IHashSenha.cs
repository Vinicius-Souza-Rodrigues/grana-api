namespace Application.Interfaces;

public interface IHashSenha
{
    string Gerar(string senha);

    bool Verificar(string senhaHash, string senha);
}
