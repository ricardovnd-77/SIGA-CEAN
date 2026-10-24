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

    // ---------- Alterar a quantidade ----------
    // Só o MovimentacaoService deve chamar estes métodos. Regra geral: se algo for inválido, lança
    // InvalidOperationException ANTES de alterar qualquer coisa (nada muda pela metade).

    // Soma a quantidade ao saldo da MESMA validade; se não existir, cria o saldo.
    // Duas entradas sem validade (null) também caem no mesmo saldo.
    public void AumentarEstoque(Produto produto, int quantidade, DateOnly? dataValidade)
    {
        ValidarQuantidadePositiva(quantidade);

        var saldo = BuscarSaldo(produto, dataValidade);
        if (saldo is null)
            produto.Saldos.Add(new SaldoValidade { DataValidade = dataValidade, Quantidade = quantidade });
        else
            saldo.Quantidade += quantidade;
    }

    // Retira a quantidade pelo critério FEFO (primeiro a vencer, primeiro a sair; sem validade sai por último).
    // Devolve de quais validades saiu, como objetos NOVOS (o MovimentacaoService guarda isso para o cancelamento).

    // PREVISÃO do FEFO: de quais validades a quantidade sairia (a que vence primeiro, depois as seguintes; sem validade
    // por último). NÃO altera nada. É o ÚNICO lugar onde o FEFO é decidido: o DiminuirEstoque usa esta previsão.
    // Recusa quantidade <= 0 e quantidade maior que o estoque (mesmas regras da saída). Devolve objetos NOVOS.
    public List<SaldoValidade> PrevisaoFefo(Produto produto, int quantidade)
    {
        ValidarQuantidadePositiva(quantidade);
        if (quantidade > produto.Quantidade)
            throw new InvalidOperationException($"Estoque insuficiente. Quantidade disponível: {produto.Quantidade}.");

        var previstos = new List<SaldoValidade>();
        var faltaRetirar = quantidade;

        foreach (var saldo in SaldosEmOrdemFefo(produto))
        {
            if (faltaRetirar == 0)
                break;

            var retirar = Math.Min(saldo.Quantidade, faltaRetirar);
            faltaRetirar -= retirar;
            previstos.Add(new SaldoValidade { DataValidade = saldo.DataValidade, Quantidade = retirar });
        }

        return previstos;
    }

    // Retira a quantidade pelo critério FEFO (primeiro a vencer, primeiro a sair; sem validade sai por último).
    // Devolve de quais validades saiu, como objetos NOVOS (o MovimentacaoService guarda isso para o cancelamento).
    public List<SaldoValidade> DiminuirEstoque(Produto produto, int quantidade)
    {
        // A previsão valida tudo ANTES de mexer nos saldos: o estoque nunca fica negativo nem muda pela metade.
        var retirados = PrevisaoFefo(produto, quantidade);

        foreach (var retirado in retirados)
            BuscarSaldo(produto, retirado.DataValidade)!.Quantidade -= retirado.Quantidade;

        // Saldo zerado não fica na lista.
        produto.Saldos.RemoveAll(s => s.Quantidade == 0);
        return retirados;
    }


    // Ajusta o estoque para a quantidade contada (balanço). Se sobrou, entra SEM validade; se faltou, sai pelo FEFO.
    // Devolve o que foi retirado (lista vazia se a diferença for zero ou positiva).
    public List<SaldoValidade> AtualizarQuantidade(Produto produto, int quantidadeApurada)
    {
        if (quantidadeApurada < 0)
            throw new InvalidOperationException("A quantidade apurada não pode ser negativa.");

        var diferenca = quantidadeApurada - produto.Quantidade;

        if (diferenca > 0)
        {
            AumentarEstoque(produto, diferenca, null);
            return new List<SaldoValidade>();
        }

        if (diferenca < 0)
            return DiminuirEstoque(produto, -diferenca);

        return new List<SaldoValidade>();
    }

    // Desfaz uma ENTRADA (usado ao cancelar/corrigir): retira a quantidade do saldo daquela validade.
    // Se o saldo não tem quantidade suficiente (parte do que entrou já foi usada), recusa e não altera nada.
    public void DesfazerEntrada(Produto produto, int quantidade, DateOnly? dataValidade)
    {
        ValidarQuantidadePositiva(quantidade);

        var saldo = BuscarSaldo(produto, dataValidade);
        if (saldo is null || saldo.Quantidade < quantidade)
            throw new InvalidOperationException(
                "Não é possível desfazer esta entrada, pois parte do que entrou já foi utilizada.");

        saldo.Quantidade -= quantidade;
        if (saldo.Quantidade == 0)
            produto.Saldos.Remove(saldo);
    }

    // ---------- Apoio (privados) ----------

    private static void ValidarQuantidadePositiva(int quantidade)
    {
        if (quantidade <= 0)
            throw new InvalidOperationException("A quantidade deve ser maior que zero.");
    }

    // Saldo com a mesma validade (null = sem validade).
    private static SaldoValidade? BuscarSaldo(Produto produto, DateOnly? dataValidade) =>
        produto.Saldos.FirstOrDefault(s => s.DataValidade == dataValidade);

    // FEFO: validade mais próxima primeiro; o saldo sem validade vai por último.
    private static List<SaldoValidade> SaldosEmOrdemFefo(Produto produto) =>
        produto.Saldos
            .Where(s => s.Quantidade > 0)
            .OrderBy(s => s.DataValidade is null)
            .ThenBy(s => s.DataValidade)
            .ToList();
}