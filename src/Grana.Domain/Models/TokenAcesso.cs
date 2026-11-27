namespace Domain.Models;

public sealed record TokenAcesso(string Token, DateTime ExpiraEm)
{
    public const int ValidadeEmHoras = 2;
}
