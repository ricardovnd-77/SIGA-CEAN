using SigaCean.Models;

namespace SigaCean.Tests;

public class ProdutoServiceTests
{
    private readonly Cenario _c = new();

    // ---------- Cadastrar ----------

    [Fact]
    public void Cadastro_Valido_Guarda_O_Produto_Com_Textos_Limpos()
    {
        var produto = _c.Produtos.Cadastrar("  Dipirona  ", Categoria.Medicamento, " ampola ", "  500 mg ", 10);

        Assert.Equal("Dipirona", produto.Nome);
        Assert.Equal("ampola", produto.Apresentacao);
        Assert.Equal("500 mg", produto.Especificacao);
        Assert.Equal(10, produto.QuantidadeMinima);
        Assert.True(produto.Ativo);
        Assert.Contains(produto, _c.Dados.Produtos);
    }

    [Fact]
    public void Texto_Opcional_Vazio_Vira_Null()
    {
        var produto = _c.Produtos.Cadastrar("Ração", Categoria.Alimentar, "   ", "", null);

        Assert.Null(produto.Apresentacao);
        Assert.Null(produto.Especificacao);
        Assert.Null(produto.QuantidadeMinima);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cadastro_Sem_Nome_E_Recusado_E_Nada_E_Guardado(string nome)
    {
        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Cadastrar(nome, Categoria.Insumo, null, null, null));

        Assert.Empty(_c.Dados.Produtos);
    }

    [Fact]
    public void Cadastro_Com_Quantidade_Minima_Negativa_E_Recusado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Cadastrar("Seringa", Categoria.Insumo, null, null, -1));

        Assert.Empty(_c.Dados.Produtos);
    }

    // ---------- Alterar ----------

    [Fact]
    public void Alterar_Muda_Os_Dados_Cadastrais()
    {
        var produto = _c.NovoProduto("Dipirona", Categoria.Medicamento, minimo: 5);

        _c.Produtos.Alterar(produto.Id, "  Dipirona Sódica ", Categoria.Insumo, " frasco ", "", 8);

        Assert.Equal("Dipirona Sódica", produto.Nome);
        Assert.Equal(Categoria.Insumo, produto.Categoria);
        Assert.Equal("frasco", produto.Apresentacao);
        Assert.Null(produto.Especificacao);
        Assert.Equal(8, produto.QuantidadeMinima);
    }

    [Fact]
    public void Alterar_Nao_Mexe_Na_Quantidade_Nem_No_Campo_Ativo()
    {
        var produto = _c.NovoProdutoComEstoque(12, new DateOnly(2030, 1, 1));
        produto.Ativo = false;
        var antes = Cenario.Foto(produto);

        _c.Produtos.Alterar(produto.Id, "Outro nome", Categoria.Higiene, null, null, null);

        Assert.Equal(12, produto.Quantidade);
        Assert.Equal(antes, Cenario.Foto(produto));
        Assert.False(produto.Ativo);
    }

    [Fact]
    public void Alterar_Produto_Inexistente_E_Recusado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Alterar(Guid.NewGuid(), "Qualquer", Categoria.Insumo, null, null, null));
    }

    [Fact]
    public void Alterar_Com_Dados_Invalidos_E_Recusado_E_Nada_Muda()
    {
        var produto = _c.NovoProduto("Dipirona", Categoria.Medicamento, minimo: 5);

        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Alterar(produto.Id, "  ", Categoria.Insumo, "x", "y", 9));
        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Alterar(produto.Id, "Novo", Categoria.Insumo, "x", "y", -2));

        Assert.Equal("Dipirona", produto.Nome);
        Assert.Equal(Categoria.Medicamento, produto.Categoria);
        Assert.Null(produto.Apresentacao);
        Assert.Equal(5, produto.QuantidadeMinima);
    }

    // ---------- Pesquisar ----------

    [Fact]
    public void Pesquisa_Ignora_Acentos_E_Maiusculas()
    {
        var injetavel = _c.NovoProduto("DIPIRONA INJETÁVEL");
        _c.NovoProduto("Ração Premium");

        var resultado = _c.Produtos.Pesquisar("injetavel");

        Assert.Same(injetavel, Assert.Single(resultado));
    }

    [Fact]
    public void Pesquisa_Com_Acento_Digitado_Acha_Nome_Sem_Acento()
    {
        var racao = _c.NovoProduto("Racao filhote");

        var resultado = _c.Produtos.Pesquisar("RAÇÃO");

        Assert.Same(racao, Assert.Single(resultado));
    }

    [Fact]
    public void Pesquisa_Acha_Por_Parte_Do_Nome()
    {
        _c.NovoProduto("Soro fisiológico");
        _c.NovoProduto("Seringa 5 ml");

        var resultado = _c.Produtos.Pesquisar("fisio");

        Assert.Equal("Soro fisiológico", Assert.Single(resultado).Nome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Pesquisa_Com_Texto_Vazio_Devolve_Todos_Os_Ativos_Em_Ordem_De_Nome(string? texto)
    {
        _c.NovoProduto("Vacina");
        _c.NovoProduto("Alcool");
        _c.NovoProduto("Soro");

        var resultado = _c.Produtos.Pesquisar(texto);

        Assert.Equal(new[] { "Alcool", "Soro", "Vacina" }, resultado.Select(p => p.Nome));
    }

    [Fact]
    public void Pesquisa_Nao_Devolve_Produto_Inativo()
    {
        var inativo = _c.NovoProduto("Dipirona antiga");
        inativo.Ativo = false;
        _c.NovoProduto("Dipirona nova");

        var resultado = _c.Produtos.Pesquisar("dipirona");

        Assert.Equal("Dipirona nova", Assert.Single(resultado).Nome);
    }

    [Fact]
    public void Pesquisa_Sem_Resultado_Devolve_Lista_Vazia()
    {
        _c.NovoProduto("Dipirona");

        Assert.Empty(_c.Produtos.Pesquisar("xyz"));
    }

    // ---------- Filtrar ----------

    [Fact]
    public void Filtrar_Por_Categoria_Devolve_So_Ativos_Dela_Em_Ordem_De_Nome()
    {
        _c.NovoProduto("Zeta", Categoria.Insumo);
        _c.NovoProduto("Alfa", Categoria.Insumo);
        _c.NovoProduto("Dipirona", Categoria.Medicamento);
        var inativo = _c.NovoProduto("Inativo", Categoria.Insumo);
        inativo.Ativo = false;

        var resultado = _c.Produtos.Filtrar(Categoria.Insumo);

        Assert.Equal(new[] { "Alfa", "Zeta" }, resultado.Select(p => p.Nome));
    }

    [Fact]
    public void Filtrar_Com_Null_Devolve_Todas_As_Categorias()
    {
        _c.NovoProduto("B", Categoria.Insumo);
        _c.NovoProduto("A", Categoria.Medicamento);
        _c.NovoProduto("C", Categoria.Microchip);

        var resultado = _c.Produtos.Filtrar(null);

        Assert.Equal(new[] { "A", "B", "C" }, resultado.Select(p => p.Nome));
    }
}
