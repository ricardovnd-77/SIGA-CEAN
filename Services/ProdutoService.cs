using System.Globalization;
using SigaCean.Data;
using SigaCean.Models;

namespace SigaCean.Services;

// Cadastro e consulta de produtos. NÃO mexe em quantidade: isso é do EstoqueService.
//
// REGRA DESTE PROJETO: o Service valida sozinho e recusa o que é inválido com InvalidOperationException,
// sem alterar nada. As telas também validam (para dar mensagem boa), mas não podem ser a única barreira.
public class ProdutoService
{
    private readonly AppData _dados;

    public ProdutoService(AppData dados)
    {
        _dados = dados;
    }

    // Já implementado, como exemplo do padrão: validar, ajustar, guardar.
    public Produto Cadastrar(string nome, Categoria categoria, string? apresentacao, string? especificacao, int? quantidadeMinima)
    {
        ValidarDados(nome, quantidadeMinima);

        var produto = new Produto
        {
            Nome = nome.Trim(),
            Categoria = categoria,
            Apresentacao = LimparTexto(apresentacao),
            Especificacao = LimparTexto(especificacao),
            QuantidadeMinima = quantidadeMinima
        };

        _dados.Produtos.Add(produto);
        return produto;
    }

    public List<Produto> ObterTodos() => _dados.Produtos.Where(p => p.Ativo).ToList();

    public Produto ObterPorId(Guid id) =>
        _dados.Produtos.FirstOrDefault(p => p.Id == id)
        ?? throw new InvalidOperationException("Produto não encontrado.");

    // Altera os dados cadastrais. NÃO mexe na quantidade (isso é do EstoqueService) nem no campo Ativo.
    public void Alterar(Guid id, string nome, Categoria categoria, string? apresentacao, string? especificacao, int? quantidadeMinima)
    {
        var produto = ObterPorId(id); // recusa se o produto não existe
        ValidarDados(nome, quantidadeMinima); // valida tudo antes de alterar

        produto.Nome = nome.Trim();
        produto.Categoria = categoria;
        produto.Apresentacao = LimparTexto(apresentacao);
        produto.Especificacao = LimparTexto(especificacao);
        produto.QuantidadeMinima = quantidadeMinima;
    }

    // Pesquisa por parte do nome, ignorando maiúsculas e ACENTOS ("injetavel" acha "INJETÁVEL").
    // Texto vazio devolve todos. Só produtos ativos, ordenados por nome.
    public List<Produto> Pesquisar(string? texto)
    {
        var busca = texto?.Trim();
        var ativos = _dados.Produtos.Where(p => p.Ativo);

        if (!string.IsNullOrEmpty(busca))
            ativos = ativos.Where(p => ContemTexto(p.Nome, busca));

        return OrdenarPorNome(ativos);
    }

    // Filtra por categoria (null = todas). Só produtos ativos, ordenados por nome.
    // Filtros por SITUAÇÃO do estoque (sem estoque, abaixo do mínimo, validade...) usam o EstoqueService.
    public List<Produto> Filtrar(Categoria? categoria)
    {
        var ativos = _dados.Produtos.Where(p => p.Ativo);

        if (categoria is not null)
            ativos = ativos.Where(p => p.Categoria == categoria);

        return OrdenarPorNome(ativos);
    }

    // ---------- Apoio (privados) ----------

    // Regras de validação dos dados cadastrais (as mesmas para Cadastrar e Alterar).
    private static void ValidarDados(string nome, int? quantidadeMinima)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new InvalidOperationException("Informe o nome do produto.");
        if (quantidadeMinima < 0)
            throw new InvalidOperationException("A quantidade mínima não pode ser negativa.");
    }

    // Texto opcional: limpa os espaços; vazio vira null.
    private static string? LimparTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // Procura "busca" dentro de "nome", sem diferenciar maiúsculas nem acentos.
    private static bool ContemTexto(string nome, string busca) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            nome, busca, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static List<Produto> OrdenarPorNome(IEnumerable<Produto> produtos) =>
        produtos.OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
}