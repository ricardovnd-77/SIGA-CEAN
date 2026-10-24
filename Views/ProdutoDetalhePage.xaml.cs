using SigaCean.Services;

namespace SigaCean.Views;

// Ficha de consulta de um produto: situação, saldos por validade (na ordem FEFO) e as últimas movimentações.
// Os dois perfis consultam; as ações (Entrada, Saída, Editar) só aparecem para o Administrativo.
// A tela só mostra: a situação vem do EstoqueService (via ProdutoLinha) e o histórico vem do MovimentacaoService.
public partial class ProdutoDetalhePage : ContentPage
{
    private const int MovimentacoesExibidas = 10;

    private readonly ProdutoService _produtos;
    private readonly EstoqueService _estoque;
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    private Guid? _produtoId;

    public ProdutoDetalhePage(ProdutoService produtos, EstoqueService estoque, MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _produtos = produtos;
        _estoque = estoque;
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;
    }

    // Quem abre a ficha diz de qual produto ela é (a tela só guarda o identificador e busca o produto no Service).
    public void Carregar(Guid produtoId)
    {
        _produtoId = produtoId;
        Atualizar();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Atualizar();
    }

    private void Atualizar()
    {
        if (_produtoId is not { } id) return;

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var produto = _produtos.ObterPorId(id);
        var linha = new ProdutoLinha(produto, _estoque, hoje);

        NomeLabel.Text = produto.Nome;
        CategoriaLabel.Text = TextosDeEstoque.CategoriaComApresentacao(produto);
        AcoesLayout.IsVisible = _usuarios.EhAdministrativo;

        SituacaoCard.BindingContext = linha;
        MinimoLabel.Text = TextosDeEstoque.MinimoNaFicha(produto.QuantidadeMinima);

        // Saldos na ordem FEFO (a ordem vem do EstoqueService).
        var saldos = ProdutoLinha.SaldosEmOrdemFefo(produto, _estoque)
            .Select((saldo, i) => new SaldoLinha(saldo, i + 1, hoje))
            .ToList();
        BindableLayout.SetItemsSource(SaldosLista, saldos);
        SemSaldosLabel.IsVisible = saldos.Count == 0;

        // Só consulta: sem botões de corrigir/cancelar (isso é do Histórico).
        var movimentacoes = _movimentacoes.ConsultarHistorico()
            .Where(m => m.Produto.Id == produto.Id)
            .Take(MovimentacoesExibidas)
            .Select(m => new MovimentacaoLinha(m, administrativo: false))
            .ToList();
        BindableLayout.SetItemsSource(MovimentacoesLista, movimentacoes);
        SemMovimentacoesLabel.IsVisible = movimentacoes.Count == 0;
    }

    private async void OnVoltarClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    // Entrada, Saída e Editar abrem a tela respectiva (que fica no menu) já com este produto escolhido.
    private async void OnEntradaClicked(object? sender, EventArgs e)
    {
        if (_produtoId is { } id && Shell.Current is AppShell shell) await shell.IrParaEntrada(id);
    }

    private async void OnSaidaClicked(object? sender, EventArgs e)
    {
        if (_produtoId is { } id && Shell.Current is AppShell shell) await shell.IrParaSaida(id);
    }

    private async void OnEditarClicked(object? sender, EventArgs e)
    {
        if (_produtoId is { } id && Shell.Current is AppShell shell) await shell.IrParaProdutos(id);
    }
}
