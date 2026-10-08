namespace SigaCean.Models;

// Parte do estoque de um produto que tem a MESMA data de validade.
//
// ATENÇÃO: isto NÃO é controle de lote (o CEAN não controla lote e não existe código de lote no sistema).
// Serve só para saber o que vence primeiro e o que já venceu, porque duas entradas do mesmo produto
// podem chegar com validades diferentes.
//
// Entradas com a mesma validade (ou as duas sem validade) somam no mesmo saldo.
public class SaldoValidade
{
    public DateOnly? DataValidade { get; set; } // nulo = estoque sem validade conhecida
    public int Quantidade { get; set; }
}
