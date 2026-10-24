using SigaCean.Services;

namespace SigaCean.Views;

// Painel: visão geral do estoque (Administrativo). Os indicadores e as listas vêm dos Services; a tela só conta e mostra.
// Clicar em um quadro abre o Estoque já filtrado por aquela situação.
public partial class DashboardPage : ContentPage
{
    private const int QuantidadeDeItens = 8;

    private static readonly Color Vermelho = Color.FromArgb("#D95757");
    private static readonly Color Laranja = Color.FromArgb("#F59B24");
    private static readonly Color Verde = Color.FromArgb("#169B62");

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

        MostrarQuadro(SemEstoqueLabel, SemEstoqueContagem, produtos.Count(l => l.SemEstoque), Vermelho);
        MostrarQuadro(AbaixoDoMinimoLabel, AbaixoDoMinimoContagem, produtos.Count(l => l.AbaixoDoMinimo), Laranja);
        MostrarQuadro(VencidosLabel, VencidosContagem, produtos.Count(l => l.Vencido), Vermelho);
        MostrarQuadro(ProximosLabel, ProximosContagem, produtos.Count(l => l.ValidadeProxima), Laranja);
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

    // O número do quadro e o texto "N produtos"; zerado fica verde (nada precisa de atenção).
    private static void MostrarQuadro(Label numero, Label contagem, int quantidade, Color corDoAlerta)
    {
        numero.Text = quantidade.ToString();
        numero.TextColor = quantidade == 0 ? Verde : corDoAlerta;
        contagem.Text = TextosDeEstoque.ContagemDeProdutos(quantidade);
    }

    // ---------- Navegação ----------

    // O quadro diz qual situação abrir (CommandParameter no XAML).
    private async void OnQuadroTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string nome && Enum.TryParse<FiltroDeSituacao>(nome, out var situacao) && Shell.Current is AppShell shell)
            await shell.IrParaEstoque(situacao);
    }

    private async void OnEntradaClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is AppShell shell) await shell.IrParaEntrada();
    }

    private async void OnSaidaClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is AppShell shell) await shell.IrParaSaida();
    }

    private async void OnVerTodasClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is AppShell shell) await shell.IrParaHistorico();
    }
}
