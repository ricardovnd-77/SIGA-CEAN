using SigaCean.Services;

namespace SigaCean.Views;

// Escolha do período (mensal, trimestral, anual ou personalizado). Quem calcula os períodos e o nome deles é o
// PeriodoRelatorio (Services); aqui só se escolhe e se avisa a tela (PeriodoAlterado) para ela buscar os dados de novo.
// É reutilizável: o Relatório de Consumo usa e outras telas de relatório também podem usar.
public partial class SeletorPeriodoView : VerticalStackLayout
{
    // Avisa que o período mudou (trocou o tipo, usou Anterior/Próximo ou aplicou as datas).
    public event EventHandler? PeriodoAlterado;

    public TipoPeriodo Tipo { get; private set; } = TipoPeriodo.Mensal;
    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; } // último dia do período (só a data, sem hora)

    // Fim do período até o último instante do dia final: é o que o RelatorioService espera receber.
    public DateTime FimInclusivo => Fim.Date.AddDays(1).AddTicks(-1);

    public string Descricao => PeriodoRelatorio.Descrever(Tipo, Inicio, Fim);

    public SeletorPeriodoView()
    {
        InitializeComponent();
    }

    // Escolhe o tipo. Mensal, trimestral e anual ficam no período que contém o início atual (ou hoje, na primeira vez);
    // o personalizado parte do último período mostrado, para o usuário só ajustar as datas.
    public void Escolher(TipoPeriodo tipo)
    {
        if (tipo == TipoPeriodo.Personalizado)
        {
            if (Inicio == default)
                (Inicio, Fim) = (DateTime.Today.AddDays(-30), DateTime.Today);

            DataInicioPicker.Date = Inicio;
            DataFimPicker.Date = Fim;
        }
        else
        {
            (Inicio, Fim) = PeriodoRelatorio.Contendo(tipo, Inicio == default ? DateTime.Today : Inicio);
        }

        Tipo = tipo;
        MostrarMensagem(null);
        AtualizarTela();
        PeriodoAlterado?.Invoke(this, EventArgs.Empty);
    }

    private void OnTipoClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is string nome && Enum.TryParse<TipoPeriodo>(nome, out var tipo))
            Escolher(tipo);
    }

    private void OnAnteriorClicked(object? sender, EventArgs e) => Deslocar(-1);

    private void OnProximoClicked(object? sender, EventArgs e) => Deslocar(1);

    private void Deslocar(int passos)
    {
        (Inicio, Fim) = PeriodoRelatorio.Deslocar(Tipo, Inicio, passos);
        AtualizarTela();
        PeriodoAlterado?.Invoke(this, EventArgs.Empty);
    }

    // Período personalizado: recusa data inicial depois da final (a mesma regra e a mesma mensagem do Service).
    private void OnAplicarClicked(object? sender, EventArgs e)
    {
        var inicio = DataInicioPicker.Date ?? DateTime.Today;
        var fim = DataFimPicker.Date ?? DateTime.Today;
        if (inicio > fim)
        {
            MostrarMensagem("A data inicial não pode ser depois da data final.");
            return;
        }

        MostrarMensagem(null);
        (Inicio, Fim) = (inicio.Date, fim.Date);
        AtualizarTela();
        PeriodoAlterado?.Invoke(this, EventArgs.Empty);
    }

    private void AtualizarTela()
    {
        Marcar(MensalButton, Tipo == TipoPeriodo.Mensal);
        Marcar(TrimestralButton, Tipo == TipoPeriodo.Trimestral);
        Marcar(AnualButton, Tipo == TipoPeriodo.Anual);
        Marcar(PersonalizadoButton, Tipo == TipoPeriodo.Personalizado);

        NavegacaoLayout.IsVisible = Tipo != TipoPeriodo.Personalizado;
        PersonalizadoLayout.IsVisible = Tipo == TipoPeriodo.Personalizado;
        PeriodoLabel.Text = Descricao;
    }

    // O tipo escolhido fica preenchido; os outros só com contorno. TryGetValue também olha os dicionários mesclados
    // (o indexador Resources["..."] não olha).
    private static void Marcar(Button botao, bool selecionado)
    {
        var chave = selecionado ? "ChipEscolhido" : "Chip";
        if (Application.Current!.Resources.TryGetValue(chave, out var estilo) && estilo is Style style)
            botao.Style = style;
    }

    private void MostrarMensagem(string? texto)
    {
        MensagemLabel.Text = texto ?? string.Empty;
        MensagemLabel.IsVisible = !string.IsNullOrEmpty(texto);
    }
}
