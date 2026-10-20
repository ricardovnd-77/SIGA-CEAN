using SigaCean.Data;
using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Tests;

// Testa os dados de exemplo do CEAN (Data/DadosCean.cs). Aqui NÃO se testa regra de estoque (isso é dos testes dos Services):
// só se confere que os dados carregados batem com a planilha e que o Carregar não faz nada além do saldo inicial.
public class DadosCeanTests
{
    // Um AppData novo, já com os dados do CEAN carregados.
    private static AppData Carregado()
    {
        var dados = new AppData();
        DadosCean.Carregar(dados);
        return dados;
    }

    private static Produto Achar(AppData dados, string nome) =>
        dados.Produtos.Single(p => p.Nome == nome);

    [Fact]
    public void Nenhum_Nome_De_Produto_Se_Repete()
    {
        var nomes = DadosCean.Produtos.Select(p => p.Nome.Trim().ToUpperInvariant()).ToList();

        Assert.Equal(nomes.Count, nomes.Distinct().Count());
    }

    [Fact]
    public void Nenhuma_Quantidade_E_Negativa()
    {
        Assert.DoesNotContain(DadosCean.Produtos, p => p.Quantidade < 0);

        var dados = Carregado();
        Assert.DoesNotContain(dados.Produtos, p => p.Quantidade < 0);
    }

    [Fact]
    public void Todos_Os_Produtos_Da_Planilha_Sao_Cadastrados_Uma_Vez_Cada()
    {
        var dados = Carregado();

        Assert.Equal(DadosCean.Produtos.Length, dados.Produtos.Count);
        foreach (var (nome, quantidade) in DadosCean.Produtos)
            Assert.Equal(quantidade, Achar(dados, nome).Quantidade);
    }

    [Fact]
    public void Estoque_De_Produtos_Conhecidos_Bate_Com_A_Planilha()
    {
        var dados = Carregado();

        Assert.Equal(269, Achar(dados, "CLORIDRATO DE TRAMADOL - 50 MG - INJETÁVEL, AMPOLA").Quantidade);
        Assert.Equal(63, Achar(dados, "DIPIRONA - 500 MG - INJETÁVEL, AMPOLA").Quantidade);
        Assert.Equal(400, Achar(dados, "MICROCHIP").Quantidade);
    }

    [Fact]
    public void Produtos_Com_Quantidade_Zero_Aparecem_Como_Sem_Estoque()
    {
        var dados = Carregado();
        var estoque = new EstoqueService(dados);

        // "DIPIRONA 500 MG, FEBRAX - INJETÁVEL, FRASCO 50 ML" tem 0 na planilha.
        var zerado = Achar(dados, "DIPIRONA 500 MG, FEBRAX - INJETÁVEL, FRASCO 50 ML");
        Assert.Equal(SituacaoEstoque.SemEstoque, estoque.VerificarSituacao(zerado));

        // Todos os que a planilha informa como 0 ficam sem estoque; os demais não.
        foreach (var (nome, quantidade) in DadosCean.Produtos)
        {
            var situacao = estoque.VerificarSituacao(Achar(dados, nome));
            if (quantidade == 0)
                Assert.Equal(SituacaoEstoque.SemEstoque, situacao);
            else
                Assert.NotEqual(SituacaoEstoque.SemEstoque, situacao);
        }
    }

    [Fact]
    public void Carregar_Nao_Cria_Movimentacoes()
    {
        var dados = Carregado();

        Assert.Empty(dados.Movimentacoes);
    }

    [Fact]
    public void Carregar_Nao_Mexe_Nos_Usuarios_De_Teste()
    {
        var dados = Carregado();

        Assert.Equal(2, dados.Usuarios.Count);
    }

    [Fact]
    public void Estoque_Entra_Sem_Validade_E_Sem_Quantidade_Minima()
    {
        var dados = Carregado();

        foreach (var produto in dados.Produtos)
        {
            Assert.Null(produto.QuantidadeMinima);
            Assert.All(produto.Saldos, saldo => Assert.Null(saldo.DataValidade));
        }
    }

    [Fact]
    public void Categorias_Sao_Medicamento_Menos_Os_Itens_Separados_Pela_Equipe()
    {
        var dados = Carregado();

        Assert.Equal(Categoria.Microchip, Achar(dados, "MICROCHIP").Categoria);
        Assert.Equal(Categoria.Alimentar, Achar(dados, "PAPINHA DE DESMAME 1KG").Categoria);
        Assert.Equal(Categoria.Higiene, Achar(dados, "SHAMPOO, CLOREXIDINA 20 G - FRASCO 500 ML").Categoria);
        Assert.Equal(Categoria.Higiene, Achar(dados, "SHAMPOO, CETOCONAZOL 0,5 G + CLOREXIDINA 20 G - FRASCO 500 ML").Categoria);

        Assert.Equal(Categoria.Medicamento, Achar(dados, "DIPIRONA - 500 MG - INJETÁVEL, AMPOLA").Categoria);
        Assert.Equal(Categoria.Medicamento, Achar(dados, "TESTE DE CINOMOSE").Categoria); // decisão da equipe

        // Só esses 4 itens fogem de Medicamento.
        Assert.Equal(4, dados.Produtos.Count(p => p.Categoria != Categoria.Medicamento));
    }

    [Fact]
    public void AppData_Novo_Continua_Vazio_Sem_Chamar_O_Carregar()
    {
        var dados = new AppData();

        Assert.Empty(dados.Produtos);
        Assert.Empty(dados.Movimentacoes);
    }
}
