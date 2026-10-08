using SigaCean.Data;
using SigaCean.Models;

namespace SigaCean.Services;

// Tudo sobre a QUANTIDADE e a SITUAÇÃO do estoque. É o ÚNICO lugar onde estas regras são definidas:
// dashboard, tela de estoque, relatórios e filtros devem CHAMAR estes métodos, nunca repetir a conta.
public class EstoqueService
{
    // Quantos dias antes do vencimento o produto passa a ser "próximo do vencimento".
    // 30 dias é uma decisão da equipe (o CEAN não definiu); fica aqui para ser fácil de mudar.
    public const int DiasAlertaValidade = 30;

    private readonly AppData _dados;

    public EstoqueService(AppData dados)
    {
        _dados = dados;
    }

    // ---------- Consulta ----------

    public List<Produto> ConsultarEstoque() => _dados.Produtos.Where(p => p.Ativo).ToList();

    // ---------- Regras de situação (já definidas) ----------

    // Zerado é zerado, tenha ou não quantidade mínima cadastrada.
    public bool EstaSemEstoque(Produto produto) => produto.Quantidade == 0;

    // REGRA: quantidade <= mínima é estoque baixo (estar exatamente no mínimo já conta).
    // Produto sem quantidade mínima cadastrada nunca está "abaixo do mínimo".
    public bool EstaAbaixoDoMinimo(Produto produto) =>
        produto.QuantidadeMinima is not null && produto.Quantidade <= produto.QuantidadeMinima.Value;

    public SituacaoEstoque VerificarSituacao(Produto produto) =>
        EstaSemEstoque(produto) ? SituacaoEstoque.SemEstoque
        : EstaAbaixoDoMinimo(produto) ? SituacaoEstoque.AbaixoDoMinimo
        : SituacaoEstoque.Normal;

    // Estoque negativo nunca deve acontecer; este método existe para conferir (testes, depuração).
    public bool EstaNegativo(Produto produto) => produto.Quantidade < 0 || produto.Saldos.Any(s => s.Quantidade < 0);

    // Validade: só conta o que AINDA EXISTE em estoque (saldo com quantidade > 0).
    // Um saldo já zerado, mesmo com data passada, não deixa o produto "vencido".
    public bool TemValidadeVencida(Produto produto, DateOnly hoje) =>
        produto.Saldos.Any(s => s.Quantidade > 0 && s.DataValidade is not null && s.DataValidade < hoje);

    // "Próximo do vencimento": vence dentro de DiasAlertaValidade dias (e ainda não venceu).
    // Quem já tem algo vencido aparece como VENCIDO, não também como "próximo".
    public bool TemValidadeProxima(Produto produto, DateOnly hoje) =>
        !TemValidadeVencida(produto, hoje) &&
        produto.Saldos.Any(s => s.Quantidade > 0 && s.DataValidade is not null &&
                                s.DataValidade.Value.DayNumber - hoje.DayNumber <= DiasAlertaValidade);

    // ---------- Alterar a quantidade (a fazer) ----------
    // Só o MovimentacaoService deve chamar estes métodos. Regra geral: se algo for inválido, lançar
    // InvalidOperationException ANTES de alterar qualquer coisa (nada pode mudar pela metade).

    // TODO: somar a quantidade ao saldo da mesma validade (cria o saldo se não existir). quantidade > 0.
    public void AumentarEstoque(Produto produto, int quantidade, DateOnly? dataValidade)
    {
        throw new NotImplementedException("TODO: EstoqueService.AumentarEstoque");
    }

    // TODO: retirar a quantidade pelo critério FEFO (primeiro a vencer, primeiro a sair; sem validade sai por último).
    // Recusar se quantidade <= 0 ou se for maior que produto.Quantidade (o estoque NUNCA fica negativo).
    // Devolver de quais saldos saiu, para o MovimentacaoService guardar na movimentação (cancelamento usa isso).
    // Remover da lista os saldos que ficarem zerados.
    public List<SaldoValidade> DiminuirEstoque(Produto produto, int quantidade)
    {
        throw new NotImplementedException("TODO: EstoqueService.DiminuirEstoque");
    }

    // TODO: ajustar o estoque para a quantidade apurada na contagem (>= 0). Usa Aumentar/Diminuir internamente.
    public void AtualizarQuantidade(Produto produto, int quantidadeApurada)
    {
        throw new NotImplementedException("TODO: EstoqueService.AtualizarQuantidade");
    }
}
