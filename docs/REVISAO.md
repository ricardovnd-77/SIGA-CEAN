# Revisão do app (SIGA-CEAN)

Revisão das telas do app na estrutura atual (`SigaCean.Maui/Presentation/Views`), feita em 06/11/2026.

## Como esta revisão foi feita

- **Leitura do código e dos testes** de cada tela e dos Services: para cada caso de erro da lista abaixo, conferi a
  mensagem que o app mostra e se existe teste automático de que o estoque fica intacto (219 testes em `SigaCean.Tests`).
  Isso está marcado com ✔ na coluna **Código**.
- **Teste manual (F5)**: ✔ na coluna **F5** significa que o caso foi conferido **no app rodando** em 06/11/2026; ☐ é
  conferência que ainda falta (de preferência em outro computador). Nada foi marcado ✔ sem ter sido visto na tela.
- Itens visuais (corte na janela mínima, fluidez da lista) e a janela "Salvar como" dos PDFs só se provam no F5.

Esperado em todos os casos: mensagem clara em português, sem travar e sem alterar o estoque.

## Resumo

| Situação | Quantidade |
|---|---|
| ok | todos os casos de erro listados |
| pendente de F5 | os itens com ☐ (principalmente PDFs e janela mínima) |
| pendências de decisão | 2, ambas com decisão registrada abaixo |

## Perfis e menu

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| `admin` / `admin123` entra com Painel, Estoque, Movimentações, Relatório, Gráficos e Rel. por categoria | ok | ✔ | ✔ |
| `vet` / `vet123` entra com Painel, Estoque e Movimentações, só para consulta, **e registra saídas** | ok | ✔ | ✔ |
| Veterinário vê só o botão **− Saída** (Painel, Estoque, Movimentações e ficha do produto) | ok | ✔ | ✔ |
| Veterinário não vê Entrada, Cadastrar produto, Ajuste de balanço, Editar, Corrigir/Cancelar nem Relatórios/Gráficos | ok | ✔ | ✔ |
| Saída feita pelo Veterinário aparece no histórico com "resp.: Dr. Veterinário (exemplo)" | ok | ✔ | ✔ |
| Sair e entrar com outro perfil: os dados continuam | ok (só a sessão termina) | ✔ | ✔ |

## Tabela por tela

### Login

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Usuário ou senha errados | ok: "Usuário ou senha inválidos." e não entra | ✔ | ✔ |
| Enter no campo de senha entra | ok | ✔ | ✔ |
| "Mostrar senha" | ok | ✔ | ☐ |

### Painel

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Abrir com os 134 produtos reais: 42 sem estoque, 0 nos demais quadros | ok (a planilha real não tem mínimo nem validade; ver "Observações") | ✔ | ✔ |
| "Últimas movimentações" mostra as mais recentes do histórico real (713 movimentações) | ok | ✔ | ✔ |
| Quadro abre o Estoque já filtrado; "Ver todas" abre Movimentações | ok | ✔ | ☐ |
| Atalhos **+ Entrada** e **− Saída** (Veterinário só vê a saída) | ok | ✔ | ✔ |

### Estoque

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Botões de situação com contagem: Todos, Sem estoque, **Estoque crítico**, Abaixo do mínimo, Validade próxima, Vencido | ok (134 / 42 / 0 / 0 / 0 / 0 com os dados reais) | ✔ | ✔ |
| Busca por nome sem acento / em minúsculas ("injetavel") | ok | ✔ | ☐ |
| Filtro de categoria combinado com a situação | ok | ✔ | ☐ |
| Botões de ação do Administrativo: Cadastrar produto, Entrada, Saída e Ajuste de balanço | ok | ✔ | ✔ |
| Lista com os 134 produtos rola e continua fluida | só o F5 prova | — | ☐ |
| Nomes longos não ficam cortados e nada corta com a janela em 900x600 | só o F5 prova | — | ☐ |

### Cadastrar e editar produto (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Salvar com nome vazio | ok: "Informe o nome do produto." | ✔ | ✔ |
| Salvar sem categoria | ok: "Selecione a categoria." | ✔ | ☐ |
| Estoque mínimo inválido | ok: "Informe um estoque mínimo válido." | ✔ | ☐ |
| Produto com o mesmo nome e apresentação | ok: "Já existe um produto ..." e nada é gravado | ✔ | ☐ |
| Editar um produto não altera a quantidade em estoque | ok | ✔ | ☐ |

### Ficha do produto

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Situação, saldos por validade na ordem em que saem (FEFO) e últimas movimentações | ok | ✔ | ✔ |
| Administrativo: **+ Entrada**, **− Saída** e **Editar produto**; Veterinário: só **− Saída** | ok | ✔ | ✔ |

### Entrada (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Registrar sem escolher produto da lista | ok: "Selecione um produto da lista." | ✔ | ☐ |
| Quantidade vazia / zero / negativa / texto | ok: "Informe uma quantidade válida." | ✔ | ☐ |
| Sem o tipo de origem | ok: "Selecione o tipo de origem." | ✔ | ☐ |
| Campo **Identificação de origem** (antes "Número AF") aparece em qualquer origem e é opcional | ok | ✔ | ✔ |
| Janela de confirmação mostra "Estoque: A → B"; escolher "Revisar" não grava | ok | ✔ | ✔ |
| Entrada com validade: o saldo aparece separado e na ordem FEFO | ok | ✔ | ☐ |
| Entrada com **validade no passado** | aceita; a tela avisa que o produto entra como vencido (ver **Pendência 2**) | ✔ | ☐ |

### Saída (os dois perfis)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Sem produto, sem destino, sem "retirado por" ou quantidade inválida | ok: "Selecione um produto da lista.", "Informe o destino.", "Informe quem está retirando o produto.", "Informe uma quantidade válida." | ✔ | ☐ |
| Saída maior que o estoque | ok: "Estoque insuficiente: há apenas N unidade(s) de ..." e o estoque não muda | ✔ | ☐ |
| Previsão do FEFO antes de confirmar ("Vai sair de: ...") | ok | ✔ | ☐ |
| Saída válida: confirmação com "Estoque: A → B" e faixa "Saída registrada ... Estoque agora: N" | ok (32 → 30 no teste) | ✔ | ✔ |
| Saída pelo Veterinário | ok | ✔ | ✔ |

### Ajuste de balanço (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Sem quantidade apurada | ok: "Informe a quantidade apurada na contagem." | ✔ | ☐ |
| Ajuste registrado | ok: faixa "Ajuste de balanço registrado ... Estoque agora: N" e o ajuste vai para o histórico | ✔ | ☐ |

### Movimentações

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Histórico real (713 movimentações), 100 por vez com "Mostrar mais" | ok | ✔ | ✔ |
| Filtros de tipo, situação, busca (produto, retirante, destino ou documento) e período combinados | ok | ✔ | ☐ |
| Movimentação cancelada continua na lista, com o selo "Cancelada" (a corrigida ganha o selo "Corrigida") | ok | ✔ | ✔ |
| Só o Administrativo toca numa movimentação ativa para corrigir ou cancelar (o Administrativo abre a tela; o toque do Veterinário não foi testado) | ok | ✔ | ☐ |

### Corrigir/Cancelar movimentação (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Sem motivo | ok: "Informe o motivo antes de continuar." e nada muda | ✔ | ✔ |
| Cancelar com motivo | ok: faixa "Movimentação cancelada ... O estoque foi atualizado." e a linha vira "Cancelada" | ✔ | ✔ |
| Cancelar entrada cujo produto já foi consumido | recusado, a movimentação continua Ativa | ✔ | ☐ |
| Corrigir com motivo | ok: o efeito é desfeito e a movimentação fica "Corrigida"; o lançamento certo é feito à mão | ✔ | ☐ |

### Relatório de Consumo (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| Mês atual sem movimentação mostra onde está a última ("A última movimentação registrada é de setembro de 2026; use Anterior") | ok | ✔ | ✔ |
| Mensal, Trimestral, Anual e Personalizado (data inicial depois da final é recusada), categoria e ordenação | ok | ✔ | ☐ |
| Só movimentação ativa entra no consumo | ok | ✔ | ☐ |
| **Gerar PDF** (janela "Salvar como" e abertura do arquivo) | o PDF é testado automaticamente (válido com dados reais e sem movimentações); a janela do Windows só o F5 prova | ✔ | ☐ |

### Gráficos (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| A tela abre com o período Anual (2026), barras e "Estoque atual (unidades)": Medicamento 4.478 un. (92%) | ok | ✔ | ✔ |
| Trocar entre Barras, Pizza e Rosca; "Uso no período" e "Produtos cadastrados" | ok | ✔ | ☐ |
| Cartão por categoria com Mais usado, Menos usado, produtos sem saída e % do uso total | ok | ✔ | ☐ |
| Produtos de uma categoria: "Mais usados" e "Menos usados" (10 produtos) | ok | ✔ | ☐ |

### Relatório por categoria (Administrativo)

| O que foi testado | Resultado | Código | F5 |
|---|---|---|---|
| A tela abre no ano (2026): 4.621 unidades usadas, 4 categorias com uso, Medicamento a mais usada (91%) | ok | ✔ | ✔ |
| **Gerar PDF** | igual ao Relatório de Consumo: PDF testado automaticamente; a janela só o F5 prova | ✔ | ☐ |

## Pendências (decisões registradas)

### Pendência 1: erros de português nos nomes dos produtos (`Infrastructure/DadosCean.cs`)

Os nomes seguem **exatamente a planilha real do CEAN** e aparecem assim em todas as telas. Alguns têm erros de digitação:

| Como está | Provável correto |
|---|---|
| `LIDOICAÍNA` | LIDOCAÍNA |
| `CLUVALANATO` | CLAVULANATO |
| `ENROFLAXACINO` | ENROFLOXACINO |
| `BENZILPENINCILINA` | BENZILPENICILINA |
| `MELOXICAN` | MELOXICAM |
| `SUPLEMETO` | SUPLEMENTO |
| `PROTEÍCO` | PROTEICO |
| `CAPSULA`, `VITAMINICO`, `ACIDO` | CÁPSULA, VITAMÍNICO, ÁCIDO |

**Decisão registrada:** os nomes ficam como na planilha. Corrigir exigiria mudar a fonte (a planilha) e conferir os testes
que dependem desses nomes. Se o grupo quiser outra decisão, a tabela acima é a lista do que trocar.

### Pendência 2: Entrada aceita validade que já passou

**Decisão registrada:** a entrada com validade passada **continua aceita** (pode ser um produto que chegou vencido). A tela
de Entrada avisa, na conferência, que o produto vai aparecer como vencido.

## Observações (sem ação necessária)

- A planilha real do CEAN não tem estoque mínimo nem validade. Logo após abrir o app: Painel com 42 sem estoque e 0 nos
  outros quadros; os filtros "Estoque crítico", "Abaixo do mínimo" e de validade só mostram algo depois que um mínimo é
  cadastrado (Estoque → ficha → Editar produto) ou que uma entrada com validade é feita. O roteiro de demonstração já prevê
  essa preparação.
- Números com ponto de milhar ("1.000") podem ser recusados como inválidos; digitar só os algarismos funciona.
- Os dados ficam só em memória: cancelamentos, entradas e saídas de teste somem ao fechar o programa.
