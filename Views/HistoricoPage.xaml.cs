using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Histórico de movimentações, com corrigir e cancelar (só Administrativo). Nada é apagado: o MovimentacaoService
// desfaz o efeito no estoque e a movimentação continua na lista com o status e o motivo.
public partial class HistoricoPage : ContentPage
{
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    private readonly List<TipoMovimentacao> _tipos = Enum.GetValues<TipoMovimentacao>().ToList();
    private readonly List<StatusMovimentacao> _status = Enum.GetValues<StatusMovimentacao>().ToList();
    private bool _carregando;

    // Desenhar centenas de linhas de uma vez deixa a tela lenta: mostra-se um bloco e "Mostrar mais" traz o próximo.
    private const int TamanhoDoBloco = 100;
    private int _limiteExibido = TamanhoDoBloco;

    public HistoricoPage(MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;

        _carregando = true;
        TipoPicker.ItemsSource = new[] { "Todos os tipos" }.Concat(_tipos.Select(Rotulos.NomeDoTipo)).ToList();
        TipoPicker.SelectedIndex = 0;
        StatusPicker.ItemsSource = new[] { "Todas as situações" }.Concat(_status.Select(Rotulos.NomeDoStatus)).ToList();
        StatusPicker.SelectedIndex = 0;
        InicioPicker.Date = DateTime.Today.AddDays(-30);
        FimPicker.Date = DateTime.Today;
        _carregando = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Atualizar(recomecar: true);
    }

    // Mudou a busca ou um filtro: a lista volta a mostrar só o primeiro bloco.
    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (!_carregando) Atualizar(recomecar: true);
    }

    private void OnMostrarMaisClicked(object? sender, EventArgs e)
    {
        _limiteExibido += TamanhoDoBloco;
        Atualizar();
    }

    private void OnFiltroToggled(object? sender, ToggledEventArgs e) => OnFiltroChanged(sender, e);
    private void OnFiltroDateChanged(object? sender, DateChangedEventArgs e) => OnFiltroChanged(sender, e);

    // recomecar = true volta ao primeiro bloco de 100; ao corrigir/cancelar a lista continua do mesmo tamanho.
    private void Atualizar(bool recomecar = false)
    {
        if (recomecar) _limiteExibido = TamanhoDoBloco;
        var administrativo = _usuarios.EhAdministrativo;
        List<Movimentacao> movimentacoes;

        if (PeriodoSwitch.IsToggled)
        {
            try
            {
                movimentacoes = _movimentacoes.FiltrarPorPeriodo(InicioPicker.Date ?? DateTime.Today, FimPicker.Date ?? DateTime.Today);
            }
            catch (InvalidOperationException ex)
            {
                // Início depois do fim: o Service recusa e a tela só mostra a mensagem.
                ListaMovimentacoes.ItemsSource = new List<MovimentacaoLinha>();
                MostrarMaisButton.IsVisible = false;
                ResumoLabel.Text = ex.Message;
                return;
            }
        }
        else
        {
            movimentacoes = _movimentacoes.ConsultarHistorico();
        }

        var total = movimentacoes.Count;
        var busca = BuscaEntry.Text;

        var filtradas = movimentacoes
            .Where(m => Rotulos.MovimentacaoContem(m, busca))
            .Where(m => TipoPicker.SelectedIndex <= 0 || m.Tipo == _tipos[TipoPicker.SelectedIndex - 1])
            .Where(m => StatusPicker.SelectedIndex <= 0 || m.Status == _status[StatusPicker.SelectedIndex - 1])
            .ToList();

        ListaMovimentacoes.ItemsSource = filtradas
            .Take(_limiteExibido)
            .Select(m => new MovimentacaoLinha(m, administrativo))
            .ToList();

        var restantes = filtradas.Count - _limiteExibido;
        MostrarMaisButton.IsVisible = restantes > 0;
        if (restantes > 0) MostrarMaisButton.Text = Rotulos.MostrarMais(restantes);

        ResumoLabel.Text = Rotulos.TotalFiltrado(filtradas.Count, total);
    }

    // ---------- Corrigir e cancelar ----------

    private async void OnCorrigirClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: MovimentacaoLinha linha }) return;

        var motivo = await DisplayPromptAsync(
            "Corrigir movimentação",
            "O efeito desta movimentação no estoque será desfeito e ela ficará marcada como corrigida.\n" +
            "Depois, registre a movimentação certa em Entrada ou Saída.\n\nInforme o motivo da correção:",
            "Corrigir", "Voltar", "Motivo (obrigatório)");
        if (motivo is null) return; // tocou em Voltar

        await EncerrarAsync(() => _movimentacoes.Corrigir(linha.Movimentacao.Id, motivo),
            "Movimentação corrigida. Registre agora a movimentação correta em Entrada ou Saída.");
    }

    private async void OnCancelarClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: MovimentacaoLinha linha }) return;

        // Só o perfil Administrativo cancela (essa checagem é da tela, como combinado no Service).
        if (!_usuarios.EhAdministrativo)
        {
            await DisplayAlertAsync("Cancelar", "Apenas o perfil Administrativo pode cancelar movimentações.", "OK");
            return;
        }

        var motivo = await DisplayPromptAsync(
            "Cancelar movimentação",
            "O efeito desta movimentação no estoque será desfeito e ela ficará marcada como cancelada.\n\nInforme o motivo do cancelamento:",
            "Cancelar movimentação", "Voltar", "Motivo (obrigatório)");
        if (motivo is null) return;

        await EncerrarAsync(() => _movimentacoes.Cancelar(linha.Movimentacao.Id, motivo),
            "Movimentação cancelada. O estoque voltou ao que era antes dela.");
    }

    // Executa corrigir/cancelar. Se o Service recusar (motivo vazio, entrada já consumida...), mostra a mensagem e nada muda.
    private async Task EncerrarAsync(Action acao, string mensagemDeSucesso)
    {
        try
        {
            acao();
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Não foi possível concluir", ex.Message, "OK");
            return;
        }

        Atualizar();
        await DisplayAlertAsync("Histórico", mensagemDeSucesso, "OK");
    }
}
