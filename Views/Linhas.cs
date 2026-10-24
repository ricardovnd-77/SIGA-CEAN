using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Views;

// Cada classe abaixo prepara os dados de UMA linha de lista para a tela. Só formatam o que os Services já decidiram
// (situação, validade vencida/próxima...): nenhuma regra de estoque é calculada aqui.

// Uma linha da lista de produtos (Estoque, Produtos e Dashboard).
public class ProdutoLinha
{
    private static readonly Color Vermelho = Color.FromArgb("#D95757");
    private static readonly Color Laranja = Color.FromArgb("#F59B24");
    private static readonly Color Verde = Color.FromArgb("#169B62");
    private static readonly Color Cinza = Color.FromArgb("#718589");

    public ProdutoLinha(Produto produto, EstoqueService estoque, DateOnly hoje)
    {
        Produto = produto;

        // Todas as situações vêm do EstoqueService.
        var situacao = estoque.VerificarSituacao(produto);
        SemEstoque = situacao == SituacaoEstoque.SemEstoque;
        AbaixoDoMinimo = situacao == SituacaoEstoque.AbaixoDoMinimo;
        Vencido = estoque.TemValidadeVencida(produto, hoje);
        ValidadeProxima = estoque.TemValidadeProxima(produto, hoje);

        // Saldos na ordem FEFO: quem decide a ordem é o EstoqueService (a tela não reordena).
        var saldos = SaldosEmOrdemFefo(produto, estoque);

        // A validade mais próxima do que ainda existe em estoque: o primeiro saldo COM data na fila FEFO (só para mostrar).
        var proxima = saldos.FirstOrDefault(s => s.DataValidade is not null)?.DataValidade;
        if (proxima is { } data)
        {
            ValidadeTexto = TextosDeEstoque.DescreverValidade(data, hoje);
            CorValidade = Vencido ? Vermelho : ValidadeProxima ? Laranja : Cinza;
        }

        // Saldos por validade numa linha ("12/11/2026: 4 un. · sem validade: 3 un."): só quando vale a pena.
        SaldosTexto = TextosDeEstoque.ResumoDosSaldos(saldos);

        CorQuantidade = SemEstoque ? Vermelho : AbaixoDoMinimo ? Laranja : Verde;
    }

    public Produto Produto { get; }

    public string Nome => Produto.Nome;
    public string Detalhe => TextosDeEstoque.CategoriaComApresentacao(Produto);
    public int Quantidade => Produto.Quantidade;

    public bool MostrarMinimo => Produto.QuantidadeMinima is not null;
    public string MinimoTexto => TextosDeEstoque.Minimo(Produto.QuantidadeMinima);

    public bool SemEstoque { get; }
    public bool AbaixoDoMinimo { get; }
    public bool Vencido { get; }
    public bool ValidadeProxima { get; }
    public bool PrecisaDeAtencao => SemEstoque || AbaixoDoMinimo || Vencido || ValidadeProxima;

    // Este produto aparece no filtro escolhido? (as situações já foram perguntadas ao EstoqueService no construtor)
    public bool Atende(FiltroDeSituacao filtro) =>
        TextosDeEstoque.Atende(filtro, SemEstoque, AbaixoDoMinimo, Vencido, ValidadeProxima);

    // Os saldos com estoque, na ordem em que SAEM (FEFO). A ordem é decidida pelo EstoqueService: a previsão de retirar o
    // estoque inteiro devolve exatamente essa fila. Sem estoque não há o que prever (a previsão recusa quantidade zero).
    public static List<SaldoValidade> SaldosEmOrdemFefo(Produto produto, EstoqueService estoque) =>
        produto.Quantidade > 0 ? estoque.PrevisaoFefo(produto, produto.Quantidade) : new List<SaldoValidade>();

    public string ValidadeTexto { get; } = string.Empty;
    public bool MostrarValidade => ValidadeTexto.Length > 0;
    public Color CorValidade { get; } = Cinza;

    public string SaldosTexto { get; } = string.Empty;
    public bool MostrarSaldos => SaldosTexto.Length > 0;

    public Color CorQuantidade { get; }
}

// Uma linha da lista "Saldos por validade" da ficha do produto (já na ordem FEFO).
public class SaldoLinha
{
    private readonly SaldoValidade _saldo;
    private readonly int _posicao;
    private readonly DateOnly _hoje;

    public SaldoLinha(SaldoValidade saldo, int posicao, DateOnly hoje)
    {
        _saldo = saldo;
        _posicao = posicao;
        _hoje = hoje;
    }

    public string OrdemTexto => TextosDeEstoque.OrdemDoSaldo(_posicao);
    public string ValidadeTexto => TextosDeEstoque.DescreverSaldo(_saldo.DataValidade, _hoje);
    public string QuantidadeTexto => TextosDeEstoque.QuantidadeDoSaldo(_saldo.Quantidade);
}

// Uma linha do Histórico (e das últimas movimentações do Painel).
public class MovimentacaoLinha
{
    private static readonly Color Vermelho = Color.FromArgb("#D95757");
    private static readonly Color Laranja = Color.FromArgb("#F59B24");
    private static readonly Color Verde = Color.FromArgb("#169B62");
    private static readonly Color Primaria = Color.FromArgb("#087F78");
    private static readonly Color Cinza = Color.FromArgb("#718589");

    public MovimentacaoLinha(Movimentacao movimentacao, bool administrativo)
    {
        Movimentacao = movimentacao;
        PodeEncerrar = administrativo && movimentacao.Status == StatusMovimentacao.Ativa;
    }

    public Movimentacao Movimentacao { get; }

    // Corrigir e cancelar só aparecem para o perfil Administrativo e só para o que ainda está ativo.
    public bool PodeEncerrar { get; }

    public string Produto => Rotulos.ProdutoNaLista(Movimentacao.Produto);
    public string DataTexto => Movimentacao.Data.ToString("dd/MM/yyyy HH:mm");
    public string TipoTexto => Rotulos.NomeDoTipo(Movimentacao.Tipo);

    public string QuantidadeTexto => Movimentacao.Tipo switch
    {
        TipoMovimentacao.Entrada => $"+{Movimentacao.Quantidade}",
        TipoMovimentacao.Saida => $"−{Movimentacao.Quantidade}",
        _ => Rotulos.ComSinal(Movimentacao.Quantidade)
    };

    // Movimentação corrigida ou cancelada fica apagada na cor: o efeito dela já foi desfeito.
    public Color CorQuantidade => Movimentacao.Status != StatusMovimentacao.Ativa ? Cinza : Movimentacao.Tipo switch
    {
        TipoMovimentacao.Entrada => Verde,
        TipoMovimentacao.Saida => Laranja,
        _ => Movimentacao.Quantidade < 0 ? Laranja : Primaria
    };

    public string StatusTexto => Rotulos.NomeDoStatus(Movimentacao.Status);
    public bool MostrarStatus => Movimentacao.Status != StatusMovimentacao.Ativa;
    public Color CorStatus => Movimentacao.Status == StatusMovimentacao.Cancelada ? Vermelho : Laranja;

    public string Motivo => $"Motivo: {Movimentacao.MotivoCorrecaoOuCancelamento}";
    public bool MostrarMotivo => MostrarStatus && !string.IsNullOrWhiteSpace(Movimentacao.MotivoCorrecaoOuCancelamento);

    // Os campos que importam para cada tipo de movimentação, numa linha só.
    public string Detalhes
    {
        get
        {
            var m = Movimentacao;
            var partes = new List<string>();

            switch (m.Tipo)
            {
                case TipoMovimentacao.Entrada:
                    if (m.TipoOrigem is { } origem) partes.Add($"Origem: {Rotulos.NomeDaOrigem(origem)}");
                    if (!string.IsNullOrWhiteSpace(m.Origem)) partes.Add(m.Origem);
                    if (!string.IsNullOrWhiteSpace(m.NumeroAF)) partes.Add($"AF {m.NumeroAF}");
                    if (!string.IsNullOrWhiteSpace(m.Documento)) partes.Add($"Doc. {m.Documento}");
                    if (m.DataValidade is { } validade) partes.Add($"Validade {validade:dd/MM/yyyy}");
                    break;

                case TipoMovimentacao.Saida:
                    if (!string.IsNullOrWhiteSpace(m.Destino)) partes.Add($"Destino: {m.Destino}");
                    if (!string.IsNullOrWhiteSpace(m.RetiradoPor)) partes.Add($"Retirado por {m.RetiradoPor}");
                    break;

                case TipoMovimentacao.AjusteBalanco:
                    partes.Add($"Sistema: {m.QuantidadeAnterior} → contado: {m.QuantidadeApurada}");
                    break;
            }

            if (m.UsuarioResponsavel is not null) partes.Add($"Por {m.UsuarioResponsavel.Nome}");
            if (!string.IsNullOrWhiteSpace(m.Observacao)) partes.Add($"Obs.: {m.Observacao}");
            return string.Join(" · ", partes);
        }
    }

    public bool MostrarDetalhes => Detalhes.Length > 0;

    // "20/10/2026 13:18 · Destino: Canil · Retirado por Maria": a linha dos relatórios de entradas e saídas.
    public string DataEDetalhes => MostrarDetalhes ? $"{DataTexto} · {Detalhes}" : DataTexto;
}

// Uma linha de qualquer relatório: título, uma linha de detalhe e o número em destaque à direita.
public class LinhaRelatorio
{
    public required string Titulo { get; init; }
    public string Detalhe { get; init; } = string.Empty;
    public string Destaque { get; init; } = string.Empty;
    public Color CorDestaque { get; init; } = Color.FromArgb("#153B43");
    public bool MostrarDetalhe => Detalhe.Length > 0;
}
