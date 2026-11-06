# Roteiro de demonstração – SIGA-CEAN (8 a 10 minutos)

Logins: `admin` / `admin123` (Administrativo) e `vet` / `vet123` (Veterinário).

Menu lateral do Administrativo: **Painel, Estoque, Movimentações, Relatório, Gráficos e Rel. por categoria**.
Entrada, Saída, Cadastrar produto e Ajuste de balanço são **botões** dentro dessas telas (não são itens do menu).

## Antes de apresentar (preparar, uns 3 minutos)

O sistema guarda tudo só em memória. Ele abre com os **134 produtos** e o **histórico real de 713 movimentações** do CEAN,
mas **sem estoque mínimo e sem validade** (a planilha não traz esses dados). Por isso o Painel só mostra "sem estoque" até
você cadastrar mínimos e validades. Prepare tudo e **deixe o programa aberto**: se fechar, volta ao estado inicial.

1. Abra o programa e entre como `admin`.
2. **Estoque mínimo:** Estoque → toque em um produto → **Editar produto** → defina o estoque mínimo. Sugestão:
   - um produto com pouco estoque (por exemplo, a dipirona oral, que tem 3) com mínimo **10**: fica **estoque crítico**;
   - outro com 4 un. com mínimo **6**: fica **abaixo do mínimo**.
3. **Validade próxima:** na ficha do produto, **+ Entrada** com "Possui data de validade" ligada e uma data daqui a uns
   15 dias. Faça outra entrada do mesmo produto com validade mais longa (serve para mostrar o FEFO).
4. **Consumo:** faça de 4 a 6 saídas pequenas em categorias diferentes (um medicamento, um alimento, um insumo...), com
   destino e "retirado por". Isso alimenta os Gráficos e os Relatórios do mês atual.
5. Volte ao **Painel**, confira os alertas e ensaie uma vez com o roteiro abaixo.

## Roteiro (clicar + falar + resultado esperado)

| Min | Tela | O que clicar | O que falar | Resultado esperado |
|---|---|---|---|---|
| 0:00 | Abertura | Tela de login | O CEAN controla o estoque em planilha: não avisa o que está acabando nem o que vence, e não registra quem retirou. O SIGA-CEAN resolve isso. É um app desktop em .NET MAUI, com dados em memória. | Login com a logo do CEAN. |
| 0:40 | Login | Entrar como `admin` (usar "Mostrar senha") | Dois perfis: o Administrativo faz tudo; o Veterinário consulta e registra saídas. | Menu completo e o nome do usuário no canto. |
| 1:10 | Painel | Ver os quadros; tocar em "Abaixo do mínimo" | O Painel mostra o que precisa de atenção. Cada quadro abre o Estoque já filtrado. | Estoque aberto só com os produtos daquela situação. |
| 1:50 | Estoque | Buscar "injetavel" (sem acento); filtro de categoria; botão "Estoque crítico" | São 134 produtos reais do CEAN. A busca ignora acento e maiúsculas. Crítico é quando o estoque está na metade do mínimo ou menos. | Lista filtrada, com a contagem em cada botão. |
| 2:30 | Ficha do produto | Tocar no produto da validade próxima | Os saldos aparecem na ordem em que saem (FEFO): o que vence primeiro sai primeiro. | "1º" a validade mais próxima, depois a mais longa. |
| 3:10 | Entrada | Botão **+ Entrada**: produto, quantidade, tipo de origem (compra), **identificação de origem** (ex.: número da AF), documento, validade | Entrada soma ao estoque. A identificação de origem e o documento dizem de onde veio. | Janela de confirmação, depois a faixa "Entrada registrada... Estoque agora: N". |
| 3:50 | Saída (FEFO) | Botão **− Saída**: escolher o produto e digitar a quantidade (sem confirmar ainda) | Antes de gravar, o sistema mostra de onde a quantidade vai sair. | "Vai sair de: ..." com as validades. |
| 4:20 | Recusa | Digitar uma quantidade maior que o estoque e registrar | O estoque nunca fica negativo, e nada é alterado. | "Estoque insuficiente: há apenas N unidade(s)..." |
| 4:50 | Saída válida | Preencher destino e "Retirado por" e confirmar | Destino e quem retirou são obrigatórios. | Confirmação mostrando "Estoque: A → B" e a faixa "Saída registrada". |
| 5:20 | Ajuste de balanço | Na tela Estoque, botão **Ajuste de balanço**: informar a quantidade apurada na contagem | Para quando a contagem física difere do sistema. | A faixa "Ajuste de balanço registrado" e o ajuste no histórico. |
| 5:50 | Movimentações | Menu Movimentações: buscar um produto; tocar em uma movimentação ativa → **Cancelar Movimentação** com motivo | Nada é apagado: cancelar ou corrigir mantém o registro com situação e motivo e desfaz o efeito no estoque. Só o Administrativo faz isso. | Faixa "Movimentação cancelada..." e a linha com o selo "Cancelada". |
| 6:40 | Relatório de Consumo | Menu Relatório (mês atual), depois Anual; **Gerar PDF** | O relatório de consumo foi apontado pelo CEAN como a maior prioridade. Dá para mudar o período, filtrar por categoria e ordenar. | Tabela com entradas, saídas, ajustes, média por mês e estoque; "Salvar como" e o PDF abre. |
| 7:30 | Gráficos | Menu Gráficos: Barras, Pizza e Rosca; "Uso no período"; escolher uma categoria e alternar "Mais usados" e "Menos usados" | Pedido da professora: estoque por categoria e o produto mais e menos usado em cada uma. Uso = saídas no período. | Gráfico, legenda com valores e percentuais, cartões por categoria. |
| 8:30 | Relatório por categoria | Menu "Rel. por categoria" → **Gerar PDF** | Mesmo conteúdo dos gráficos, em relatório para imprimir. | Resumo, cartões por categoria e o PDF. |
| 9:10 | Perfil Veterinário | Sair; entrar como `vet`; registrar uma saída | O Veterinário vê Painel, Estoque e Movimentações só para consulta e registra saídas. Não faz entrada, ajuste, correção nem relatórios. | Menu com Painel, Estoque e Movimentações; só o botão "− Saída"; saída registrada com "resp.: Dr. Veterinário". |
| 9:40 | Fechamento | — | Regras testadas automaticamente (219 testes). Código no GitHub: ricardovnd-77/SIGA-CEAN. | — |

## Perguntas prováveis da banca

- **Por que não tem banco de dados?** Decisão do escopo acadêmico: só memória, para focar nas regras de estoque. As regras
  ficam nos Services e não dependem de como os dados são guardados; trocar a camada `Infrastructure` por um banco seria o
  próximo passo.
- **Como funciona o FEFO?** Cada entrada guarda sua validade em um saldo (`LoteEstoque`, que não é controle de lote). A
  saída retira primeiro do saldo que vence antes; o que não tem validade sai por último. A tela só mostra a previsão; a
  regra está em `Produto` e é a mesma da baixa real.
- **O que acontece ao fechar o programa?** Tudo volta ao estado inicial (os produtos e o histórico reais do CEAN). Só o PDF
  que o usuário escolhe salvar vira arquivo.
- **Quais são os perfis?** Administrativo faz tudo. Veterinário consulta Painel, Estoque e Movimentações e registra saídas;
  não registra entrada, não ajusta, não corrige nem cancela e não vê relatórios nem gráficos.
- **E se alguém digitar errado?** Os Services validam sozinhos: quantidade inválida, saída maior que o estoque, produto
  duplicado ou falta de "retirado por" são recusados sem alterar nada.
- **Por que a saída exige "retirado por"?** O CEAN respondeu que hoje não identifica quem retira e que "seria útil".
- **O que é "estoque crítico"?** Quantidade igual ou menor que metade do mínimo; é um caso mais grave de "abaixo do mínimo".
- **Limitações?** Sem banco de dados, sem código de barras, sem controle de lote (a validade é por entrada), sem módulo
  veterinário (animais, prontuário). Os dados reais não trazem mínimo nem validade, então esses alertas dependem do cadastro.

## Se algo der errado

- **O programa fechou:** os dados voltam ao início. Abra, entre como `admin` e refaça só os passos da preparação que o
  roteiro usa.
- **O PDF não abre:** mostre a tela do relatório e diga que o PDF é salvo no lugar que o usuário escolher.
- **Pouco tempo:** corte o Ajuste de balanço e o Relatório por categoria (o Gráfico já mostra o mesmo conteúdo).

## Checklist do dia

- [ ] Notebook carregado; .NET SDK 10 e a carga de trabalho `maui-windows` instalados (se for compilar no local)
- [ ] Projeto em caminho curto (por exemplo, `C:\SIGA-CEAN`), já compilado
- [ ] Preparação feita e programa aberto, sem fechar
- [ ] Resolução e zoom testados no projetor
- [ ] Pasta para salvar os PDFs definida
