using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using SigaCean.Models;
using SigaCean.Services;
using SigaCean.Views;
// O MAUI tem tipos com os mesmos nomes; aqui valem os do MigraDoc.
using Color = MigraDoc.DocumentObjectModel.Color;
using Colors = MigraDoc.DocumentObjectModel.Colors;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;

namespace SigaCean.Reports;

// Monta o PDF do Relatório de Consumo com os MESMOS dados que aparecem na tela (as linhas já vêm prontas do
// RelatorioService). Aqui só se desenha: nenhuma conta de estoque é feita neste arquivo. Não usa MAUI, por isso
// entra no projeto de testes (SigaCean.Tests/RelatorioPdfTests.cs).
public static class RelatorioConsumoPdf
{
    private static readonly Color Verde = Color.FromRgb(0x07, 0x5F, 0x5B); // cor principal do sistema
    private static readonly Color Cinza = Color.FromRgb(0x5F, 0x6B, 0x6A);
    private static readonly Color Linha = Color.FromRgb(0xD9, 0xE2, 0xE1);

    // Grava o PDF em "caminho". "categoria" é o texto do filtro (nulo ou "Todas..." = sem filtro, não aparece no PDF).
    public static void Gerar(
        string caminho,
        IReadOnlyList<ItemRelatorioConsumo> itens,
        TipoPeriodo tipo,
        DateTime inicio,
        DateTime fim,
        string? categoria,
        string? usuario)
    {
        // Usa as fontes instaladas no Windows (Arial), sem precisar embutir arquivos de fonte.
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;

        var documento = new Document();
        documento.Info.Title = "Relatório de Consumo - CEAN";
        DefinirEstilos(documento);

        var secao = documento.AddSection();
        secao.PageSetup.PageFormat = PageFormat.A4;
        secao.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
        secao.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
        secao.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        secao.PageSetup.BottomMargin = Unit.FromCentimeter(2);

        MontarCabecalho(secao, tipo, inicio, fim, categoria, usuario);
        MontarResumo(secao, itens);
        MontarTabela(secao, itens);
        MontarRodape(secao);

        var renderizador = new PdfDocumentRenderer { Document = documento };
        renderizador.RenderDocument();
        renderizador.PdfDocument.Save(caminho);
    }

    // Nome que a janela "Salvar como" já sugere: Relatorio_Consumo_CEAN_Mensal_2026-03.pdf etc.
    public static string NomeSugerido(TipoPeriodo tipo, DateTime inicio, DateTime fim)
    {
        var periodo = tipo switch
        {
            TipoPeriodo.Mensal => $"Mensal_{inicio:yyyy-MM}",
            TipoPeriodo.Trimestral => $"Trimestral_{inicio:yyyy}-T{(inicio.Month - 1) / 3 + 1}",
            TipoPeriodo.Anual => $"Anual_{inicio:yyyy}",
            _ => $"{inicio:yyyyMMdd}_a_{fim:yyyyMMdd}"
        };
        return $"Relatorio_Consumo_CEAN_{periodo}.pdf";
    }

    private static void DefinirEstilos(Document documento)
    {
        var normal = documento.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 10;

        var rodape = documento.Styles[StyleNames.Footer]!;
        rodape.Font.Size = 8;
        rodape.Font.Color = Cinza;
    }

    private static void MontarCabecalho(Section secao, TipoPeriodo tipo, DateTime inicio, DateTime fim, string? categoria, string? usuario)
    {
        var sobretitulo = secao.AddParagraph("CEAN • CENTRO DE APOIO AOS ANIMAIS");
        sobretitulo.Format.Font.Size = 8;
        sobretitulo.Format.Font.Bold = true;
        sobretitulo.Format.Font.Color = Cinza;

        var titulo = secao.AddParagraph(tipo == TipoPeriodo.Personalizado ? "Relatório de Consumo" : $"Relatório de Consumo {tipo.ToString().ToLowerInvariant()}");
        titulo.Format.Font.Size = 20;
        titulo.Format.Font.Bold = true;
        titulo.Format.Font.Color = Verde;
        titulo.Format.SpaceBefore = 2;

        // A descrição do período vem do PeriodoRelatorio (a mesma que a tela mostra).
        var periodo = secao.AddParagraph($"Período: {PeriodoRelatorio.Descrever(tipo, inicio, fim)}  ({inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy})");
        periodo.Format.SpaceBefore = 6;

        if (!string.IsNullOrWhiteSpace(categoria) && !categoria.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            secao.AddParagraph($"Categoria: {categoria}");

        var emissao = secao.AddParagraph(
            $"Emitido em {DateTime.Now:dd/MM/yyyy 'às' HH:mm}" + (string.IsNullOrWhiteSpace(usuario) ? string.Empty : $" por {usuario}"));
        emissao.Format.Font.Size = 9;
        emissao.Format.Font.Color = Cinza;
        emissao.Format.SpaceAfter = 12;
    }

    private static void MontarResumo(Section secao, IReadOnlyList<ItemRelatorioConsumo> itens)
    {
        var entradas = itens.Sum(i => i.TotalEntradas);
        var saidas = itens.Sum(i => i.TotalSaidas);
        var ajustes = itens.Sum(i => i.TotalAjustes);

        var resumo = secao.AddParagraph(
            $"{itens.Count} produto(s) com movimentação  •  Entradas: {entradas}  •  Saídas: {saidas}  •  Ajustes: {Rotulos.ComSinal(ajustes)}");
        resumo.Format.Font.Bold = true;
        resumo.Format.SpaceAfter = 10;
    }

    private static void MontarTabela(Section secao, IReadOnlyList<ItemRelatorioConsumo> itens)
    {
        if (itens.Count == 0)
        {
            var vazio = secao.AddParagraph("Nenhuma movimentação encontrada no período.");
            vazio.Format.Font.Color = Cinza;
            return;
        }

        var tabela = secao.AddTable();
        tabela.Borders.Color = Linha;
        tabela.Borders.Bottom.Width = 0.5;
        tabela.Rows.LeftIndent = 0;

        // A4 com margens de 1,8 cm sobra 17,4 cm de largura: 6,4 + 2,0 + 2,0 + 2,0 + 2,5 + 2,5 = 17,4.
        tabela.AddColumn(Unit.FromCentimeter(6.4));
        foreach (var largura in new[] { 2.0, 2.0, 2.0, 2.5, 2.5 })
            tabela.AddColumn(Unit.FromCentimeter(largura));

        var cabecalho = tabela.AddRow();
        cabecalho.HeadingFormat = true; // repete o cabeçalho em cada página
        cabecalho.Shading.Color = Verde;
        cabecalho.Format.Font.Color = Colors.White;
        cabecalho.Format.Font.Bold = true;
        cabecalho.TopPadding = 4;
        cabecalho.BottomPadding = 4;
        Celula(cabecalho, 0, "Produto", ParagraphAlignment.Left);
        Celula(cabecalho, 1, "Entradas", ParagraphAlignment.Right);
        Celula(cabecalho, 2, "Saídas", ParagraphAlignment.Right);
        Celula(cabecalho, 3, "Ajustes", ParagraphAlignment.Right);
        Celula(cabecalho, 4, "Média/mês", ParagraphAlignment.Right);
        Celula(cabecalho, 5, "Estoque", ParagraphAlignment.Right);

        foreach (var item in itens)
        {
            var linha = tabela.AddRow();
            linha.TopPadding = 3;
            linha.BottomPadding = 3;
            Celula(linha, 0, Rotulos.ProdutoNaLista(item.Produto), ParagraphAlignment.Left);
            Celula(linha, 1, item.TotalEntradas.ToString(), ParagraphAlignment.Right);
            Celula(linha, 2, item.TotalSaidas.ToString(), ParagraphAlignment.Right);
            Celula(linha, 3, Rotulos.ComSinal(item.TotalAjustes), ParagraphAlignment.Right);
            Celula(linha, 4, item.MediaMensalSaidas.ToString("0.#"), ParagraphAlignment.Right);
            Celula(linha, 5, item.EstoqueAtual.ToString(), ParagraphAlignment.Right);
        }
    }

    // Rodapé: "SIGA-CEAN • Página 1 de 3".
    private static void MontarRodape(Section secao)
    {
        var rodape = secao.Footers.Primary.AddParagraph();
        rodape.Format.Alignment = ParagraphAlignment.Center;
        rodape.AddText("SIGA-CEAN  •  Página ");
        rodape.AddPageField();
        rodape.AddText(" de ");
        rodape.AddNumPagesField();
    }

    private static void Celula(Row linha, int coluna, string texto, ParagraphAlignment alinhamento)
    {
        var celula = linha.Cells[coluna];
        celula.VerticalAlignment = VerticalAlignment.Center;
        var paragrafo = celula.AddParagraph(texto);
        paragrafo.Format.Alignment = alinhamento;
    }
}
