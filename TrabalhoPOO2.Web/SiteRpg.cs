using System.Globalization;

namespace TrabalhoPOO2.Web;

// ======================================================
// INTERFACE WEB MÍNIMA PARA OS TESTES COM SELENIUM
// ======================================================
// O sistema original (TrabalhoPOO2/Program.cs) é um app de console, e o Selenium
// só consegue testar páginas web. Esta interface NÃO reimplementa regras de negócio:
// cada ação chama as mesmas classes do sistema (GerenciadorPersonagens, fábricas de
// personagem, Inventario, EquiparItem...), então os testes funcionais exercitam o
// mesmo código do menu de console. As mensagens mostradas na página são as mesmas
// que o sistema escreve no console.
public static class SiteRpg
{
    public static void Main(string[] args)
    {
        string url = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5080";
        var app = Criar(args, url);
        Console.WriteLine($"Site do RPG rodando em {url} (Ctrl+C para parar)");
        app.Run();
    }

    // Usado pelo Main e pela suíte Selenium (que sobe o site sozinha antes dos testes)
    public static WebApplication Criar(string[] args, string? url = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        if (url != null)
            builder.WebHost.UseUrls(url);

        var app = builder.Build();
        var gerenciador = GerenciadorPersonagens.Instancia;

        // ---------- Página inicial: criar e listar personagens ----------
        app.MapGet("/", (string? msg, string? erro) =>
            Html(Paginas.Inicio(gerenciador.Personagens, msg, erro)));

        app.MapPost("/personagens", async (HttpRequest req) =>
        {
            var f = await req.ReadFormAsync();

            if (!int.TryParse(f["nivel"], out int nivel))
                return Ir("/", erro: "❌ Nível inicial deve ser um número inteiro");

            PersonagemFactory? factory = f["classe"].ToString() switch
            {
                "1" => new GuerreiroFactory(),
                "2" => new MagoFactory(),
                "3" => new DruidaFactory(),
                "4" => new BardoFactory(),
                _ => null
            };
            if (factory == null)
                return Ir("/", erro: "❌ Classe inválida");

            var (personagem, saida) = SaidaSistema.Executar(() =>
                gerenciador.CriarPersonagem(factory, f["nome"].ToString(), f["raca"].ToString(),
                                            f["alinhamento"].ToString(), nivel));

            if (personagem == null)
                return Ir("/", erro: SaidaSistema.Resumo(saida, sucesso: false));

            SaidaSistema.Executar(() => { ItensIniciais(personagem); return true; });
            gerenciador.PersonagemAtual = personagem;

            int id = gerenciador.Personagens.IndexOf(personagem);
            return Ir($"/personagem/{id}", msg: SaidaSistema.Resumo(saida, sucesso: true));
        });

        // ---------- Página do personagem ----------
        app.MapGet("/personagem/{id:int}", (int id, string? msg, string? erro) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");
            gerenciador.PersonagemAtual = p;
            return Html(Paginas.Personagem(id, p, msg, erro));
        });

        app.MapPost("/personagem/{id:int}/xp", async (int id, HttpRequest req) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");

            var f = await req.ReadFormAsync();
            if (!int.TryParse(f["xp"], out int xp))
                return Ir($"/personagem/{id}", erro: "❌ XP deve ser um número inteiro");

            return Executar($"/personagem/{id}", () => { p.AdicionarExperiencia(xp); return true; });
        });

        // ---------- Inventário ----------
        app.MapPost("/personagem/{id:int}/itens", async (int id, HttpRequest req) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");

            var f = await req.ReadFormAsync();
            return Executar($"/personagem/{id}", () => p.Inventario.AdicionarItem(CriarItem(f)));
        });

        app.MapPost("/personagem/{id:int}/itens/exemplo", (int id) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");

            SaidaSistema.Executar(() => { ItensExemplo(p.Inventario); return true; });
            return Ir($"/personagem/{id}", msg: "✅ Itens de exemplo adicionados ao inventário!");
        });

        app.MapPost("/personagem/{id:int}/remover", async (int id, HttpRequest req) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");

            var f = await req.ReadFormAsync();
            return Executar($"/personagem/{id}", () => p.Inventario.RemoverItem(f["item"].ToString()));
        });

        app.MapPost("/personagem/{id:int}/equipar", async (int id, HttpRequest req) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");

            var f = await req.ReadFormAsync();
            if (p.Inventario.BuscarItem(f["item"].ToString()) is not ItemEquipamento item)
                return Ir($"/personagem/{id}", erro: "❌ Item não encontrado ou não equipável");

            return Executar($"/personagem/{id}", () => p.EquiparItem(item));
        });

        app.MapPost("/personagem/{id:int}/desequipar", async (int id, HttpRequest req) =>
        {
            var p = Buscar(gerenciador, id);
            if (p == null) return Ir("/", erro: "❌ Personagem não encontrado");

            var f = await req.ReadFormAsync();
            return Executar($"/personagem/{id}", () => p.DesequiparItem(f["slot"].ToString()));
        });

        // ---------- Gancho de teste: limpa os dados em memória ----------
        app.MapGet("/reset", () =>
        {
            gerenciador.Personagens.Clear();
            gerenciador.PersonagemAtual = null!;
            return Results.Redirect("/");
        });

        return app;
    }

    // Executa uma ação do sistema e mostra a mensagem que o próprio sistema escreveu no console
    private static IResult Executar(string destino, Func<bool> acao)
    {
        try
        {
            var (ok, saida) = SaidaSistema.Executar(acao);
            string resumo = SaidaSistema.Resumo(saida, ok);
            return ok ? Ir(destino, msg: resumo) : Ir(destino, erro: resumo);
        }
        catch (Exception ex)
        {
            return Ir(destino, erro: $"❌ Erro: {ex.Message}");
        }
    }

    private static Personagem? Buscar(GerenciadorPersonagens g, int id) =>
        id >= 0 && id < g.Personagens.Count ? g.Personagens[id] : null;

    private static IResult Html(string conteudo) =>
        Results.Content(conteudo, "text/html; charset=utf-8");

    private static IResult Ir(string caminho, string? msg = null, string? erro = null)
    {
        string url = caminho;
        if (msg != null) url += "?msg=" + Uri.EscapeDataString(msg);
        else if (erro != null) url += "?erro=" + Uri.EscapeDataString(erro);
        return Results.Redirect(url);
    }

    // Monta o item com os mesmos construtores usados no menu "Adicionar item" do console
    private static IItemInventario CriarItem(IFormCollection f)
    {
        string nome = f["nome"].ToString();
        double peso = LerNumero(f["peso"], "Peso");
        decimal valor = (decimal)LerNumero(f["valor"], "Valor");
        string raridade = Texto(f["raridade"], "Comum");
        int bonus = LerInteiro(f["bonus"], 0);
        int quantidade = LerInteiro(f["quantidade"], 1);
        string efeito = f["efeito"].ToString();

        return f["tipo"].ToString() switch
        {
            "arma" => new ItemArma(nome, peso, valor, Texto(efeito, "Corte"), bonus, quantidade, raridade),
            "equipamento" => new ItemEquipamento(nome, peso, valor, f["slot"].ToString(), bonus, 0, raridade),
            "consumivel" => new ItemConsumivel(nome, peso, valor, efeito, quantidade, raridade),
            "municao" => new ItemMunicao(nome, peso, valor, quantidade, Texto(efeito, "Flecha"), raridade),
            _ => throw new ArgumentException("Tipo de item inválido")
        };
    }

    // Mesmos itens que o Menu.AdicionarItensIniciais() do console dá a cada classe
    private static void ItensIniciais(Personagem personagem)
    {
        switch (personagem.Classe)
        {
            case "Guerreiro":
                personagem.Inventario.AdicionarItem(new ItemArma("Espada Longa", 3.0, 50, "Corte", 5));
                personagem.Inventario.AdicionarItem(new ItemEquipamento("Armadura de Couro", 8.0, 30, "Torso", 3));
                personagem.Inventario.AdicionarItem(new ItemConsumivel("Poção de Cura", 0.5, 20, "Restaura 25 HP", 3));
                break;
            case "Mago":
                personagem.Inventario.AdicionarItem(new ItemArma("Cajado Arcano", 2.0, 80, "Mágico", 8, 3));
                personagem.Inventario.AdicionarItem(new ItemEquipamento("Túnica de Mago", 2.0, 40, "Torso", 1));
                personagem.Inventario.AdicionarItem(new ItemConsumivel("Poção de Mana", 0.5, 25, "Restaura 30 MP", 3));
                break;
            case "Druida":
                personagem.Inventario.AdicionarItem(new ItemArma("Foice Natural", 2.5, 45, "Corte", 6));
                personagem.Inventario.AdicionarItem(new ItemConsumivel("Sementes Mágicas", 0.3, 15, "Cresce plantas rapidamente", 5));
                break;
            case "Bardo":
                personagem.Inventario.AdicionarItem(new ItemArma("Alaúde", 1.5, 60, "Contusão", 4));
                personagem.Inventario.AdicionarItem(new ItemConsumivel("Pó de Fada", 0.2, 35, "Aumenta Carisma temporariamente", 2));
                break;
        }
    }

    // Mesmos itens que a opção 8 do menu de inventário (Menu.CriarItensExemplo) do console
    private static void ItensExemplo(Inventario inventario)
    {
        inventario.AdicionarItem(new ItemArma("Espada de Aço", 3.5, 120, "Corte", 8));
        inventario.AdicionarItem(new ItemArma("Arco Longo", 2.0, 80, "Perfuração", 6, 10));
        inventario.AdicionarItem(new ItemEquipamento("Elmo de Ferro", 2.5, 60, "Cabeça", 4));
        inventario.AdicionarItem(new ItemEquipamento("Botas de Couro", 1.5, 30, "Pés", 2));
        inventario.AdicionarItem(new ItemConsumivel("Poção de Cura Maior", 0.5, 50, "Restaura 50 HP", 2, "Raro"));
        inventario.AdicionarItem(new ItemConsumivel("Elixir de Força", 0.3, 75, "Aumenta Força em +5 por 1 hora", 1, "Épico"));
        inventario.AdicionarItem(new ItemMunicao("Flechas", 0.1, 2, 20, "Flecha", "Comum"));

        var espadaLendaria = new ItemArma("Lâmina do Dragão", 4.0, 1000, "Fogo", 25, 1, "Lendário");
        espadaLendaria.AdicionarRequisito("força");
        espadaLendaria.AdicionarRequisito("destreza");
        inventario.AdicionarItem(espadaLendaria);
    }

    // Aceita "0,5" e "0.5"
    private static double LerNumero(string? valor, string campo)
    {
        string texto = (valor ?? "").Trim().Replace(',', '.');
        if (!double.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out double numero))
            throw new ArgumentException($"{campo} deve ser um número");
        return numero;
    }

    private static int LerInteiro(string? valor, int padrao) =>
        int.TryParse((valor ?? "").Trim(), out int n) ? n : padrao;

    private static string Texto(string? valor, string padrao) =>
        string.IsNullOrWhiteSpace(valor) ? padrao : valor.Trim();
}

// Captura o que as classes do sistema escrevem no console durante uma ação,
// para a página mostrar exatamente a mesma mensagem que o menu de console mostraria.
public static class SaidaSistema
{
    private static readonly object trava = new();

    public static (T resultado, List<string> linhas) Executar<T>(Func<T> acao)
    {
        lock (trava)
        {
            var original = Console.Out;
            var buffer = new StringWriter();
            Console.SetOut(buffer);
            try
            {
                T resultado = acao();
                var linhas = buffer.ToString()
                    .Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .ToList();
                return (resultado, linhas);
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }

    public static string Resumo(List<string> linhas, bool sucesso)
    {
        if (!sucesso)
            return linhas.LastOrDefault(l => l.StartsWith("❌")) ?? linhas.LastOrDefault() ?? "❌ Operação não realizada";

        return linhas.LastOrDefault(l => l.Contains("subiu para o nível"))
            ?? linhas.LastOrDefault(l => l.StartsWith("✅"))
            ?? linhas.LastOrDefault()
            ?? "✅ Operação realizada";
    }
}
