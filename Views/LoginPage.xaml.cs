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

    // A tela de login abre com o cursor no usuário.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoginEntry.Focus();
    }

    // Enter no usuário passa para a senha.
    private void OnLoginCompleted(object? sender, EventArgs e) => SenhaEntry.Focus();

    private void OnMostrarSenhaChanged(object? sender, CheckedChangedEventArgs e) => SenhaEntry.IsPassword = !e.Value;

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
