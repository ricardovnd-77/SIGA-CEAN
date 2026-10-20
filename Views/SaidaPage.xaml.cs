using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Saída e ajuste de balanço (Administrativo). A tela coleta os dados; o MovimentacaoService valida, aplica o FEFO
// e grava. A tela só confere o básico antes, para dar uma mensagem clara.
public partial class SaidaPage : ContentPage
{
    private static readonly Color Primaria = Color.FromArgb("#087F78");
    private static readonly Color Laranja = Color.FromArgb("#F59B24");
    private static readonly Color Branco = Colors.White;
    private static readonly Color Tinta = Color.FromArgb("#153B43");
    private static readonly Color Linha = Color.FromArgb("#DBE9E8");

    private readonly ProdutoService _produtos;
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    private List<Produto> _lista = new();
    private bool _modoAjuste;

    public SaidaPage(ProdutoService produtos, MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _produtos = produtos;
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;
        AplicarModo();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarProdutos();
    }

    // Recarrega a lista de produtos (podem ter mudado em outra tela) mantendo a escolha atual.
    private void CarregarProdutos()
    {
        var escolhido = ProdutoSelecionado?.Id;
        _lista = _produtos.Pesquisar(null);
        ProdutoPicker.ItemsSource = _lista.Select(Rotulos.ProdutoNaLista).ToList();
        ProdutoPicker.SelectedIndex = escolhido is { } id ? _lista.FindIndex(p => p.Id == id) : -1;
        AtualizarDisponivel();
    }

    private Produto? ProdutoSelecionado =>
        ProdutoPicker.SelectedIndex >= 0 && ProdutoPicker.SelectedIndex < _lista.Count ? _lista[ProdutoPicker.SelectedIndex] : null;

    private void OnProdutoChanged(object? sender, EventArgs e)
    {
        AtualizarDisponivel();
        AtualizarDiferenca();
    }

    private void AtualizarDisponivel()
    {
        var produto = ProdutoSelecionado;
        DisponivelLabel.Text = produto is not null ? $"Disponível em estoque: {produto.Quantidade} un."
            : _lista.Count == 0 ? "Nenhum produto cadastrado. Cadastre primeiro em Produtos."
            : string.Empty;
    }

    // ---------- Saída ou ajuste ----------

    private void OnModoSaidaClicked(object? sender, EventArgs e) => TrocarModo(ajuste: false);
    private void OnModoAjusteClicked(object? sender, EventArgs e) => TrocarModo(ajuste: true);

    private void TrocarModo(bool ajuste)
    {
        _modoAjuste = ajuste;
        MensagemErroLabel.IsVisible = false;
        MensagemSucessoLabel.IsVisible = false;
        AplicarModo();
    }

    private void AplicarModo()
    {
        SaidaPainel.IsVisible = !_modoAjuste;
        AjustePainel.IsVisible = _modoAjuste;

        Destacar(SaidaModoButton, selecionado: !_modoAjuste);
        Destacar(AjusteModoButton, selecionado: _modoAjuste);

        RegistrarButton.Text = _modoAjuste ? "Registrar ajuste" : "Registrar saída";
        RegistrarButton.BackgroundColor = _modoAjuste ? Primaria : Laranja;
        RegistrarButton.BorderColor = _modoAjuste ? Primaria : Laranja;
        AtualizarDiferenca();
    }

    // O botão do modo escolhido fica preenchido; o outro fica branco.
    private static void Destacar(Button botao, bool selecionado)
    {
        botao.BackgroundColor = selecionado ? Primaria : Branco;
        botao.TextColor = selecionado ? Branco : Tinta;
        botao.BorderColor = selecionado ? Primaria : Linha;
    }

    // "Sistema: 12 · diferença: −2": só mostra a conta; quem aplica o ajuste é o MovimentacaoService.
    private void OnApuradaChanged(object? sender, TextChangedEventArgs e) => AtualizarDiferenca();

    private void AtualizarDiferenca()
    {
        var produto = ProdutoSelecionado;
        var apurada = Rotulos.LerInteiro(ApuradaEntry.Text);

        DiferencaLabel.Text = produto is null || apurada is null || apurada < 0
            ? string.Empty
            : $"No sistema: {produto.Quantidade} un. · diferença: {Rotulos.ComSinal(apurada.Value - produto.Quantidade)}";
    }

    // ---------- Registrar ----------

    private async void OnRegistrarClicked(object? sender, EventArgs e)
    {
        MensagemErroLabel.IsVisible = false;
        MensagemSucessoLabel.IsVisible = false;

        var produto = ProdutoSelecionado;
        if (produto is null)
        {
            MostrarErro("Escolha um produto.");
            return;
        }

        if (_modoAjuste)
            await RegistrarAjusteAsync(produto);
        else
            await RegistrarSaidaAsync(produto);
    }

    private async Task RegistrarSaidaAsync(Produto produto)
    {
        var quantidade = Rotulos.LerInteiro(QuantidadeEntry.Text);
        if (quantidade is null || quantidade <= 0)
        {
            MostrarErro("Informe uma quantidade válida, maior que zero.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinoEntry.Text))
        {
            MostrarErro("Informe o destino da saída.");
            return;
        }

        if (quantidade > produto.Quantidade)
        {
            MostrarErro($"Estoque insuficiente: há apenas {produto.Quantidade} unidade(s) de \"{produto.Nome}\".");
            return;
        }

        var confirmou = await DisplayAlertAsync(
            "Confirmar saída",
            $"Produto: {Rotulos.ProdutoNaLista(produto)}\n" +
            $"Quantidade: {quantidade}\n" +
            $"Estoque: {produto.Quantidade} → {produto.Quantidade - quantidade}\n" +
            $"Destino: {DestinoEntry.Text.Trim()}" +
            (string.IsNullOrWhiteSpace(RetiradoPorEntry.Text) ? string.Empty : $"\nRetirado por: {RetiradoPorEntry.Text.Trim()}"),
            "Confirmar", "Revisar");
        if (!confirmou) return;

        Movimentacao movimentacao;
        try
        {
            movimentacao = _movimentacoes.RegistrarSaida(produto, quantidade.Value, DestinoEntry.Text, RetiradoPorEntry.Text,
                _usuarios.UsuarioLogado, ObservacaoEntry.Text);
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        // O FEFO foi aplicado pelo Service: aqui só se mostra de quais validades a quantidade saiu.
        var origem = string.Join("; ", movimentacao.SaldosConsumidos.Select(s =>
            $"{s.Quantidade} un. {(s.DataValidade is { } d ? $"com validade {d:dd/MM/yyyy}" : "sem validade informada")}"));

        MostrarSucesso($"Saída registrada: {produto.Nome} (−{quantidade}). Saiu de: {origem}. Estoque agora: {produto.Quantidade}.");
        LimparFormulario();
    }

    private async Task RegistrarAjusteAsync(Produto produto)
    {
        var apurada = Rotulos.LerInteiro(ApuradaEntry.Text);
        if (apurada is null || apurada < 0)
        {
            MostrarErro("Informe a quantidade contada (zero ou mais).");
            return;
        }

        var diferenca = apurada.Value - produto.Quantidade;
        var confirmou = await DisplayAlertAsync(
            "Confirmar ajuste de balanço",
            $"Produto: {Rotulos.ProdutoNaLista(produto)}\n" +
            $"No sistema: {produto.Quantidade}\n" +
            $"Contado: {apurada}\n" +
            $"Diferença: {Rotulos.ComSinal(diferenca)}" +
            (diferenca == 0 ? "\n(Sem diferença: a conferência será registrada mesmo assim.)" : string.Empty),
            "Confirmar", "Revisar");
        if (!confirmou) return;

        try
        {
            _movimentacoes.RegistrarAjusteBalanco(produto, apurada.Value, _usuarios.UsuarioLogado, ObservacaoEntry.Text);
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        MostrarSucesso($"Ajuste registrado: {produto.Nome} ({Rotulos.ComSinal(diferenca)}). Estoque agora: {produto.Quantidade}.");
        LimparFormulario();
    }

    private void LimparFormulario()
    {
        ProdutoPicker.SelectedIndex = -1;
        QuantidadeEntry.Text = string.Empty;
        DestinoEntry.Text = string.Empty;
        RetiradoPorEntry.Text = string.Empty;
        ApuradaEntry.Text = string.Empty;
        ObservacaoEntry.Text = string.Empty;
        DiferencaLabel.Text = string.Empty;
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }

    private void MostrarSucesso(string mensagem)
    {
        MensagemSucessoLabel.Text = mensagem;
        MensagemSucessoLabel.IsVisible = true;
    }
}
