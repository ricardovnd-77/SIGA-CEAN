namespace SigaCean.Models;

public class Usuario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty; // texto puro: é um protótipo acadêmico, sem persistência e sem acesso externo
    public Perfil Perfil { get; set; }
}
