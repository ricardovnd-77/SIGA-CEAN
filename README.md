# SIGA-CEAN (base para o grupo)

Sistema de controle de estoque do **CEAN – Centro de Apoio aos Animais** (Cruzeiro-SP), projeto acadêmico da FATEC.

Esta é a **estrutura inicial**: pastas, classes, armazenamento em memória e telas básicas. As funcionalidades ainda
**não** estão implementadas; cada uma está marcada com `TODO` no código e foi pensada para ser dividida entre o grupo.

Tecnologias: C#, .NET 10, .NET MAUI (Windows), XAML, POO. **Sem banco de dados e sem arquivos**: os dados existem só em
memória enquanto o programa roda (ao fechar, tudo se perde; isso é intencional).

## Como rodar

Requisitos: Windows 10 (19041+) ou 11, [.NET SDK 10](https://dotnet.microsoft.com/download) e a carga de trabalho do MAUI.

```bash
dotnet workload install maui-windows
dotnet build SigaCean.csproj
```

Depois abra `bin\Debug\net10.0-windows10.0.19041.0\win-x64\SigaCean.exe`, ou abra `SigaCean.slnx` no Visual Studio e
execute com F5.

Logins de teste (fictícios): `admin` / `admin123` (Administrativo) e `vet` / `vet123` (Veterinário).

## Estrutura

```
SIGA-CEAN
├── Models      → só dados (classes simples, sem regra)
├── Services    → as regras do sistema
├── Data        → AppData: as listas em memória
├── Views       → as telas (XAML + code-behind)
├── Resources   → estilos, cores, fontes e imagens
├── App.xaml / App.xaml.cs        → início do programa (abre o login)
├── AppShell.xaml / .cs           → menu lateral (muda conforme o perfil)
└── MauiProgram.cs                → configuração e registro dos Services e telas
```

| Pasta | Responsabilidade |
|---|---|
| `Models` | Representar os dados: `Produto`, `Categoria`, `Movimentacao`, `Usuario` e os tipos de apoio. **Sem lógica.** |
| `Services` | Todas as regras de negócio. Validam e recusam o que é inválido (`InvalidOperationException`). |
| `Data` | `AppData` guarda `Produtos`, `Movimentacoes` e `Usuarios`. Só os Services mexem nas listas. |
| `Views` | Mostrar e coletar dados. **Não** devem ter regra de negócio: chamam um Service. |
| `Resources` | Visual (cores, estilos). Já pronto. |

## Os Services

| Service | O que faz | Estado |
|---|---|---|
| `UsuarioService` | Login/logout e perfil de quem está usando | pronto |
| `ProdutoService` | Cadastrar, consultar, alterar, pesquisar, filtrar produtos | `Cadastrar`, `ObterTodos`, `ObterPorId` prontos; o resto é `TODO` |
| `EstoqueService` | Regras de situação (sem estoque, abaixo do mínimo, validade) e alteração da quantidade | regras de situação prontas; `AumentarEstoque`, `DiminuirEstoque` (FEFO) e `AtualizarQuantidade` são `TODO` |
| `MovimentacaoService` | Entrada, saída, ajuste de balanço, histórico, período, corrigir, cancelar | `ConsultarHistorico` pronto; o resto é `TODO` |
| `RelatorioService` | Relatório de consumo, entradas, saídas, estoque disponível, estoque baixo, validade próxima | tudo `TODO` |

## Regras que todo o código deve respeitar

1. Entrada **aumenta** o estoque; saída **diminui**. O estoque **nunca** fica negativo.
2. Estoque baixo: `quantidade <= quantidade mínima` (exatamente no mínimo já conta). Essa conta existe **só** em
   `EstoqueService`; telas e relatórios chamam o método, nunca repetem a conta.
3. Operação inválida **não altera nada** (nem o estoque, nem o histórico). O Service valida **antes** de alterar.
4. O Service valida sozinho: a tela também valida (para dar uma boa mensagem), mas não pode ser a única barreira.
5. Saída usa o critério **FEFO**: sai primeiro o que vence primeiro; o que não tem validade sai por último.
6. Corrigir e cancelar **não apagam** a movimentação: ela continua no histórico, com o status e o motivo (obrigatório).
7. Entrada registra origem (compra, concessão ou doação) e documento/AF quando houver; saída registra destino
   (e quem retirou).
8. Perfis: **Administrativo** faz tudo; **Veterinário** só consulta o estoque (não ganha permissão administrativa).
9. **Sem controle de lote, sem código de barras, sem banco de dados, sem arquivos, sem módulo veterinário.**

## Divisão sugerida em partes

Cada parte tem arquivos próprios, para o grupo mexer o mínimo possível nos mesmos arquivos. Preencham o responsável.

| # | Parte | Service | Tela | Responsável |
|---|---|---|---|---|
| 1 | Quantidade do estoque (**fazer primeiro**, as partes 3, 4 e 5 dependem dela) | `EstoqueService`: `AumentarEstoque`, `DiminuirEstoque`, `AtualizarQuantidade` | `EstoquePage` | |
| 2 | Produtos | `ProdutoService`: `Alterar`, `Pesquisar`, `Filtrar` | `ProdutosPage` | |
| 3 | Entrada | `MovimentacaoService.RegistrarEntrada` | `EntradaPage` | |
| 4 | Saída e ajuste de balanço | `MovimentacaoService.RegistrarSaida`, `RegistrarAjusteBalanco` | `SaidaPage` | |
| 5 | Histórico, correção e cancelamento | `MovimentacaoService.FiltrarPorPeriodo`, `Corrigir`, `Cancelar` | `HistoricoPage` | |
| 6 | Relatórios e Dashboard | `RelatorioService` | `RelatoriosPage`, `DashboardPage` | |

`MovimentacaoService` é usado pelas partes 3, 4 e 5: cada um mexe só no **seu método** e avisa o grupo antes de
mudar a assinatura de qualquer método.

## Combinados para o código

- Toda **tela nova** precisa ser registrada em `MauiProgram.cs` (`AddTransient`) e, se for aparecer no menu, em `AppShell.xaml.cs`.
- Toda **regra nova** vai para um Service, nunca para o XAML ou o code-behind.
- Use os estilos de `Resources/Styles` (`PageTitle`, `PrimaryButton`, `FieldLabel`...) para as telas ficarem iguais.
- Mensagens ao usuário em português, claras, sem mostrar erro técnico.
- Dados de exemplo (produtos e quantidades reais da planilha do CEAN) ainda não foram carregados. Podem entrar em uma
  tarefa à parte, dentro de `Data`.

## Decisões de arquitetura (vindas da auditoria do projeto anterior)

- `Categoria` é um **enum** (conjunto pequeno e fechado), não uma classe nem uma lista.
- A validade fica **por entrada**, não por produto: `Produto.Saldos` guarda `SaldoValidade` (data + quantidade) e
  `Quantidade` é a soma. Assim dá para saber o que vence primeiro e o que já venceu. **Não é lote** (não existe código de lote).
- `Movimentacao` ganhou o tipo `AjusteBalanco` (conferência física, que existe na planilha real do CEAN), o `Status`
  (ativa, corrigida, cancelada) e os campos `RetiradoPor`, `Documento` e `MotivoCorrecaoOuCancelamento`.
- As informações de aquisição ficam na **entrada** (`Movimentacao`), não no produto.
- Só conta para a validade o que ainda existe em estoque: um saldo zerado, mesmo com data passada, não deixa o produto "vencido".
- Se o Dashboard não tiver indicador realmente útil para o CEAN, a página pode ser removida (foi mantida porque estava no plano).
