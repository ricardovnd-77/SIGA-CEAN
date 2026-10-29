namespace SigaCean.Views;

// Janela "Salvar como" do Windows, usada pelas telas que geram PDF. É a ÚNICA exceção à regra de não gravar
// arquivos: o PDF que o próprio usuário escolhe onde salvar. Nenhum dado do sistema é guardado em arquivo.
public static class SalvarComo
{
    // Devolve o caminho escolhido, ou null se o usuário cancelar.
    public static async Task<string?> PdfAsync(string nomeSugerido)
    {
        var seletor = new global::Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(nomeSugerido),
            SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        seletor.FileTypeChoices.Add("Documento PDF", [".pdf"]);

        // O seletor do Windows precisa saber de qual janela ele foi aberto.
        var janela = Application.Current?.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (janela is null) return null;
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, WinRT.Interop.WindowNative.GetWindowHandle(janela));

        var arquivo = await seletor.PickSaveFileAsync();
        return arquivo?.Path;
    }
}
