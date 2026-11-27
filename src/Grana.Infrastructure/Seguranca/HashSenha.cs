using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Infraestructure.Seguranca;

public sealed class HashSenha : IHashSenha
{
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public string Gerar(string senha) => _passwordHasher.HashPassword(null, senha);

    public bool Verificar(string senhaHash, string senha)
    {
        if (string.IsNullOrEmpty(senhaHash) || string.IsNullOrEmpty(senha))
            return false;

        return _passwordHasher.VerifyHashedPassword(null, senhaHash, senha) != PasswordVerificationResult.Failed;
    }
}
