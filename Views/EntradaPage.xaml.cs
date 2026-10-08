using SigaCean.Services;

namespace SigaCean.Views;

public partial class EntradaPage : ContentPage
{
    private readonly ProdutoService _produtos;
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    public EntradaPage(ProdutoService produtos, MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _produtos = produtos;
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;
    }
}
