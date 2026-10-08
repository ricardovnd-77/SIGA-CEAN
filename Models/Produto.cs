namespace SigaCean.Models;

// Representa um produto controlado pelo CEAN. Só guarda dados: as regras (situação do estoque, validade, etc.)
// ficam nos Services, para existir um único lugar onde cada regra é definida.
public class Produto
{
    public Guid Id { get; init; } = Guid.NewGuid(); // identificador interno; o CEAN não usa código de produto nem de barras
    public string Nome { get; set; } = string.Empty; // nome/nomenclatura
    public Categoria Categoria { get; set; }
    public string? Apresentacao { get; set; } // ex.: "ampola", "frasco 100 ml" (relevante sobretudo para medicamentos)
    public string? Especificacao { get; set; } // ex.: "50 mg", "injetável"
    public int? QuantidadeMinima { get; set; } // opcional: a planilha do CEAN não informa
    public bool Ativo { get; set; } = true;

    // Estoque do produto, separado por data de validade (veja SaldoValidade).
    public List<SaldoValidade> Saldos { get; } = new();

    // Quantidade total em estoque = soma dos saldos.
    public int Quantidade => Saldos.Sum(s => s.Quantidade);
}
