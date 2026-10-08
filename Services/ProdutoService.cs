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
        if (string.IsNullOrWhiteSpace(nome))
            throw new InvalidOperationException("Informe o nome do produto.");
        if (quantidadeMinima < 0)
            throw new InvalidOperationException("A quantidade mínima não pode ser negativa.");

        var produto = new Produto
        {
            Nome = nome.Trim(),
            Categoria = categoria,
            Apresentacao = string.IsNullOrWhiteSpace(apresentacao) ? null : apresentacao.Trim(),
            Especificacao = string.IsNullOrWhiteSpace(especificacao) ? null : especificacao.Trim(),
            QuantidadeMinima = quantidadeMinima
        };

        _dados.Produtos.Add(produto);
        return produto;
    }

    public List<Produto> ObterTodos() => _dados.Produtos.Where(p => p.Ativo).ToList();

    public Produto ObterPorId(Guid id) =>
        _dados.Produtos.FirstOrDefault(p => p.Id == id)
        ?? throw new InvalidOperationException("Produto não encontrado.");

    // TODO: alterar os dados cadastrais de um produto (mesmas validações do Cadastrar).
    public void Alterar(Guid id, string nome, Categoria categoria, string? apresentacao, string? especificacao, int? quantidadeMinima)
    {
        throw new NotImplementedException("TODO: ProdutoService.Alterar");
    }

    // TODO: pesquisar por parte do nome, ignorando maiúsculas e ACENTOS ("injetavel" deve achar "INJETÁVEL").
    // Dica: string.Compare / CompareInfo.IndexOf com CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace.
    // Texto vazio = devolve todos. Só produtos ativos.
    public List<Produto> Pesquisar(string? texto)
    {
        throw new NotImplementedException("TODO: ProdutoService.Pesquisar");
    }

    // TODO: filtrar por categoria (null = todas). Só produtos ativos.
    // Filtros por SITUAÇÃO do estoque (sem estoque, abaixo do mínimo, validade...) usam o EstoqueService.
    public List<Produto> Filtrar(Categoria? categoria)
    {
        throw new NotImplementedException("TODO: ProdutoService.Filtrar");
    }
}
