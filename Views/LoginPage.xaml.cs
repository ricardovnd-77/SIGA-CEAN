using Microsoft.Extensions.DependencyInjection;
using SigaCean.Services;

namespace SigaCean.Views;

public partial class LoginPage : ContentPage
{
    private readonly UsuarioService _usuarios;
    private readonly IServiceProvider _servicos;

    public LoginPage(UsuarioService usuarios, IServiceProvider servicos)
    {
        InitializeComponent();
        _usuarios = usuarios;
        _servicos = servicos;
    }

    private void OnEntrarClicked(object? sender, EventArgs e)
    {
        var usuario = _usuarios.Login(LoginEntry.Text ?? string.Empty, SenhaEntry.Text ?? string.Empty);
        if (usuario is null)
        {
            MensagemErroLabel.Text = "Usuário ou senha inválidos.";
            MensagemErroLabel.IsVisible = true;
            return;
        }

        // Entrou: troca a tela de login pelo menu principal (o menu depende do perfil de quem entrou).
        Application.Current!.Windows[0].Page = _servicos.GetRequiredService<AppShell>();
    }
}
