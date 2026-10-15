using SigaCean.Data;
using SigaCean.Models;

namespace SigaCean.Services;

// Registra e consulta as movimentações. Para mexer na quantidade, SEMPRE chama o EstoqueService.
//
// REGRA DE OURO: uma operação inválida não altera NADA, nem o estoque nem o histórico.
// Valide tudo no começo; só depois altere o estoque e só então grave a movimentação.
// Se algo for inválido: lançar InvalidOperationException com uma mensagem clara em português.
public class MovimentacaoService
{
    private readonly AppData _dados;
    private readonly EstoqueService _estoque;

    public MovimentacaoService(AppData dados, EstoqueService estoque)
    {
        _dados = dados;
        _estoque = estoque;
    }

    // ---------- Registrar ----------

    // Entrada: aumenta o estoque (com a validade, se houver) e grava a movimentação.
    public Movimentacao RegistrarEntrada(Produto produto, int quantidade, TipoOrigemEntrada tipoOrigem,
        string? origem, string? numeroAF, string? documento, DateOnly? dataValidade,
        Usuario? responsavel, string? observacao = null)
    {
        ValidarProdutoAtivo(produto);
        ValidarQuantidadePositiva(quantidade);

        _estoque.AumentarEstoque(produto, quantidade, dataValidade);

        var movimentacao = new Movimentacao
        {
            Produto = produto,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = quantidade,
            UsuarioResponsavel = responsavel,
            Observacao = LimparTexto(observacao),
            TipoOrigem = tipoOrigem,
            Origem = LimparTexto(origem),
            NumeroAF = LimparTexto(numeroAF),
            Documento = LimparTexto(documento),
            DataValidade = dataValidade
        };

        _dados.Movimentacoes.Add(movimentacao);
        return movimentacao;
    }

    // Saída: diminui o estoque pelo FEFO, guarda em SaldosConsumidos de onde saiu e grava a movimentação.
    public Movimentacao RegistrarSaida(Produto produto, int quantidade, string destino, string? retiradoPor,
        Usuario? responsavel, string? observacao = null)
    {
        ValidarProdutoAtivo(produto);
        ValidarQuantidadePositiva(quantidade);
        if (string.IsNullOrWhiteSpace(destino))
            throw new InvalidOperationException("Informe o destino da saída.");
        if (quantidade > produto.Quantidade)
            throw new InvalidOperationException($"Estoque insuficiente. Quantidade disponível: {produto.Quantidade}.");

        var retirados = _estoque.DiminuirEstoque(produto, quantidade);

        var movimentacao = new Movimentacao
        {
            Produto = produto,
            Tipo = TipoMovimentacao.Saida,
            Quantidade = quantidade,
            UsuarioResponsavel = responsavel,
            Observacao = LimparTexto(observacao),
            Destino = destino.Trim(),
            RetiradoPor = LimparTexto(retiradoPor)
        };
        movimentacao.SaldosConsumidos.AddRange(retirados);

        _dados.Movimentacoes.Add(movimentacao);
        return movimentacao;
    }

    // Ajuste de balanço (contagem física). Quantidade = DIFERENÇA aplicada (positiva se sobrou, negativa se faltou).
    // Mesmo sem diferença a movimentação é gravada: a conferência aconteceu.
    public Movimentacao RegistrarAjusteBalanco(Produto produto, int quantidadeApurada, Usuario? responsavel,
        string? observacao = null)
    {
        ValidarProdutoAtivo(produto);
        if (quantidadeApurada < 0)
            throw new InvalidOperationException("A quantidade apurada não pode ser negativa.");

        var quantidadeAnterior = produto.Quantidade;
        var retirados = _estoque.AtualizarQuantidade(produto, quantidadeApurada);

        var movimentacao = new Movimentacao
        {
            Produto = produto,
            Tipo = TipoMovimentacao.AjusteBalanco,
            Quantidade = quantidadeApurada - quantidadeAnterior,
            UsuarioResponsavel = responsavel,
            Observacao = LimparTexto(observacao),
            QuantidadeAnterior = quantidadeAnterior,
            QuantidadeApurada = quantidadeApurada
        };
        movimentacao.SaldosConsumidos.AddRange(retirados); // vazio se a diferença não foi negativa

        _dados.Movimentacoes.Add(movimentacao);
        return movimentacao;
    }

    // ---------- Consultar ----------

    // Já implementado: histórico completo, do mais recente para o mais antigo.
    // Movimentações corrigidas ou canceladas continuam aparecendo (com o status).
    public List<Movimentacao> ConsultarHistorico() =>
        _dados.Movimentacoes.OrderByDescending(m => m.Data).ToList();

    // Movimentações do período, de TODOS os status, da mais recente para a mais antiga.
    // O dia final entra INTEIRO.
    public List<Movimentacao> FiltrarPorPeriodo(DateTime inicio, DateTime fim)
    {
        if (inicio > fim)
            throw new InvalidOperationException("A data inicial não pode ser depois da data final.");

        var limite = fim.Date.AddDays(1).AddTicks(-1);

        return _dados.Movimentacoes
            .Where(m => m.Data >= inicio && m.Data <= limite)
            .OrderByDescending(m => m.Data)
            .ToList();
    }

    // ---------- Corrigir e cancelar ----------

    // Corrige: desfaz o efeito no estoque e marca como Corrigida. NÃO cria a movimentação nova:
    // quem corrige registra a certa depois, como uma nova.
    public void Corrigir(Guid movimentacaoId, string motivo)
    {
        EncerrarMovimentacao(movimentacaoId, motivo, StatusMovimentacao.Corrigida);
    }

    // Cancela: mesmas regras do Corrigir, mas marca como Cancelada.
    // (Só o perfil Administrativo cancela: essa checagem fica na tela/menu.)
    public void Cancelar(Guid movimentacaoId, string motivo)
    {
        EncerrarMovimentacao(movimentacaoId, motivo, StatusMovimentacao.Cancelada);
    }

    // ---------- Apoio (privados) ----------

    // Parte comum de Corrigir e Cancelar. A movimentação NÃO é apagada: só muda o status e ganha o motivo.
    private void EncerrarMovimentacao(Guid movimentacaoId, string motivo, StatusMovimentacao novoStatus)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Informe o motivo.");

        var movimentacao = _dados.Movimentacoes.FirstOrDefault(m => m.Id == movimentacaoId)
            ?? throw new InvalidOperationException("Movimentação não encontrada.");

        if (movimentacao.Status != StatusMovimentacao.Ativa)
            throw new InvalidOperationException("Esta movimentação já foi corrigida ou cancelada e não pode ser alterada de novo.");

        // Se não der para desfazer, o EstoqueService recusa ANTES de alterar e o status continua Ativa.
        DesfazerEfeitoNoEstoque(movimentacao);

        movimentacao.Status = novoStatus;
        movimentacao.MotivoCorrecaoOuCancelamento = motivo.Trim();
    }

    // Desfaz, no estoque, o que a movimentação fez.
    private void DesfazerEfeitoNoEstoque(Movimentacao movimentacao)
    {
        switch (movimentacao.Tipo)
        {
            case TipoMovimentacao.Entrada:
                _estoque.DesfazerEntrada(movimentacao.Produto, movimentacao.Quantidade, movimentacao.DataValidade);
                break;

            case TipoMovimentacao.Saida:
                DevolverSaldosConsumidos(movimentacao);
                break;

            case TipoMovimentacao.AjusteBalanco:
                if (movimentacao.Quantidade > 0)
                    // Sobra que entrou sem validade: retira de volta.
                    _estoque.DesfazerEntrada(movimentacao.Produto, movimentacao.Quantidade, null);
                else if (movimentacao.Quantidade < 0)
                    // Falta que saiu pelo FEFO: devolve à validade de origem.
                    DevolverSaldosConsumidos(movimentacao);
                // Diferença zero: não há o que desfazer no estoque.
                break;
        }
    }

    // Devolve cada item de SaldosConsumidos à validade de onde saiu.
    private void DevolverSaldosConsumidos(Movimentacao movimentacao)
    {
        foreach (var saldo in movimentacao.SaldosConsumidos)
            _estoque.AumentarEstoque(movimentacao.Produto, saldo.Quantidade, saldo.DataValidade);
    }

    private static void ValidarProdutoAtivo(Produto produto)
    {
        if (!produto.Ativo)
            throw new InvalidOperationException("Este produto está inativo e não pode receber movimentações.");
    }

    private static void ValidarQuantidadePositiva(int quantidade)
    {
        if (quantidade <= 0)
            throw new InvalidOperationException("A quantidade deve ser maior que zero.");
    }

    // Texto opcional: limpa os espaços; vazio vira null.
    private static string? LimparTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}