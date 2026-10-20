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
        Atualizar();
    }

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (!_carregando) Atualizar();
    }

    private void OnFiltroToggled(object? sender, ToggledEventArgs e) => OnFiltroChanged(sender, e);
    private void OnFiltroDateChanged(object? sender, DateChangedEventArgs e) => OnFiltroChanged(sender, e);

    private void Atualizar()
    {
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
                ResumoLabel.Text = ex.Message;
                return;
            }
        }
        else
        {
            movimentacoes = _movimentacoes.ConsultarHistorico();
        }

        var total = movimentacoes.Count;
        var busca = BuscaEntry.Text?.Trim();

        var filtradas = movimentacoes
            .Where(m => string.IsNullOrEmpty(busca) || Rotulos.Contem(m.Produto.Nome, busca))
            .Where(m => TipoPicker.SelectedIndex <= 0 || m.Tipo == _tipos[TipoPicker.SelectedIndex - 1])
            .Where(m => StatusPicker.SelectedIndex <= 0 || m.Status == _status[StatusPicker.SelectedIndex - 1])
            .Select(m => new MovimentacaoLinha(m, administrativo))
            .ToList();

        ListaMovimentacoes.ItemsSource = filtradas;
        ResumoLabel.Text = total == 0
            ? "Nenhuma movimentação registrada ainda."
            : $"{filtradas.Count} de {total} movimentação(ões)";
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
