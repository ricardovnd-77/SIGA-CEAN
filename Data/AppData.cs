using SigaCean.Models;

namespace SigaCean.Data;

// ÚNICO lugar onde os dados existem: coleções em memória.
// Não há banco de dados nem arquivo: ao fechar o programa, tudo se perde (isso é intencional, faz parte das restrições
// do projeto). NÃO crie nenhuma forma de gravar estes dados.
//
// Uma única instância é criada no MauiProgram e entregue aos Services, que são os únicos que devem alterar estas listas.
public class AppData
{
    public List<Produto> Produtos { get; } = new();
    public List<Movimentacao> Movimentacoes { get; } = new();
    public List<Usuario> Usuarios { get; } = new();

    public AppData()
    {
        // Usuários FICTÍCIOS, só para poder testar os dois perfis.
        Usuarios.Add(new Usuario { Nome = "Funcionário (exemplo)", Login = "admin", Senha = "admin123", Perfil = Perfil.Administrativo });
        Usuarios.Add(new Usuario { Nome = "Veterinário (exemplo)", Login = "vet", Senha = "vet123", Perfil = Perfil.Veterinario });
    }
}
