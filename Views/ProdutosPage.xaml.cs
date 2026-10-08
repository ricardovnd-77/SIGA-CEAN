using SigaCean.Services;

namespace SigaCean.Views;

public partial class ProdutosPage : ContentPage
{
    private readonly ProdutoService _produtos;

    public ProdutosPage(ProdutoService produtos)
    {
        InitializeComponent();
        _produtos = produtos;
    }
}
