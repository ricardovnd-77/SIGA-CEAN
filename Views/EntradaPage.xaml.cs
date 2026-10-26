using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Registro de entrada (Administrativo). A tela coleta os dados; o MovimentacaoService valida, aumenta o estoque e grava.
public partial class EntradaPage : ContentPage
{
    private readonly ProdutoService _produtos;
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    private readonly List<TipoOrigemEntrada> _origens = Enum.GetValues<TipoOrigemEntrada>().ToList();
    private List<Produto> _lista = new();
    private Guid? _produtoPreSelecionado; // produto escolhido por outra tela (ficha do produto, Estoque)

    public EntradaPage(ProdutoService produtos, MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _produtos = produtos;
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;

        OrigemPicker.ItemsSource = _origens.Select(Rotulos.NomeDaOrigem).ToList();
        ValidadePicker.Date = DateTime.Today.AddMonths(6);
        AtualizarCamposDaOrigem();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarProdutos();
        FocarPrimeiroCampoVazio();
    }

    // O cursor já começa onde falta preencher: sem produto escolhido, no produto; com produto, na quantidade.
    private void FocarPrimeiroCampoVazio()
    {
        if (ProdutoSelecionado is null) ProdutoPicker.Focus();
        else QuantidadeEntry.Focus();
    }

    // Outra tela abre a Entrada já com este produto escolhido (vale na próxima vez que a tela aparecer).
    public void PreSelecionar(Guid produtoId) => _produtoPreSelecionado = produtoId;

    // Recarrega a lista de produtos (podem ter sido cadastrados em outra tela) mantendo a escolha atual.
    private void CarregarProdutos()
    {
        var escolhido = _produtoPreSelecionado ?? ProdutoSelecionado?.Id;
        _produtoPreSelecionado = null;
        _lista = _produtos.Pesquisar(null);
        ProdutoPicker.ItemsSource = _lista.Select(Rotulos.ProdutoNaLista).ToList();
        ProdutoPicker.SelectedIndex = escolhido is { } id ? _lista.FindIndex(p => p.Id == id) : -1;
        AtualizarEstoqueAtual();
    }

    private Produto? ProdutoSelecionado =>
        ProdutoPicker.SelectedIndex >= 0 && ProdutoPicker.SelectedIndex < _lista.Count ? _lista[ProdutoPicker.SelectedIndex] : null;

    private void OnProdutoChanged(object? sender, EventArgs e) => AtualizarEstoqueAtual();

    private void AtualizarEstoqueAtual()
    {
        var produto = ProdutoSelecionado;
        EstoqueAtualLabel.Text = produto is not null ? $"Estoque atual: {produto.Quantidade} un."
            : _lista.Count == 0 ? "Nenhum produto cadastrado. Cadastre primeiro em Produtos."
            : string.Empty;
    }

    private void OnOrigemChanged(object? sender, EventArgs e) => AtualizarCamposDaOrigem();

    // O número da AF só faz sentido para compra.
    private void AtualizarCamposDaOrigem()
    {
        AFPainel.IsVisible = OrigemPicker.SelectedIndex >= 0 && _origens[OrigemPicker.SelectedIndex] == TipoOrigemEntrada.Compra;
        if (!AFPainel.IsVisible) AFEntry.Text = string.Empty;
    }

    private void OnValidadeToggled(object? sender, ToggledEventArgs e)
    {
        ValidadePicker.IsVisible = e.Value;
        AtualizarAvisoDeValidade();
    }

    private void OnValidadeSelected(object? sender, DateChangedEventArgs e) => AtualizarAvisoDeValidade();

    // Validade no passado continua aceita (decisão do grupo); a tela só avisa que o produto vai aparecer como vencido.
    private void AtualizarAvisoDeValidade()
    {
        DateOnly? validade = ValidadeSwitch.IsToggled ? DateOnly.FromDateTime(ValidadePicker.Date ?? DateTime.Today) : null;
        AvisoValidadeLabel.Text = Rotulos.AvisoDeValidadePassada(validade, DateOnly.FromDateTime(DateTime.Today));
        AvisoValidadeLabel.IsVisible = AvisoValidadeLabel.Text.Length > 0;
    }

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

        var quantidade = Rotulos.LerInteiro(QuantidadeEntry.Text);
        if (quantidade is null || quantidade <= 0)
        {
            MostrarErro("Informe uma quantidade válida, maior que zero.");
            return;
        }

        if (OrigemPicker.SelectedIndex < 0)
        {
            MostrarErro("Escolha a origem da entrada.");
            return;
        }

        var origem = _origens[OrigemPicker.SelectedIndex];
        DateOnly? validade = ValidadeSwitch.IsToggled ? DateOnly.FromDateTime(ValidadePicker.Date ?? DateTime.Today) : null;

        // Conferência antes de gravar: erro de quantidade é um dos problemas mais comuns da planilha.
        var confirmou = await DisplayAlertAsync(
            "Confirmar entrada",
            $"Produto: {Rotulos.ProdutoNaLista(produto)}\n" +
            $"Quantidade: {quantidade}\n" +
            $"Estoque: {produto.Quantidade} → {produto.Quantidade + quantidade}\n" +
            $"Origem: {Rotulos.NomeDaOrigem(origem)}\n" +
            $"Validade: {(validade is { } v ? v.ToString("dd/MM/yyyy") : "sem validade")}",
            "Confirmar", "Revisar");
        if (!confirmou) return;

        try
        {
            _movimentacoes.RegistrarEntrada(produto, quantidade.Value, origem, OrigemDetalheEntry.Text, AFEntry.Text,
                DocumentoEntry.Text, validade, _usuarios.UsuarioLogado, ObservacaoEntry.Text);
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        MensagemSucessoLabel.Text = $"Entrada registrada: {produto.Nome} (+{quantidade}). Estoque agora: {produto.Quantidade}.";
        MensagemSucessoLabel.IsVisible = true;
        LimparFormulario();
    }

    private void LimparFormulario()
    {
        ProdutoPicker.SelectedIndex = -1;
        QuantidadeEntry.Text = string.Empty;
        OrigemPicker.SelectedIndex = -1;
        OrigemDetalheEntry.Text = string.Empty;
        AFEntry.Text = string.Empty;
        DocumentoEntry.Text = string.Empty;
        ValidadeSwitch.IsToggled = false;
        ValidadePicker.Date = DateTime.Today.AddMonths(6);
        ObservacaoEntry.Text = string.Empty;
        AtualizarCamposDaOrigem();
        AtualizarAvisoDeValidade();
        ProdutoPicker.Focus(); // pronto para o próximo registro
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
