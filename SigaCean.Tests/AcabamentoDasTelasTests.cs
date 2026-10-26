using System.Globalization;
using SigaCean.Data;
using SigaCean.Models;
using SigaCean.Services;
using SigaCean.Views;

namespace SigaCean.Tests;

// Testa SÓ os textos novos das telas de Entrada, Saída e Histórico (Views/Rotulos.cs): a previsão "Vai sair de",
// o aviso de validade passada, a busca do Histórico e os contadores. As telas usam MAUI e não compilam aqui;
// as regras (FEFO, "retirado por" obrigatório) continuam sendo testadas nos testes dos Services.
//
// A cultura é fixada em "invariante" durante cada teste para o formato das datas não depender do idioma do Windows.
public class AcabamentoDasTelasTests : IDisposable
{
    private readonly CultureInfo _culturaOriginal = CultureInfo.CurrentCulture;

    public AcabamentoDasTelasTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culturaOriginal;
    }

    private static readonly DateOnly Hoje = new(2026, 10, 2);

    private static Movimentacao Saida(string produto, string? retiradoPor = null, string? destino = null, string? observacao = null) =>
        new()
        {
            Produto = new Produto { Nome = produto },
            Tipo = TipoMovimentacao.Saida,
            Quantidade = 1,
            RetiradoPor = retiradoPor,
            Destino = destino,
            Observacao = observacao
        };

    // ---------- "Vai sair de" ----------

    [Fact]
    public void Previsao_Escreve_Cada_Parcela_Com_Ou_Sem_Validade()
    {
        var saldos = new List<SaldoValidade>
        {
            new() { DataValidade = new DateOnly(2026, 11, 12), Quantidade = 4 },
            new() { DataValidade = null, Quantidade = 3 }
        };

        Assert.Equal("4 un. com validade 12/11/2026; 3 un. sem validade informada", Rotulos.Previsao(saldos));
    }

    [Fact]
    public void Previsao_Vazia_Nao_Gera_Texto()
    {
        Assert.Equal(string.Empty, Rotulos.Previsao(new List<SaldoValidade>()));
    }

    [Fact]
    public void Previsao_Usa_O_Que_O_Service_Calcula_E_Nao_Altera_O_Estoque()
    {
        var dados = new AppData();
        var estoque = new EstoqueService(dados);
        var movimentacoes = new MovimentacaoService(dados, estoque);
        var produto = new Produto { Nome = "Dipirona" };
        dados.Produtos.Add(produto);
        estoque.AumentarEstoque(produto, 3, null);
        estoque.AumentarEstoque(produto, 4, new DateOnly(2026, 11, 12));

        var texto = Rotulos.Previsao(movimentacoes.PrevisaoDeSaida(produto, 5));

        // FEFO: primeiro o que tem validade, depois o que não tem.
        Assert.Equal("4 un. com validade 12/11/2026; 1 un. sem validade informada", texto);
        Assert.Equal(7, produto.Quantidade); // só previsão: nada saiu
    }

    // ---------- Aviso de validade passada ----------

    [Fact]
    public void AvisoDeValidadePassada_Avisa_So_Quando_A_Data_Ja_Passou()
    {
        const string aviso = "Esta validade já passou: o produto vai aparecer como vencido.";

        Assert.Equal(aviso, Rotulos.AvisoDeValidadePassada(Hoje.AddDays(-1), Hoje));
        Assert.Equal(string.Empty, Rotulos.AvisoDeValidadePassada(Hoje, Hoje)); // vence hoje: ainda não passou
        Assert.Equal(string.Empty, Rotulos.AvisoDeValidadePassada(Hoje.AddDays(30), Hoje));
        Assert.Equal(string.Empty, Rotulos.AvisoDeValidadePassada(null, Hoje)); // sem validade
    }

    // ---------- Busca do Histórico ----------

    [Fact]
    public void Busca_Acha_Por_Produto_Retirante_Destino_Documento_E_Observacao()
    {
        var movimentacao = new Movimentacao
        {
            Produto = new Produto { Nome = "Dipirona" },
            Tipo = TipoMovimentacao.Saida,
            Quantidade = 2,
            RetiradoPor = "Maria Souza",
            Destino = "Canil",
            Documento = "NF 4521",
            Observacao = "Reposição urgente"
        };

        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "dipi"));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "maria"));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "canil"));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "4521"));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "urgente"));
        Assert.False(Rotulos.MovimentacaoContem(movimentacao, "clínica"));
    }

    [Fact]
    public void Busca_Ignora_Maiusculas_Acentos_E_Espacos_Nas_Pontas()
    {
        var movimentacao = Saida("Dipirona", retiradoPor: "José", observacao: "Reposição");

        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "JOSE"));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "reposicao"));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "  dipirona  "));
    }

    [Fact]
    public void Busca_Vazia_Acha_Tudo_E_Campos_Nulos_Nao_Quebram()
    {
        var movimentacao = Saida("Dipirona"); // sem retirante, destino, documento nem observação

        Assert.True(Rotulos.MovimentacaoContem(movimentacao, null));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, ""));
        Assert.True(Rotulos.MovimentacaoContem(movimentacao, "   "));
        Assert.False(Rotulos.MovimentacaoContem(movimentacao, "canil"));
    }

    // ---------- Contadores ----------

    [Fact]
    public void TotalFiltrado_Mostra_A_Lista_Filtrada_Em_Relacao_Ao_Total()
    {
        Assert.Equal("120 de 870 movimentações", Rotulos.TotalFiltrado(120, 870));
        Assert.Equal("0 de 870 movimentações", Rotulos.TotalFiltrado(0, 870));
        Assert.Equal("1 de 1 movimentação", Rotulos.TotalFiltrado(1, 1));
        Assert.Equal("Nenhuma movimentação registrada ainda.", Rotulos.TotalFiltrado(0, 0));
    }

    [Fact]
    public void MostrarMais_Informa_Quantas_Faltam()
    {
        Assert.Equal("Mostrar mais (20 restantes)", Rotulos.MostrarMais(20));
        Assert.Equal("Mostrar mais (1 restante)", Rotulos.MostrarMais(1));
    }
}
