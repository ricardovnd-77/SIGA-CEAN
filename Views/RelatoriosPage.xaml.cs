using SigaCean.Models;
using SigaCean.Reports;
using SigaCean.Services;

namespace SigaCean.Views;

// Relatórios (Administrativo). Todos os números vêm do RelatorioService; a tela só escolhe o relatório e mostra o resultado.
// O "Consumo no período" tem painel próprio (período, ordem, busca, resumo e PDF); as outras opções são listas simples.
public partial class RelatoriosPage : ContentPage
{
    private static readonly Color Verde = Color.FromArgb("#169B62");
    private static readonly Color Laranja = Color.FromArgb("#F59B24");
    private static readonly Color Vermelho = Color.FromArgb("#D95757");

    private enum Relatorio { Consumo, Entradas, Saidas, EstoqueDisponivel, EstoqueBaixo, ValidadeProxima }

    private readonly RelatorioService _relatorios;
    private readonly UsuarioService _usuarios;

    private readonly List<(Relatorio Tipo, string Nome)> _opcoes =
    [
        (Relatorio.Consumo, "Consumo no período"),
        (Relatorio.Entradas, "Entradas no período"),
        (Relatorio.Saidas, "Saídas no período"),
        (Relatorio.EstoqueDisponivel, "Estoque disponível"),
        (Relatorio.EstoqueBaixo, "Estoque baixo"),
        (Relatorio.ValidadeProxima, "Validade próxima")
    ];
    private readonly List<(OrdemRelatorio Ordem, string Nome)> _ordens =
    [
        (OrdemRelatorio.MaiorConsumo, "Maior consumo"),
        (OrdemRelatorio.PorNome, "Nome do produto")
    ];
    private readonly List<Categoria> _categorias = Enum.GetValues<Categoria>().ToList();
    private List<ItemRelatorioConsumo> _itensConsumo = new(); // o que o Service devolveu para o período, antes da busca por nome
    private bool _carregando;

    public RelatoriosPage(RelatorioService relatorios, UsuarioService usuarios)
    {
        InitializeComponent();
        _relatorios = relatorios;
        _usuarios = usuarios;

        _carregando = true;
        RelatorioPicker.ItemsSource = _opcoes.Select(o => o.Nome).ToList();
        RelatorioPicker.SelectedIndex = 0;
        CategoriaPicker.ItemsSource = new[] { "Todas as categorias" }.Concat(_categorias.Select(Rotulos.NomeDaCategoria)).ToList();
        CategoriaPicker.SelectedIndex = 0;
        OrdemPicker.ItemsSource = _ordens.Select(o => o.Nome).ToList();
        OrdemPicker.SelectedIndex = 0;
        InicioPicker.Date = DateTime.Today.AddDays(-30);
        FimPicker.Date = DateTime.Today;
        SeletorPeriodo.Escolher(TipoPeriodo.Mensal); // o Consumo abre no mês atual
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

    // Consumo: painel próprio (período, categoria, ordem, busca, resumo e PDF). Entradas e Saídas: só as datas.
    // As demais opções não usam período nem categoria.
    private void AplicarFiltrosVisiveis()
    {
        var tipo = Escolhido;
        var consumo = tipo == Relatorio.Consumo;

        PeriodoPainel.IsVisible = tipo is Relatorio.Entradas or Relatorio.Saidas;
        CategoriaPicker.IsVisible = consumo;
        OrdemPicker.IsVisible = consumo;
        ConsumoPainel.IsVisible = consumo;
        ListaConsumo.IsVisible = consumo;
        ListaRelatorio.IsVisible = !consumo;
        ResumoLabel.IsVisible = !consumo;
    }

    private void OnPeriodoAlterado(object? sender, EventArgs e)
    {
        if (!_carregando) GerarConsumo();
    }

    private void OnPesquisaTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_carregando) AtualizarListaConsumo();
    }

    private void Gerar()
    {
        if (Escolhido == Relatorio.Consumo)
        {
            GerarConsumo();
            return;
        }

        var inicio = InicioPicker.Date ?? DateTime.Today.AddDays(-30);
        var fim = FimPicker.Date ?? DateTime.Today;
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        List<LinhaRelatorio> linhas;
        string resumo;

        try
        {
            switch (Escolhido)
            {
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

    // ---------- Consumo no período ----------

    // Busca o relatório no Service para o período escolhido (a tela não calcula nada).
    private void GerarConsumo()
    {
        Categoria? categoria = CategoriaPicker.SelectedIndex > 0 ? _categorias[CategoriaPicker.SelectedIndex - 1] : null;
        var ordem = _ordens[Math.Max(OrdemPicker.SelectedIndex, 0)].Ordem;

        try
        {
            _itensConsumo = _relatorios.GerarRelatorioConsumo(SeletorPeriodo.Inicio, SeletorPeriodo.FimInclusivo, categoria, ordem);
        }
        catch (InvalidOperationException ex)
        {
            // Data inicial depois da final: o Service recusa e a tela só mostra a mensagem.
            _itensConsumo = new List<ItemRelatorioConsumo>();
            AtualizarListaConsumo();
            MostrarMensagemConsumo(ex.Message);
            return;
        }

        AtualizarListaConsumo();
    }

    // Os produtos do período que passam pela busca por nome: é o que aparece na tela, no resumo e no PDF.
    private List<ItemRelatorioConsumo> ItensConsumoFiltrados()
    {
        var termo = PesquisaEntry.Text?.Trim();
        return string.IsNullOrEmpty(termo)
            ? _itensConsumo.ToList()
            : _itensConsumo.Where(i => Rotulos.Contem(Rotulos.ProdutoNaLista(i.Produto), termo)).ToList();
    }

    private void AtualizarListaConsumo()
    {
        var itens = ItensConsumoFiltrados();

        ListaConsumo.ItemsSource = itens.Select(i => new ConsumoLinha(i)).ToList();

        ResumoProdutosLabel.Text = itens.Count.ToString();
        ResumoEntradasLabel.Text = itens.Sum(i => i.TotalEntradas).ToString();
        ResumoSaidasLabel.Text = itens.Sum(i => i.TotalSaidas).ToString();
        ResumoAjustesLabel.Text = Rotulos.ComSinal(itens.Sum(i => i.TotalAjustes));

        GerarPdfButton.IsEnabled = itens.Count > 0;

        // Período sem nenhuma movimentação: diz onde está a última, para ninguém achar que o relatório quebrou.
        MostrarMensagemConsumo(itens.Count == 0 ? TextoSemMovimentacao(periodoVazio: _itensConsumo.Count == 0) : null);
    }

    private string TextoSemMovimentacao(bool periodoVazio)
    {
        const string padrao = "Nenhuma movimentação encontrada no período.";
        if (!periodoVazio || _relatorios.DataDaUltimaMovimentacao() is not { } ultima)
            return padrao;

        // A última movimentação está dentro do período (o que sobrou vazio foi o filtro de categoria): não há o que sugerir.
        var dia = ultima.Date;
        if (dia >= SeletorPeriodo.Inicio.Date && dia <= SeletorPeriodo.Fim.Date)
            return padrao;

        // O nome do mês vem do PeriodoRelatorio ("março de 2026"), sem depender do idioma do Windows.
        var mes = PeriodoRelatorio.Descrever(TipoPeriodo.Mensal, ultima, ultima);
        var caminho = SeletorPeriodo.Tipo == TipoPeriodo.Personalizado ? "ajuste as datas do período"
            : dia < SeletorPeriodo.Inicio.Date ? "use \"Anterior\" para chegar até lá"
            : "use \"Próximo\" para chegar até lá";

        return $"{padrao} A última movimentação registrada é de {mes}; {caminho}.";
    }

    private void MostrarMensagemConsumo(string? texto)
    {
        MensagemConsumoLabel.Text = texto ?? string.Empty;
        MensagemConsumoLabel.IsVisible = !string.IsNullOrEmpty(texto);
    }

    // Pede onde salvar, grava o PDF com o que está na tela e abre o arquivo.
    private async void OnGerarPdfClicked(object? sender, EventArgs e)
    {
        var itens = ItensConsumoFiltrados();
        if (itens.Count == 0) return;

        try
        {
            var caminho = await SalvarComo.PdfAsync(RelatorioConsumoPdf.NomeSugerido(SeletorPeriodo.Tipo, SeletorPeriodo.Inicio, SeletorPeriodo.Fim));
            if (caminho is null) return; // o usuário cancelou a escolha

            string? categoria = CategoriaPicker.SelectedIndex > 0 ? Rotulos.NomeDaCategoria(_categorias[CategoriaPicker.SelectedIndex - 1]) : null;
            RelatorioConsumoPdf.Gerar(caminho, itens, SeletorPeriodo.Tipo, SeletorPeriodo.Inicio, SeletorPeriodo.Fim, categoria, _usuarios.UsuarioLogado?.Nome);
            MostrarMensagemConsumo($"PDF salvo em: {caminho}");

            await Launcher.Default.OpenAsync(new OpenFileRequest("Relatório de Consumo", new ReadOnlyFile(caminho)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MostrarMensagemConsumo("Não foi possível salvar o PDF. Verifique se o arquivo não está aberto em outro programa e tente novamente.");
        }
        catch (Exception)
        {
            MostrarMensagemConsumo("Não foi possível gerar o PDF. Tente novamente.");
        }
    }
}
