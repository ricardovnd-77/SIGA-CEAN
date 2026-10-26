using System.Globalization;
using SigaCean.Models;

namespace SigaCean.Views;

// Textos que as telas mostram para os enums e para os dados do produto. Fica num lugar só para todas as telas
// escreverem igual. Aqui só se FORMATA texto: nenhuma regra de estoque é decidida neste arquivo.
public static class Rotulos
{
    public static string NomeDaCategoria(Categoria categoria) => categoria switch
    {
        Categoria.Medicamento => "Medicamento",
        Categoria.Insumo => "Insumo",
        Categoria.Alimentar => "Alimentar",
        Categoria.EquipamentoHospitalar => "Equipamento hospitalar",
        Categoria.Higiene => "Higiene",
        Categoria.Microchip => "Microchip",
        _ => categoria.ToString()
    };

    public static string NomeDaOrigem(TipoOrigemEntrada origem) => origem switch
    {
        TipoOrigemEntrada.Compra => "Compra",
        TipoOrigemEntrada.Concessao => "Concessão",
        TipoOrigemEntrada.Doacao => "Doação",
        _ => origem.ToString()
    };

    public static string NomeDoTipo(TipoMovimentacao tipo) => tipo switch
    {
        TipoMovimentacao.Entrada => "Entrada",
        TipoMovimentacao.Saida => "Saída",
        TipoMovimentacao.AjusteBalanco => "Ajuste de balanço",
        _ => tipo.ToString()
    };

    public static string NomeDoStatus(StatusMovimentacao status) => status switch
    {
        StatusMovimentacao.Ativa => "Ativa",
        StatusMovimentacao.Corrigida => "Corrigida",
        StatusMovimentacao.Cancelada => "Cancelada",
        _ => status.ToString()
    };

    // "Medicamento · Frasco 10 ml · 50 mg" (só entram as partes que existem).
    public static string DetalheDoProduto(Produto produto)
    {
        var partes = new List<string> { NomeDaCategoria(produto.Categoria) };
        if (!string.IsNullOrWhiteSpace(produto.Apresentacao)) partes.Add(produto.Apresentacao);
        if (!string.IsNullOrWhiteSpace(produto.Especificacao)) partes.Add(produto.Especificacao);
        return string.Join(" · ", partes);
    }

    // Texto do produto nas listas de escolha: "Dipirona · Ampola 2 ml · 500 mg" (sem a categoria).
    public static string ProdutoNaLista(Produto produto)
    {
        var partes = new List<string> { produto.Nome };
        if (!string.IsNullOrWhiteSpace(produto.Apresentacao)) partes.Add(produto.Apresentacao);
        if (!string.IsNullOrWhiteSpace(produto.Especificacao)) partes.Add(produto.Especificacao);
        return string.Join(" · ", partes);
    }

    // "vence em 12/03/2027 (41 dias)", "vence amanhã (..)", "venceu em 01/09/2026 (há 38 dias)".
    public static string Validade(DateOnly data, DateOnly hoje)
    {
        var dias = data.DayNumber - hoje.DayNumber;
        return dias switch
        {
            < 0 => $"venceu em {data:dd/MM/yyyy} (há {-dias} {(dias == -1 ? "dia" : "dias")})",
            0 => $"vence hoje ({data:dd/MM/yyyy})",
            1 => $"vence amanhã ({data:dd/MM/yyyy})",
            _ => $"vence em {data:dd/MM/yyyy} ({dias} dias)"
        };
    }

    // "+5", "−3" ou "0" (com o sinal de menos tipográfico, para ficar legível).
    public static string ComSinal(int valor) => valor switch
    {
        > 0 => $"+{valor}",
        < 0 => $"−{-valor}",
        _ => "0"
    };

    // "4 un. com validade 12/11/2026; 3 un. sem validade informada": de onde uma saída vai retirar o estoque (FEFO).
    // Recebe a previsão que o MovimentacaoService já calculou; aqui só se escreve o texto.
    public static string Previsao(IEnumerable<SaldoValidade> saldos) =>
        string.Join("; ", saldos.Select(s => s.DataValidade is { } data
            ? $"{s.Quantidade} un. com validade {data:dd/MM/yyyy}"
            : $"{s.Quantidade} un. sem validade informada"));

    // Aviso (laranja) da tela de Entrada quando a validade digitada já passou. A entrada continua sendo aceita
    // (decisão do grupo): o produto só vai aparecer como vencido. Vazio quando não há o que avisar.
    public static string AvisoDeValidadePassada(DateOnly? validade, DateOnly hoje) =>
        validade is { } data && data < hoje
            ? "Esta validade já passou: o produto vai aparecer como vencido."
            : string.Empty;

    // A busca do Histórico acha a movimentação pelo produto, por quem retirou, pelo destino, pelo documento e pela
    // observação (sem diferenciar maiúsculas nem acentos). Busca vazia acha tudo.
    public static bool MovimentacaoContem(Movimentacao movimentacao, string? busca)
    {
        if (string.IsNullOrWhiteSpace(busca)) return true;
        var termo = busca.Trim();
        return Contem(movimentacao.Produto.Nome, termo)
            || Contem(movimentacao.RetiradoPor, termo)
            || Contem(movimentacao.Destino, termo)
            || Contem(movimentacao.Documento, termo)
            || Contem(movimentacao.Observacao, termo);
    }

    // "120 de 870 movimentações" (a lista filtrada em relação ao total do histórico).
    public static string TotalFiltrado(int filtradas, int total) =>
        total == 0 ? "Nenhuma movimentação registrada ainda."
        : $"{filtradas} de {total} {(total == 1 ? "movimentação" : "movimentações")}";

    // "Mostrar mais (20 restantes)" (o botão do Histórico, que mostra um bloco de cada vez).
    public static string MostrarMais(int restantes) => $"Mostrar mais ({restantes} {(restantes == 1 ? "restante" : "restantes")})";

    // Procura "busca" dentro de "texto", sem diferenciar maiúsculas nem acentos (mesmo critério do ProdutoService.Pesquisar).
    public static bool Contem(string? texto, string busca) =>
        texto is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto, busca, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    // Lê um número inteiro digitado (vazio ou inválido = null).
    public static int? LerInteiro(string? texto) =>
        int.TryParse(texto?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var valor) ? valor : null;
}
