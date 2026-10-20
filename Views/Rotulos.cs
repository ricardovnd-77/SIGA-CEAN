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

    // Procura "busca" dentro de "texto", sem diferenciar maiúsculas nem acentos (mesmo critério do ProdutoService.Pesquisar).
    public static bool Contem(string? texto, string busca) =>
        texto is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto, busca, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    // Lê um número inteiro digitado (vazio ou inválido = null).
    public static int? LerInteiro(string? texto) =>
        int.TryParse(texto?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var valor) ? valor : null;
}
