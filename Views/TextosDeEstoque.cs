using SigaCean.Models;

namespace SigaCean.Views;

// Qual situação o filtro do Estoque (e os quadros do Painel) mostra. Fica aqui, e não em Models, porque é só uma
// escolha de TELA: quem diz se um produto está sem estoque, vencido etc. continua sendo o EstoqueService.
public enum FiltroDeSituacao
{
    Todos,
    SemEstoque,
    AbaixoDoMinimo,
    Vencidos,
    ValidadeProxima
}

// Textos de apresentação do estoque (validade, saldo, categoria + apresentação), escritos uma vez só para a lista,
// a ficha do produto e o Painel. Aqui só se FORMATA texto: nenhuma regra de estoque é decidida neste arquivo.
// Não usa MAUI, por isso entra no projeto de testes (SigaCean.Tests/ApresentacaoTests.cs).
public static class TextosDeEstoque
{
    // "vence em 12/03/2027 (41 dias)", "vence amanhã (...)", "venceu em 01/09/2026 (há 38 dias)".
    // Usa o texto que já existe em Rotulos (uma escrita só).
    public static string DescreverValidade(DateOnly data, DateOnly hoje) => Rotulos.Validade(data, hoje);

    // Um saldo na ficha do produto: com data, descreve a validade; sem data, ele sai por último (FEFO).
    public static string DescreverSaldo(DateOnly? validade, DateOnly hoje) =>
        validade is { } data ? DescreverValidade(data, hoje) : "Sai por último";

    // "5 un." (a quantidade de um saldo).
    public static string QuantidadeDoSaldo(int quantidade) => $"{quantidade} un.";

    // "1º", "2º"... (a posição do saldo na fila FEFO).
    public static string OrdemDoSaldo(int posicao) => $"{posicao}º";

    // "Medicamento · Ampola · 50 mg": categoria, apresentação e especificação (só as partes que existem).
    public static string CategoriaComApresentacao(Produto produto) => Rotulos.DetalheDoProduto(produto);

    // "mínimo: 10" (nas listas; quem chama só mostra quando o produto tem mínimo).
    public static string Minimo(int? quantidadeMinima) => $"mínimo: {quantidadeMinima}";

    // Na ficha do produto, o mínimo é sempre explicado: ou o valor, ou o aviso de que não foi definido.
    public static string MinimoNaFicha(int? quantidadeMinima) =>
        quantidadeMinima is { } minimo ? $"Estoque mínimo: {minimo}" : "Estoque mínimo não definido.";

    // Saldos por validade numa linha só: "12/11/2026: 4 un. · sem validade: 3 un.".
    // Recebe os saldos JÁ NA ORDEM FEFO (quem decide a ordem é o EstoqueService). Só vale a pena mostrar quando há
    // alguma validade ou mais de um saldo; senão devolve vazio.
    public static string ResumoDosSaldos(IReadOnlyList<SaldoValidade> saldosEmOrdemFefo)
    {
        var comSaldo = saldosEmOrdemFefo.Where(s => s.Quantidade > 0).ToList();
        var temValidade = comSaldo.Any(s => s.DataValidade is not null);

        if (comSaldo.Count <= 1 && !temValidade)
            return string.Empty;

        return string.Join(" · ", comSaldo.Select(s =>
            $"{(s.DataValidade is { } d ? d.ToString("dd/MM/yyyy") : "sem validade")}: {QuantidadeDoSaldo(s.Quantidade)}"));
    }

    // Texto do quadro do Painel: "nenhum produto", "1 produto", "5 produtos".
    public static string ContagemDeProdutos(int quantidade) => quantidade switch
    {
        0 => "nenhum produto",
        1 => "1 produto",
        _ => $"{quantidade} produtos"
    };

    // Texto do botão de filtro do Estoque.
    public static string NomeDoFiltro(FiltroDeSituacao filtro) => filtro switch
    {
        FiltroDeSituacao.Todos => "Todos",
        FiltroDeSituacao.SemEstoque => "Sem estoque",
        FiltroDeSituacao.AbaixoDoMinimo => "Abaixo do mínimo",
        FiltroDeSituacao.Vencidos => "Vencidos",
        FiltroDeSituacao.ValidadeProxima => "Validade próxima",
        _ => filtro.ToString()
    };

    // Liga o botão de filtro à situação que o produto JÁ TEM. As quatro situações vêm do EstoqueService
    // (a tela só repassa); aqui só se escolhe qual delas o filtro olha.
    public static bool Atende(FiltroDeSituacao filtro, bool semEstoque, bool abaixoDoMinimo, bool vencido, bool validadeProxima) =>
        filtro switch
        {
            FiltroDeSituacao.SemEstoque => semEstoque,
            FiltroDeSituacao.AbaixoDoMinimo => abaixoDoMinimo,
            FiltroDeSituacao.Vencidos => vencido,
            FiltroDeSituacao.ValidadeProxima => validadeProxima,
            _ => true
        };
}
