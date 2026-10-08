using Microsoft.Extensions.DependencyInjection;
using SigaCean.Services;
using SigaCean.Views;

namespace SigaCean;

// Menu lateral. O que aparece depende do perfil de quem entrou:
//   Administrativo -> todas as áreas
//   Veterinário    -> só a consulta do estoque (não recebe permissões administrativas)
public partial class AppShell : Shell
{
    private readonly IServiceProvider _servicos;
    private readonly UsuarioService _usuarios;

    public AppShell(IServiceProvider servicos, UsuarioService usuarios)
    {
        InitializeComponent();
        _servicos = servicos;
        _usuarios = usuarios;

        if (usuarios.EhAdministrativo)
            Items.Add(CriarItem("Dashboard", servicos.GetRequiredService<DashboardPage>()));

        Items.Add(CriarItem("Estoque", servicos.GetRequiredService<EstoquePage>()));

        if (usuarios.EhAdministrativo)
        {
            Items.Add(CriarItem("Produtos", servicos.GetRequiredService<ProdutosPage>()));
            Items.Add(CriarItem("Entrada", servicos.GetRequiredService<EntradaPage>()));
            Items.Add(CriarItem("Saída", servicos.GetRequiredService<SaidaPage>()));
            Items.Add(CriarItem("Histórico", servicos.GetRequiredService<HistoricoPage>()));
            Items.Add(CriarItem("Relatórios", servicos.GetRequiredService<RelatoriosPage>()));
        }
    }

    private static FlyoutItem CriarItem(string titulo, ContentPage pagina) => new()
    {
        Title = titulo,
        Items = { new ShellContent { Title = titulo, Content = pagina } }
    };

    // Volta para o login para trocar de usuário. Os dados em memória continuam; só a sessão termina.
    private async void OnSairClicked(object? sender, EventArgs e)
    {
        var confirmou = await DisplayAlertAsync("Sair", $"Encerrar a sessão de {_usuarios.UsuarioLogado?.Nome}?", "Sair", "Continuar");
        if (!confirmou) return;

        _usuarios.Logout();
        Application.Current!.Windows[0].Page = new NavigationPage(_servicos.GetRequiredService<LoginPage>());
    }
}
