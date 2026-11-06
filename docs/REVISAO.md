# Revisão do app (SIGA-CEAN)

## Como esta revisão foi feita

- **Leitura do código** de todas as telas (`Views/`), dos Services e dos dados: para cada caso de erro da lista abaixo, conferi qual mensagem o app mostra e se o estoque fica intacto. Isso está marcado com ✔ na coluna **Código**.
- **Teste manual (F5)**: a coluna **F5** fica com `☐` para quem rodar o app marcar `✔` (ou `✘` se não acontecer o esperado). Itens visuais (corte na janela mínima de 900x600, fluidez da lista) não dá para ver pelo código e estão com **—** na coluna Código.

Legenda do resultado: **ok** (o código faz o esperado), **corrigido** (era problema de tela e foi ajustado), **pendente** (não é problema de tela; anotado para o grupo).

Esperado em todos os casos: mensagem clara em português, sem travar e sem alterar o estoque.

## Atualização de 06/11/2026 (fechamento)

Depois desta revisão entraram: histórico real do CEAN, Painel/Estoque/ficha, Relatório de Consumo com PDF, Gráficos, Relatório
por Categoria, "Estoque crítico" e as duas mudanças pedidas pelo orientador do CEAN: o campo "Número AF" virou **Identificação
de origem** e o **Veterinário passou a registrar saídas** (e a ver Painel e Histórico, só para consulta).
As tabelas abaixo foram atualizadas. Na coluna **F5**, ✔ significa que a conferência foi feita no app rodando em 06/11/2026
(por quem preparou o fechamento); ☐ continua sendo conferência pendente. Não foi feito teste em outro computador.

## Resumo

| Situação | Quantidade |
|---|---|
| ok | todos os casos de erro pedidos |
| corrigido | 1 (Enter no campo de senha do login) |
| pendente | 2 (ver "Pendências"; as duas já têm decisão registrada) |

## Tabela por tela

### Login (`LoginPage`)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Usuário e senha vazios | ok: "Usuário ou senha inválidos." | ✔ | ☐ |
| Senha errada / usuário inexistente | ok: mesma mensagem, não entra | ✔ | ☐ |
| `admin` / `admin123` entra com o menu completo | ok | ✔ | ☐ |
| `vet` / `vet123` entra com **Painel, Estoque, Saída e Histórico** (consulta, mais o registro de saída) | ok (o menu é montado pelo perfil em `AppShell`) | ✔ | ✔ |
| Apertar **Enter** no campo de senha para entrar | **corrigido**: antes só o botão funcionava; agora o `Completed` da senha chama o mesmo método do botão | ✔ | ☐ |
| Sair e entrar com outro perfil: os dados continuam | ok (só a sessão termina) | ✔ | ☐ |

### Dashboard (Administrativo e Veterinário)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Abrir com os 134 produtos reais: 42 sem estoque, 0 nos demais cartões | ok (a planilha real não tem mínimo nem validade; veja "Observações") | ✔ | ☐ |
| "Precisam de atenção" mostra no máximo 8, os urgentes primeiro | ok | ✔ | ☐ |
| "Últimas movimentações" mostra as mais recentes do histórico real; a mensagem "Nenhuma movimentação registrada ainda." só aparece sem histórico | ok | ✔ | ☐ |
| Veterinário vê o Painel só com o atalho de **Saída** (sem o de Entrada) | ok | ✔ | ✔ |
| Volta a atualizar depois de uma entrada/saída | ok (recarrega em `OnAppearing`) | ✔ | ☐ |

### Estoque (Administrativo e Veterinário)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Busca por nome sem acento / em minúsculas ("injetavel") | ok (usa `ProdutoService.Pesquisar`) | ✔ | ☐ |
| Busca sem resultado | ok: "Nada para mostrar." | ✔ | ☐ |
| Filtros de categoria e de situação combinados | ok | ✔ | ☐ |
| Filtros "Abaixo do mínimo" e "Validade..." logo na abertura | ok, mas vazios (nenhum produto real tem mínimo/validade) | ✔ | ☐ |
| Lista com os 134 produtos rola e continua fluida | `CollectionView` virtualiza; só o F5 confirma a fluidez | — | ☐ |
| Nomes longos (até 107 caracteres) não ficam cortados | os rótulos quebram linha, mas só o F5 prova | — | ☐ |
| Nada cortado com a janela em 900x600 | só o F5 prova | — | ☐ |
| Veterinário vê a lista e a ficha (só com o botão de Saída), mas não tem menu de Produtos, Entrada, Relatórios, Gráficos nem Relatório por categoria | ok | ✔ | ✔ |
| Botão **Estoque crítico** (quantidade <= metade do mínimo) com a contagem; o selo aparece na lista e na ficha | ok (0 com os dados reais, que não têm mínimo) | ✔ | ✔ |

### Produtos

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Salvar com nome vazio | ok: "Informe o nome do produto." | ✔ | ☐ |
| Salvar sem categoria | ok: "Escolha a categoria." | ✔ | ☐ |
| Quantidade mínima com texto ("abc") | ok: "A quantidade mínima deve ser um número inteiro." | ✔ | ☐ |
| Quantidade mínima negativa (-5) | ok: "A quantidade mínima não pode ser negativa." (vem do Service) | ✔ | ☐ |
| Quantidade mínima vazia | ok: aceita (é opcional) | ✔ | ☐ |
| Editar um produto não altera a quantidade em estoque | ok (aviso na tela e o Service não mexe nisso) | ✔ | ☐ |
| Cadastrar produto novo e achá-lo na busca | ok | ✔ | ☐ |

### Entrada

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Registrar sem escolher produto | ok: "Escolha um produto." | ✔ | ☐ |
| Quantidade vazia / zero / negativa / texto | ok: "Informe uma quantidade válida, maior que zero." | ✔ | ☐ |
| Sem escolher a origem | ok: "Escolha a origem da entrada." | ✔ | ☐ |
| Campo **Identificação de origem** (antes "Número da AF") aparece em qualquer origem e é opcional | ok | ✔ | ✔ |
| Escolher "Revisar" na confirmação não grava nada | ok | ✔ | ☐ |
| Com validade: o saldo aparece separado no Estoque | ok | ✔ | ☐ |
| Entrada com **validade no passado** | ok no funcionamento, mas ver **Pendência 2** | ✔ | ☐ |
| Janela 900x600: formulário rola (está dentro de `ScrollView`) | o `ScrollView` existe; só o F5 confirma | — | ☐ |

### Saída e Ajuste de balanço

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Sem produto | ok: "Escolha um produto." | ✔ | ☐ |
| Quantidade vazia / zero / negativa / texto | ok: "Informe uma quantidade válida, maior que zero." | ✔ | ☐ |
| Sem destino | ok: "Informe o destino da saída." | ✔ | ☐ |
| Saída maior que o estoque | ok: "Estoque insuficiente: há apenas N unidade(s) de ..." e o estoque não muda | ✔ | ☐ |
| Ordem das conferências na tela: quantidade, destino, estoque | com destino vazio a mensagem é a do destino, mesmo com quantidade exagerada | ✔ | ☐ |
| Saída válida: mensagem mostra de qual validade saiu (FEFO) | ok | ✔ | ✔ |
| **Veterinário** registra uma saída (menu Saída ou botão na ficha); a tela não mostra o "Ajuste de balanço" para ele | ok (estoque 32 → 30 no teste) | ✔ | ✔ |
| Ajuste de balanço pelo Veterinário é recusado também na tela ("Só o perfil Administrativo faz ajuste de balanço.") | ok | ✔ | ☐ |
| Ajuste sem quantidade contada / texto / negativa | ok: "Informe a quantidade contada (zero ou mais)." | ✔ | ☐ |
| Ajuste sem diferença é registrado mesmo assim, com aviso na confirmação | ok | ✔ | ☐ |
| Texto "No sistema: N un. · diferença: ±X" aparece ao digitar | ok | ✔ | ☐ |

### Histórico

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Corrigir / Cancelar com motivo vazio | ok: alerta "Informe o motivo." e nada muda | ✔ | ☐ |
| Tocar em "Voltar" no pedido de motivo | ok: nada acontece | ✔ | ☐ |
| Cancelar entrada cujo produto já foi consumido | ok: "Não é possível desfazer esta entrada, pois parte do que entrou já foi utilizada." e continua Ativa | ✔ | ☐ |
| Cancelada/Corrigida continua na lista, com selo e "Motivo: ..." | ok | ✔ | ☐ |
| Veterinário vê o Histórico (100 de 713 por vez, com "Mostrar mais (613 restantes)") sem os botões Corrigir e Cancelar | ok | ✔ | ✔ |
| Movimentação já encerrada não mostra mais os botões | ok | ✔ | ☐ |
| Período com data inicial depois da final | ok: lista esvazia e o resumo mostra "A data inicial não pode ser depois da data final." | ✔ | ☐ |
| Filtros de tipo, situação, busca e período combinados | ok | ✔ | ☐ |
| Janela 900x600: linha de filtros e de período cabem | só o F5 prova | — | ☐ |

### Relatórios

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Os 6 relatórios abrem sem erro, mesmo sem movimentações | ok: "Nenhuma movimentação ativa neste período." no consumo | ✔ | ☐ |
| Período com data inicial depois da final (consumo, entradas, saídas) | ok: mensagem do Service no resumo, lista vazia | ✔ | ☐ |
| Movimentação cancelada/corrigida não entra no consumo | ok (só `Ativa` conta) | ✔ | ☐ |
| Filtro de categoria só aparece no consumo; período só nos 3 relatórios por período | ok | ✔ | ☐ |
| "Validade próxima" mostra a validade mais próxima do que existe | ok | ✔ | ☐ |

### Gráficos (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| A tela abre com o período Anual (2026), barras e "Estoque atual (unidades)": Medicamento 4.478 un. (92%) | ok | ✔ | ✔ |
| Trocar entre Barras, Pizza e Rosca | ok (o desenho muda; nenhum dado é recalculado na tela) | ✔ | ☐ |
| "Uso no período (saídas)" e "Produtos cadastrados" | ok | ✔ | ☐ |
| Cartão por categoria com Mais usado, Menos usado, produtos sem saída e % do uso total | ok | ✔ | ☐ |
| Produtos de uma categoria: botões "Mais usados" e "Menos usados" (10 produtos) | ok | ✔ | ☐ |
| Veterinário não tem o menu Gráficos | ok | ✔ | ✔ |

### Relatório por Categoria (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| A tela abre no ano (2026): 4.621 unidades usadas, 4 categorias com uso, Medicamento a mais usada (91%) | ok | ✔ | ✔ |
| Anterior/Próximo e Período personalizado (data inicial depois da final é recusada) | ok | ✔ | ☐ |
| **Gerar PDF** (janela "Salvar como" e abertura do arquivo) | o PDF é testado automaticamente (válido com dados reais e sem saídas); a janela do Windows só o F5 prova | ✔ | ☐ |

### Relatórios: Consumo no período

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Mês atual sem movimentação mostra onde está a última ("A última movimentação registrada é de setembro de 2026; use Anterior") | ok | ✔ | ✔ |
| Mensal, Trimestral, Anual e Personalizado, categoria, ordenação e busca | ok | ✔ | ☐ |
| **Gerar PDF** | igual ao Relatório por Categoria: PDF testado automaticamente; a janela só o F5 prova | ✔ | ☐ |

## Pendências (não são problema de tela: avisar o grupo)

### Pendência 1: erros de português nos nomes dos produtos (`Data/DadosCean.cs`)

Não corrigi porque o arquivo é da parte "dados do CEAN" e o comentário diz que os nomes seguem a planilha exatamente (e há testes sobre esses dados). Mesmo assim, estes nomes aparecem em todas as telas e na apresentação:

| Como está | Provável correto |
|---|---|
| `LIDOICAÍNA` (3 produtos) | LIDOCAÍNA |
| `CLUVALANATO` (2 produtos) | CLAVULANATO |
| `ENROFLAXACINO` (4 produtos) | ENROFLOXACINO |
| `BENZILPENINCILINA` | BENZILPENICILINA |
| `MELOXICAN` (2 produtos) | MELOXICAM |
| `SUPLEMETO` (2 produtos) | SUPLEMENTO |
| `PROTEÍCO` | PROTEICO |
| `CAPSULA` (10 produtos) | CÁPSULA |
| `VITAMINICO`, `ACIDO` | VITAMÍNICO, ÁCIDO |

**Como reproduzir:** abrir o app e ir em Estoque; buscar "meloxican" ou "lidoica".
**Decisão registrada:** os nomes ficam **exatamente como na planilha real do CEAN**, igual ao projeto completo apresentado no ACELERA. Corrigir exigiria mudar a fonte (a planilha) e os testes de `DadosCeanTests`. Se o grupo quiser outra decisão, a tabela acima é a lista do que trocar.

### Pendência 2: Entrada aceita validade que já passou

`MovimentacaoService.RegistrarEntrada` não valida a data de validade, então uma entrada com validade de ontem é aceita e o produto passa a aparecer como "Validade vencida".

**Como reproduzir:** Entrada → escolher um produto, quantidade 1, origem Doação → ligar "Este produto tem data de validade" → escolher uma data passada → Confirmar.
**O que acontece:** entra normalmente; Estoque e Dashboard mostram "Validade vencida".
**Decisão registrada:** a entrada com validade passada **continua aceita** (pode ser um produto que chegou vencido) e a tela de Entrada avisa antes de confirmar que ele vai aparecer como vencido.

## Observações (sem ação necessária)

- A planilha real do CEAN não tem estoque mínimo nem validade. Logo após abrir o app: Dashboard com 42 sem estoque e 0 nos outros cartões; os filtros "Abaixo do mínimo" e de validade só mostram algo depois que o mínimo é cadastrado (Produtos) ou que uma entrada com validade é feita. O roteiro de demonstração já prevê essa preparação.
- O mesmo conceito aparece com nomes parecidos: "Vencendo em breve" (cartão do Dashboard), "Vence em breve" (selo do produto) e "Validade próxima" (filtro do Estoque e relatório). É consistente o bastante para a banca; se sobrar tempo, vale unificar.
- Números com ponto de milhar ("1.000") são recusados como inválidos; digitar só os algarismos funciona.