using SigaCean.Services;

namespace SigaCean.Views;

// Visão geral do estoque (Administrativo). Os indicadores e as listas vêm dos Services; a tela só conta e mostra.
public partial class DashboardPage : ContentPage
{
    private const int QuantidadeDeItens = 8;

    private readonly EstoqueService _estoque;
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    public DashboardPage(EstoqueService estoque, MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _estoque = estoque;
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Atualizar();
    }

    private void Atualizar()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        // Situação de cada produto: sempre perguntada ao EstoqueService (a regra não é repetida aqui).
        var produtos = _estoque.ConsultarEstoque().Select(p => new ProdutoLinha(p, _estoque, hoje)).ToList();

        SemEstoqueLabel.Text = produtos.Count(l => l.SemEstoque).ToString();
        AbaixoDoMinimoLabel.Text = produtos.Count(l => l.AbaixoDoMinimo).ToString();
        VencidosLabel.Text = produtos.Count(l => l.Vencido).ToString();
        ProximosLabel.Text = produtos.Count(l => l.ValidadeProxima).ToString();
        TotalLabel.Text = $"{produtos.Count} produto(s) cadastrado(s) · {produtos.Count(l => l.Quantidade > 0)} com estoque disponível";

        var atencao = produtos
            .Where(l => l.PrecisaDeAtencao)
            .OrderByDescending(l => l.SemEstoque || l.Vencido) // o mais urgente primeiro
            .ThenBy(l => l.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Take(QuantidadeDeItens)
            .ToList();
        BindableLayout.SetItemsSource(AtencaoLista, atencao);
        AtencaoVazioLabel.Text = produtos.Count == 0
            ? "Nenhum produto cadastrado ainda."
            : "Nenhum produto precisa de atenção agora.";
        AtencaoVazioLabel.IsVisible = atencao.Count == 0;

        var recentes = _movimentacoes.ConsultarHistorico()
            .Take(QuantidadeDeItens)
            .Select(m => new MovimentacaoLinha(m, _usuarios.EhAdministrativo))
            .ToList();
        BindableLayout.SetItemsSource(MovimentacoesLista, recentes);
        MovimentacoesVazioLabel.IsVisible = recentes.Count == 0;
    }
}
