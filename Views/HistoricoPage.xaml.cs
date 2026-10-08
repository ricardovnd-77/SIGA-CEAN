using SigaCean.Services;

namespace SigaCean.Views;

public partial class HistoricoPage : ContentPage
{
    private readonly MovimentacaoService _movimentacoes;
    private readonly UsuarioService _usuarios;

    public HistoricoPage(MovimentacaoService movimentacoes, UsuarioService usuarios)
    {
        InitializeComponent();
        _movimentacoes = movimentacoes;
        _usuarios = usuarios;
    }
}
