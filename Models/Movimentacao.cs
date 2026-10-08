namespace SigaCean.Models;

// Uma movimentação do estoque: entrada, saída ou ajuste de balanço.
// É uma classe só; os campos de "Entrada", "Saída" e "Ajuste" abaixo só são preenchidos para o tipo correspondente.
// Só guarda dados: quem altera o estoque e valida as regras é o MovimentacaoService.
public class Movimentacao
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Produto Produto { get; init; } // sempre um produto já cadastrado, nunca texto livre
    public TipoMovimentacao Tipo { get; init; }

    // Entrada e saída: quantidade positiva.
    // Ajuste de balanço: é a DIFERENÇA aplicada (positiva se sobrou, negativa se faltou).
    public int Quantidade { get; init; }

    public DateTime Data { get; init; } = DateTime.Now;
    public StatusMovimentacao Status { get; set; } = StatusMovimentacao.Ativa;
    public Usuario? UsuarioResponsavel { get; set; } // opcional: o CEAN não registra isso hoje, mas "seria útil"
    public string? Observacao { get; set; }
    public string? MotivoCorrecaoOuCancelamento { get; set; } // obrigatório ao corrigir ou cancelar

    // --- Só para ENTRADA ---
    public TipoOrigemEntrada? TipoOrigem { get; init; }
    public string? Origem { get; init; } // detalhe da origem, ex.: nome do doador ou do departamento
    public string? NumeroAF { get; init; } // só faz sentido para compra
    public string? Documento { get; init; } // nota fiscal, pedido de compra ou documento de transferência/doação
    public DateOnly? DataValidade { get; init; } // validade do que entrou (opcional)

    // --- Só para SAÍDA ---
    public string? Destino { get; init; } // ex.: "Canil"
    public string? RetiradoPor { get; init; } // quem levou o produto

    // --- Só para AJUSTE DE BALANÇO ---
    public int? QuantidadeAnterior { get; init; } // o que o sistema tinha
    public int? QuantidadeApurada { get; init; } // o que foi contado

    // De quais saldos (validades) a quantidade saiu. Preenchido em saídas e ajustes para baixo,
    // para que um cancelamento devolva a quantidade à validade de onde ela saiu.
    public List<SaldoValidade> SaldosConsumidos { get; } = new();
}
