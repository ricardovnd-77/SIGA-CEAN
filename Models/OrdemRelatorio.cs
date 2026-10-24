namespace SigaCean.Models;

// Como o Relatório de Consumo é ordenado.
public enum OrdemRelatorio
{
    MaiorConsumo, // mais saídas primeiro e, em empate, por nome (é o padrão)
    PorNome
}