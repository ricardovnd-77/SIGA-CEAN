using SigaCean.Services;

namespace SigaCean.Tests;

public class PeriodoRelatorioTests
{
    [Fact]
    public void Mensal_Vai_Do_Primeiro_Ao_Ultimo_Dia_Do_Mes()
    {
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Mensal, new DateTime(2026, 2, 15, 13, 0, 0));

        Assert.Equal(new DateTime(2026, 2, 1), inicio);
        Assert.Equal(new DateTime(2026, 2, 28), fim);
    }

    [Fact]
    public void Mensal_Em_Ano_Bissexto_Termina_No_Dia_29()
    {
        var (_, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Mensal, new DateTime(2028, 2, 10));

        Assert.Equal(new DateTime(2028, 2, 29), fim);
    }

    [Theory]
    [InlineData(1, 1, 3, 31)]
    [InlineData(5, 4, 6, 30)]
    [InlineData(9, 7, 9, 30)]
    [InlineData(12, 10, 12, 31)]
    public void Trimestral_Cobre_Tres_Meses_Do_Calendario(int mesDaReferencia, int mesInicial, int mesFinal, int diaFinal)
    {
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Trimestral, new DateTime(2026, mesDaReferencia, 10));

        Assert.Equal(new DateTime(2026, mesInicial, 1), inicio);
        Assert.Equal(new DateTime(2026, mesFinal, diaFinal), fim);
    }

    [Fact]
    public void Anual_Vai_De_Primeiro_De_Janeiro_A_Trinta_E_Um_De_Dezembro()
    {
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Anual, new DateTime(2026, 8, 3));

        Assert.Equal(new DateTime(2026, 1, 1), inicio);
        Assert.Equal(new DateTime(2026, 12, 31), fim);
    }

    [Fact]
    public void Periodo_Personalizado_Nao_Tem_Calculo_Automatico()
    {
        Assert.Throws<ArgumentException>(() => PeriodoRelatorio.Contendo(TipoPeriodo.Personalizado, DateTime.Today));
        Assert.Throws<ArgumentException>(() => PeriodoRelatorio.Deslocar(TipoPeriodo.Personalizado, DateTime.Today, 1));
    }

    [Fact]
    public void Deslocar_Mensal_Para_Tras_Atravessa_O_Ano()
    {
        var (inicio, fim) = PeriodoRelatorio.Deslocar(TipoPeriodo.Mensal, new DateTime(2026, 1, 1), -1);

        Assert.Equal(new DateTime(2025, 12, 1), inicio);
        Assert.Equal(new DateTime(2025, 12, 31), fim);
    }

    [Fact]
    public void Deslocar_Trimestral_Para_Frente_Vai_Ao_Proximo_Trimestre()
    {
        var (inicio, fim) = PeriodoRelatorio.Deslocar(TipoPeriodo.Trimestral, new DateTime(2026, 1, 1), 1);

        Assert.Equal(new DateTime(2026, 4, 1), inicio);
        Assert.Equal(new DateTime(2026, 6, 30), fim);
    }

    [Fact]
    public void Deslocar_Anual_Muda_De_Ano()
    {
        var (inicio, fim) = PeriodoRelatorio.Deslocar(TipoPeriodo.Anual, new DateTime(2026, 1, 1), -1);

        Assert.Equal(new DateTime(2025, 1, 1), inicio);
        Assert.Equal(new DateTime(2025, 12, 31), fim);
    }

    [Fact]
    public void Descrever_Escreve_O_Periodo_Em_Portugues()
    {
        Assert.Equal("março de 2026",
            PeriodoRelatorio.Descrever(TipoPeriodo.Mensal, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)));
        Assert.Equal("2º trimestre de 2026 (abril a junho)",
            PeriodoRelatorio.Descrever(TipoPeriodo.Trimestral, new DateTime(2026, 4, 1), new DateTime(2026, 6, 30)));
        Assert.Equal("2026",
            PeriodoRelatorio.Descrever(TipoPeriodo.Anual, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));
    }

    [Fact]
    public void Meses_No_Periodo_Nunca_E_Menor_Que_Um()
    {
        Assert.Equal(1m, PeriodoRelatorio.MesesNoPeriodo(new DateTime(2026, 10, 5), new DateTime(2026, 10, 5)));
        Assert.Equal(1.018m, Math.Round(PeriodoRelatorio.MesesNoPeriodo(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)), 3));
        Assert.Equal(12m, Math.Round(PeriodoRelatorio.MesesNoPeriodo(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)), 0));
    }
}