using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Consulta do estoque (Administrativo e Veterinário). Só mostra: a situação de cada produto vem do EstoqueService
// e a busca vem do ProdutoService.
public partial class EstoquePage : ContentPage
{
    private readonly EstoqueService _estoque;
    private readonly ProdutoService _produtos;

    private readonly List<Categoria> _categorias = Enum.GetValues<Categoria>().ToList();
    private bool _carregando;

    public EstoquePage(EstoqueService estoque, ProdutoService produtos)
    {
        InitializeComponent();
        _estoque = estoque;
        _produtos = produtos;

        _carregando = true;
        CategoriaPicker.ItemsSource = new[] { "Todas as categorias" }
            .Concat(_categorias.Select(Rotulos.NomeDaCategoria)).ToList();
        CategoriaPicker.SelectedIndex = 0;
        SituacaoPicker.ItemsSource = new[] { "Todas as situações", "Sem estoque", "Abaixo do mínimo", "Validade vencida", "Validade próxima" };
        SituacaoPicker.SelectedIndex = 0;
        _carregando = false;
    }

    // A tela é criada uma vez só (menu lateral): recarrega toda vez que aparece, para refletir entradas e saídas novas.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Atualizar();
    }

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (!_carregando) Atualizar();
    }

    private void Atualizar()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        Categoria? categoria = CategoriaPicker.SelectedIndex > 0 ? _categorias[CategoriaPicker.SelectedIndex - 1] : null;

        var todos = _estoque.ConsultarEstoque().Select(p => new ProdutoLinha(p, _estoque, hoje)).ToList();

        // Busca pelo nome: é o ProdutoService quem pesquisa (ignora maiúsculas e acentos).
        var encontrados = _produtos.Pesquisar(BuscaEntry.Text).ToHashSet();

        var linhas = todos
            .Where(l => encontrados.Contains(l.Produto))
            .Where(l => categoria is null || l.Produto.Categoria == categoria)
            .Where(l => SituacaoPicker.SelectedIndex switch
            {
                1 => l.SemEstoque,
                2 => l.AbaixoDoMinimo,
                3 => l.Vencido,
                4 => l.ValidadeProxima,
                _ => true
            })
            .OrderBy(l => l.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        ListaProdutos.ItemsSource = linhas;

        ResumoLabel.Text = todos.Count == 0
            ? "Nenhum produto cadastrado ainda. Cadastre em Produtos e registre a primeira entrada."
            : $"{linhas.Count} de {todos.Count} produto(s) · " +
              $"{todos.Count(l => l.SemEstoque)} sem estoque · {todos.Count(l => l.AbaixoDoMinimo)} abaixo do mínimo · " +
              $"{todos.Count(l => l.Vencido)} com validade vencida · {todos.Count(l => l.ValidadeProxima)} vencendo em breve";
    }
}
