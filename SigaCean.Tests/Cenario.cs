using SigaCean.Data;
using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Tests;

// Monta um "mundo" novo e vazio para cada teste: dados em memória + todos os Services ligados entre si.
public class Cenario
{
    public AppData Dados { get; } = new();
    public EstoqueService Estoque { get; }
    public ProdutoService Produtos { get; }
    public MovimentacaoService Movimentacoes { get; }
    public RelatorioService Relatorios { get; }

    public Cenario()
    {
        Estoque = new EstoqueService(Dados);
        Produtos = new ProdutoService(Dados);
        Movimentacoes = new MovimentacaoService(Dados, Estoque);
        Relatorios = new RelatorioService(Dados, Estoque);
    }

    public Produto NovoProduto(string nome = "Dipirona", Categoria categoria = Categoria.Medicamento, int? minimo = null) =>
        Produtos.Cadastrar(nome, categoria, null, null, minimo);

    // Produto já com estoque (entradas feitas direto pelo EstoqueService, sem gerar movimentação).
    public Produto NovoProdutoComEstoque(int quantidade, DateOnly? validade = null, string nome = "Dipirona",
        Categoria categoria = Categoria.Medicamento, int? minimo = null)
    {
        var produto = NovoProduto(nome, categoria, minimo);
        Estoque.AumentarEstoque(produto, quantidade, validade);
        return produto;
    }

    // Adiciona direto em AppData uma movimentação com data e status específicos (Data é "init").
    public Movimentacao AdicionarMovimentacao(Produto produto, TipoMovimentacao tipo, int quantidade, DateTime data,
        StatusMovimentacao status = StatusMovimentacao.Ativa)
    {
        var movimentacao = new Movimentacao
        {
            Produto = produto,
            Tipo = tipo,
            Quantidade = quantidade,
            Data = data,
            Status = status
        };
        Dados.Movimentacoes.Add(movimentacao);
        return movimentacao;
    }

    public static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    // Foto do estoque (validade + quantidade de cada saldo), para comparar "antes" e "depois".
    public static string Foto(Produto produto) =>
        string.Join("|", produto.Saldos.Select(s => $"{s.DataValidade:yyyy-MM-dd}={s.Quantidade}"));
}
