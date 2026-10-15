using SigaCean.Models;

namespace SigaCean.Tests;

public class MovimentacaoServiceTests
{
    private readonly Cenario _c = new();

    private static readonly DateOnly Venc1 = new(2030, 1, 10);
    private static readonly DateOnly Venc2 = new(2030, 6, 20);

    private Movimentacao Entrar(Produto produto, int quantidade, DateOnly? validade = null) =>
        _c.Movimentacoes.RegistrarEntrada(produto, quantidade, TipoOrigemEntrada.Compra,
            null, null, null, validade, null);

    private Movimentacao Sair(Produto produto, int quantidade) =>
        _c.Movimentacoes.RegistrarSaida(produto, quantidade, "Canil", null, null);

    // ---------- Entrada ----------

    [Fact]
    public void Entrada_Valida_Aumenta_O_Estoque_E_Grava_A_Movimentacao()
    {
        var produto = _c.NovoProduto();

        var mov = _c.Movimentacoes.RegistrarEntrada(produto, 10, TipoOrigemEntrada.Doacao,
            "  Maria  ", " ", " NF 123 ", Venc1, null, "  obs  ");

        Assert.Equal(10, produto.Quantidade);
        Assert.Equal(Venc1, Assert.Single(produto.Saldos).DataValidade);
        Assert.Same(mov, Assert.Single(_c.Dados.Movimentacoes));
        Assert.Equal(TipoMovimentacao.Entrada, mov.Tipo);
        Assert.Equal(StatusMovimentacao.Ativa, mov.Status);
        Assert.Equal(10, mov.Quantidade);
        Assert.Equal(TipoOrigemEntrada.Doacao, mov.TipoOrigem);
        Assert.Equal("Maria", mov.Origem);
        Assert.Null(mov.NumeroAF);
        Assert.Equal("NF 123", mov.Documento);
        Assert.Equal(Venc1, mov.DataValidade);
        Assert.Equal("obs", mov.Observacao);
        Assert.True((DateTime.Now - mov.Data).Duration() < TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Entrada_Com_Quantidade_Zero_Ou_Negativa_E_Recusada_E_Nada_Muda(int quantidade)
    {
        var produto = _c.NovoProduto();

        Assert.Throws<InvalidOperationException>(() => Entrar(produto, quantidade));

        Assert.Equal(0, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    [Fact]
    public void Entrada_Em_Produto_Inativo_E_Recusada_E_Nada_Muda()
    {
        var produto = _c.NovoProduto();
        produto.Ativo = false;

        Assert.Throws<InvalidOperationException>(() => Entrar(produto, 5));

        Assert.Equal(0, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    // ---------- Saída ----------

    [Fact]
    public void Saida_Valida_Diminui_O_Estoque_Pelo_FEFO_E_Guarda_De_Onde_Saiu()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 10, Venc2);
        Entrar(produto, 5, Venc1);

        var mov = _c.Movimentacoes.RegistrarSaida(produto, 8, "  Canil ", "  João ", null, "obs");

        Assert.Equal(7, produto.Quantidade);
        Assert.Equal(TipoMovimentacao.Saida, mov.Tipo);
        Assert.Equal(8, mov.Quantidade);
        Assert.Equal("Canil", mov.Destino);
        Assert.Equal("João", mov.RetiradoPor);
        Assert.Equal(2, mov.SaldosConsumidos.Count);
        Assert.Equal(Venc1, mov.SaldosConsumidos[0].DataValidade);
        Assert.Equal(5, mov.SaldosConsumidos[0].Quantidade);
        Assert.Equal(Venc2, mov.SaldosConsumidos[1].DataValidade);
        Assert.Equal(3, mov.SaldosConsumidos[1].Quantidade);
    }

    [Fact]
    public void Saida_Sem_Retirado_Por_E_Permitida()
    {
        var produto = _c.NovoProdutoComEstoque(5);

        var mov = _c.Movimentacoes.RegistrarSaida(produto, 1, "Canil", "  ", null);

        Assert.Null(mov.RetiradoPor);
    }

    [Fact]
    public void Saida_Maior_Que_O_Estoque_E_Recusada_E_Estoque_E_Historico_Nao_Mudam()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 5, Venc1);
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => Sair(produto, 6));

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Single(_c.Dados.Movimentacoes); // só a entrada
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Saida_Sem_Destino_E_Recusada_E_Nada_Muda(string destino)
    {
        var produto = _c.NovoProdutoComEstoque(5);

        Assert.Throws<InvalidOperationException>(() =>
            _c.Movimentacoes.RegistrarSaida(produto, 1, destino, null, null));

        Assert.Equal(5, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Saida_Com_Quantidade_Zero_Ou_Negativa_E_Recusada(int quantidade)
    {
        var produto = _c.NovoProdutoComEstoque(5);

        Assert.Throws<InvalidOperationException>(() => Sair(produto, quantidade));

        Assert.Equal(5, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    [Fact]
    public void Saida_De_Produto_Inativo_E_Recusada()
    {
        var produto = _c.NovoProdutoComEstoque(5);
        produto.Ativo = false;

        Assert.Throws<InvalidOperationException>(() => Sair(produto, 1));

        Assert.Equal(5, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    // ---------- Ajuste de balanço ----------

    [Fact]
    public void Ajuste_Para_Mais_Registra_Diferenca_Positiva()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        var mov = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 13, null, "contagem");

        Assert.Equal(13, produto.Quantidade);
        Assert.Equal(TipoMovimentacao.AjusteBalanco, mov.Tipo);
        Assert.Equal(3, mov.Quantidade);
        Assert.Equal(10, mov.QuantidadeAnterior);
        Assert.Equal(13, mov.QuantidadeApurada);
        Assert.Empty(mov.SaldosConsumidos);
    }

    [Fact]
    public void Ajuste_Para_Menos_Registra_Diferenca_Negativa_E_Guarda_O_Que_Saiu()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 5, Venc1);
        Entrar(produto, 5, Venc2);

        var mov = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 3, null);

        Assert.Equal(3, produto.Quantidade);
        Assert.Equal(-7, mov.Quantidade);
        Assert.Equal(10, mov.QuantidadeAnterior);
        Assert.Equal(3, mov.QuantidadeApurada);
        Assert.Equal(7, mov.SaldosConsumidos.Sum(s => s.Quantidade));
        Assert.Equal(Venc1, mov.SaldosConsumidos[0].DataValidade);
    }

    [Fact]
    public void Ajuste_Sem_Diferenca_Grava_A_Movimentacao_Mesmo_Assim()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        var mov = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 10, null);

        Assert.Equal(0, mov.Quantidade);
        Assert.Equal(10, produto.Quantidade);
        Assert.Same(mov, Assert.Single(_c.Dados.Movimentacoes));
    }

    [Fact]
    public void Ajuste_Com_Quantidade_Apurada_Negativa_E_Recusado_E_Nada_Muda()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.RegistrarAjusteBalanco(produto, -1, null));

        Assert.Equal(10, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    [Fact]
    public void Ajuste_Em_Produto_Inativo_E_Recusado()
    {
        var produto = _c.NovoProdutoComEstoque(10);
        produto.Ativo = false;

        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.RegistrarAjusteBalanco(produto, 5, null));

        Assert.Equal(10, produto.Quantidade);
        Assert.Empty(_c.Dados.Movimentacoes);
    }

    // ---------- Cancelar e corrigir ----------

    [Fact]
    public void Cancelar_Entrada_Devolve_O_Estoque_E_Marca_A_Movimentacao()
    {
        var produto = _c.NovoProduto();
        var entrada = Entrar(produto, 10, Venc1);

        _c.Movimentacoes.Cancelar(entrada.Id, "  lançada errado ");

        Assert.Equal(0, produto.Quantidade);
        Assert.Empty(produto.Saldos);
        Assert.Equal(StatusMovimentacao.Cancelada, entrada.Status);
        Assert.Equal("lançada errado", entrada.MotivoCorrecaoOuCancelamento);
        Assert.Contains(entrada, _c.Dados.Movimentacoes); // não é apagada
    }

    [Fact]
    public void Cancelar_Entrada_Cujo_Estoque_Ja_Foi_Usado_E_Recusado_E_Nada_Muda()
    {
        var produto = _c.NovoProduto();
        var entrada = Entrar(produto, 10, Venc1);
        Sair(produto, 4);
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Cancelar(entrada.Id, "erro"));

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal(StatusMovimentacao.Ativa, entrada.Status);
        Assert.Null(entrada.MotivoCorrecaoOuCancelamento);
    }

    [Fact]
    public void Cancelar_Saida_Devolve_A_Quantidade_A_Validade_De_Origem()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 5, Venc1);
        Entrar(produto, 5, Venc2);
        var saida = Sair(produto, 7); // 5 do Venc1 + 2 do Venc2

        _c.Movimentacoes.Cancelar(saida.Id, "saída lançada por engano");

        Assert.Equal(10, produto.Quantidade);
        Assert.Equal(5, produto.Saldos.Single(s => s.DataValidade == Venc1).Quantidade);
        Assert.Equal(5, produto.Saldos.Single(s => s.DataValidade == Venc2).Quantidade);
        Assert.Equal(StatusMovimentacao.Cancelada, saida.Status);
    }

    [Fact]
    public void Cancelar_Saida_Recria_O_Saldo_Que_Tinha_Sido_Zerado()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 5, Venc1);
        var saida = Sair(produto, 5);
        Assert.Empty(produto.Saldos);

        _c.Movimentacoes.Cancelar(saida.Id, "erro");

        var saldo = Assert.Single(produto.Saldos);
        Assert.Equal(Venc1, saldo.DataValidade);
        Assert.Equal(5, saldo.Quantidade);
    }

    [Fact]
    public void Corrigir_Desfaz_O_Efeito_E_Marca_Como_Corrigida_Sem_Criar_Movimentacao_Nova()
    {
        var produto = _c.NovoProduto();
        var entrada = Entrar(produto, 10, Venc1);

        _c.Movimentacoes.Corrigir(entrada.Id, "quantidade errada");

        Assert.Equal(0, produto.Quantidade);
        Assert.Equal(StatusMovimentacao.Corrigida, entrada.Status);
        Assert.Equal("quantidade errada", entrada.MotivoCorrecaoOuCancelamento);
        Assert.Single(_c.Dados.Movimentacoes);
    }

    [Fact]
    public void Cancelar_Ajuste_Para_Cima_Retira_A_Sobra()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var ajuste = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 14, null);

        _c.Movimentacoes.Cancelar(ajuste.Id, "contagem errada");

        Assert.Equal(10, produto.Quantidade);
        Assert.Equal(Venc1, Assert.Single(produto.Saldos).DataValidade);
    }

    [Fact]
    public void Cancelar_Ajuste_Para_Cima_Cuja_Sobra_Ja_Foi_Usada_E_Recusado()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var ajuste = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 14, null); // +4 sem validade
        Sair(produto, 14); // FEFO: 10 do Venc1 e depois as 4 sem validade -> tudo usado
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Cancelar(ajuste.Id, "erro"));

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal(StatusMovimentacao.Ativa, ajuste.Status);
    }

    [Fact]
    public void Cancelar_Ajuste_Para_Baixo_Devolve_O_Que_Saiu()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 5, Venc1);
        Entrar(produto, 5, Venc2);
        var ajuste = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 3, null); // -7

        _c.Movimentacoes.Cancelar(ajuste.Id, "contagem errada");

        Assert.Equal(10, produto.Quantidade);
        Assert.Equal(5, produto.Saldos.Single(s => s.DataValidade == Venc1).Quantidade);
        Assert.Equal(5, produto.Saldos.Single(s => s.DataValidade == Venc2).Quantidade);
    }

    [Fact]
    public void Cancelar_Ajuste_Sem_Diferenca_Nao_Mexe_No_Estoque()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var ajuste = _c.Movimentacoes.RegistrarAjusteBalanco(produto, 10, null);
        var antes = Cenario.Foto(produto);

        _c.Movimentacoes.Cancelar(ajuste.Id, "conferência repetida");

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal(StatusMovimentacao.Cancelada, ajuste.Status);
    }

    [Fact]
    public void Cancelar_Duas_Vezes_E_Recusado_E_O_Estoque_Nao_Muda_De_Novo()
    {
        var produto = _c.NovoProduto();
        Entrar(produto, 5, Venc1);
        var saida = Sair(produto, 3);
        _c.Movimentacoes.Cancelar(saida.Id, "erro");
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Cancelar(saida.Id, "de novo"));
        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Corrigir(saida.Id, "de novo"));

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal("erro", saida.MotivoCorrecaoOuCancelamento);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelar_Ou_Corrigir_Sem_Motivo_E_Recusado_E_Nada_Muda(string motivo)
    {
        var produto = _c.NovoProduto();
        var entrada = Entrar(produto, 5, Venc1);

        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Cancelar(entrada.Id, motivo));
        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Corrigir(entrada.Id, motivo));

        Assert.Equal(5, produto.Quantidade);
        Assert.Equal(StatusMovimentacao.Ativa, entrada.Status);
    }

    [Fact]
    public void Cancelar_Movimentacao_Inexistente_E_Recusado()
    {
        Assert.Throws<InvalidOperationException>(() => _c.Movimentacoes.Cancelar(Guid.NewGuid(), "motivo"));
    }

    // ---------- Histórico e período ----------

    [Fact]
    public void Periodo_Inclui_O_Dia_Final_Inteiro()
    {
        var produto = _c.NovoProduto();
        var noInicio = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 1, 0, 0, 0));
        var fimDoDia = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 5, 23, 59, 59));
        var meioDoDia = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 5, 14, 30, 0));
        var diaSeguinte = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 6, 0, 0, 0));
        var antes = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 9, 30, 23, 59, 59));

        var resultado = _c.Movimentacoes.FiltrarPorPeriodo(new DateTime(2026, 10, 1), new DateTime(2026, 10, 5));

        Assert.Equal(3, resultado.Count);
        Assert.Contains(noInicio, resultado);
        Assert.Contains(fimDoDia, resultado);
        Assert.Contains(meioDoDia, resultado);
        Assert.DoesNotContain(diaSeguinte, resultado);
        Assert.DoesNotContain(antes, resultado);
    }

    [Fact]
    public void Periodo_Vem_Da_Mais_Recente_Para_A_Mais_Antiga()
    {
        var produto = _c.NovoProduto();
        var a = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 1, 9, 0, 0));
        var b = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 3, 9, 0, 0));
        var c = _c.AdicionarMovimentacao(produto, TipoMovimentacao.Entrada, 1, new DateTime(2026, 10, 2, 9, 0, 0));

        var resultado = _c.Movimentacoes.FiltrarPorPeriodo(new DateTime(2026, 10, 1), new DateTime(2026, 10, 5));

        Assert.Equal(new[] { b, c, a }, resultado);
    }

    [Fact]
    public void Periodo_Com_Inicio_Depois_Do_Fim_E_Recusado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _c.Movimentacoes.FiltrarPorPeriodo(new DateTime(2026, 10, 10), new DateTime(2026, 10, 1)));
    }

    [Fact]
    public void Movimentacoes_Canceladas_E_Corrigidas_Continuam_No_Historico_E_No_Periodo()
    {
        var produto = _c.NovoProduto();
        var cancelada = Entrar(produto, 5, Venc1);
        var corrigida = Entrar(produto, 3, Venc2);
        var ativa = Entrar(produto, 2, null);
        _c.Movimentacoes.Cancelar(cancelada.Id, "erro");
        _c.Movimentacoes.Corrigir(corrigida.Id, "erro");

        var historico = _c.Movimentacoes.ConsultarHistorico();
        var periodo = _c.Movimentacoes.FiltrarPorPeriodo(DateTime.Today, DateTime.Today);

        Assert.Equal(3, historico.Count);
        Assert.Equal(3, periodo.Count);
        Assert.Contains(cancelada, periodo);
        Assert.Contains(corrigida, periodo);
        Assert.Contains(ativa, periodo);
        Assert.Equal(StatusMovimentacao.Cancelada, cancelada.Status);
        Assert.Equal(StatusMovimentacao.Corrigida, corrigida.Status);
    }
}
