using SigaCean.Data;
using SigaCean.Models;

namespace SigaCean.Tests;

// Testa SÓ a camada Data: o AppData guarda coleções em memória, nada além disso.
// Não há testes de regra de negócio aqui (essas ficam nos testes dos Services).
public class AppDataTests
{
    [Fact]
    public void AppData_Novo_Tem_Produtos_E_Movimentacoes_Vazios()
    {
        var dados = new AppData();

        Assert.Empty(dados.Produtos);
        Assert.Empty(dados.Movimentacoes);
    }

    [Fact]
    public void AppData_Novo_Tem_Exatamente_Dois_Usuarios_Um_Administrativo_E_Um_Veterinario()
    {
        var dados = new AppData();

        Assert.Equal(2, dados.Usuarios.Count);
        Assert.Single(dados.Usuarios, u => u.Perfil == Perfil.Administrativo);
        Assert.Single(dados.Usuarios, u => u.Perfil == Perfil.Veterinario);
    }

    [Fact]
    public void Duas_Instancias_De_AppData_Nao_Compartilham_Listas()
    {
        var primeiro = new AppData();
        var segundo = new AppData();

        // As listas são objetos diferentes...
        Assert.NotSame(primeiro.Produtos, segundo.Produtos);
        Assert.NotSame(primeiro.Movimentacoes, segundo.Movimentacoes);
        Assert.NotSame(primeiro.Usuarios, segundo.Usuarios);

        // ...e mexer em uma instância não muda a outra (prova de que nada é estático).
        primeiro.Produtos.Add(new Produto { Nome = "Dipirona" });
        Assert.Single(primeiro.Produtos);
        Assert.Empty(segundo.Produtos);
    }

    [Fact]
    public void As_Listas_Sao_So_De_Leitura_Nao_Podem_Ser_Trocadas()
    {
        // "Somente get": a lista é criada uma vez e nenhuma outra pode ocupar o lugar dela.
        Assert.Null(typeof(AppData).GetProperty(nameof(AppData.Produtos))!.SetMethod);
        Assert.Null(typeof(AppData).GetProperty(nameof(AppData.Movimentacoes))!.SetMethod);
        Assert.Null(typeof(AppData).GetProperty(nameof(AppData.Usuarios))!.SetMethod);
    }

    [Fact]
    public void Adicionar_E_Consultar_Produto_Devolve_O_Mesmo_Objeto()
    {
        var dados = new AppData();
        var produto = new Produto { Nome = "Dipirona", Categoria = Categoria.Medicamento };

        dados.Produtos.Add(produto);

        Assert.Single(dados.Produtos);
        Assert.Same(produto, dados.Produtos[0]); // mesma referência, sem cópia
    }

    [Fact]
    public void Adicionar_E_Consultar_Usuario_Devolve_O_Mesmo_Objeto()
    {
        var dados = new AppData();
        var usuario = new Usuario { Nome = "Teste", Login = "teste", Senha = "123", Perfil = Perfil.Administrativo };

        dados.Usuarios.Add(usuario);

        Assert.Same(usuario, dados.Usuarios.Single(u => u.Login == "teste"));
    }

    [Fact]
    public void Adicionar_E_Consultar_Movimentacao_Devolve_O_Mesmo_Objeto()
    {
        var dados = new AppData();
        var produto = new Produto { Nome = "Dipirona" };
        var movimentacao = new Movimentacao { Produto = produto, Tipo = TipoMovimentacao.Entrada, Quantidade = 5 };

        dados.Movimentacoes.Add(movimentacao);

        Assert.Single(dados.Movimentacoes);
        Assert.Same(movimentacao, dados.Movimentacoes[0]); // mesma referência, sem cópia
    }

    [Fact]
    public void Movimentacao_Guarda_A_Referencia_Ao_Produto_E_Ao_Usuario_Responsavel()
    {
        var dados = new AppData();
        var produto = new Produto { Nome = "Dipirona" };
        var responsavel = dados.Usuarios.First(u => u.Perfil == Perfil.Administrativo);
        dados.Produtos.Add(produto);

        var movimentacao = new Movimentacao
        {
            Produto = produto,
            Tipo = TipoMovimentacao.Saida,
            Quantidade = 2,
            UsuarioResponsavel = responsavel
        };
        dados.Movimentacoes.Add(movimentacao);

        Assert.Same(produto, dados.Movimentacoes[0].Produto);
        Assert.Same(responsavel, dados.Movimentacoes[0].UsuarioResponsavel);
    }
}
