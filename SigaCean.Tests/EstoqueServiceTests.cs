using SigaCean.Models;

namespace SigaCean.Tests;

public class EstoqueServiceTests
{
    private readonly Cenario _c = new();

    private static void AssertSaldo(SaldoValidade saldo, DateOnly? validade, int quantidade)
    {
        Assert.Equal(validade, saldo.DataValidade);
        Assert.Equal(quantidade, saldo.Quantidade);
    }

    private static readonly DateOnly Venc1 = new(2030, 1, 10);
    private static readonly DateOnly Venc2 = new(2030, 6, 20);
    private static readonly DateOnly Venc3 = new(2031, 3, 5);

    // ---------- Aumentar ----------

    [Fact]
    public void Entradas_Com_A_Mesma_Validade_Somam_No_Mesmo_Saldo()
    {
        var produto = _c.NovoProduto();

        _c.Estoque.AumentarEstoque(produto, 10, Venc1);
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);

        var saldo = Assert.Single(produto.Saldos);
        Assert.Equal(15, saldo.Quantidade);
        Assert.Equal(15, produto.Quantidade);
    }

    [Fact]
    public void Entradas_Sem_Validade_Tambem_Somam_No_Mesmo_Saldo()
    {
        var produto = _c.NovoProduto();

        _c.Estoque.AumentarEstoque(produto, 4, null);
        _c.Estoque.AumentarEstoque(produto, 6, null);

        var saldo = Assert.Single(produto.Saldos);
        Assert.Null(saldo.DataValidade);
        Assert.Equal(10, saldo.Quantidade);
    }

    [Fact]
    public void Entradas_Com_Validades_Diferentes_Ficam_Em_Saldos_Separados()
    {
        var produto = _c.NovoProduto();

        _c.Estoque.AumentarEstoque(produto, 10, Venc1);
        _c.Estoque.AumentarEstoque(produto, 5, Venc2);
        _c.Estoque.AumentarEstoque(produto, 3, null);

        Assert.Equal(3, produto.Saldos.Count);
        Assert.Equal(18, produto.Quantidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Aumentar_Com_Quantidade_Zero_Ou_Negativa_E_Recusado_E_Nada_Muda(int quantidade)
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.AumentarEstoque(produto, quantidade, Venc2));

        Assert.Equal(antes, Cenario.Foto(produto));
    }

    // ---------- Diminuir (FEFO) ----------

    [Fact]
    public void Saida_Consome_Primeiro_O_Saldo_Que_Vence_Primeiro()
    {
        var produto = _c.NovoProduto();
        // Entradas fora de ordem de propósito.
        _c.Estoque.AumentarEstoque(produto, 10, Venc2);
        _c.Estoque.AumentarEstoque(produto, 10, Venc1);

        var retirados = _c.Estoque.DiminuirEstoque(produto, 4);

        var retirado = Assert.Single(retirados);
        Assert.Equal(Venc1, retirado.DataValidade);
        Assert.Equal(4, retirado.Quantidade);
        Assert.Equal(6, produto.Saldos.Single(s => s.DataValidade == Venc1).Quantidade);
        Assert.Equal(10, produto.Saldos.Single(s => s.DataValidade == Venc2).Quantidade);
    }

    [Fact]
    public void Saldo_Sem_Validade_E_Consumido_Por_Ultimo()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, null); // sem validade (entrou primeiro)
        _c.Estoque.AumentarEstoque(produto, 5, Venc3);

        var retirados = _c.Estoque.DiminuirEstoque(produto, 5);

        var retirado = Assert.Single(retirados);
        Assert.Equal(Venc3, retirado.DataValidade);
        Assert.Equal(5, produto.Saldos.Single().Quantidade);
        Assert.Null(produto.Saldos.Single().DataValidade); // o sem validade continua lá
    }

    [Fact]
    public void Saida_Atravessa_Varios_Saldos_Pela_Ordem_De_Validade()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 3, null);
        _c.Estoque.AumentarEstoque(produto, 5, Venc3);
        _c.Estoque.AumentarEstoque(produto, 4, Venc1);
        _c.Estoque.AumentarEstoque(produto, 6, Venc2);

        // 4 (Venc1) + 6 (Venc2) + 2 (Venc3) = 12
        var retirados = _c.Estoque.DiminuirEstoque(produto, 12);

        Assert.Equal(3, retirados.Count);
        AssertSaldo(retirados[0], Venc1, 4);
        AssertSaldo(retirados[1], Venc2, 6);
        AssertSaldo(retirados[2], Venc3, 2);
        Assert.Equal(6, produto.Quantidade); // sobraram 3 do Venc3 e 3 sem validade
    }

    [Fact]
    public void Quando_O_Estoque_Todo_Sai_O_Sem_Validade_Sai_Por_Ultimo()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 3, null);
        _c.Estoque.AumentarEstoque(produto, 2, Venc1);

        var retirados = _c.Estoque.DiminuirEstoque(produto, 5);

        Assert.Equal(2, retirados.Count);
        Assert.Equal(Venc1, retirados[0].DataValidade);
        Assert.Null(retirados[1].DataValidade);
    }

    [Fact]
    public void Saldo_Que_Fica_Zerado_E_Removido_Da_Lista()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);
        _c.Estoque.AumentarEstoque(produto, 5, Venc2);

        _c.Estoque.DiminuirEstoque(produto, 5);

        var saldo = Assert.Single(produto.Saldos);
        Assert.Equal(Venc2, saldo.DataValidade);
    }

    [Fact]
    public void Retirar_Tudo_Deixa_A_Lista_De_Saldos_Vazia()
    {
        var produto = _c.NovoProdutoComEstoque(7, Venc1);

        _c.Estoque.DiminuirEstoque(produto, 7);

        Assert.Empty(produto.Saldos);
        Assert.Equal(0, produto.Quantidade);
    }

    [Fact]
    public void Lista_Devolvida_Tem_Objetos_Novos_E_Nao_Os_Saldos_Do_Produto()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var saldoOriginal = produto.Saldos.Single();

        var retirados = _c.Estoque.DiminuirEstoque(produto, 3);

        Assert.NotSame(saldoOriginal, retirados.Single());
        Assert.Equal(3, retirados.Single().Quantidade);
        Assert.Equal(7, saldoOriginal.Quantidade);
    }

    [Fact]
    public void Saida_Maior_Que_O_Estoque_E_Recusada_E_Nada_Muda()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);
        _c.Estoque.AumentarEstoque(produto, 3, Venc2);
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.DiminuirEstoque(produto, 9));

        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal(8, produto.Quantidade);
        Assert.False(_c.Estoque.EstaNegativo(produto));
    }

    [Fact]
    public void Saida_Exatamente_Igual_Ao_Estoque_E_Permitida()
    {
        var produto = _c.NovoProdutoComEstoque(8, Venc1);

        _c.Estoque.DiminuirEstoque(produto, 8);

        Assert.Equal(0, produto.Quantidade);
        Assert.True(_c.Estoque.EstaSemEstoque(produto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-20)]
    public void Diminuir_Com_Quantidade_Zero_Ou_Negativa_E_Recusado_E_Nada_Muda(int quantidade)
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.DiminuirEstoque(produto, quantidade));

        Assert.Equal(antes, Cenario.Foto(produto));
    }

    // ---------- Atualizar quantidade (balanço) ----------

    [Fact]
    public void Atualizar_Para_Mais_Entra_Sem_Validade_E_Nao_Retira_Nada()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        var retirados = _c.Estoque.AtualizarQuantidade(produto, 14);

        Assert.Empty(retirados);
        Assert.Equal(14, produto.Quantidade);
        Assert.Equal(4, produto.Saldos.Single(s => s.DataValidade is null).Quantidade);
        Assert.Equal(10, produto.Saldos.Single(s => s.DataValidade == Venc1).Quantidade);
    }

    [Fact]
    public void Atualizar_Para_Menos_Retira_Pelo_FEFO_E_Devolve_O_Que_Saiu()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);
        _c.Estoque.AumentarEstoque(produto, 5, Venc2);

        var retirados = _c.Estoque.AtualizarQuantidade(produto, 3);

        Assert.Equal(3, produto.Quantidade);
        Assert.Equal(2, retirados.Count);
        AssertSaldo(retirados[0], Venc1, 5);
        AssertSaldo(retirados[1], Venc2, 2);
    }

    [Fact]
    public void Atualizar_Para_A_Mesma_Quantidade_Nao_Muda_Nada()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var antes = Cenario.Foto(produto);

        var retirados = _c.Estoque.AtualizarQuantidade(produto, 10);

        Assert.Empty(retirados);
        Assert.Equal(antes, Cenario.Foto(produto));
    }

    [Fact]
    public void Atualizar_Para_Zero_Esvazia_O_Estoque()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        _c.Estoque.AtualizarQuantidade(produto, 0);

        Assert.Empty(produto.Saldos);
    }

    [Fact]
    public void Atualizar_Para_Quantidade_Negativa_E_Recusado_E_Nada_Muda()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.AtualizarQuantidade(produto, -1));

        Assert.Equal(antes, Cenario.Foto(produto));
    }

    // ---------- Desfazer entrada ----------

    [Fact]
    public void Desfazer_Entrada_Retira_Do_Saldo_Da_Mesma_Validade()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 10, Venc1);
        _c.Estoque.AumentarEstoque(produto, 10, Venc2);

        _c.Estoque.DesfazerEntrada(produto, 4, Venc2);

        Assert.Equal(10, produto.Saldos.Single(s => s.DataValidade == Venc1).Quantidade);
        Assert.Equal(6, produto.Saldos.Single(s => s.DataValidade == Venc2).Quantidade);
    }

    [Fact]
    public void Desfazer_Entrada_Que_Zera_O_Saldo_Remove_O_Saldo()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        _c.Estoque.DesfazerEntrada(produto, 10, Venc1);

        Assert.Empty(produto.Saldos);
    }

    [Fact]
    public void Desfazer_Entrada_Cujo_Estoque_Ja_Foi_Usado_E_Recusado_E_Nada_Muda()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);
        _c.Estoque.DiminuirEstoque(produto, 7); // sobraram 3
        var antes = Cenario.Foto(produto);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.DesfazerEntrada(produto, 10, Venc1));

        Assert.Equal(antes, Cenario.Foto(produto));
    }

    [Fact]
    public void Desfazer_Entrada_De_Validade_Que_Nao_Existe_E_Recusado()
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.DesfazerEntrada(produto, 1, Venc2));
        Assert.Equal(10, produto.Quantidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Desfazer_Entrada_Com_Quantidade_Invalida_E_Recusado(int quantidade)
    {
        var produto = _c.NovoProdutoComEstoque(10, Venc1);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.DesfazerEntrada(produto, quantidade, Venc1));
        Assert.Equal(10, produto.Quantidade);
    }

    // ---------- Regras de situação (já existentes) ----------

    [Fact]
    public void Estoque_Exatamente_No_Minimo_Conta_Como_Abaixo_Do_Minimo()
    {
        var produto = _c.NovoProdutoComEstoque(5, minimo: 5);

        Assert.True(_c.Estoque.EstaAbaixoDoMinimo(produto));
        Assert.Equal(SituacaoEstoque.AbaixoDoMinimo, _c.Estoque.VerificarSituacao(produto));
    }

    [Fact]
    public void Estoque_Acima_Do_Minimo_E_Normal()
    {
        var produto = _c.NovoProdutoComEstoque(6, minimo: 5);

        Assert.False(_c.Estoque.EstaAbaixoDoMinimo(produto));
        Assert.Equal(SituacaoEstoque.Normal, _c.Estoque.VerificarSituacao(produto));
    }

    [Fact]
    public void Saldo_Zerado_Com_Validade_Passada_Nao_Deixa_O_Produto_Vencido()
    {
        var ontem = Cenario.Hoje.AddDays(-1);
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, ontem);
        Assert.True(_c.Estoque.TemValidadeVencida(produto, Cenario.Hoje)); // enquanto existe, está vencido

        _c.Estoque.DiminuirEstoque(produto, 5);

        Assert.False(_c.Estoque.TemValidadeVencida(produto, Cenario.Hoje));
        Assert.False(_c.Estoque.TemValidadeProxima(produto, Cenario.Hoje));
    }

    [Fact]
    public void Saldo_Com_Quantidade_Zero_E_Validade_Passada_Nao_Conta_Como_Vencido()
    {
        var produto = _c.NovoProduto();
        produto.Saldos.Add(new SaldoValidade { DataValidade = Cenario.Hoje.AddDays(-10), Quantidade = 0 });

        Assert.False(_c.Estoque.TemValidadeVencida(produto, Cenario.Hoje));
    }

    // ---------- Previsão do FEFO ----------

    [Fact]
    public void Previsao_Fefo_Mostra_As_Validades_Em_Ordem_E_Nao_Altera_O_Estoque()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 10, Venc2);
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);
        var antes = Cenario.Foto(produto);

        var previsao = _c.Estoque.PrevisaoFefo(produto, 8);

        Assert.Equal(2, previsao.Count);
        AssertSaldo(previsao[0], Venc1, 5);
        AssertSaldo(previsao[1], Venc2, 3);
        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.Equal(15, produto.Quantidade);
    }

    [Fact]
    public void Previsao_Fefo_Deixa_O_Saldo_Sem_Validade_Por_Ultimo()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 4, null);
        _c.Estoque.AumentarEstoque(produto, 3, Venc1);

        var previsao = _c.Estoque.PrevisaoFefo(produto, 5);

        Assert.Equal(2, previsao.Count);
        AssertSaldo(previsao[0], Venc1, 3);
        AssertSaldo(previsao[1], null, 2);
    }

    [Fact]
    public void Previsao_Fefo_Da_Quantidade_Toda_Devolve_Todos_Os_Saldos()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 4, Venc2);
        _c.Estoque.AumentarEstoque(produto, 3, Venc1);

        var previsao = _c.Estoque.PrevisaoFefo(produto, 7);

        Assert.Equal(7, previsao.Sum(s => s.Quantidade));
        Assert.Equal(2, previsao.Count);
        Assert.Equal(7, produto.Quantidade);
    }

    [Fact]
    public void Previsao_Fefo_Maior_Que_O_Estoque_E_Recusada_E_Nada_Muda()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);
        var antes = Cenario.Foto(produto);

        var erro = Assert.Throws<InvalidOperationException>(() => _c.Estoque.PrevisaoFefo(produto, 6));

        Assert.Contains("5", erro.Message);
        Assert.Equal(antes, Cenario.Foto(produto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Previsao_Fefo_Com_Quantidade_Zero_Ou_Negativa_E_Recusada(int quantidade)
    {
        var produto = _c.NovoProdutoComEstoque(5);

        Assert.Throws<InvalidOperationException>(() => _c.Estoque.PrevisaoFefo(produto, quantidade));
        Assert.Equal(5, produto.Quantidade);
    }

    [Fact]
    public void Previsao_Fefo_Devolve_Objetos_Novos_Que_Nao_Mexem_No_Estoque_Se_Alterados()
    {
        var produto = _c.NovoProdutoComEstoque(5, Venc1);

        var previsao = _c.Estoque.PrevisaoFefo(produto, 2);
        previsao[0].Quantidade = 999;

        Assert.Equal(5, produto.Quantidade);
    }

    [Fact]
    public void Diminuir_Estoque_Retira_Exatamente_O_Que_A_Previsao_Mostrou()
    {
        var produto = _c.NovoProduto();
        _c.Estoque.AumentarEstoque(produto, 10, Venc2);
        _c.Estoque.AumentarEstoque(produto, 5, Venc1);
        _c.Estoque.AumentarEstoque(produto, 4, null);

        var previsao = _c.Estoque.PrevisaoFefo(produto, 17);
        var retirados = _c.Estoque.DiminuirEstoque(produto, 17);

        Assert.Equal(previsao.Count, retirados.Count);
        for (var i = 0; i < previsao.Count; i++)
            AssertSaldo(retirados[i], previsao[i].DataValidade, previsao[i].Quantidade);

        Assert.Equal(2, produto.Quantidade);
        AssertSaldo(Assert.Single(produto.Saldos), null, 2);
    }
}
