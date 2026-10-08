namespace SigaCean.Models;

// Categorias de produto. O conjunto é pequeno e fechado, então um enum basta (não precisa de classe nem de lista).
// Os nomes mostrados na tela ficam para quem fizer a tela (ex.: EquipamentoHospitalar -> "Equipamento hospitalar").
public enum Categoria
{
    Medicamento,
    Insumo,
    Alimentar,
    EquipamentoHospitalar,
    Higiene,
    Microchip
}
