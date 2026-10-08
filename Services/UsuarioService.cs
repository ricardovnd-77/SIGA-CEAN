using SigaCean.Data;
using SigaCean.Models;

namespace SigaCean.Services;

// Quem está usando o sistema agora. Os dois perfis (Administrativo e Veterinário) saem daqui.
public class UsuarioService
{
    private readonly AppData _dados;

    public UsuarioService(AppData dados)
    {
        _dados = dados;
    }

    public Usuario? UsuarioLogado { get; private set; }

    public bool EhAdministrativo => UsuarioLogado?.Perfil == Perfil.Administrativo;

    // Devolve o usuário se login e senha conferem; senão devolve null.
    public Usuario? Login(string login, string senha)
    {
        var usuario = _dados.Usuarios.FirstOrDefault(u =>
            string.Equals(u.Login, login?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (usuario is null || usuario.Senha != senha)
            return null;

        UsuarioLogado = usuario;
        return usuario;
    }

    public void Logout() => UsuarioLogado = null;
}
