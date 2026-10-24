using Microsoft.Extensions.DependencyInjection;
using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Consulta do estoque (Administrativo e Veterinário). Só mostra: a situação de cada produto vem do EstoqueService
// e a busca vem do ProdutoService. Clicar em um produto abre a ficha dele.
public partial class EstoquePage : ContentPage
{
    private readonly EstoqueService _estoque;
    private readonly ProdutoService _produtos;
    private readonly IServiceProvider _servicos;

    private readonly List<Categoria> _categorias = Enum.GetValues<Categoria>().ToList();
    private readonly Dictionary<FiltroDeSituacao, Button> _chips = new();
    private FiltroDeSituacao _situacao = FiltroDeSituacao.Todos;
    private bool _carregando;

    public EstoquePage(EstoqueService estoque, ProdutoService produtos, IServiceProvider servicos)
    {
        InitializeComponent();
        _estoque = estoque;
        _produtos = produtos;
        _servicos = servicos;

        _carregando = true;
        CategoriaPicker.ItemsSource = new[] { "Todas as categorias" }
            .Concat(_categorias.Select(Rotulos.NomeDaCategoria)).ToList();
        CategoriaPicker.SelectedIndex = 0;
        _carregando = false;

        CriarChips();
    }

    // A tela é criada uma vez só (menu lateral): recarrega toda vez que aparece, para refletir entradas e saídas novas.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Atualizar();
    }

    // O Painel usa este método para abrir o Estoque já filtrado por uma situação (limpa a busca e a categoria).
    public void AplicarSituacao(FiltroDeSituacao situacao)
    {
        _situacao = situacao;

        _carregando = true;
        BuscaEntry.Text = string.Empty;
        CategoriaPicker.SelectedIndex = 0;
        _carregando = false;

        Atualizar();
    }

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (!_carregando) Atualizar();
    }

    // ---------- Botões de situação (chips) ----------

    // Um botão por situação, criado uma vez só; Atualizar muda o texto (com a contagem) e qual está escolhido.
    private void CriarChips()
    {
        foreach (var filtro in Enum.GetValues<FiltroDeSituacao>())
        {
            var chip = new Button { Margin = new Thickness(3) };
            chip.Clicked += (_, _) =>
            {
                _situacao = filtro;
                Atualizar();
            };
            _chips[filtro] = chip;
            SituacaoChips.Children.Add(chip);
        }
    }

    // Procura o estilo (Chip / ChipEscolhido) em Resources/Styles. TryGetValue também olha os dicionários mesclados
    // (o indexador Resources["..."] não olha).
    private static Style? Estilo(string chave) =>
        Application.Current!.Resources.TryGetValue(chave, out var valor) ? valor as Style : null;

    // "Sem estoque (42)": o nome do filtro e quantos produtos têm aquela situação (a conta usa as linhas já calculadas).
    private void AtualizarChips(List<ProdutoLinha> todos)
    {
        foreach (var (filtro, chip) in _chips)
        {
            chip.Text = $"{TextosDeEstoque.NomeDoFiltro(filtro)} ({todos.Count(l => l.Atende(filtro))})";
            chip.Style = Estilo(filtro == _situacao ? "ChipEscolhido" : "Chip");
        }
    }

    // ---------- Lista ----------

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
            .Where(l => l.Atende(_situacao))
            .OrderBy(l => l.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        ListaProdutos.ItemsSource = linhas;
        AtualizarChips(todos);

        ResumoLabel.Text = todos.Count == 0
            ? "Nenhum produto cadastrado ainda. Cadastre em Produtos e registre a primeira entrada."
            : $"{linhas.Count} de {todos.Count} produto(s)";
    }

    // Os dois perfis abrem a ficha (consulta); as ações da ficha só aparecem para o Administrativo.
    private async void OnProdutoTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: ProdutoLinha linha }) return;

        var ficha = _servicos.GetRequiredService<ProdutoDetalhePage>();
        ficha.Carregar(linha.Produto.Id);
        await Navigation.PushAsync(ficha);
    }
}
