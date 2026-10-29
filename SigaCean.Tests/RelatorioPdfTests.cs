using PdfSharp.Pdf.IO;
using SigaCean.Data;
using SigaCean.Models;
using SigaCean.Reports;
using SigaCean.Services;

namespace SigaCean.Tests;

// Testa o PDF do Relatório de Consumo (Reports/RelatorioConsumoPdf.cs): gera o arquivo de verdade, numa pasta temporária,
// e confere que é um PDF válido. Os números do relatório vêm do RelatorioService (testado em RelatorioServiceTests);
// aqui só se prova que o PDF é gerado com dados reais, sem movimentações e com os nomes de arquivo esperados.
public class RelatorioPdfTests
{
    // Os produtos e o histórico reais do CEAN carregados (do jeito que o MauiProgram faz) e o service de relatórios.
    private static RelatorioService RelatoriosComDadosReais()
    {
        var dados = new AppData();
        DadosCean.Carregar(dados);
        HistoricoCean.Carregar(dados);
        return new RelatorioService(dados, new EstoqueService(dados));
    }

    // Gera o PDF num arquivo temporário, passa o caminho para a conferência e apaga o arquivo no fim.
    private static void ComPdfGerado(Action<string> gerar, Action<string> conferir)
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"siga-cean-teste-{Guid.NewGuid():N}.pdf");
        try
        {
            gerar(caminho);
            conferir(caminho);
        }
        finally
        {
            if (File.Exists(caminho)) File.Delete(caminho);
        }
    }

    // Um PDF válido começa com "%PDF-", termina com "%%EOF" e o leitor de PDF consegue abrir e contar as páginas.
    private static int ConferirPdfValido(string caminho)
    {
        Assert.True(File.Exists(caminho), "O arquivo PDF não foi criado.");

        var bytes = File.ReadAllBytes(caminho);
        Assert.True(bytes.Length > 500, "O PDF está pequeno demais para ter conteúdo.");
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        Assert.Contains("%%EOF", System.Text.Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length)));

        using var documento = PdfReader.Open(caminho, PdfDocumentOpenMode.Import);
        return documento.PageCount;
    }

    // ---------- Gerar ----------

    [Fact]
    public void Gerar_Com_Dados_Reais_Cria_Um_Pdf_Valido_Com_Varias_Paginas()
    {
        var relatorios = RelatoriosComDadosReais();

        // O ano da última movimentação do histórico real: é onde há consumo para mostrar.
        var ultima = relatorios.DataDaUltimaMovimentacao();
        Assert.NotNull(ultima);
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Anual, ultima!.Value);

        var itens = relatorios.GerarRelatorioConsumo(inicio, fim);
        Assert.NotEmpty(itens);

        ComPdfGerado(
            caminho => RelatorioConsumoPdf.Gerar(caminho, itens, TipoPeriodo.Anual, inicio, fim, "Todas as categorias", "Administrativo (teste)"),
            caminho =>
            {
                var paginas = ConferirPdfValido(caminho);

                // Com dezenas de produtos a tabela não cabe numa página só (o cabeçalho se repete em cada uma).
                if (itens.Count > 60)
                    Assert.True(paginas > 1, $"Esperava mais de uma página para {itens.Count} produtos, mas saiu {paginas}.");
            });
    }

    [Fact]
    public void Gerar_Sem_Movimentacoes_Cria_Um_Pdf_Valido_De_Uma_Pagina()
    {
        var inicio = new DateTime(2020, 1, 1);
        var fim = new DateTime(2020, 1, 31);

        ComPdfGerado(
            caminho => RelatorioConsumoPdf.Gerar(caminho, Array.Empty<ItemRelatorioConsumo>(), TipoPeriodo.Mensal, inicio, fim, null, null),
            caminho => Assert.Equal(1, ConferirPdfValido(caminho)));
    }

    [Fact]
    public void Gerar_Com_Filtro_De_Categoria_E_Periodo_Personalizado_Cria_Um_Pdf_Valido()
    {
        var relatorios = RelatoriosComDadosReais();
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Anual, relatorios.DataDaUltimaMovimentacao()!.Value);

        var itens = relatorios.GerarRelatorioConsumo(inicio, fim, Categoria.Medicamento, OrdemRelatorio.PorNome);

        ComPdfGerado(
            caminho => RelatorioConsumoPdf.Gerar(caminho, itens, TipoPeriodo.Personalizado, inicio, fim, "Medicamento", "Maria"),
            caminho => ConferirPdfValido(caminho));
    }

    [Fact]
    public void Gerar_Duas_Vezes_Seguidas_Funciona()
    {
        // O usuário pode clicar em "Gerar PDF" várias vezes: a configuração de fontes é repetida a cada vez.
        var itens = Array.Empty<ItemRelatorioConsumo>();
        var inicio = new DateTime(2026, 3, 1);
        var fim = new DateTime(2026, 3, 31);

        for (var i = 0; i < 2; i++)
            ComPdfGerado(
                caminho => RelatorioConsumoPdf.Gerar(caminho, itens, TipoPeriodo.Mensal, inicio, fim, null, null),
                caminho => ConferirPdfValido(caminho));
    }

    [Fact]
    public void Gerar_Nao_Altera_Os_Dados()
    {
        var relatorios = RelatoriosComDadosReais();
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Anual, relatorios.DataDaUltimaMovimentacao()!.Value);
        var itens = relatorios.GerarRelatorioConsumo(inicio, fim);
        var estoqueAntes = itens.Select(i => i.Produto.Quantidade).ToList();

        ComPdfGerado(
            caminho => RelatorioConsumoPdf.Gerar(caminho, itens, TipoPeriodo.Anual, inicio, fim, null, null),
            caminho => ConferirPdfValido(caminho));

        Assert.Equal(estoqueAntes, itens.Select(i => i.Produto.Quantidade).ToList());
    }

    // ---------- Nome sugerido ----------

    [Fact]
    public void NomeSugerido_Mensal_Usa_Ano_E_Mes()
    {
        Assert.Equal("Relatorio_Consumo_CEAN_Mensal_2026-03.pdf",
            RelatorioConsumoPdf.NomeSugerido(TipoPeriodo.Mensal, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)));
    }

    [Theory]
    [InlineData(2026, 1, "2026-T1")]
    [InlineData(2026, 4, "2026-T2")]
    [InlineData(2026, 7, "2026-T3")]
    [InlineData(2026, 10, "2026-T4")]
    public void NomeSugerido_Trimestral_Usa_Ano_E_Numero_Do_Trimestre(int ano, int mesInicial, string esperado)
    {
        var inicio = new DateTime(ano, mesInicial, 1);
        var fim = inicio.AddMonths(3).AddDays(-1);

        Assert.Equal($"Relatorio_Consumo_CEAN_Trimestral_{esperado}.pdf", RelatorioConsumoPdf.NomeSugerido(TipoPeriodo.Trimestral, inicio, fim));
    }

    [Fact]
    public void NomeSugerido_Anual_Usa_So_O_Ano()
    {
        Assert.Equal("Relatorio_Consumo_CEAN_Anual_2026.pdf",
            RelatorioConsumoPdf.NomeSugerido(TipoPeriodo.Anual, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));
    }

    [Fact]
    public void NomeSugerido_Personalizado_Usa_As_Duas_Datas()
    {
        Assert.Equal("Relatorio_Consumo_CEAN_20260301_a_20260315.pdf",
            RelatorioConsumoPdf.NomeSugerido(TipoPeriodo.Personalizado, new DateTime(2026, 3, 1), new DateTime(2026, 3, 15)));
    }

    [Fact]
    public void NomeSugerido_Termina_Em_Pdf_E_Nao_Tem_Caractere_Proibido_Em_Nome_De_Arquivo()
    {
        foreach (var tipo in Enum.GetValues<TipoPeriodo>())
        {
            var nome = RelatorioConsumoPdf.NomeSugerido(tipo, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            Assert.EndsWith(".pdf", nome);
            Assert.True(nome.IndexOfAny(Path.GetInvalidFileNameChars()) < 0, $"O nome \"{nome}\" tem caractere inválido.");
        }
    }
}
