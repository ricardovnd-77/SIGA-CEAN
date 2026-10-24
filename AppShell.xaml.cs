using Microsoft.Extensions.DependencyInjection;
using SigaCean.Services;
using SigaCean.Views;

namespace SigaCean;

// Menu lateral. O que aparece depende do perfil de quem entrou:
//   Administrativo -> todas as áreas
//   Veterinário    -> só a consulta do estoque (não recebe permissões administrativas)
//
// Também é daqui que as telas navegam umas para as outras (IrParaEstoque, IrParaEntrada...): cada método só funciona
// se o perfil de quem entrou tem aquela área no menu, então o Veterinário nunca chega numa tela administrativa.
public partial class AppShell : Shell
{
    private readonly IServiceProvider _servicos;
    private readonly UsuarioService _usuarios;

    // As telas do menu (uma de cada) e o item do menu onde cada uma está.
    private readonly Dictionary<Type, ContentPage> _paginas = new();
    private readonly Dictionary<Type, FlyoutItem> _itens = new();

    public AppShell(IServiceProvider servicos, UsuarioService usuarios)
    {
        InitializeComponent();
        _servicos = servicos;
        _usuarios = usuarios;

        if (usuarios.EhAdministrativo)
            Items.Add(CriarItem("Painel", servicos.GetRequiredService<DashboardPage>()));

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

    private FlyoutItem CriarItem(string titulo, ContentPage pagina)
    {
        var item = new FlyoutItem
        {
            Title = titulo,
            Items = { new ShellContent { Title = titulo, Content = pagina } }
        };
        _paginas[pagina.GetType()] = pagina;
        _itens[pagina.GetType()] = item;
        return item;
    }

    // ---------- Navegação entre telas do menu ----------

    // Abre o Estoque já filtrado por uma situação (usado pelos quadros do Painel).
    public Task IrParaEstoque(FiltroDeSituacao situacao) =>
        IrParaAsync<EstoquePage>(pagina => pagina.AplicarSituacao(situacao));

    public Task IrParaHistorico() => IrParaAsync<HistoricoPage>();

    // Abre a Entrada, com o produto já escolhido quando houver um.
    public Task IrParaEntrada(Guid? produtoId = null) =>
        IrParaAsync<EntradaPage>(pagina => { if (produtoId is { } id) pagina.PreSelecionar(id); });

    // Abre a Saída, com o produto já escolhido quando houver um.
    public Task IrParaSaida(Guid? produtoId = null) =>
        IrParaAsync<SaidaPage>(pagina => { if (produtoId is { } id) pagina.PreSelecionar(id); });

    // Abre Produtos já editando o produto escolhido, quando houver um.
    public Task IrParaProdutos(Guid? produtoId = null) =>
        IrParaAsync<ProdutosPage>(pagina => { if (produtoId is { } id) pagina.PreSelecionar(id); });

    private async Task IrParaAsync<T>(Action<T>? preparar = null) where T : ContentPage
    {
        // Perfil sem acesso a esta tela (ex.: Veterinário): não faz nada.
        if (!_itens.TryGetValue(typeof(T), out var item) || _paginas[typeof(T)] is not T pagina)
            return;

        // Se havia uma ficha de produto aberta por cima da tela atual, fecha: senão ela voltaria a aparecer depois.
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopToRootAsync(false);

        preparar?.Invoke(pagina);
        CurrentItem = item;
    }

    // Volta para o login para trocar de usuário. Os dados em memória continuam; só a sessão termina.
    private async void OnSairClicked(object? sender, EventArgs e)
    {
        var confirmou = await DisplayAlertAsync("Sair", $"Encerrar a sessão de {_usuarios.UsuarioLogado?.Nome}?", "Sair", "Continuar");
        if (!confirmou) return;

        _usuarios.Logout();
        Application.Current!.Windows[0].Page = new NavigationPage(_servicos.GetRequiredService<LoginPage>());
    }
}
