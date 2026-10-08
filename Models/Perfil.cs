namespace SigaCean.Models;

public enum Perfil
{
    Administrativo, // acesso às operações administrativas
    Veterinario // só consulta o estoque; NÃO recebe permissões administrativas
}
