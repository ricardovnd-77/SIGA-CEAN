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

    // TODO: registrar uma ENTRADA.
    // Validar: produto ativo; quantidade > 0; tipo de origem informado.
    // Efeito: aumenta o estoque (com a validade, se houver) e grava a movimentação (Tipo = Entrada, Data = agora).
    public Movimentacao RegistrarEntrada(Produto produto, int quantidade, TipoOrigemEntrada tipoOrigem,
        string? origem, string? numeroAF, string? documento, DateOnly? dataValidade,
        Usuario? responsavel, string? observacao = null)
    {
        throw new NotImplementedException("TODO: MovimentacaoService.RegistrarEntrada");
    }

    // TODO: registrar uma SAÍDA.
    // Validar: produto ativo; quantidade > 0; destino obrigatório; quantidade <= estoque disponível.
    // Efeito: diminui o estoque (FEFO), guarda em SaldosConsumidos de onde saiu e grava a movimentação.
    public Movimentacao RegistrarSaida(Produto produto, int quantidade, string destino, string? retiradoPor,
        Usuario? responsavel, string? observacao = null)
    {
        throw new NotImplementedException("TODO: MovimentacaoService.RegistrarSaida");
    }

    // TODO: registrar um AJUSTE DE BALANÇO (contagem física). quantidadeApurada >= 0.
    // A movimentação guarda QuantidadeAnterior, QuantidadeApurada e a diferença em Quantidade.
    public Movimentacao RegistrarAjusteBalanco(Produto produto, int quantidadeApurada, Usuario? responsavel,
        string? observacao = null)
    {
        throw new NotImplementedException("TODO: MovimentacaoService.RegistrarAjusteBalanco");
    }

    // Já implementado: histórico completo, do mais recente para o mais antigo.
    // Movimentações corrigidas ou canceladas continuam aparecendo (com o status).
    public List<Movimentacao> ConsultarHistorico() =>
        _dados.Movimentacoes.OrderByDescending(m => m.Data).ToList();

    // TODO: filtrar por período. O dia final entra INTEIRO (use fim.Date.AddDays(1).AddTicks(-1) como limite).
    // Pode receber filtros opcionais de tipo e de produto.
    public List<Movimentacao> FiltrarPorPeriodo(DateTime inicio, DateTime fim)
    {
        throw new NotImplementedException("TODO: MovimentacaoService.FiltrarPorPeriodo");
    }

    // TODO: corrigir uma movimentação. Só movimentação ATIVA; motivo obrigatório.
    // Desfaz o efeito no estoque (entrada retira, saída devolve a SaldosConsumidos) e marca como Corrigida.
    // Se não der para desfazer (ex.: o que entrou já foi usado), recusar e não mudar nada.
    // Quem corrige depois registra a movimentação certa como uma NOVA.
    public void Corrigir(Guid movimentacaoId, string motivo)
    {
        throw new NotImplementedException("TODO: MovimentacaoService.Corrigir");
    }

    // TODO: cancelar uma movimentação. Mesmas regras do Corrigir, mas marca como Cancelada.
    // Só o perfil Administrativo corrige/cancela (essa checagem fica na tela/menu, mas o motivo é validado aqui).
    public void Cancelar(Guid movimentacaoId, string motivo)
    {
        throw new NotImplementedException("TODO: MovimentacaoService.Cancelar");
    }
}
