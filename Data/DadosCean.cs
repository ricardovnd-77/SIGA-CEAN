using SigaCean.Models;
using SigaCean.Services;

namespace SigaCean.Data;

// Dados de exemplo: os produtos e as quantidades reais da planilha do CEAN ("ESTOQUE DE MEDICAMENTOS - 2026 - CEAN.xlsx",
// aba "ESTOQUE DE MEDICAMENTOS", coluna "EM ESTOQUE"), copiados à mão para o código (fonte: DadosCean.cs do repositório
// leomc06/ACELERA). Não há leitura de arquivo: tudo está escrito aqui, e os dados continuam existindo só em memória.
//
// Fica separado do AppData de propósito: "new AppData()" continua começando vazio (os testes dependem disso).
// Quem chama o Carregar é o MauiProgram, uma vez, ao criar o AppData do aplicativo.
//
// A planilha não informa categoria, validade nem estoque mínimo:
//   - categoria: Medicamento para quase tudo, menos os itens listados em CategoriaDiferenteDeMedicamento;
//   - validade: nenhuma (o estoque entra num saldo sem data);
//   - quantidade mínima: em branco (null).
public static class DadosCean
{
    // Os nomes seguem exatamente a planilha (já contêm apresentação e especificação, por isso esses campos ficam em branco).
    public static readonly (string Nome, int Quantidade)[] Produtos =
    [
        ("CLORIDRATO DE TRAMADOL - 50 MG - INJETÁVEL, AMPOLA", 269),
        ("DIPIRONA - 500 MG - INJETÁVEL, AMPOLA", 63),
        ("PRADOR, MELOXICAM 1 MG + DIPIRONA 50 MG - INJETÁVEL, FRASCO 100 ML", 4),
        ("DIPIRONA 500 MG, FEBRAX - INJETÁVEL, FRASCO 50 ML", 0),
        ("DIPIRONA 500 MG - ORAL, FRASCO 20 ML", 3),
        ("DIPIRONA 500 MG - ORAL, FRASCO 10 ML", 0),
        ("BUSCOPAN - HYPOCINA COMPOSTA, BUTILBROMETO DE ESCOPOLAMINA 4 MG/ML + DIPIRONA 500 MG/ML - INJETÁVEL, AMPOLA", 50),
        ("CITRATO DE FENTANILA, UNIFENTAL - 78,5 MG - INJETÁVEL, AMPOLA", 35),
        ("SULFATO DE MORFINA 10 MG - INJETÁVEL, AMPOLA", 104),
        ("EGG, ÉTER GLICERIL GUAIACOL - 10 G - INJETÁVEL, FRASCO 100 ML", 0),
        ("DETOMIDINA - 1% - INJETÁVEL, FRASCO 5 ML", 1),
        ("CLORIDRATO DE LIDOICAÍNA - 50 MG - SEM VASO - INJETÁVEL, FRASCO 100 ML", 39),
        ("CLORIDRATO DE LIDOICAÍNA MONOIDRATADO - 2%, 20 MG - COM VASO - INJETÁVEL, FRASCO 20 ML", 16),
        ("CLORIDRATO DE LIDOICAÍNA MONOIDRATADO - 30 MG - POMADA, FRASCO 20 MG", 4),
        ("PROPOFOL 1% - 10 MG - INJETÁVEL, FRASCO 20 ML", 44),
        ("APROMAZIN 1 % - INJETÁVEL, FRASCO 20 ML", 2),
        ("FENOBARBITAL - 200 MG - INJETÁVEL, AMPOLA 2 ML", 99),
        ("FENOBARBITAL 100 MG - COMPRIMIDO", 250),
        ("XILAZIN, CLORIDRATO DE XILAZINA 2% - INJETÁVEL, FRASCO 10 ML", 0),
        ("XILAZIN, CLORIDRATO DE XILAZINA 2% - INJETÁVEL, FRASCO 50 ML", 11),
        ("CLORIDRATO DE CETAMINA - 10 MG, FRASCO 10 ML", 0),
        ("CLORIDRATO DE CETAMINA - 10 MG, FRASCO 50 ML", 0),
        ("KETALEX, CLORIDRATO DE CETAMINA - 10 MG, FRASCO 50 ML", 4),
        ("DIAZEPAM - 10 MG - COMPRIMIDO", 100),
        ("DIAZEPAM - 5 MG - INJETÁVEL, AMPOLA", 0),
        ("CEFALEXINA - 500 MG - CAPSULA", 40),
        ("CEFALEXINA - 300 MG - CAPSULA", 0),
        ("CEFALEXINA - 200 MG - CAPSULA", 200),
        ("CEFALEXINA - 150 MG - CAPSULA", 450),
        ("CEFALEXINA - 100 MG - CAPSULA", 450),
        ("CEFALEXINA - 75 MG - CAPSULA", 400),
        ("ENROFLAXACINO, FLOBIOTIC - 150 MG - CAIXA COM 10 COMPRIMIDOS", 118),
        ("ENROFLAXACINO, FLOBIOTIC - 50 MG - CAIXA COM 10 COMPRIMIDOS", 7),
        ("ENROFLAXACINO - 10 MG - INJETÁVEL, FRASCO 50 ML", 0),
        ("ENROFLAXACINO, FLOBIOTIC - 10 MG - INJETÁVEL, FRASCO 500 ML", 0),
        ("PENTABIÓTICO, BENZILPENINCILINA, DIHIDROESTREPTOMICINA - INJETÁVEL, FRASCO COM 100ML", 0),
        ("AMOXICILINA TRIHIDRATADA COM CLUVALANATO DE POTÁSSIO, AGEMOXI - 250 MG - CAIXA COM 10 COMPRIMIDOS", 0),
        ("AMOXICILINA TRIHIDRATADA COM CLUVALANATO DE POTÁSSIO, SILMOX - 250 MG - CAIXA COM 10 COMPRIMIDOS", 0),
        ("AZITROMICINA DI - HIDRATADA - 500 MG - CAIXA COM 5 COMPRIMIDOS", 80),
        ("CLINDAMICINA, CLINBACTER - 150 MG - CAIXA COM 14 COMPRIMIDOS", 2),
        ("FOSFATO DE CLINDAMICINA, HYCLIN - 150 MG - INJETÁVEL, AMPOLA", 50),
        ("DOXICICLINA 100 MG - CAIXA COM 15 COMP", 0),
        ("DOXICICLINA 100 MG - CAIXA COM 20 COMP", 100),
        ("SULFATO DE GENTAMICINA 40 MG - INJETÁVEL, AMPOLA", 50),
        ("TEICOPLANINA, BACTOMAX 400 MG - INJETÁVEL", 5),
        ("CEFTRIAXONA SÓDICA - 0,31 - INJETÁVEL, FRASCO 1,0 MG", 0),
        ("CEFTRIAXONA SÓDICA - 1 G - INJETÁVEL, FRASCO 1,0 MG", 15),
        ("CEFTIFUR 50 MG - INJETÁVEL, FRASCO 100 ML", 1),
        ("MEROPENEM - 1140 MG - INJETÁVEL, FRASCO 1,0 G", 13),
        ("ERTAPENEM SÓDICO - 1146 MG - INJETÁVEL, FRASCO 1,0 G", 5),
        ("NEOMICINA + BACITRACINA 5 MG/250 G - POMADA, FRASCO 15 G", 2),
        ("TOBRAMICINA 3 MG - GOTAS, FRASCO 5 ML", 0),
        ("AURITOP, GEL OTOLÓGICO 30 ML", 1),
        ("OTODERMIN, SOLUÇÃO OTOLÓGICA", 0),
        ("RIFAMICINA 10 MG - SPRAY, FRASCO 20 ML", 18),
        ("MELOXICAM - 2,0 MG - CAPSULA", 0),
        ("MELOXICAN - 1,0 MG - CAPSULA", 0),
        ("MELOXICAN - 0,5 MG - CAPSULA", 300),
        ("MELOXICAM, OVERXICAM - 0,3 MG - INJETÁVEL, FRASCO 50 ML", 1),
        ("MELOXICAM, MAXITEC - 0,3 MG - INJETÁVEL, FRASCO 50 ML", 6),
        ("MELOXICAM, MELOXIFARM - 2,0 MG - CAIXA 10 COMP", 24),
        ("MELOXICAM, MELOXIFARM - 1,0 MG - CAIXA 10 COMP", 11),
        ("CARPROFENO, CARPROFLAN - 100 MG - CAIXA 10 COMP", 59),
        ("CETOPROFENO 50 MG - INJETÁVEL, AMPOLA", 99),
        ("DEXAMETASONA 4 MG, CITOBÊ DEXA - INJETÁVEL, AMPOLA 1 ML", 0),
        ("PREDNISOLONA - 40 MG - CAIXA 2 COMP", 0),
        ("PREDNISOLONA - 20 MG - CAIXA 2 COMP", 0),
        ("FLUNIXINA, FLUMEDIN - 8,29 MG - FRASCO 50 ML", 1),
        ("FLUMETASONA, FLUCORTAN 0,5MG - INJETÁVEL, FRASCO 10 ML", 4),
        ("DEXAMETASONA 200 MG, 100ML FRASCO 50ML", 3),
        ("BETAMETASONA - 5 MG/2 MG - INJETÁVEL, AMPOLA", 5),
        ("FOSFATO DE DEXAMETASONA - 4 MG - INJETÁVEL, AMPOLA", 90),
        ("SUPLEMETO AMINOÁCIDO VITAMÍNICO, ORGANEW PET - PROBIÓTICO - FRASCO PASTOSO 12 G", 5),
        ("SUPLEMETO PROTEÍCO VITAMÍNICO, ORGANEW PET - PROBIÓTICO - FRASCO 100G", 4),
        ("SUPLEMENTO VITAMÍNICO MINERAL , GLICOPAN PET - FRASCO 250 ML", 0),
        ("SUPLEMENTO VITAMÍNICO MINERAL , GLICOPAN GOLD- FRASCO 125 ML", 3),
        ("SUPLEMENTO VITAMÍNICO MINERAL , HEMOLITAN - FRASCO 30 ML", 5),
        ("SUPLEMENTO VITAMÍNICO MINERAL , HEMOLITAN - FRASCO 1 LITRO", 0),
        ("SUPLEMENTO SUBSTITUTO DE LEITE MATERNO, 300 G, P/CÃES E GATOS", 0),
        ("PAPINHA DE DESMAME 1KG", 0),
        ("SUPLEMENTO DE GLUCONATO DE CALCIO, FRASCO COM 100 ML - INJETÁVEL", 3),
        ("ANTITÓXICO HEPATOPROTETOR, VITAMINADO - INJETÁVEL, FRASCO 100 ML", 3),
        ("SULFATO DE ATROPINA 0,10 G, 20 ML - INJETÁVEL", 6),
        ("POTENAY, COMPLEXO B + MEFENTERMINA - 10 ML - INJETÁVEL, FRASCO 10 ML", 8),
        ("GLICOSE 50% - INJETÁVEL, AMPOLA 10 ML", 120),
        ("POLIVITAMÍNICO SEM MINERAIS, FRUTOVITAM - 10 ML - AMPOLA", 100),
        ("PROBIÓTICO - BIOSAN FLORA B12", 0),
        ("SUPLEMENTO VITAMINICO AMINOCANI'S 10 COMPRIMIDOS", 3),
        ("POLIVITAMÍNICO DO COMPLEXO B, HIPLEX - 2 ML - AMPOLA", 27),
        ("VERMICIDA, PRAZIQUANTEL, HELFINE PLUS CÃES - CAIXA COM 4 COMP", 0),
        ("VERMICIDA, PRAZIQUANTEL - CAPSULA", 0),
        ("VERMICIDA, PRAZIQUANTEL, CHERMITAL GATOS - CAIXA COM 4 COMP", 9),
        ("VERMIFUGO PARA CÃO FILHOTE, VERMOTRIX PLUS BABY - FRASCO 20 ML", 0),
        ("VERMIFUGO PARA CÃES E GATOS, PIRANTEL, VERMEX - FRASCO 20ML", 0),
        ("IVERMECTINA 6 MG, CAIXA COM 8 COMPRIMIDOS", 10),
        ("DELTAMETRINA 25 G, BUTOX - FRASCO 1 L", 0),
        ("AMITRAZ 12,5 G - FRASCO 200 ML, IBATOX", 1),
        ("AMITRAZ 12,5 G - FRASCO 1 L", 4),
        ("ALBENDAZOL 10 G/% - FRASCO 1 L", 1),
        ("SPRAY PRATA, ANTIPARASITÁRIO AEROSSOL, SILVERBAC - FRASCO 500 ML", 3),
        ("NITEMPIRAM, CAPSTAR 11 MG - ATÉ 11,5 KG - CAIXA COM 6 COMP", 0),
        ("NITEMPIRAM, CAPSTAR 57MG - 11 KG/57 KG - CAIXA COM 6 COMP", 0),
        ("NITEMPIRAM, INVICTO 57 MG - 11 KG/57 KG - CAIXA COM 20 COMP", 1),
        ("NITEMPIRAM, INVICTO 11 MG - ATÉ 11 KG - CAIXA COM 20 COMP", 3),
        ("SHAMPOO, CLOREXIDINA 20 G - FRASCO 500 ML", 0),
        ("SHAMPOO, CETOCONAZOL 0,5 G + CLOREXIDINA 20 G - FRASCO 500 ML", 0),
        ("ADVOCATE, IMIDACLOPRIDO 100 MG/MOXIDECTINA 10 MG - 4/8 KG", 4),
        ("REVOLUTION 12%,SELAMECTINA - 10/20 KG", 3),
        ("ADVANTAGE MAX3, IMIDACLOPRIDO 100 MG/PERMETRINA 500 MG - 25 + KG", 4),
        ("FIPRONIL 1%, TOPLINE - 1 LITRO", 0),
        ("BRAVECTO, FLURALANER 250 MG - 4,5/10 KG", 18),
        ("BRAVECTO, FLURALANER 500 MG - 10/20 KG", 23),
        ("BRAVECTO, FLURALANER 1000 MG - 20/40 KG", 17),
        ("SIMPARIC, SORALANER 20 MG - 5/10 KG", 17),
        ("SIMPARIC, SORALANER 40 MG - 10/20 KG", 19),
        ("NEXGARD, AFOXOLANER 68 MG - 10/25 KG - CAIXA COM 3 COMP", 4),
        ("NEXGARD, AFOXOLANER 136 MG - 25/50 KG - CAIXA COM 3 COMP", 22),
        ("METERGOLINA 2 MG, SEC LAC - CAIXA COM 16 COMP", 40),
        ("METERGOLINA 0,5 MG, SEC LAC - CAIXA COM 16 COMP", 0),
        ("FAMOTIDINA 40 MG/50ML - INJETÁVEL, FRASCO/SERINGA", 12),
        ("OMEPRAZOL SÓDICO, 40 MG - INJETÁVEL, FRASCO/AMPOLA", 60),
        ("CLORIDRATO DE PROMETAZINA 25 MG - INJETÁVEL, AMPOLA", 100),
        ("LEVETIRACETAM, 500 MG - CAIXA COM 7 COMP", 0),
        ("BROMEXINA, ALIV V, 300 MG - NJETÁVEL, FRANCO 50 ML", 6),
        ("ANTICONCEPCIONAL PARA CADELAS E GATAS - INIBIDEX", 29),
        ("SORO ANTIOFÍDICO POLIVALENTE, CROTÁLICO/BOTRÓPICO, FRASCO 50 ML - INJETÁVEL, AMPOLA", 5),
        ("POMADA HIDRATANTE - RICINIUS 100 G - FRASCO 250 G", 0),
        ("TIMOMODULINA 200MG - LEUCOGEN USO ORAL", 0),
        ("TACROLIMO, TARFIC 0,1", 1),
        ("CLORETO DE POTÁSSIO 19,1% - INJETÁVEL, AMPOLA", 15),
        ("ACIDO TRANEXÂMICO, 50 MG/ML - INJETÁVEL, AMPOLA", 32),
        ("VACINA VENCOMAX - INJETÁVEL, DOSES", 0),
        ("TESTE DE CINOMOSE", 20),
        ("MICROCHIP", 400),
    ];

    // Todo item entra como Medicamento, menos estes, que a equipe decidiu separar.
    // (Teste de cinomose, pomada e suplementos continuam como Medicamento, por decisão da equipe.)
    private static readonly Dictionary<string, Categoria> CategoriaDiferenteDeMedicamento = new()
    {
        ["MICROCHIP"] = Categoria.Microchip,
        ["PAPINHA DE DESMAME 1KG"] = Categoria.Alimentar,
        ["SHAMPOO, CLOREXIDINA 20 G - FRASCO 500 ML"] = Categoria.Higiene,
        ["SHAMPOO, CETOCONAZOL 0,5 G + CLOREXIDINA 20 G - FRASCO 500 ML"] = Categoria.Higiene,
    };

    // Carrega o saldo inicial. Passa pelos Services (as regras ficam nos Services) e NÃO cria movimentações:
    // é saldo inicial, e movimentações falsas distorceriam o relatório de consumo.
    public static void Carregar(AppData dados)
    {
        var produtoService = new ProdutoService(dados);
        var estoqueService = new EstoqueService(dados);

        foreach (var (nome, quantidade) in Produtos)
        {
            var categoria = CategoriaDiferenteDeMedicamento.GetValueOrDefault(nome, Categoria.Medicamento);
            var produto = produtoService.Cadastrar(nome, categoria, null, null, null);

            // Produto com quantidade 0 é só cadastrado (o estoque não aceita entrada de zero).
            if (quantidade > 0)
                estoqueService.AumentarEstoque(produto, quantidade, null);
        }
    }
}
