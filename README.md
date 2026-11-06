# SIGA-CEAN

Sistema de controle de estoque do **CEAN – Centro de Apoio aos Animais** (Cruzeiro-SP).
Projeto acadêmico do curso de Análise e Desenvolvimento de Sistemas (FATEC).

Aplicativo desktop feito em **C# com .NET 10 e .NET MAUI**, para **Windows**.
Os dados ficam **somente em memória**: ao fechar o programa, tudo volta ao estado inicial
(o estoque e o histórico reais do CEAN carregados pelo código). Não há banco de dados nem arquivos de dados.

## O que o sistema faz

- **Painel** (os dois perfis): o que precisa de atenção (sem estoque, abaixo do mínimo, vencidos, validade próxima);
  cada quadro abre o Estoque já filtrado.
- **Estoque**: busca por nome (ignora maiúsculas e acentos), filtro por categoria e botões de situação com a contagem
  de cada uma (sem estoque, estoque crítico, abaixo do mínimo, vencidos, validade próxima).
- **Ficha do produto**: situação, saldos por validade na ordem em que saem (FEFO) e últimas movimentações.
- **Produtos** (Administrativo): cadastro e edição, com recusa de produto duplicado. Categorias fixas: Medicamento,
  Insumo, Alimentar, Equipamento hospitalar, Higiene e Microchip.
- **Entrada** (Administrativo): origem (compra, concessão ou doação), identificação de origem (por exemplo, o número da
  AF de uma compra), documento (nota fiscal ou pedido) e validade opcional.
- **Saída** (os dois perfis): destino e quem retirou, ambos obrigatórios. A saída usa primeiro o que vence antes e a tela
  mostra de qual validade o produto vai sair antes de confirmar. Recusa saída maior que o estoque.
- **Ajuste de balanço** (Administrativo, na tela de Saída): corrige o estoque com base na contagem física.
- **Histórico** (os dois perfis consultam): busca por produto, retirante, destino ou documento; filtros por tipo, situação e
  período; mostra 100 por vez, com "Mostrar mais". Corrigir ou cancelar uma movimentação (só o Administrativo) exige
  motivo, mantém o registro no histórico e desfaz o efeito no estoque.
- **Relatórios** (Administrativo): Consumo no período (mensal, trimestral, anual ou personalizado, com média por mês,
  ordenação e **PDF**), Entradas, Saídas, Estoque disponível, Estoque baixo e Validade próxima.
- **Gráficos** (Administrativo): barras, pizza ou rosca por categoria (estoque atual, uso no período ou número de
  produtos), com o produto mais e o menos usado em cada categoria.
- **Relatório por categoria** (Administrativo): uso de cada categoria e seus produtos mais e menos usados, na tela e em
  **PDF**.
- Confirmação antes de gravar, login por usuário e botão Sair para trocar de usuário.

### Perfis

| Perfil | O que acessa |
|---|---|
| **Administrativo** | Tudo: Painel, Estoque, Produtos, Entrada, Saída (e ajuste de balanço), Histórico (corrigir e cancelar), Relatórios, Gráficos e Relatório por categoria |
| **Veterinário** | Consulta o Painel, o Estoque (com a ficha do produto) e o Histórico e **registra saídas**. Não cadastra produtos, não registra entrada, não faz ajuste, não corrige nem cancela e não vê relatórios nem gráficos |

## Requisitos

- Windows 10 (versão 19041 ou mais nova) ou Windows 11
- [.NET SDK 10](https://dotnet.microsoft.com/download) (o `global.json` fixa a versão 10.0.402 ou a mais nova da mesma família)
- Carga de trabalho do MAUI para Windows
- Internet no primeiro build, para baixar os pacotes NuGet
- Clone o projeto em uma pasta de caminho curto (por exemplo, `C:\SIGA-CEAN`): caminhos muito longos fazem o build do
  MAUI falhar no Windows

O Visual Studio é opcional. Se for usá-lo, a carga de trabalho
"Desenvolvimento de interface do usuário multiplataforma do .NET" já inclui o MAUI.

## Como rodar

```bash
# 1. conferir o SDK
dotnet --version

# 2. instalar a carga de trabalho do MAUI (uma vez só)
dotnet workload install maui-windows

# 3. clonar e compilar
git clone https://github.com/ricardovnd-77/SIGA-CEAN.git
cd SIGA-CEAN
dotnet build SigaCean.csproj

# 4. abrir o programa
bin\Debug\net10.0-windows10.0.19041.0\win-x64\SigaCean.exe
```

Também é possível abrir `SigaCean.slnx` no Visual Studio e executar com F5.
Se o build reclamar da carga de trabalho, rode o terminal como administrador e repita o passo 2.

## Login de teste

Usuários fictícios, criados só para demonstração:

| Usuário | Senha | Perfil |
|---|---|---|
| `admin` | `admin123` | Administrativo |
| `vet` | `vet123` | Veterinário |

## Testes automatizados

```bash
dotnet test SigaCean.Tests
```

São 344 testes das regras de estoque, validade, estoque mínimo, movimentações (inclusive correção e cancelamento),
busca, histórico, relatórios, gráficos (textos), PDFs, perfis e importação do histórico. Os Services também são
testados sozinhos (a tela não é a única barreira): saída sem "retirado por", produto duplicado e ids inexistentes são
recusados sem alterar nada.

## Dados de exemplo

Ao abrir, o app carrega os **134 produtos** e as quantidades da planilha real do CEAN (`Data/DadosCean.cs`) e o
**histórico real de 713 movimentações** (`Data/HistoricoCean.cs`), que só é registrado: não altera o estoque carregado.
A planilha não informa estoque mínimo nem validade, então os alertas de mínimo e de validade só aparecem depois que um
mínimo é cadastrado (Produtos) ou que uma entrada com validade é feita. Produto com quantidade zero aparece como
"SEM ESTOQUE". Os nomes dos produtos seguem exatamente a planilha.

## Estrutura

```
SIGA-CEAN
├── Models      → só dados (classes simples, sem regra)
├── Services    → as regras do sistema
├── Data        → AppData (listas em memória), DadosCean e HistoricoCean (dados reais)
├── Views       → as telas (XAML + code-behind) e os textos que elas mostram
├── Reports     → os PDFs (sem MAUI)
├── Resources   → estilos, cores, fontes e imagens
├── SigaCean.Tests → testes automatizados (xUnit)
├── docs        → ROTEIRO.md (demonstração) e REVISAO.md (revisão das telas)
├── App.xaml / App.xaml.cs   → início do programa (abre o login)
├── AppShell.xaml / .cs      → menu lateral (muda conforme o perfil)
└── MauiProgram.cs           → configuração e registro dos Services e telas
```

| Pasta | Responsabilidade |
|---|---|
| `Models` | Representar os dados: `Produto`, `Categoria`, `Movimentacao`, `Usuario` e os tipos de apoio. **Sem lógica.** |
| `Services` | Todas as regras de negócio. Validam e recusam o que é inválido (`InvalidOperationException`). |
| `Data` | `AppData` guarda `Produtos`, `Movimentacoes` e `Usuarios`. Só os Services mexem nas listas. |
| `Views` | Mostrar e coletar dados. **Não** têm regra de negócio: chamam um Service. |
| `Reports` | Montam os PDFs dos relatórios. |

### Os Services

| Service | O que faz |
|---|---|
| `UsuarioService` | Login/logout, perfil de quem está usando e quem pode registrar saída |
| `ProdutoService` | Cadastrar, consultar, alterar, pesquisar e filtrar produtos; recusa duplicados |
| `EstoqueService` | Situação do estoque (sem estoque, abaixo do mínimo, crítico, validade), saldos por validade e FEFO |
| `MovimentacaoService` | Entrada, saída, ajuste de balanço, histórico, período, corrigir e cancelar |
| `RelatorioService` | Consumo por período e por categoria, entradas, saídas, estoque disponível, estoque baixo e validade próxima |

## Regras que todo o código respeita

1. Entrada **aumenta** o estoque; saída **diminui**. O estoque **nunca** fica negativo.
2. Estoque baixo: `quantidade <= quantidade mínima` (exatamente no mínimo já conta); crítico: metade do mínimo ou menos;
   validade próxima: 30 dias. Essas contas existem **só** em `EstoqueService`.
3. Operação inválida **não altera nada** (nem o estoque, nem o histórico). O Service valida **antes** de alterar.
4. O Service valida sozinho: a tela também valida (para dar uma boa mensagem), mas não é a única barreira.
5. Saída usa o critério **FEFO**: sai primeiro o que vence primeiro; o que não tem validade sai por último.
6. Corrigir e cancelar **não apagam** a movimentação: ela continua no histórico, com o status e o motivo (obrigatório).
7. Entrada registra origem (compra, concessão ou doação), identificação de origem e documento quando houver; saída
   registra destino e quem retirou.
8. "Uso" nos gráficos e no relatório por categoria = saídas ativas no período; entradas, ajustes e movimentações
   corrigidas ou canceladas não contam.
9. **Sem controle de lote, sem código de barras, sem banco de dados, sem arquivos de dados, sem módulo veterinário.**
   A única coisa gravada em arquivo é o PDF que o usuário escolhe salvar.

## Combinados para o código

- Toda **tela nova** é registrada em `MauiProgram.cs` (`AddTransient`) e, se aparecer no menu, em `AppShell.xaml.cs`.
- Toda **regra nova** vai para um Service, nunca para o XAML ou o code-behind.
- Use os estilos de `Resources/Styles` (`PageTitle`, `PrimaryButton`, `FieldLabel`...) para as telas ficarem iguais.
- Mensagens ao usuário em português, claras, sem mostrar erro técnico.

## Decisões de arquitetura

- `Categoria` é um **enum** (conjunto pequeno e fechado), não uma classe nem uma lista.
- A validade fica **por entrada**, não por produto: `Produto.Saldos` guarda `SaldoValidade` (data + quantidade) e
  `Quantidade` é a soma. Assim dá para saber o que vence primeiro e o que já venceu. **Não é lote** (não existe código de lote).
- `Movimentacao` tem o tipo `AjusteBalanco` (conferência física, que existe na planilha real do CEAN), o `Status`
  (ativa, corrigida, cancelada) e os campos `RetiradoPor`, `Documento`, `IdentificacaoOrigem` e
  `MotivoCorrecaoOuCancelamento`.
- As informações de aquisição ficam na **entrada** (`Movimentacao`), não no produto.
- Só conta para a validade o que ainda existe em estoque: um saldo zerado, mesmo com data passada, não deixa o produto "vencido".
- Entrada com validade que já passou é aceita (o produto passa a aparecer como vencido) e a tela avisa antes de confirmar.

## Dependência externa

Os PDFs usam o [PDFsharp-MigraDoc](https://github.com/empira/PDFsharp) 6.2.4 (licença MIT), restaurado
automaticamente pelo `dotnet build`. Os gráficos usam só o `Microsoft.Maui.Graphics`, que já vem com o MAUI.
