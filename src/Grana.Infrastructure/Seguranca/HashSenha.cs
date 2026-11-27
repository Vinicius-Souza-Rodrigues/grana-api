using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Infraestructure.Seguranca;

public sealed class HashSenha : IHashSenha
{
    private readonly PasswordHasher<Usuario> _passwordHasher = new();
    private readonly string _hashFicticio;

    public HashSenha()
    {
        _hashFicticio = _passwordHasher.HashPassword(null, Guid.NewGuid().ToString());
    }

    public string Gerar(string senha) => _passwordHasher.HashPassword(null, senha);

    public bool Verificar(string senhaHash, string senha)
    {
        var hashExiste = !string.IsNullOrEmpty(senhaHash);
        var resultado = _passwordHasher.VerifyHashedPassword(null, hashExiste ? senhaHash : _hashFicticio, senha ?? string.Empty);

        return hashExiste && !string.IsNullOrEmpty(senha) && resultado != PasswordVerificationResult.Failed;
    }
}
