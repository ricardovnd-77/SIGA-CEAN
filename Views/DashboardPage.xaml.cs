using SigaCean.Services;

namespace SigaCean.Views;

public partial class DashboardPage : ContentPage
{
    private readonly EstoqueService _estoque;

    public DashboardPage(EstoqueService estoque)
    {
        InitializeComponent();
        _estoque = estoque;
    }
}
