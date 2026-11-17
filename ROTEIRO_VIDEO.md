# Roteiro do vídeo (máx. 10 min)

Antes de gravar: abra `TrabalhodeTestes.sln` no Visual Studio e deixe a pasta do projeto aberta no Explorador de Arquivos.
Gravar a tela: abra a Ferramenta de Captura → modo Gravar (ícone de câmera) → Novo → selecione a tela inteira → Iniciar.
Ative o microfone na barra da gravação para narrar ao vivo.

| Tempo | O que mostrar | O que falar |
|---|---|---|
| 0:00–0:40 | `README_TESTES.md` | Grupo, sistema escolhido (gerenciador de personagens de RPG em C#), e o que tem no projeto: 10 testes unitários, 10 testes Selenium e 10 manuais. |
| 0:40–2:30 | `TrabalhoPOO2.Tests/TestesUnitarios.cs` | Usamos xUnit. Mostrar CT06 (valor-limite: peso igual à capacidade), CT08 (dois valores-limite, 0 e 101, com `[Theory]`) e CT09 (equipar com slot inválido). Todos seguem o padrão Arrange / Act / Assert. |
| 2:30–3:30 | Duplo clique em `rodar_testes_unitarios.cmd` | Resultado: 6 passam e 4 falham. As falhas não são erro do teste: são defeitos reais do sistema (D1 a D4). |
| 3:30–4:30 | `TrabalhoPOO2.Web/SiteRpg.cs` | O sistema é de console e o Selenium só testa páginas web. Por isso criamos uma tela web mínima que chama as mesmas classes do sistema (GerenciadorPersonagens, Inventario, EquiparItem), sem reescrever regra nenhuma. |
| 4:30–5:30 | `TrabalhoPOO2.SeleniumTests/PaginaRpg.cs`, `SiteRpgFixture.cs` e o CT18 em `TestesFuncionaisSelenium.cs` | A fixture sobe o site e abre o Chrome. A `PaginaRpg` é um Page Object: concentra os seletores e as ações, e os testes ficam legíveis. |
| 5:30–8:30 | Duplo clique em `rodar_testes_selenium.cmd` (o Chrome abre sozinho) | Narrar cada teste conforme ele aparece: criar personagem, nome inválido, nível 101, XP, adicionar, remover e equipar item. No CT19, a tela mostra "Requer força 10 (atual: 0)" mesmo com o Guerreiro tendo força 17. No CT20, o inventário zera ao subir de nível. |
| 8:30–9:30 | Janela com o resultado (8 passaram, 2 falharam) + tabela de defeitos do README | Resumir os defeitos e as propostas de solução. |
