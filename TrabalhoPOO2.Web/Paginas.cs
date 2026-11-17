using System.Globalization;
using System.Net;
using System.Text;

namespace TrabalhoPOO2.Web;

// Gera o HTML das páginas. Os ids/atributos data-* são os seletores usados pelo Selenium.
public static class Paginas
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private static string E(string? texto) => WebUtility.HtmlEncode(texto ?? "");

    public static string Inicio(List<Personagem> personagens, string? msg, string? erro)
    {
        var sb = new StringBuilder();

        sb.Append("""
            <section class="card">
              <h2>Criar personagem</h2>
              <form id="formCriar" method="post" action="/personagens" class="grade">
                <label>Nome <input id="nome" name="nome" autocomplete="off"></label>
                <label>Raça <input id="raca" name="raca" autocomplete="off"></label>
                <label>Alinhamento <input id="alinhamento" name="alinhamento" placeholder="Leal Bom, Neutro Mau..." autocomplete="off"></label>
                <label>Nível inicial (1-100) <input id="nivel" name="nivel" value="1" autocomplete="off"></label>
                <label>Classe
                  <select id="classe" name="classe">
                    <option value="1">Guerreiro</option>
                    <option value="2">Mago</option>
                    <option value="3">Druida</option>
                    <option value="4">Bardo</option>
                  </select>
                </label>
                <div class="acoes"><button id="btnCriar" type="submit">Criar personagem</button></div>
              </form>
            </section>
            """);

        sb.Append($"""
            <section class="card">
              <h2>Personagens cadastrados (<span id="totalPersonagens">{personagens.Count}</span>)</h2>
              <ul id="listaPersonagens">
            """);

        if (personagens.Count == 0)
            sb.Append("""<li class="vazio">Nenhum personagem cadastrado.</li>""");

        for (int i = 0; i < personagens.Count; i++)
        {
            var p = personagens[i];
            sb.Append($"""
                <li data-nome="{E(p.Nome)}"><a class="link-personagem" href="/personagem/{i}">{E(p.Nome)}</a>
                — {E(p.Classe)} nível {p.Nivel} ({E(p.Raca)})</li>
                """);
        }
        sb.Append("</ul></section>");

        return Layout("Personagens", "inicio", sb.ToString(), msg, erro);
    }

    public static string Personagem(int id, Personagem p, string? msg, string? erro)
    {
        var inv = p.Inventario;
        var sb = new StringBuilder();

        sb.Append($"""
            <p><a id="voltar" href="/">← Voltar para a lista</a></p>
            <section class="card">
              <h2 id="nomePersonagem">{E(p.Nome)}</h2>
              <dl class="status">
                <dt>Classe</dt><dd id="classePersonagem">{E(p.Classe)}</dd>
                <dt>Raça</dt><dd id="racaPersonagem">{E(p.Raca)}</dd>
                <dt>Alinhamento</dt><dd id="alinhamentoPersonagem">{E(p.Alinhamento)}</dd>
                <dt>Nível</dt><dd id="nivelPersonagem">{p.Nivel}</dd>
                <dt>XP</dt><dd id="xpPersonagem">{p.Experiencia} / {p.ExperienciaProximoNivel}</dd>
                <dt>Vida</dt><dd id="vidaPersonagem">{p.VidaAtual} / {p.VidaMaxima}</dd>
                <dt>Mana</dt><dd id="manaPersonagem">{p.ManaAtual} / {p.ManaMaxima}</dd>
                <dt>Força</dt><dd id="forcaPersonagem">{p.Forca}</dd>
                <dt>Destreza</dt><dd id="destrezaPersonagem">{p.Destreza}</dd>
                <dt>Constituição</dt><dd id="constituicaoPersonagem">{p.Constituicao}</dd>
                <dt>Inteligência</dt><dd id="inteligenciaPersonagem">{p.Inteligencia}</dd>
                <dt>Sabedoria</dt><dd id="sabedoriaPersonagem">{p.Sabedoria}</dd>
                <dt>Carisma</dt><dd id="carismaPersonagem">{p.Carisma}</dd>
              </dl>
              <form id="formXp" method="post" action="/personagem/{id}/xp" class="linha">
                <label>Experiência a adicionar <input id="qtdXp" name="xp" autocomplete="off"></label>
                <button id="btnAdicionarXp" type="submit">Adicionar XP</button>
              </form>
            </section>
            """);

        // Equipamentos
        sb.Append("""<section class="card"><h2>Equipamentos</h2><ul id="equipamentos">""");
        foreach (var slot in p.Equipamentos)
        {
            if (slot.Value == null)
            {
                sb.Append($"""<li data-slot="{E(slot.Key)}"><strong>{E(slot.Key)}:</strong> <span class="equipado vazio">—</span></li>""");
            }
            else
            {
                sb.Append($"""
                    <li data-slot="{E(slot.Key)}"><strong>{E(slot.Key)}:</strong> <span class="equipado">{E(slot.Value.Nome)}</span>
                      <form method="post" action="/personagem/{id}/desequipar" class="inline">
                        <input type="hidden" name="slot" value="{E(slot.Key)}">
                        <button type="submit" class="desequipar secundario">Desequipar</button>
                      </form>
                    </li>
                    """);
            }
        }
        sb.Append("</ul></section>");

        // Inventário
        sb.Append($"""
            <section class="card">
              <h2>Inventário</h2>
              <p>Peso: <span id="pesoInventario">{inv.PesoAtual.ToString("F1", PtBr)} / {inv.CapacidadeMaxima.ToString("F1", PtBr)} kg</span>
                 · Itens: <span id="qtdItens">{inv.QuantidadeItens}</span></p>
              <table id="tabelaInventario">
                <thead><tr><th>Nome</th><th>Tipo</th><th>Peso</th><th>Valor</th><th>Raridade</th><th>Ações</th></tr></thead>
                <tbody>
            """);

        foreach (var item in inv.GetItens())
        {
            string equipar = item is ItemEquipamento
                ? $"""
                  <form method="post" action="/personagem/{id}/equipar" class="inline">
                    <input type="hidden" name="item" value="{E(item.Nome)}">
                    <button type="submit" class="equipar">Equipar</button>
                  </form>
                  """
                : "";

            sb.Append($"""
                <tr data-item="{E(item.Nome)}">
                  <td class="nome">{E(item.Nome)}</td>
                  <td class="tipo">{E(item.Tipo)}</td>
                  <td>{item.Peso.ToString("F1", PtBr)} kg</td>
                  <td>{item.Valor.ToString("N2", PtBr)}</td>
                  <td>{E(item.Raridade)}</td>
                  <td>{equipar}
                    <form method="post" action="/personagem/{id}/remover" class="inline">
                      <input type="hidden" name="item" value="{E(item.Nome)}">
                      <button type="submit" class="remover secundario">Remover</button>
                    </form>
                  </td>
                </tr>
                """);
        }

        sb.Append($"""
                </tbody>
              </table>
              <form id="formItensExemplo" method="post" action="/personagem/{id}/itens/exemplo">
                <button id="btnItensExemplo" type="submit" class="secundario">Criar itens de exemplo</button>
              </form>
            </section>
            """);

        // Adicionar item (mesmos campos do menu de console)
        sb.Append($"""
            <section class="card">
              <h2>Adicionar item</h2>
              <form id="formItem" method="post" action="/personagem/{id}/itens" class="grade">
                <label>Tipo
                  <select id="tipoItem" name="tipo">
                    <option value="arma">Arma</option>
                    <option value="equipamento">Equipamento</option>
                    <option value="consumivel">Consumível</option>
                    <option value="municao">Munição</option>
                  </select>
                </label>
                <label>Nome <input id="nomeItem" name="nome" autocomplete="off"></label>
                <label>Peso (kg) <input id="pesoItem" name="peso" autocomplete="off"></label>
                <label>Valor <input id="valorItem" name="valor" autocomplete="off"></label>
                <label>Raridade
                  <select id="raridadeItem" name="raridade">
                    <option>Comum</option><option>Raro</option><option>Épico</option><option>Lendário</option>
                  </select>
                </label>
                <label>Slot (só equipamento) <input id="slotItem" name="slot" placeholder="Cabeça, Torso, Mãos, Pés" autocomplete="off"></label>
                <label>Bônus (ataque/defesa) <input id="bonusItem" name="bonus" value="0" autocomplete="off"></label>
                <label>Cargas / quantidade / alcance <input id="quantidadeItem" name="quantidade" value="1" autocomplete="off"></label>
                <label>Efeito / tipo de dano <input id="efeitoItem" name="efeito" autocomplete="off"></label>
                <div class="acoes"><button id="btnAdicionarItem" type="submit">Adicionar item</button></div>
              </form>
            </section>
            """);

        // Habilidades
        sb.Append("""<section class="card"><h2>Habilidades</h2><ul id="habilidades">""");
        foreach (var h in p.Habilidades)
            sb.Append($"""<li data-habilidade="{E(h.Nome)}"><strong>{E(h.Nome)}</strong> ({E(h.Tipo)}) — custo {h.CustoMana} de mana</li>""");
        sb.Append("</ul></section>");

        return Layout(p.Nome, "personagem", sb.ToString(), msg, erro);
    }

    private static string Layout(string titulo, string pagina, string corpo, string? msg, string? erro)
    {
        string mensagem = "";
        if (!string.IsNullOrEmpty(erro))
            mensagem = $"""<div id="mensagem" class="msg erro">{E(erro)}</div>""";
        else if (!string.IsNullOrEmpty(msg))
            mensagem = $"""<div id="mensagem" class="msg sucesso">{E(msg)}</div>""";

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{E(titulo)}} — RPG</title>
              <style>
                * { box-sizing: border-box; }
                body { margin: 0; font-family: "Segoe UI", system-ui, sans-serif; background: #f3f1ec; color: #222; }
                header { background: #2d2a32; color: #f3f1ec; padding: 14px 24px; }
                header h1 { margin: 0; font-size: 20px; font-weight: 600; }
                main { max-width: 960px; margin: 0 auto; padding: 20px 16px 40px; }
                .card { background: #fff; border: 1px solid #e0dcd2; border-radius: 8px; padding: 16px 20px; margin-bottom: 16px; }
                .card h2 { margin: 0 0 12px; font-size: 17px; }
                .grade { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 10px 14px; }
                .grade .acoes { grid-column: 1 / -1; }
                .linha { display: flex; gap: 10px; align-items: end; margin-top: 12px; }
                label { display: flex; flex-direction: column; font-size: 13px; color: #555; gap: 4px; }
                input, select { font: inherit; padding: 6px 8px; border: 1px solid #c9c4b8; border-radius: 5px; color: #222; }
                button { font: inherit; padding: 7px 14px; border: 0; border-radius: 5px; background: #6b3fa0; color: #fff; cursor: pointer; }
                button.secundario { background: #e9e5f2; color: #4a2d70; }
                form.inline { display: inline; }
                .msg { padding: 10px 14px; border-radius: 6px; margin-bottom: 16px; font-weight: 500; }
                .msg.sucesso { background: #e3f3e6; color: #1d5b2a; border: 1px solid #b7dcc0; }
                .msg.erro { background: #fbe6e4; color: #8a1f17; border: 1px solid #efbdb7; }
                .status { display: grid; grid-template-columns: max-content 1fr max-content 1fr; gap: 4px 14px; margin: 0; }
                .status dt { color: #666; } .status dd { margin: 0; font-weight: 600; }
                table { width: 100%; border-collapse: collapse; margin: 8px 0 12px; font-size: 14px; }
                th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid #eee; }
                th { color: #666; font-weight: 600; }
                ul { padding-left: 20px; margin: 0; } li { margin: 4px 0; }
                .vazio { color: #999; }
                a { color: #6b3fa0; }
              </style>
            </head>
            <body data-pagina="{{pagina}}">
              <header><h1>Sistema de Gerenciamento de Personagens de RPG</h1></header>
              <main>
                {{mensagem}}
                {{corpo}}
              </main>
            </body>
            </html>
            """;
    }
}
