using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Relatórios (Administrativo). Todos os números vêm do RelatorioService; a tela só escolhe o relatório e mostra o resultado.
public partial class RelatoriosPage : ContentPage
{
    private static readonly Color Verde = Color.FromArgb("#169B62");
    private static readonly Color Laranja = Color.FromArgb("#F59B24");
    private static readonly Color Vermelho = Color.FromArgb("#D95757");
    private static readonly Color Tinta = Color.FromArgb("#153B43");

    private enum Relatorio { Consumo, Entradas, Saidas, EstoqueDisponivel, EstoqueBaixo, ValidadeProxima }

    private readonly RelatorioService _relatorios;

    private readonly List<(Relatorio Tipo, string Nome)> _opcoes =
    [
        (Relatorio.Consumo, "Consumo no período"),
        (Relatorio.Entradas, "Entradas no período"),
        (Relatorio.Saidas, "Saídas no período"),
        (Relatorio.EstoqueDisponivel, "Estoque disponível"),
        (Relatorio.EstoqueBaixo, "Estoque baixo"),
        (Relatorio.ValidadeProxima, "Validade próxima")
    ];
    private readonly List<Categoria> _categorias = Enum.GetValues<Categoria>().ToList();
    private bool _carregando;

    public RelatoriosPage(RelatorioService relatorios)
    {
        InitializeComponent();
        _relatorios = relatorios;

        _carregando = true;
        RelatorioPicker.ItemsSource = _opcoes.Select(o => o.Nome).ToList();
        RelatorioPicker.SelectedIndex = 0;
        CategoriaPicker.ItemsSource = new[] { "Todas as categorias" }.Concat(_categorias.Select(Rotulos.NomeDaCategoria)).ToList();
        CategoriaPicker.SelectedIndex = 0;
        InicioPicker.Date = DateTime.Today.AddDays(-30);
        FimPicker.Date = DateTime.Today;
        _carregando = false;
        AplicarFiltrosVisiveis();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Gerar();
    }

    private Relatorio Escolhido => _opcoes[Math.Max(RelatorioPicker.SelectedIndex, 0)].Tipo;

    private void OnRelatorioChanged(object? sender, EventArgs e)
    {
        if (_carregando) return;
        AplicarFiltrosVisiveis();
        Gerar();
    }

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (!_carregando) Gerar();
    }

    private void OnFiltroDateChanged(object? sender, DateChangedEventArgs e) => OnFiltroChanged(sender, e);

    // O período vale para consumo, entradas e saídas; a categoria só para o consumo.
    private void AplicarFiltrosVisiveis()
    {
        var tipo = Escolhido;
        PeriodoPainel.IsVisible = tipo is Relatorio.Consumo or Relatorio.Entradas or Relatorio.Saidas;
        CategoriaPicker.IsVisible = tipo == Relatorio.Consumo;
    }

    private void Gerar()
    {
        var inicio = InicioPicker.Date ?? DateTime.Today.AddDays(-30);
        var fim = FimPicker.Date ?? DateTime.Today;
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        List<LinhaRelatorio> linhas;
        string resumo;

        try
        {
            switch (Escolhido)
            {
                case Relatorio.Consumo:
                {
                    Categoria? categoria = CategoriaPicker.SelectedIndex > 0 ? _categorias[CategoriaPicker.SelectedIndex - 1] : null;
                    var itens = _relatorios.GerarRelatorioConsumo(inicio, fim, categoria);
                    linhas = itens.Select(i => new LinhaRelatorio
                    {
                        Titulo = Rotulos.ProdutoNaLista(i.Produto),
                        Detalhe = $"{Rotulos.NomeDaCategoria(i.Produto.Categoria)} · Entradas: {i.TotalEntradas} · " +
                                  $"Saídas: {i.TotalSaidas} · Ajustes: {Rotulos.ComSinal(i.TotalAjustes)}",
                        Destaque = $"Estoque: {i.EstoqueAtual}",
                        CorDestaque = Tinta
                    }).ToList();
                    resumo = itens.Count == 0
                        ? "Nenhuma movimentação ativa neste período."
                        : $"{itens.Count} produto(s) · {itens.Sum(i => i.TotalEntradas)} entradas · {itens.Sum(i => i.TotalSaidas)} saídas no período";
                    break;
                }

                case Relatorio.Entradas:
                {
                    var entradas = _relatorios.ObterEntradas(inicio, fim);
                    linhas = entradas.Select(m => new LinhaRelatorio
                    {
                        Titulo = Rotulos.ProdutoNaLista(m.Produto),
                        Detalhe = new MovimentacaoLinha(m, false).DataEDetalhes,
                        Destaque = $"+{m.Quantidade}",
                        CorDestaque = Verde
                    }).ToList();
                    resumo = $"{entradas.Count} entrada(s) · {entradas.Sum(m => m.Quantidade)} unidade(s) recebida(s)";
                    break;
                }

                case Relatorio.Saidas:
                {
                    var saidas = _relatorios.ObterSaidas(inicio, fim);
                    linhas = saidas.Select(m => new LinhaRelatorio
                    {
                        Titulo = Rotulos.ProdutoNaLista(m.Produto),
                        Detalhe = new MovimentacaoLinha(m, false).DataEDetalhes,
                        Destaque = $"−{m.Quantidade}",
                        CorDestaque = Laranja
                    }).ToList();
                    resumo = $"{saidas.Count} saída(s) · {saidas.Sum(m => m.Quantidade)} unidade(s) retirada(s)";
                    break;
                }

                case Relatorio.EstoqueDisponivel:
                {
                    var produtos = _relatorios.ObterEstoqueDisponivel();
                    linhas = produtos.Select(p => new LinhaRelatorio
                    {
                        Titulo = Rotulos.ProdutoNaLista(p),
                        Detalhe = Rotulos.NomeDaCategoria(p.Categoria),
                        Destaque = $"{p.Quantidade} un.",
                        CorDestaque = Verde
                    }).ToList();
                    resumo = $"{produtos.Count} produto(s) com estoque disponível";
                    break;
                }

                case Relatorio.EstoqueBaixo:
                {
                    var produtos = _relatorios.ObterProdutosComEstoqueBaixo();
                    linhas = produtos.Select(p => new LinhaRelatorio
                    {
                        Titulo = Rotulos.ProdutoNaLista(p),
                        Detalhe = $"{Rotulos.NomeDaCategoria(p.Categoria)} · mínimo: {p.QuantidadeMinima}",
                        Destaque = $"{p.Quantidade} un.",
                        CorDestaque = p.Quantidade == 0 ? Vermelho : Laranja
                    }).ToList();
                    resumo = $"{produtos.Count} produto(s) no mínimo ou abaixo dele";
                    break;
                }

                default:
                {
                    var produtos = _relatorios.ObterProdutosProximosDaValidade();
                    linhas = produtos.Select(p =>
                    {
                        // Mostra a validade mais próxima do que ainda existe em estoque.
                        var proxima = p.Saldos.Where(s => s.Quantidade > 0 && s.DataValidade is not null)
                            .Min(s => s.DataValidade)!.Value;
                        return new LinhaRelatorio
                        {
                            Titulo = Rotulos.ProdutoNaLista(p),
                            Detalhe = $"{Rotulos.NomeDaCategoria(p.Categoria)} · {Rotulos.Validade(proxima, hoje)}",
                            Destaque = $"{p.Quantidade} un.",
                            CorDestaque = Laranja
                        };
                    }).ToList();
                    resumo = $"{produtos.Count} produto(s) vencendo em até {EstoqueService.DiasAlertaValidade} dias";
                    break;
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            // Data inicial depois da final: o Service recusa e a tela só mostra a mensagem.
            ListaRelatorio.ItemsSource = new List<LinhaRelatorio>();
            ResumoLabel.Text = ex.Message;
            return;
        }

        ListaRelatorio.ItemsSource = linhas;
        ResumoLabel.Text = resumo;
    }
}
