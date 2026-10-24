using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Cadastro e edição de produtos (Administrativo). A tela coleta e mostra; quem valida e grava é o ProdutoService.
public partial class ProdutosPage : ContentPage
{
    private readonly ProdutoService _produtos;
    private readonly EstoqueService _estoque;

    private readonly List<Categoria> _categorias = Enum.GetValues<Categoria>().ToList();
    private Guid? _editandoId; // nulo = cadastrando um produto novo
    private bool _carregando;

    public ProdutosPage(ProdutoService produtos, EstoqueService estoque)
    {
        InitializeComponent();
        _produtos = produtos;
        _estoque = estoque;

        _carregando = true;
        CategoriaPicker.ItemsSource = _categorias.Select(Rotulos.NomeDaCategoria).ToList();
        FiltroCategoriaPicker.ItemsSource = new[] { "Todas as categorias" }
            .Concat(_categorias.Select(Rotulos.NomeDaCategoria)).ToList();
        FiltroCategoriaPicker.SelectedIndex = 0;
        _carregando = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AtualizarLista();
    }

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (!_carregando) AtualizarLista();
    }

    private void AtualizarLista()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        Categoria? categoria = FiltroCategoriaPicker.SelectedIndex > 0 ? _categorias[FiltroCategoriaPicker.SelectedIndex - 1] : null;

        // Pesquisar e Filtrar são do ProdutoService; a tela só cruza os dois resultados.
        var porNome = _produtos.Pesquisar(BuscaEntry.Text);
        var porCategoria = _produtos.Filtrar(categoria).ToHashSet();
        var linhas = porNome.Where(porCategoria.Contains).Select(p => new ProdutoLinha(p, _estoque, hoje)).ToList();

        ListaProdutos.ItemsSource = linhas;

        var total = _produtos.ObterTodos().Count;
        ResumoLabel.Text = total == 0
            ? "Nenhum produto cadastrado ainda. Clique em \"Novo produto\" para começar."
            : $"{linhas.Count} de {total} produto(s)";
    }

    // ---------- Formulário ----------

    private void OnNovoClicked(object? sender, EventArgs e)
    {
        _editandoId = null;
        FormularioTitulo.Text = "Novo produto";
        AvisoQuantidadeLabel.IsVisible = false;
        LimparFormulario();
        AbrirFormulario();
    }

    private void OnEditarClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: ProdutoLinha linha }) return;

        AbrirParaEdicao(linha.Produto);
    }

    // Outra tela (a ficha do produto) abre Produtos já editando este produto.
    public void PreSelecionar(Guid produtoId)
    {
        try
        {
            AbrirParaEdicao(_produtos.ObterPorId(produtoId));
        }
        catch (InvalidOperationException)
        {
            // Produto não encontrado: a tela abre normalmente, sem formulário.
        }
    }

    private void AbrirParaEdicao(Produto produto)
    {
        _editandoId = produto.Id;
        FormularioTitulo.Text = $"Editar produto: {produto.Nome}";
        AvisoQuantidadeLabel.IsVisible = true;

        NomeEntry.Text = produto.Nome;
        CategoriaPicker.SelectedIndex = _categorias.IndexOf(produto.Categoria);
        ApresentacaoEntry.Text = produto.Apresentacao;
        EspecificacaoEntry.Text = produto.Especificacao;
        MinimoEntry.Text = produto.QuantidadeMinima?.ToString();
        MensagemErroLabel.IsVisible = false;
        AbrirFormulario();
    }

    private void OnCancelarClicked(object? sender, EventArgs e) => FecharFormulario();

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        MensagemErroLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(NomeEntry.Text))
        {
            MostrarErro("Informe o nome do produto.");
            return;
        }
        if (CategoriaPicker.SelectedIndex < 0)
        {
            MostrarErro("Escolha a categoria.");
            return;
        }

        int? minimo = null;
        if (!string.IsNullOrWhiteSpace(MinimoEntry.Text))
        {
            minimo = Rotulos.LerInteiro(MinimoEntry.Text);
            if (minimo is null)
            {
                MostrarErro("A quantidade mínima deve ser um número inteiro.");
                return;
            }
        }

        var categoria = _categorias[CategoriaPicker.SelectedIndex];
        var nome = NomeEntry.Text.Trim();

        try
        {
            if (_editandoId is { } id)
                _produtos.Alterar(id, nome, categoria, ApresentacaoEntry.Text, EspecificacaoEntry.Text, minimo);
            else
                _produtos.Cadastrar(nome, categoria, ApresentacaoEntry.Text, EspecificacaoEntry.Text, minimo);
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        var mensagem = _editandoId is null ? $"Produto \"{nome}\" cadastrado." : $"Produto \"{nome}\" atualizado.";
        FecharFormulario();
        AtualizarLista();
        await DisplayAlertAsync("Produtos", mensagem, "OK");
    }

    private void AbrirFormulario()
    {
        FormularioCard.IsVisible = true;
        NovoButton.IsVisible = false;
        NomeEntry.Focus();
    }

    private void FecharFormulario()
    {
        FormularioCard.IsVisible = false;
        NovoButton.IsVisible = true;
        _editandoId = null;
        LimparFormulario();
    }

    private void LimparFormulario()
    {
        NomeEntry.Text = string.Empty;
        CategoriaPicker.SelectedIndex = -1;
        ApresentacaoEntry.Text = string.Empty;
        EspecificacaoEntry.Text = string.Empty;
        MinimoEntry.Text = string.Empty;
        MensagemErroLabel.IsVisible = false;
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
