using PdfSharp.Pdf.IO;
using SigaCean.Data;
using SigaCean.Models;
using SigaCean.Reports;
using SigaCean.Services;

namespace SigaCean.Tests;

// Testa o PDF do Relatório por Categoria (Reports/RelatorioCategoriaPdf.cs): gera o arquivo de verdade, numa pasta
// temporária, e confere que é um PDF válido. Os números vêm do RelatorioService (testado em ConsumoPorCategoriaTests).
public class RelatorioCategoriaPdfTests
{
    private static string CaminhoTemporario() => Path.Combine(Path.GetTempPath(), $"siga-cean-categoria-{Guid.NewGuid():N}.pdf");

    [Fact]
    public void Gerar_Com_Dados_Reais_Cria_Um_Pdf_Valido()
    {
        var dados = new AppData();
        DadosCean.Carregar(dados);
        HistoricoCean.Carregar(dados);
        var categorias = new RelatorioService(dados, new EstoqueService(dados))
            .GerarConsumoPorCategoria(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31, 23, 59, 59));
        var caminho = CaminhoTemporario();

        try
        {
            RelatorioCategoriaPdf.Gerar(caminho, categorias, TipoPeriodo.Anual, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), "Teste");

            var bytes = File.ReadAllBytes(caminho);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
            Assert.True(bytes.Length > 5_000);
            using var documento = PdfReader.Open(caminho, PdfDocumentOpenMode.Import);
            Assert.True(documento.PageCount >= 1);
        }
        finally
        {
            if (File.Exists(caminho)) File.Delete(caminho);
        }
    }

    [Fact]
    public void Gerar_Sem_Nenhuma_Saida_Ainda_Cria_O_Pdf()
    {
        var c = new Cenario();
        var categorias = c.Relatorios.GerarConsumoPorCategoria(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var caminho = CaminhoTemporario();

        try
        {
            RelatorioCategoriaPdf.Gerar(caminho, categorias, TipoPeriodo.Mensal, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), null);

            Assert.True(new FileInfo(caminho).Length > 0);
        }
        finally
        {
            if (File.Exists(caminho)) File.Delete(caminho);
        }
    }

    [Theory]
    [InlineData(TipoPeriodo.Mensal, "Relatorio_Por_Categoria_CEAN_Mensal_2026-03.pdf")]
    [InlineData(TipoPeriodo.Trimestral, "Relatorio_Por_Categoria_CEAN_Trimestral_2026-T1.pdf")]
    [InlineData(TipoPeriodo.Anual, "Relatorio_Por_Categoria_CEAN_Anual_2026.pdf")]
    public void NomeSugerido_Identifica_O_Periodo(TipoPeriodo tipo, string esperado)
    {
        Assert.Equal(esperado, RelatorioCategoriaPdf.NomeSugerido(tipo, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)));
    }
}
