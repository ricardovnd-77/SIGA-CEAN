using SigaCean.Services;

namespace SigaCean.Views;

public partial class EstoquePage : ContentPage
{
    private readonly EstoqueService _estoque;
    private readonly ProdutoService _produtos;

    public EstoquePage(EstoqueService estoque, ProdutoService produtos)
    {
        InitializeComponent();
        _estoque = estoque;
        _produtos = produtos;
    }
}
