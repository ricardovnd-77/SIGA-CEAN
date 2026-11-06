using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Tests;

// Pedido do orientador do CEAN: o Veterinário também registra saída. Entrada, produtos, ajuste, correção, cancelamento,
// relatórios e gráficos continuam só do Administrativo.
public class PermissoesPorPerfilTests
{
    private readonly Cenario _c = new();

    private UsuarioService EntrarComo(string login, string senha)
    {
        var usuarios = new UsuarioService(_c.Dados);
        Assert.NotNull(usuarios.Login(login, senha));
        return usuarios;
    }

    [Fact]
    public void Veterinario_Registra_Saida_E_Nao_E_Administrativo()
    {
        var usuarios = EntrarComo("vet", "vet123");

        Assert.True(usuarios.PodeRegistrarSaida);
        Assert.False(usuarios.EhAdministrativo);
    }

    [Fact]
    public void Administrativo_Registra_Saida_E_Administra()
    {
        var usuarios = EntrarComo("admin", "admin123");

        Assert.True(usuarios.PodeRegistrarSaida);
        Assert.True(usuarios.EhAdministrativo);
    }

    [Fact]
    public void Sem_Usuario_Logado_Nao_Ha_Acoes()
    {
        var usuarios = new UsuarioService(_c.Dados);

        Assert.False(usuarios.PodeRegistrarSaida);
        Assert.False(usuarios.EhAdministrativo);
    }

    [Fact]
    public void Saida_Do_Veterinario_Subtrai_Do_Estoque_E_Registra_Quem_Fez()
    {
        var usuarios = EntrarComo("vet", "vet123");
        var produto = _c.NovoProdutoComEstoque(10);

        var saida = _c.Movimentacoes.RegistrarSaida(produto, 4, "Canil", "Maria", usuarios.UsuarioLogado);

        Assert.Equal(6, produto.Quantidade);
        Assert.Same(usuarios.UsuarioLogado, saida.UsuarioResponsavel);
        Assert.Equal("Maria", saida.RetiradoPor);
    }

    [Fact]
    public void Saida_Do_Veterinario_Acima_Do_Saldo_Continua_Recusada()
    {
        var usuarios = EntrarComo("vet", "vet123");
        var produto = _c.NovoProdutoComEstoque(3);

        Assert.Throws<InvalidOperationException>(() =>
            _c.Movimentacoes.RegistrarSaida(produto, 4, "Canil", "Maria", usuarios.UsuarioLogado));
        Assert.Equal(3, produto.Quantidade);
    }
}
