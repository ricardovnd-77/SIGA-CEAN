using SigaCean.Models;

namespace SigaCean.Views;

// Uma linha da tabela do Relatório de Consumo na tela. Só formata os números que o RelatorioService já calculou
// (entradas, saídas, ajustes, média por mês e estoque): nenhuma conta é feita aqui.
public class ConsumoLinha
{
    public ConsumoLinha(ItemRelatorioConsumo item)
    {
        Item = item;
    }

    public ItemRelatorioConsumo Item { get; }

    public string Produto => Rotulos.ProdutoNaLista(Item.Produto);
    public string EntradasTexto => Item.TotalEntradas.ToString();
    public string SaidasTexto => Item.TotalSaidas.ToString();
    public string AjustesTexto => Rotulos.ComSinal(Item.TotalAjustes);
    public string MediaTexto => Item.MediaMensalSaidas.ToString("0.#");
    public string EstoqueTexto => Item.EstoqueAtual.ToString();
}
