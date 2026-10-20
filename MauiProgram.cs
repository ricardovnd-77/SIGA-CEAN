using Microsoft.Extensions.Logging;
using SigaCean.Data;
using SigaCean.Services;
using SigaCean.Views;

namespace SigaCean;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if WINDOWS
        // No Windows o Title do Picker vira um cabeçalho solto acima do campo, e o campo clicável fica vazio e sem borda.
        // Aqui o Title passa a ser o texto de dentro do próprio campo. Só vale para quem usar Picker nas telas.
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("TituloDentroDoCampo", (handler, picker) =>
        {
            handler.PlatformView.Header = null;
            handler.PlatformView.HeaderTemplate = null;
            handler.PlatformView.Loaded += (campo, _) =>
            {
                var combo = (Microsoft.UI.Xaml.Controls.ComboBox)campo;
                combo.Header = null;
                combo.HeaderTemplate = null;
            };
            handler.PlatformView.PlaceholderText = picker.Title;
            handler.PlatformView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            handler.PlatformView.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
            handler.PlatformView.MinHeight = 44;
            handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(12, 8, 12, 8);
            handler.PlatformView.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
            handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xB5, 0xD3, 0xD0));
            handler.PlatformView.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8);
        });

        // Campos de texto com a mesma borda arredondada dos seletores (o padrão do Windows não mostra a caixa do campo).
        static void EstilizarCampo(Microsoft.UI.Xaml.Controls.TextBox campo)
        {
            campo.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
            campo.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xB5, 0xD3, 0xD0));
            campo.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8);
            campo.MinHeight = 44;
            campo.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
        }

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BordaDoCampo", (handler, _) => EstilizarCampo(handler.PlatformView));
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("BordaDoCampo", (handler, _) => EstilizarCampo(handler.PlatformView));
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Os dados: UMA instância só, compartilhada por todos os Services (por isso Singleton).
        // O carregamento dos produtos do CEAN é feito aqui (e não no construtor do AppData, que deve começar vazio).
        builder.Services.AddSingleton<AppData>(_ =>
        {
            var dados = new AppData();
            DadosCean.Carregar(dados);
            return dados;
        });

        // Os Services. O MAUI entrega o AppData (e o EstoqueService) a quem pedir no construtor.
        // É o recurso básico do próprio MAUI; não há nenhum framework de injeção adicional.
        builder.Services.AddSingleton<UsuarioService>();
        builder.Services.AddSingleton<ProdutoService>();
        builder.Services.AddSingleton<EstoqueService>();
        builder.Services.AddSingleton<MovimentacaoService>();
        builder.Services.AddSingleton<RelatorioService>();

        // As telas (Transient = uma nova a cada uso). Toda tela nova precisa ser registrada aqui.
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<EstoquePage>();
        builder.Services.AddTransient<ProdutosPage>();
        builder.Services.AddTransient<EntradaPage>();
        builder.Services.AddTransient<SaidaPage>();
        builder.Services.AddTransient<HistoricoPage>();
        builder.Services.AddTransient<RelatoriosPage>();

        return builder.Build();
    }
}
