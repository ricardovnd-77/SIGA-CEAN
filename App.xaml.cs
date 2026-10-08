using Microsoft.Extensions.DependencyInjection;
using SigaCean.Views;

namespace SigaCean;

public partial class App : Application
{
    private readonly IServiceProvider _servicos;

    public App(IServiceProvider servicos)
    {
        InitializeComponent();
        _servicos = servicos;
    }

    // O programa sempre começa na tela de login.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var login = _servicos.GetRequiredService<LoginPage>();
        var janela = new Window(new NavigationPage(login))
        {
            MinimumWidth = 900,
            MinimumHeight = 600
        };

        // Barra de título na cor da marca.
        janela.TitleBar = new TitleBar
        {
            Title = "SIGA-CEAN",
            Icon = "cean_logo.png",
            BackgroundColor = Color.FromArgb("#075F5B"),
            ForegroundColor = Colors.White
        };

        return janela;
    }
}
