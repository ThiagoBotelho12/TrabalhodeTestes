# Projeto de Testes — TMS (PUC Minas)

Sistema testado: **Sistema de Gerenciamento de Personagens de RPG** (`TrabalhoPOO2`, C# / .NET 8).

## Estrutura

| Pasta | O que é |
|---|---|
| `TrabalhoPOO2/` | Sistema original (app de console) |
| `TrabalhoPOO2.Tests/` | **10 testes unitários** (xUnit) — CT01 a CT10 |
| `TrabalhoPOO2.Web/` | Interface web mínima que chama as mesmas classes do sistema (necessária porque o Selenium só testa páginas web) |
| `TrabalhoPOO2.SeleniumTests/` | **Suíte de 10 testes funcionais automatizados** (Selenium WebDriver + Chrome) — CT11 a CT20 |
| `TrabalhodeTestes.sln` | Solução com os 4 projetos (abrir no Visual Studio) |

A interface web não reimplementa regras: cada botão chama `GerenciadorPersonagens`, as fábricas de personagem,
`Inventario`, `EquiparItem` etc., e mostra a mesma mensagem que o sistema escreve no console.

## Como rodar

Pré-requisitos: .NET SDK 8 e Google Chrome instalado (o Selenium baixa o ChromeDriver sozinho).

```powershell
# Testes unitários
dotnet test TrabalhoPOO2.Tests --logger "console;verbosity=normal"

# Testes Selenium (sobe o site sozinho e abre o Chrome)
dotnet test TrabalhoPOO2.SeleniumTests --logger "console;verbosity=normal"

# Para gravar o vídeo: deixa cada ação mais lenta (800 ms)
$env:SELENIUM_PAUSA_MS=800; dotnet test TrabalhoPOO2.SeleniumTests --logger "console;verbosity=normal"

# Abrir o site manualmente em http://localhost:5080
dotnet run --project TrabalhoPOO2.Web
```

No Visual Studio: abrir `TrabalhodeTestes.sln` → menu **Teste → Gerenciador de Testes** → Executar todos.

Opcionais: `SELENIUM_HEADLESS=1` roda sem abrir a janela do Chrome.

## Casos de teste automatizados

Resultado = execução de 16/11/2026. Os testes marcados "Falhou" falham por **defeito real do sistema** (ver tabela de defeitos).

### Unitários (técnica: automatizada — xUnit)

| CT | Objetivo | Entrada | Saída esperada | Resultado |
|---|---|---|---|---|
| CT01 | Rejeitar item com peso zero | `ItemConsumivel` peso 0 | `ArgumentException` com "Peso" | Passou |
| CT02 | Inventário recusa item mais pesado que a capacidade | Inventário 5 kg + espada 6 kg | Não adiciona; 0 itens | Passou |
| CT03 | XP suficiente sobe de nível | Guerreiro nível 1 + 100 XP | Nível 2, XP 0 | Passou |
| CT04 | Subir de nível mantém os itens | Guerreiro com poção + 100 XP | Inventário continua com 1 item | **Falhou** (D1) |
| CT05 | Equipar item com requisito "força" | Lâmina do Dragão, Guerreiro força 17 | Item equipado no slot Arma | **Falhou** (D2) |
| CT06 | Valor-limite: peso igual à capacidade | Inventário 5 kg + armadura 5 kg | Aceita; peso 5,0 | Passou |
| CT07 | Usar munição reduz quantidade, peso e valor | 20 flechas (2 kg, 40) usar 5 | 15 flechas, 1,5 kg, 30 | Passou |
| CT08 | Nível fora de 1..100 é rejeitado | Nível 0 e nível 101 | `PersonagemException` "Nível deve estar entre 1 e 100" | Passou |
| CT09 | Slot inexistente não pode sumir com o item | Anel com slot "Anel" | Retorna false e item fica no inventário | **Falhou** (D3) |
| CT10 | Idade considera se o aniversário já passou | Nascido em 31/12 há 20 anos | 19 anos (antes de 31/12) | **Falhou** (D4) |

### Funcionais com Selenium (técnica: automatizada — Selenium WebDriver)

Pré-condição de todos: site aberto e dados zerados (a suíte faz isso sozinha antes de cada teste).

| CT | Objetivo | Entrada | Saída esperada | Resultado |
|---|---|---|---|---|
| CT11 | Criar personagem válido | Thorin, Anão, Leal Bom, nível 1, Guerreiro | Ficha do Thorin aberta e nome na lista | Passou |
| CT12 | Nome com 1 caractere | Nome "T" | Erro "Nome deve ter pelo menos 2 caracteres" | Passou |
| CT13 | Nível acima do limite | Nível 101 | Erro "Nível deve estar entre 1 e 100" | Passou |
| CT14 | Adicionar XP para subir de nível | 100 XP | "subiu para o nível 2", XP 0 / 150 | Passou |
| CT15 | Adicionar consumível válido | Elixir de Vida, 0,5 kg, valor 40 | Item na tabela, 4 itens | Passou |
| CT16 | Item acima da capacidade | Armadura de 60 kg (tem 11,5/60) | Erro "Não há espaço suficiente", continua com 3 itens | Passou |
| CT17 | Remover item | Remover Poção de Cura | Some da tabela, 2 itens | Passou |
| CT18 | Equipar e desequipar arma | Espada Longa | Vai para o slot Arma e volta ao inventário | Passou |
| CT19 | Equipar Lâmina do Dragão (itens de exemplo) | Guerreiro força 17, destreza 13 | Lâmina equipada | **Falhou** (D2) — tela mostra "Requer força 10 (atual: 0)" |
| CT20 | Subir de nível mantém os itens | 100 XP com 3 itens iniciais | Nível 2 e 3 itens | **Falhou** (D1) — inventário fica com 0 itens |

## Defeitos encontrados pelos testes automatizados

| Defeito | Testes | Causa no código (`Program.cs`) | Proposta de solução |
|---|---|---|---|
| D1 — Subir de nível apaga o inventário | CT04, CT20 | `SubirNivel()` faz `Inventario = new Inventario(...)`, criando um inventário vazio | Aumentar a capacidade do inventário existente (ex.: método `AumentarCapacidade`) em vez de criar outro |
| D2 — Requisito "força" nunca é atendido | CT05, CT19 | `ObterValorAtributo` compara com `"forca"` (sem acento), mas os itens usam `"força"`; o valor cai no `_ => 0` | Remover acentos antes de comparar ou trocar as strings por um `enum` de atributos |
| D3 — Equipar com slot inválido some com o item | CT09 | `EquiparItem` remove o item do inventário antes de acessar `Equipamentos[item.Slot]`, que lança `KeyNotFoundException` | Validar `Equipamentos.ContainsKey(item.Slot)` antes de remover; no cadastro, aceitar só os slots válidos |
| D4 — Idade errada antes do aniversário | CT10 | `Idade => DateTime.Now.Year - DataNascimento.Year` ignora mês e dia | Subtrair 1 quando o aniversário do ano ainda não chegou |
