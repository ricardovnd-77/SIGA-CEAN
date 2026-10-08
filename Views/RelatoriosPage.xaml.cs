using SigaCean.Services;

namespace SigaCean.Views;

public partial class RelatoriosPage : ContentPage
{
    private readonly RelatorioService _relatorios;

    public RelatoriosPage(RelatorioService relatorios)
    {
        InitializeComponent();
        _relatorios = relatorios;
    }
}
