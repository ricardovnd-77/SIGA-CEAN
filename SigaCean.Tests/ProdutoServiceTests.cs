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

    // ---------- Produto duplicado ----------

    private const string MensagemDuplicado = "Já existe um produto com este nome e apresentação.";

    [Fact]
    public void Cadastro_Identico_Ignorando_Maiusculas_E_Espacos_E_Recusado_E_Nada_E_Guardado()
    {
        _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, "ampola", "500 mg", null);

        var erro = Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Cadastrar("  DIPIRONA ", Categoria.Medicamento, " AMPOLA", "500 MG  ", null));

        Assert.Equal(MensagemDuplicado, erro.Message);
        Assert.Single(_c.Dados.Produtos);
    }

    [Fact]
    public void Cadastro_Identico_Ignorando_Acentos_E_Recusado()
    {
        _c.Produtos.Cadastrar("Ração Premium", Categoria.Alimentar, "Saco", "Filhotes", null);

        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Cadastrar("RACAO PREMIUM", Categoria.Alimentar, "saco", "filhotes", null));

        Assert.Single(_c.Dados.Produtos);
    }

    [Fact]
    public void Apresentacao_Ou_Especificacao_Em_Branco_Equivalem_A_Nulas_Na_Comparacao()
    {
        _c.Produtos.Cadastrar("Seringa", Categoria.Insumo, null, null, null);

        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Cadastrar("seringa", Categoria.Insumo, "   ", "", null));
    }

    [Fact]
    public void Mesmo_Nome_Com_Apresentacao_Ou_Especificacao_Diferente_E_Permitido()
    {
        _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, "ampola", "500 mg", null);

        _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, "comprimido", "500 mg", null);
        _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, "ampola", "1 g", null);
        _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, null, null, null);

        Assert.Equal(4, _c.Dados.Produtos.Count);
    }

    [Fact]
    public void Produto_Inativo_Tambem_Conta_Como_Duplicado()
    {
        var existente = _c.Produtos.Cadastrar("Vacina", Categoria.Medicamento, null, null, null);
        existente.Ativo = false;

        Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Cadastrar("Vacina", Categoria.Medicamento, null, null, null));
    }

    [Fact]
    public void ExisteIdentico_Compara_Nome_Apresentacao_E_Especificacao()
    {
        var produto = _c.Produtos.Cadastrar("Soro fisiológico", Categoria.Insumo, "Bolsa", "500 ml", null);

        Assert.True(_c.Produtos.ExisteIdentico("SORO FISIOLOGICO", "bolsa", "500 ML"));
        Assert.False(_c.Produtos.ExisteIdentico("Soro fisiológico", "Frasco", "500 ml"));
        Assert.False(_c.Produtos.ExisteIdentico("Soro fisiológico", "Bolsa", "250 ml"));
        Assert.False(_c.Produtos.ExisteIdentico("Soro", "Bolsa", "500 ml"));
        Assert.False(_c.Produtos.ExisteIdentico("SORO FISIOLOGICO", "bolsa", "500 ML", produto.Id)); // ignora o próprio
    }

    [Fact]
    public void Alterar_Para_Dados_De_Outro_Produto_E_Recusado_E_Nada_Muda()
    {
        _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, "ampola", null, null);
        var outro = _c.Produtos.Cadastrar("Tramadol", Categoria.Medicamento, "ampola", null, 3);

        var erro = Assert.Throws<InvalidOperationException>(() =>
            _c.Produtos.Alterar(outro.Id, "DIPIRONA", Categoria.Insumo, "Ampola", null, 9));

        Assert.Equal(MensagemDuplicado, erro.Message);
        Assert.Equal("Tramadol", outro.Nome);
        Assert.Equal(Categoria.Medicamento, outro.Categoria);
        Assert.Equal(3, outro.QuantidadeMinima);
    }

    [Fact]
    public void Alterar_Mantendo_Os_Proprios_Dados_Nao_E_Duplicado()
    {
        var produto = _c.Produtos.Cadastrar("Dipirona", Categoria.Medicamento, "ampola", "500 mg", null);

        _c.Produtos.Alterar(produto.Id, "DIPIRONA", Categoria.Medicamento, "Ampola", "500 mg", 12);

        Assert.Equal("DIPIRONA", produto.Nome);
        Assert.Equal(12, produto.QuantidadeMinima);
    }
}
