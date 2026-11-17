using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// ======================================================
// PADRÕES DE PROJETO UTILIZADOS
// ======================================================
// 1. Factory Method - Para criação de personagens
// 2. Strategy - Para diferentes tipos de itens
// 3. Composite - Para inventário hierárquico
// 4. Singleton - Para gerenciamento do sistema

// ======================================================
// EXCEÇÕES PERSONALIZADAS
// ======================================================
public class PersonagemException : Exception
{
    public PersonagemException(string message) : base(message) { }
}

public class InventarioException : Exception
{
    public InventarioException(string message) : base(message) { }
}

// ======================================================
// PERSISTÊNCIA EM JSON
// ======================================================
public class Persistencia
{
    private string caminho = "personagens.json";

    public void Salvar(List<Personagem> personagens)
    {
        try
        {
            var opcoes = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true
            };

            string json = JsonSerializer.Serialize(personagens, opcoes);
            File.WriteAllText(caminho, json);
            Console.WriteLine("✅ Personagens salvos com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao salvar: {ex.Message}");
            throw new PersonagemException($"Falha ao salvar personagens: {ex.Message}");
        }
    }

    public List<Personagem> Carregar()
    {
        try
        {
            if (!File.Exists(caminho))
            {
                Console.WriteLine("ℹ️ Nenhum arquivo encontrado. Criando nova lista...");
                return new List<Personagem>();
            }

            string json = File.ReadAllText(caminho);
            var opcoes = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            };

            List<Personagem> lista = JsonSerializer.Deserialize<List<Personagem>>(json, opcoes);
            Console.WriteLine("✅ Personagens carregados com sucesso!");
            return lista ?? new List<Personagem>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ Erro no formato JSON: {ex.Message}");
            return new List<Personagem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao carregar: {ex.Message}");
            throw new PersonagemException($"Falha ao carregar personagens: {ex.Message}");
        }
    }
}

// ======================================================
// SISTEMA DE INVENTÁRIO (COMPOSITE PATTERN)
// ======================================================

// Interface para itens do inventário
public interface IItemInventario
{
    string Nome { get; }
    double Peso { get; }
    decimal Valor { get; }
    string Tipo { get; }
    string Raridade { get; }
    void ExibirDetalhes();
    bool PodeSerEquipado();
}

// Classe base para todos os itens
public abstract class Item : IItemInventario
{
    public string Nome { get; protected set; }
    public double Peso { get; protected set; }
    public decimal Valor { get; protected set; }
    public string Tipo { get; protected set; }
    public string Raridade { get; protected set; } // Comum, Raro, Épico, Lendário
    
    protected Item(string nome, double peso, decimal valor, string tipo, string raridade = "Comum")
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do item não pode ser vazio");
        if (peso <= 0)
            throw new ArgumentException("Peso deve ser maior que zero");
        if (valor < 0)
            throw new ArgumentException("Valor não pode ser negativo");

        Nome = nome;
        Peso = peso;
        Valor = valor;
        Tipo = tipo;
        Raridade = raridade;
    }
    
    public virtual void ExibirDetalhes()
    {
        Console.WriteLine($"  {Nome} [{Raridade}]");
        Console.WriteLine($"    Tipo: {Tipo}");
        Console.WriteLine($"    Peso: {Peso:F1} kg");
        Console.WriteLine($"    Valor: {Valor:C}");
    }
    
    public abstract bool PodeSerEquipado();
}

// Tipos específicos de itens (STRATEGY PATTERN)
public class ItemConsumivel : Item
{
    public int Cargas { get; private set; }
    public string Efeito { get; private set; }
    
    public ItemConsumivel(string nome, double peso, decimal valor, string efeito, int cargas = 1, string raridade = "Comum")
        : base(nome, peso, valor, "Consumível", raridade)
    {
        Efeito = efeito;
        Cargas = cargas;
    }
    
    public override void ExibirDetalhes()
    {
        base.ExibirDetalhes();
        Console.WriteLine($"    Efeito: {Efeito}");
        Console.WriteLine($"    Cargas: {Cargas}");
    }
    
    public override bool PodeSerEquipado() => false;
    
    public bool Usar()
    {
        if (Cargas > 0)
        {
            Cargas--;
            Console.WriteLine($"Usado {Nome}. Efeito: {Efeito}");
            return true;
        }
        Console.WriteLine($"{Nome} não tem mais cargas!");
        return false;
    }
}

public class ItemEquipamento : Item
{
    public string Slot { get; private set; } // Cabeça, Torso, Mãos, Pés, Arma
    public int BonusDefesa { get; private set; }
    public int BonusAtaque { get; private set; }
    public List<string> AtributosRequeridos { get; private set; }
    
    public ItemEquipamento(string nome, double peso, decimal valor, string slot, int bonusDefesa = 0, int bonusAtaque = 0, string raridade = "Comum")
        : base(nome, peso, valor, "Equipamento", raridade)
    {
        Slot = slot;
        BonusDefesa = bonusDefesa;
        BonusAtaque = bonusAtaque;
        AtributosRequeridos = new List<string>();
    }
    
    public override void ExibirDetalhes()
    {
        base.ExibirDetalhes();
        Console.WriteLine($"    Slot: {Slot}");
        if (BonusAtaque > 0) Console.WriteLine($"    Ataque: +{BonusAtaque}");
        if (BonusDefesa > 0) Console.WriteLine($"    Defesa: +{BonusDefesa}");
        if (AtributosRequeridos.Count > 0)
            Console.WriteLine($"    Requer: {string.Join(", ", AtributosRequeridos)}");
    }
    
    public override bool PodeSerEquipado() => true;
    
    public void AdicionarRequisito(string atributo)
    {
        AtributosRequeridos.Add(atributo);
    }
}

public class ItemArma : ItemEquipamento
{
    public string TipoDano { get; private set; } // Corte, Perfuração, Contusão, Mágico
    public int Alcance { get; private set; } // 1 = corpo a corpo, >1 = à distância
    
    public ItemArma(string nome, double peso, decimal valor, string tipoDano,int bonusAtaque = 0, int alcance = 1, string raridade = "Comum")
        : base(nome, peso, valor, "Arma", 0, bonusAtaque, raridade)
    {
        TipoDano = tipoDano;
        Alcance = alcance;
        Tipo = "Arma";
    }
    
    public override void ExibirDetalhes()
    {
        base.ExibirDetalhes();
        Console.WriteLine($"    Tipo de Dano: {TipoDano}");
        Console.WriteLine($"    Alcance: {Alcance}");
    }
}

public class ItemMunicao : Item
{
    public int Quantidade { get; private set; }
    public string Tipo { get; private set; }
    
    public ItemMunicao(string nome, double pesoUnitario, decimal valorUnitario,int quantidade, string tipo, string raridade = "Comum")
        : base(nome, pesoUnitario * quantidade, valorUnitario * quantidade, "Munição", raridade)
    {
        Quantidade = quantidade;
        Tipo = tipo;
    }
    
    public override void ExibirDetalhes()
    {
        base.ExibirDetalhes();
        Console.WriteLine($"    Quantidade: {Quantidade}");
        Console.WriteLine($"    Tipo: {Tipo}");
    }
    
    public bool Usar(int quantidade = 1)
    {
        if (Quantidade >= quantidade)
        {
            Quantidade -= quantidade;
            Peso -= (Peso / (Quantidade + quantidade)) * quantidade;
            Valor -= (Valor / (Quantidade + quantidade)) * quantidade;
            return true;
        }
        return false;
    }
    
    public override bool PodeSerEquipado() => false;
}

// Sistema de Inventário
public class Inventario
{
    private List<IItemInventario> itens;
    public double CapacidadeMaxima { get; private set; } // em kg
    public double PesoAtual => itens.Sum(item => item.Peso);
    public int QuantidadeItens => itens.Count;
    public decimal ValorTotal => itens.Sum(item => item.Valor);
    
    public Inventario(double capacidadeMaxima = 100.0)
    {
        if (capacidadeMaxima <= 0)
            throw new ArgumentException("Capacidade deve ser maior que zero");
        
        itens = new List<IItemInventario>();
        CapacidadeMaxima = capacidadeMaxima;
    }
    public void AtualizarCapacidade(double novaCapacidade)
    {
        if (novaCapacidade <= 0)
            throw new ArgumentException("Capacidade deve ser maior que zero");

        CapacidadeMaxima = novaCapacidade;
    }
    
    public bool AdicionarItem(IItemInventario item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        if (PesoAtual + item.Peso > CapacidadeMaxima)
        {
            Console.WriteLine($"❌ Não há espaço suficiente! Peso atual: {PesoAtual:F1}/{CapacidadeMaxima:F1} kg");
            return false;
        }
        
        itens.Add(item);
        Console.WriteLine($"✅ {item.Nome} adicionado ao inventário");
        return true;
    }
    
    public bool RemoverItem(string nomeItem)
    {
        var item = itens.FirstOrDefault(i => i.Nome.Equals(nomeItem, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            itens.Remove(item);
            Console.WriteLine($"✅ {item.Nome} removido do inventário");
            return true;
        }
        
        Console.WriteLine($"❌ Item '{nomeItem}' não encontrado");
        return false;
    }
    
    public IItemInventario BuscarItem(string nomeItem)
    {
        return itens.FirstOrDefault(i => i.Nome.Equals(nomeItem, StringComparison.OrdinalIgnoreCase));
    }
    
    public List<IItemInventario> BuscarPorTipo(string tipo)
    {
        return itens.Where(i => i.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    
    public List<IItemInventario> BuscarPorRaridade(string raridade)
    {
        return itens.Where(i => i.Raridade.Equals(raridade, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    
    public void ExibirInventario()
    {
        Console.WriteLine("\n=== INVENTÁRIO ===");
        Console.WriteLine($"Peso: {PesoAtual:F1}/{CapacidadeMaxima:F1} kg");
        Console.WriteLine($"Itens: {QuantidadeItens}");
        Console.WriteLine($"Valor total: {ValorTotal:C}");
        Console.WriteLine("==================");
        
        if (itens.Count == 0)
        {
            Console.WriteLine("Inventário vazio.");
            return;
        }
        
        // Agrupar por tipo
        var grupos = itens.GroupBy(i => i.Tipo);
        
        foreach (var grupo in grupos)
        {
            Console.WriteLine($"\n{grupo.Key.ToUpper()}:");
            foreach (var item in grupo)
            {
                item.ExibirDetalhes();
            }
        }
    }
    
    public void OrdenarPorNome() => itens = itens.OrderBy(i => i.Nome).ToList();
    public void OrdenarPorPeso() => itens = itens.OrderBy(i => i.Peso).ToList();
    public void OrdenarPorValor() => itens = itens.OrderByDescending(i => i.Valor).ToList();
    
    public List<IItemInventario> GetItens() => new List<IItemInventario>(itens);
}

// ======================================================
// HABILIDADES
// ======================================================
public class Habilidade
{
    public string Nome { get; private set; }
    public int PoderBase { get; private set; }
    public int CustoMana { get; private set; }
    public string Descricao { get; private set; }
    public string Tipo { get; private set; } // Ofensiva, Defensiva, Suporte
    public int NivelRequerido { get; private set; }
    
    public Habilidade(string nome, int poderBase, int custoMana, string descricao, string tipo, int nivelRequerido = 1)
    {
        Nome = nome;
        PoderBase = poderBase;
        CustoMana = custoMana;
        Descricao = descricao;
        Tipo = tipo;
        NivelRequerido = nivelRequerido;
    }
    
    public void ExibirDetalhes()
    {
        Console.WriteLine($"  🎯 {Nome} [{Tipo}]");
        Console.WriteLine($"    Poder: {PoderBase} | Custo de Mana: {CustoMana}");
        Console.WriteLine($"    Nível requerido: {NivelRequerido}");
        Console.WriteLine($"    Descrição: {Descricao}");
    }
}

// ======================================================
// CLASSE BASE PERSONAGEM (COM INVENTÁRIO)
// ======================================================
public abstract class Personagem
{
    // Propriedades básicas
    public string Nome { get; protected set; }
    public int Nivel { get; protected set; }
    public int Experiencia { get; protected set; }
    public int ExperienciaProximoNivel { get; protected set; }
    public string Classe { get; protected set; }
    public string Raca { get; protected set; }
    public string Alinhamento { get; protected set; }
    public string Historia { get; set; }
    
    // Atributos
    public int Forca { get; protected set; }
    public int Destreza { get; protected set; }
    public int Constituicao { get; protected set; }
    public int Inteligencia { get; protected set; }
    public int Sabedoria { get; protected set; }
    public int Carisma { get; protected set; }
    
    // Status
    public int VidaMaxima { get; protected set; }
    public int VidaAtual { get; protected set; }
    public int ManaMaxima { get; protected set; }
    public int ManaAtual { get; protected set; }
    
    // Características físicas
    public double Altura { get; set; }
    public double Peso { get; set; }
    public string CorCabelo { get; set; }
    public string CorOlhos { get; set; }
    public DateTime DataNascimento { get; set; }
    public int Idade => DateTime.Now.Year - DataNascimento.Year;
    
    // Sistemas
    public Inventario Inventario { get; protected set; }
    public List<Habilidade> Habilidades { get; protected set; }
    public Dictionary<string, ItemEquipamento> Equipamentos { get; protected set; }
    
    // Construtor protegido
    protected Personagem(string nome, string raca, string alinhamento, int nivel = 1)
    {
        ValidarDadosBasicos(nome, raca, alinhamento, nivel);
        
        Nome = nome;
        Raca = raca;
        Alinhamento = alinhamento;
        Nivel = nivel;
        Experiencia = 0;
        ExperienciaProximoNivel = CalcularExpProximoNivel();
        
        Inventario = new Inventario(50 + (nivel * 10)); // Capacidade aumenta com nível
        Habilidades = new List<Habilidade>();
        Equipamentos = new Dictionary<string, ItemEquipamento>
        {
            { "Cabeça", null },
            { "Torso", null },
            { "Mãos", null },
            { "Pés", null },
            { "Arma", null }
        };
        
        Historia = "";
        CorCabelo = "Não definido";
        CorOlhos = "Não definido";
        DataNascimento = DateTime.Now.AddYears(-20);
    }
    
    private void ValidarDadosBasicos(string nome, string raca, string alinhamento, int nivel)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 2)
            throw new PersonagemException("Nome deve ter pelo menos 2 caracteres");
        if (string.IsNullOrWhiteSpace(raca))
            throw new PersonagemException("Raça não pode ser vazia");
        if (string.IsNullOrWhiteSpace(alinhamento))
            throw new PersonagemException("Alinhamento não pode ser vazio");
        if (nivel < 1 || nivel > 100)
            throw new PersonagemException("Nível deve estar entre 1 e 100");
    }
    
    public virtual void ExibirStatus()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine($"👤 {Nome} - {Classe} {Nivel}");
        Console.WriteLine($"🏹 {Raca} | {Alinhamento} | {Idade} anos");
        Console.WriteLine(new string('-', 50));
        
        Console.WriteLine("❤️  STATUS:");
        Console.WriteLine($"  Vida: {VidaAtual}/{VidaMaxima}");
        Console.WriteLine($"  Mana: {ManaAtual}/{ManaMaxima}");
        Console.WriteLine($"  XP: {Experiencia}/{ExperienciaProximoNivel} ({(Experiencia/(double)ExperienciaProximoNivel)*100:F1}%)");
        
        Console.WriteLine("\n📊 ATRIBUTOS:");
        Console.WriteLine($"  Força: {Forca} | Destreza: {Destreza}");
        Console.WriteLine($"  Constituição: {Constituicao} | Inteligência: {Inteligencia}");
        Console.WriteLine($"  Sabedoria: {Sabedoria} | Carisma: {Carisma}");
        
        Console.WriteLine("\n📏 CARACTERÍSTICAS:");
        Console.WriteLine($"  Altura: {Altura:F2}m | Peso: {Peso:F1}kg");
        Console.WriteLine($"  Cabelo: {CorCabelo} | Olhos: {CorOlhos}");
        
        if (!string.IsNullOrWhiteSpace(Historia))
        {
            Console.WriteLine("\n📖 HISTÓRIA:");
            Console.WriteLine($"  {Historia}");
        }
    }
    
    public void ExibirEquipamentos()
    {
        Console.WriteLine("\n🛡️  EQUIPAMENTOS:");
        bool temEquipamento = false;
        
        foreach (var slot in Equipamentos)
        {
            if (slot.Value != null)
            {
                Console.WriteLine($"  {slot.Key}: {slot.Value.Nome}");
                temEquipamento = true;
            }
        }
        
        if (!temEquipamento)
            Console.WriteLine("  Nenhum equipamento equipado.");
    }
    
    public bool EquiparItem(ItemEquipamento item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        // Verificar requisitos
        foreach (var requisito in item.AtributosRequeridos)
        {
            int valorAtributo = ObterValorAtributo(requisito);
            if (valorAtributo < 10) // Exemplo: requer pelo menos 10 no atributo
            {
                Console.WriteLine($"❌ Requer {requisito} 10 (atual: {valorAtributo})");
                return false;
            }
        }
        
        // Remover item do inventário
        if (!Inventario.RemoverItem(item.Nome))
            return false;
        
        // Se já tem item equipado no slot, colocar no inventário
        if (Equipamentos[item.Slot] != null)
        {
            var itemAntigo = Equipamentos[item.Slot];
            Inventario.AdicionarItem(itemAntigo);
            Console.WriteLine($"↩️ {itemAntigo.Nome} movido para inventário");
        }
        
        // Equipar novo item
        Equipamentos[item.Slot] = item;
        Console.WriteLine($"✅ {item.Nome} equipado no slot {item.Slot}");
        
        // Atualizar status baseado no equipamento
        AtualizarStatusPorEquipamento();
        
        return true;
    }
    
    public bool DesequiparItem(string slot)
    {
        if (!Equipamentos.ContainsKey(slot))
        {
            Console.WriteLine($"❌ Slot '{slot}' inválido");
            return false;
        }
        
        var item = Equipamentos[slot];
        if (item == null)
        {
            Console.WriteLine($"❌ Nenhum item equipado no slot '{slot}'");
            return false;
        }
        
        // Tentar mover para inventário
        if (Inventario.AdicionarItem(item))
        {
            Equipamentos[slot] = null;
            Console.WriteLine($"✅ {item.Nome} desequipado e movido para inventário");
            AtualizarStatusPorEquipamento();
            return true;
        }
        
        Console.WriteLine($"❌ Sem espaço no inventário para {item.Nome}");
        return false;
    }
    
    protected virtual void AtualizarStatusPorEquipamento()
    {
        // Resetar bônus
        VidaMaxima = CalcularVidaMaxima();
        ManaMaxima = CalcularManaMaxima();
        
        // Aplicar bônus dos equipamentos
        foreach (var equipamento in Equipamentos.Values)
        {
            if (equipamento != null)
            {
                VidaMaxima += equipamento.BonusDefesa * 5; // Cada ponto de defesa = +5 HP
                // Outros bônus podem ser aplicados aqui
            }
        }
        
        // Garantir que vida atual não passe a máxima
        if (VidaAtual > VidaMaxima)
            VidaAtual = VidaMaxima;
        if (ManaAtual > ManaMaxima)
            ManaAtual = ManaMaxima;
    }
    
    protected abstract int CalcularVidaMaxima();
    protected abstract int CalcularManaMaxima();
    
    private int ObterValorAtributo(string atributo)
    {
        return atributo.ToLower() switch
        {
            "forca" => Forca,
            "destreza" => Destreza,
            "constituicao" => Constituicao,
            "inteligencia" => Inteligencia,
            "sabedoria" => Sabedoria,
            "carisma" => Carisma,
            _ => 0
        };
    }
    
    public void AdicionarHabilidade(Habilidade habilidade)
    {
        if (habilidade.NivelRequerido > Nivel)
        {
            Console.WriteLine($"❌ Requer nível {habilidade.NivelRequerido} (atual: {Nivel})");
            return;
        }
        
        Habilidades.Add(habilidade);
        Console.WriteLine($"✅ Habilidade '{habilidade.Nome}' aprendida!");
    }
    
    public void ExibirHabilidades()
    {
        Console.WriteLine($"\n🎯 HABILIDADES DE {Nome}:");
        
        if (Habilidades.Count == 0)
        {
            Console.WriteLine("  Nenhuma habilidade aprendida.");
            return;
        }
        
        var agrupadas = Habilidades.GroupBy(h => h.Tipo);
        foreach (var grupo in agrupadas)
        {
            Console.WriteLine($"\n  {grupo.Key.ToUpper()}:");
            foreach (var habilidade in grupo)
            {
                habilidade.ExibirDetalhes();
            }
        }
    }
    
    public void AdicionarExperiencia(int xp)
    {
        if (xp <= 0)
            throw new ArgumentException("XP deve ser positivo");
        
        Experiencia += xp;
        Console.WriteLine($"✨ +{xp} XP ganhos!");
        
        while (Experiencia >= ExperienciaProximoNivel && Nivel < 100)
        {
            SubirNivel();
        }
    }
    
    private void SubirNivel()
    {
        Nivel++;
        Experiencia -= ExperienciaProximoNivel;
        ExperienciaProximoNivel = CalcularExpProximoNivel();

        Console.WriteLine($"\n🎉 {Nome} subiu para o nível {Nivel}!");
        Console.WriteLine("🎲 Role os dados para aumentar atributos...");

        AumentarAtributosAleatoriamente();
        Inventario.AtualizarCapacidade(CalcularCapacidadeInventario(Nivel));
        RestaurarVidaEMana();
    }

    private void AumentarAtributosAleatoriamente()
    {
        Random rand = new Random();
        Forca += rand.Next(1, 4);
        Destreza += rand.Next(1, 4);
        Constituicao += rand.Next(1, 4);
        Inteligencia += rand.Next(1, 4);
        Sabedoria += rand.Next(1, 4);
        Carisma += rand.Next(1, 4);
    }

    private static double CalcularCapacidadeInventario(int nivel)
    {
        return 50 + (nivel * 10);
    }

    private void RestaurarVidaEMana()
    {
        VidaAtual = VidaMaxima = CalcularVidaMaxima();
        ManaAtual = ManaMaxima = CalcularManaMaxima();
        AtualizarStatusPorEquipamento();
    }
    
    private int CalcularExpProximoNivel()
    {
        return (int)(100 * Math.Pow(1.5, Nivel - 1));
    }
    
    public abstract void Atacar();
    public abstract void UsarHabilidadeEspecial();
}

// ======================================================
// FACTORY METHOD PARA CRIAÇÃO DE PERSONAGENS
// ======================================================
public abstract class PersonagemFactory
{
    public abstract Personagem CriarPersonagem(string nome, string raca, string alinhamento, int nivel);
}

public class GuerreiroFactory : PersonagemFactory
{
    public override Personagem CriarPersonagem(string nome, string raca, string alinhamento, int nivel)
    {
        return new Guerreiro(nome, raca, alinhamento, nivel);
    }
}

public class MagoFactory : PersonagemFactory
{
    public override Personagem CriarPersonagem(string nome, string raca, string alinhamento, int nivel)
    {
        return new Mago(nome, raca, alinhamento, nivel);
    }
}

public class DruidaFactory : PersonagemFactory
{
    public override Personagem CriarPersonagem(string nome, string raca, string alinhamento, int nivel)
    {
        return new Druida(nome, raca, alinhamento, nivel);
    }
}

public class BardoFactory : PersonagemFactory
{
    public override Personagem CriarPersonagem(string nome, string raca, string alinhamento, int nivel)
    {
        return new Bardo(nome, raca, alinhamento, nivel);
    }
}

// ======================================================
// CLASSES ESPECÍFICAS DE PERSONAGENS
// ======================================================

public class Guerreiro : Personagem
{
    public Guerreiro(string nome, string raca, string alinhamento, int nivel = 1)
        : base(nome, raca, alinhamento, nivel)
    {
        Classe = "Guerreiro";
        Forca = 16 + nivel;
        Destreza = 12 + nivel;
        Constituicao = 14 + nivel;
        Inteligencia = 8 + nivel;
        Sabedoria = 10 + nivel;
        Carisma = 10 + nivel;
        
        VidaAtual = VidaMaxima = CalcularVidaMaxima();
        ManaAtual = ManaMaxima = CalcularManaMaxima();
        
        // Habilidades iniciais
        AdicionarHabilidade(new Habilidade("Golpe Poderoso", 15, 10, "Um golpe devastador com arma pesada", "Ofensiva", 1));
         AdicionarHabilidade(new Habilidade("Recuperar Folego", 0, 1, "Recuperar o Fôlego é uma habilidade que representa uma reserva limitada de resistência física e mental que o guerreiro pode usar para se proteger do perigo. No seu turno, você pode usar uma ação bônus para recuperar pontos de vida. ", "Suporte", 1));
         AdicionarHabilidade(new Habilidade("Pele Endurecida", 0, 5, "Aumenta defesa por um turno", "Defensiva", 2));
    }
    
    protected override int CalcularVidaMaxima() => (Constituicao * 3) + (Nivel * 10);
    protected override int CalcularManaMaxima() => (Inteligencia * 1) + (Nivel * 5);
    
    public override void Atacar()
    {
        Console.WriteLine($"⚔️ {Nome} Usa seus musculos como se fossem fibras de metal Elfico e: ataca com força pura!");
    }
    
    public override void UsarHabilidadeEspecial()
    {
        Console.WriteLine($"🔥 {Nome} usa da raiva com gatilho e como se estivisse em chamas usa: Fúria Berserker!");
    }
}

public class Mago : Personagem
{
    public Mago(string nome, string raca, string alinhamento, int nivel = 1)
        : base(nome, raca, alinhamento, nivel)
    {
        Classe = "Mago";
        Forca = 8 + nivel;
        Destreza = 10 + nivel;
        Constituicao = 12 + nivel;
        Inteligencia = 16 + nivel;
        Sabedoria = 14 + nivel;
        Carisma = 12 + nivel;
        
        VidaAtual = VidaMaxima = CalcularVidaMaxima();
        ManaAtual = ManaMaxima = CalcularManaMaxima();
        
        // Habilidades iniciais
        AdicionarHabilidade(new Habilidade("Conjuração Elemental", 20, 15, "O mago abilidoso usa da sua proeficiencia e vinculo com um ou mais elementos naturais e dobra as leis da existencia comforme sua vontade", "Ofensiva", 1));
        AdicionarHabilidade(new Habilidade("Recuperação Arcana", 0, 1, "Permite recuperar um pouco da energia mágica (slots de magia) após um descanso curto", "Suporte", 1));
        AdicionarHabilidade(new Habilidade("Escudo Arcano", 0, 10, "Cria uma barreira mágica de proteção", "Defensiva", 2));
    }
    
    protected override int CalcularVidaMaxima() => (Constituicao * 2) + (Nivel * 6);
    protected override int CalcularManaMaxima() => (Inteligencia * 4) + (Nivel * 15);
    
    public override void Atacar()
    {
        Console.WriteLine($"🔮 {Nome} Usa de seu grimorio para canalizar a energia vital que corre em seu corpo e lança um feitiço arcano!");
    }
    
    public override void UsarHabilidadeEspecial()
    {
        Console.WriteLine($"🌀 {Nome} Usa de um feitiço proibido e faz uma conjunçaõ de tempo e espaço trazendo rochas do espaço devastanto tudo em sua frente!");
    }
}

public class Druida : Personagem
{
    public Druida(string nome, string raca, string alinhamento, int nivel = 1)
        : base(nome, raca, alinhamento, nivel)
    {
        Classe = "Druida";
        Forca = 10 + nivel;
        Destreza = 12 + nivel;
        Constituicao = 14 + nivel;
        Inteligencia = 12 + nivel;
        Sabedoria = 16 + nivel;
        Carisma = 14 + nivel;
        
        VidaAtual = VidaMaxima = CalcularVidaMaxima();
        ManaAtual = ManaMaxima = CalcularManaMaxima();
        
        // Habilidades iniciais
        AdicionarHabilidade(new Habilidade("Benção Elemental", 18, 15, "Usa seu vinculo com a natureza e seu Deuses naturais criando uma pequena calamidade elemental", "Ofensiva", 1));
        AdicionarHabilidade(new Habilidade("Druidico", 0, 0, "Conhece a língua secreta dos druidas e consegue entender e se comunicar com animais e criaturas", "Suporte", 1));
        AdicionarHabilidade(new Habilidade("Forma Selvagem", 0, 20, "permitindo transformar-se em diversas feras para lutar alterando sua forma de lutar, investigar ou se deslocar, mantendo sua mente e memória.", "Suporte", 2));
    }
    
    protected override int CalcularVidaMaxima() => (Constituicao * 3) + (Nivel * 8);
    protected override int CalcularManaMaxima() => (Sabedoria * 3) + (Nivel * 12);
    
    public override void Atacar()
    {
        Console.WriteLine($"🌿 {Nome} Se torna um com a natureza causando dano elemental ao seu adiversario");
    }
    
    public override void UsarHabilidadeEspecial()
    {
        Console.WriteLine($"🐺 {Nome} Usa sua benção ao maximo se tornando um Urso gigante com capacidade de invocar nevascar e mordidas com temperaturas muito abaixo de zero");
    }
}

public class Bardo : Personagem
{
    public Bardo(string nome, string raca, string alinhamento, int nivel = 1)
        : base(nome, raca, alinhamento, nivel)
    {
        Classe = "Bardo";
        Forca = 8 + nivel;
        Destreza = 14 + nivel;
        Constituicao = 12 + nivel;
        Inteligencia = 12 + nivel;
        Sabedoria = 10 + nivel;
        Carisma = 16 + nivel;
        
        VidaAtual = VidaMaxima = CalcularVidaMaxima();
        ManaAtual = ManaMaxima = CalcularManaMaxima();
        
        // Habilidades iniciais
        AdicionarHabilidade(new Habilidade("Canção Inspiradora", 0, 8, "Aumenta moral dos aliados", "Suporte", 1));
        AdicionarHabilidade(new Habilidade("Melodia Atordoante", 12, 10, "Toca uma música que confunde inimigos", "Ofensiva", 2));
    }
    
    protected override int CalcularVidaMaxima() => (Constituicao * 2) + (Nivel * 7);
    protected override int CalcularManaMaxima() => (Carisma * 3) + (Nivel * 10);
    
    public override void Atacar()
    {
        Console.WriteLine($"🎵 {Nome} ataca com uma melodia cortante!");
    }
    
    public override void UsarHabilidadeEspecial()
    {
        Console.WriteLine($"🎼 {Nome} executa a Sinfonia do Caos!");
    }
}

// ======================================================
// SINGLETON PARA GERENCIAMENTO DO SISTEMA
// ======================================================
public class GerenciadorPersonagens
{
    private static GerenciadorPersonagens _instancia;
    public static GerenciadorPersonagens Instancia => _instancia ??= new GerenciadorPersonagens();
    
    public List<Personagem> Personagens { get; private set; }
    public Personagem PersonagemAtual { get; set; }
    private Persistencia persistencia;
    
    private GerenciadorPersonagens()
    {
        Personagens = new List<Personagem>();
        persistencia = new Persistencia();
    }
    
    public void CarregarPersonagens()
    {
        Personagens = persistencia.Carregar();
    }
    
    public void SalvarPersonagens()
    {
        persistencia.Salvar(Personagens);
    }
    
    public Personagem CriarPersonagem(PersonagemFactory factory, string nome, string raca, string alinhamento, int nivel)
    {
        try
        {
            var personagem = factory.CriarPersonagem(nome, raca, alinhamento, nivel);
            Personagens.Add(personagem);
            Console.WriteLine($"✅ Personagem {nome} criado com sucesso!");
            return personagem;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao criar personagem: {ex.Message}");
            return null;
        }
    }
}

// ======================================================
// MENU INTERATIVO ATUALIZADO
// ======================================================
public class Menu
{
    private GerenciadorPersonagens gerenciador;
    
    public Menu()
    {
        gerenciador = GerenciadorPersonagens.Instancia;
    }
    
    public void Iniciar()
    {
        Console.Title = "Sistema de Gerenciamento de Personagens de RPG";
        gerenciador.CarregarPersonagens();
        
        int opcao = -1;
        
        while (opcao != 0)
        {
            Console.Clear();
            ExibirCabecalho();
            ExibirMenuPrincipal();
            
            try
            {
                opcao = LerOpcao();
                ProcessarOpcaoPrincipal(opcao);
            }
            catch (FormatException)
            {
                Console.WriteLine("\n❌ Opção inválida! Digite um número.");
                Aguardar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Erro: {ex.Message}");
                Aguardar();
            }
        }
        
        Console.WriteLine("\n💾 Salvando personagens...");
        gerenciador.SalvarPersonagens();
        Console.WriteLine("👋 Até logo!");
    }
    
    private void ExibirCabecalho()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         SISTEMA DE GERENCIAMENTO DE PERSONAGENS RPG     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine($"Personagens cadastrados: {gerenciador.Personagens.Count}");
        if (gerenciador.PersonagemAtual != null)
            Console.WriteLine($"Personagem atual: {gerenciador.PersonagemAtual.Nome}");
        Console.WriteLine();
    }
    
    private void ExibirMenuPrincipal()
    {
        Console.WriteLine("📋 MENU PRINCIPAL:");
        Console.WriteLine("1. 👤 Criar novo personagem");
        Console.WriteLine("2. 📋 Listar todos os personagens");
        Console.WriteLine("3. 🔍 Selecionar personagem atual");
        Console.WriteLine("4. 🛠️  Gerenciar personagem atual");
        Console.WriteLine("5. 🎒 Gerenciar inventário");
        Console.WriteLine("6. 💾 Salvar personagens");
        Console.WriteLine("7. 📂 Carregar personagens");
        Console.WriteLine("0. 🚪 Sair");
        Console.Write("\n➡️  Escolha uma opção: ");
    }
    
    private int LerOpcao()
    {
        string input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException();
        
        return int.Parse(input);
    }
    
    private void ProcessarOpcaoPrincipal(int opcao)
    {
        switch (opcao)
        {
            case 1: CriarPersonagem(); break;
            case 2: ListarPersonagens(); break;
            case 3: SelecionarPersonagem(); break;
            case 4: if (gerenciador.PersonagemAtual != null) MenuGerenciarPersonagem(); break;
            case 5: if (gerenciador.PersonagemAtual != null) MenuInventario(); break;
            case 6: gerenciador.SalvarPersonagens(); Aguardar(); break;
            case 7: gerenciador.CarregarPersonagens(); Aguardar(); break;
            case 0: break;
            default: Console.WriteLine("❌ Opção inválida!"); Aguardar(); break;
        }
    }
    
    private void CriarPersonagem()
    {
        Console.Clear();
        ExibirCabecalho();
        Console.WriteLine("👤 CRIAR NOVO PERSONAGEM\n");
        
        try
        {
            Console.Write("Nome: ");
            string nome = Console.ReadLine();
            
            Console.Write("Raça: ");
            string raca = Console.ReadLine();
            
            Console.Write("Alinhamento (ex: Leal Bom, Neutro Mau): ");
            string alinhamento = Console.ReadLine();
            
            Console.Write("Nível inicial (1-100): ");
            int nivel = int.Parse(Console.ReadLine());
            
            Console.WriteLine("\n🎭 Escolha a classe:");
            Console.WriteLine("1. 🛡️  Guerreiro");
            Console.WriteLine("2. 🔮 Mago");
            Console.WriteLine("3. 🌿 Druida");
            Console.WriteLine("4. 🎵 Bardo");
            Console.Write("Opção: ");
            
            int classeOpcao = int.Parse(Console.ReadLine());
            PersonagemFactory factory = classeOpcao switch
            {
                1 => new GuerreiroFactory(),
                2 => new MagoFactory(),
                3 => new DruidaFactory(),
                4 => new BardoFactory(),
                _ => throw new PersonagemException("Classe inválida")
            };
            
            var personagem = gerenciador.CriarPersonagem(factory, nome, raca, alinhamento, nivel);
            if (personagem != null)
            {
                // Configurar características adicionais
                Console.WriteLine("\n📝 Configuração adicional:");
                
                Console.Write("Altura (m): ");
                personagem.Altura = double.Parse(Console.ReadLine());
                
                Console.Write("Peso (kg): ");
                personagem.Peso = double.Parse(Console.ReadLine());
                
                Console.Write("Cor do cabelo: ");
                personagem.CorCabelo = Console.ReadLine();
                
                Console.Write("Cor dos olhos: ");
                personagem.CorOlhos = Console.ReadLine();
                
                Console.Write("Data de nascimento (dd/mm/aaaa): ");
                personagem.DataNascimento = DateTime.Parse(Console.ReadLine());
                
                Console.Write("História/background (opcional): ");
                personagem.Historia = Console.ReadLine();
                
                // Adicionar itens iniciais
                AdicionarItensIniciais(personagem);
                
                gerenciador.PersonagemAtual = personagem;
                Console.WriteLine($"\n✅ Personagem '{nome}' criado e selecionado como atual!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro: {ex.Message}");
        }
        
        Aguardar();
    }
    
    private void AdicionarItensIniciais(Personagem personagem)
    {
        // Itens iniciais baseados na classe
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
    
    private void ListarPersonagens()
    {
        Console.Clear();
        ExibirCabecalho();
        Console.WriteLine("📋 LISTA DE PERSONAGENS\n");
        
        if (gerenciador.Personagens.Count == 0)
        {
            Console.WriteLine("Nenhum personagem cadastrado.");
        }
        else
        {
            for (int i = 0; i < gerenciador.Personagens.Count; i++)
            {
                var p = gerenciador.Personagens[i];
                Console.WriteLine($"{i + 1}. {p.Nome} - {p.Classe} Nível {p.Nivel} ({p.Raca})");
            }
        }
        
        Aguardar();
    }
    
    private void SelecionarPersonagem()
    {
        Console.Clear();
        ExibirCabecalho();
        Console.WriteLine("🔍 SELECIONAR PERSONAGEM\n");
        
        if (gerenciador.Personagens.Count == 0)
        {
            Console.WriteLine("Nenhum personagem cadastrado.");
            Aguardar();
            return;
        }
        
        for (int i = 0; i < gerenciador.Personagens.Count; i++)
        {
            var p = gerenciador.Personagens[i];
            Console.WriteLine($"{i + 1}. {p.Nome} - {p.Classe} Nível {p.Nivel}");
        }
        
        Console.Write("\n➡️  Escolha o número do personagem: ");
        int escolha = int.Parse(Console.ReadLine()) - 1;
        
        if (escolha >= 0 && escolha < gerenciador.Personagens.Count)
        {
            gerenciador.PersonagemAtual = gerenciador.Personagens[escolha];
            Console.WriteLine($"\n✅ Personagem '{gerenciador.PersonagemAtual.Nome}' selecionado!");
        }
        else
        {
            Console.WriteLine("\n❌ Número inválido!");
        }
        
        Aguardar();
    }
    
    private void MenuGerenciarPersonagem()
    {
        int opcao = -1;
        
        while (opcao != 0)
        {
            Console.Clear();
            ExibirCabecalho();
            Console.WriteLine($"🛠️  GERENCIANDO: {gerenciador.PersonagemAtual.Nome}\n");
            
            Console.WriteLine("1. 📊 Exibir status completo");
            Console.WriteLine("2. 🛡️  Exibir equipamentos");
            Console.WriteLine("3. 🎯 Exibir habilidades");
            Console.WriteLine("4. ✨ Adicionar experiência");
            Console.WriteLine("5. 📝 Editar características");
            Console.WriteLine("6. 🎲 Rolar dados de atributos");
            Console.WriteLine("0. ↩️  Voltar ao menu principal");
            Console.Write("\n➡️  Escolha uma opção: ");
            
            opcao = int.Parse(Console.ReadLine());
            
            switch (opcao)
            {
                case 1: gerenciador.PersonagemAtual.ExibirStatus(); Aguardar(); break;
                case 2: gerenciador.PersonagemAtual.ExibirEquipamentos(); Aguardar(); break;
                case 3: gerenciador.PersonagemAtual.ExibirHabilidades(); Aguardar(); break;
                case 4: AdicionarExperiencia(); break;
                case 5: EditarCaracteristicas(); break;
                case 6: RolarAtributos(); break;
                case 0: break;
                default: Console.WriteLine("❌ Opção inválida!"); Aguardar(); break;
            }
        }
    }
    
    private void AdicionarExperiencia()
    {
        Console.Write("\nQuantidade de XP a adicionar: ");
        int xp = int.Parse(Console.ReadLine());
        gerenciador.PersonagemAtual.AdicionarExperiencia(xp);
        Aguardar();
    }
    
    private void EditarCaracteristicas()
    {
        Console.WriteLine("\n📝 EDITAR CARACTERÍSTICAS:");
        Console.Write("Nova altura (m): ");
        gerenciador.PersonagemAtual.Altura = double.Parse(Console.ReadLine());
        
        Console.Write("Novo peso (kg): ");
        gerenciador.PersonagemAtual.Peso = double.Parse(Console.ReadLine());
        
        Console.Write("Nova cor do cabelo: ");
        gerenciador.PersonagemAtual.CorCabelo = Console.ReadLine();
        
        Console.Write("Nova cor dos olhos: ");
        gerenciador.PersonagemAtual.CorOlhos = Console.ReadLine();
        
        Console.Write("Nova história/background: ");
        gerenciador.PersonagemAtual.Historia = Console.ReadLine();
        
        Console.WriteLine("\n✅ Características atualizadas!");
        Aguardar();
    }
    
    private void RolarAtributos()
    {
        Console.WriteLine("\n🎲 ROLANDO ATRIBUTOS...");
        Random rand = new Random();
        
        Console.WriteLine($"Força: {rand.Next(1, 7) + rand.Next(1, 7) + rand.Next(1, 7)}");
        Console.WriteLine($"Destreza: {rand.Next(1, 7) + rand.Next(1, 7) + rand.Next(1, 7)}");
        Console.WriteLine($"Constituição: {rand.Next(1, 7) + rand.Next(1, 7) + rand.Next(1, 7)}");
        Console.WriteLine($"Inteligência: {rand.Next(1, 7) + rand.Next(1, 7) + rand.Next(1, 7)}");
        Console.WriteLine($"Sabedoria: {rand.Next(1, 7) + rand.Next(1, 7) + rand.Next(1, 7)}");
        Console.WriteLine($"Carisma: {rand.Next(1, 7) + rand.Next(1, 7) + rand.Next(1, 7)}");
        
        Aguardar();
    }
    
    private void MenuInventario()
    {
        int opcao = -1;
        
        while (opcao != 0)
        {
            Console.Clear();
            ExibirCabecalho();
            Console.WriteLine($"🎒 INVENTÁRIO DE {gerenciador.PersonagemAtual.Nome}\n");
            
            // Exibir resumo do inventário
            var inventario = gerenciador.PersonagemAtual.Inventario;
            Console.WriteLine($"📦 Capacidade: {inventario.PesoAtual:F1}/{inventario.CapacidadeMaxima:F1} kg");
            Console.WriteLine($"💰 Valor total: {inventario.ValorTotal:C}");
            Console.WriteLine($"🔢 Itens: {inventario.QuantidadeItens}");
            Console.WriteLine();
            
            Console.WriteLine("1. 📋 Exibir inventário completo");
            Console.WriteLine("2. ➕ Adicionar item");
            Console.WriteLine("3. ➖ Remover item");
            Console.WriteLine("4. 🔍 Buscar item");
            Console.WriteLine("5. 🛡️  Equipar item");
            Console.WriteLine("6. 📦 Desequipar item");
            Console.WriteLine("7. 📊 Ordenar inventário");
            Console.WriteLine("8. 🎁 Criar itens de exemplo");
            Console.WriteLine("0. ↩️  Voltar ao menu principal");
            Console.Write("\n➡️  Escolha uma opção: ");
            
            opcao = int.Parse(Console.ReadLine());
            
            switch (opcao)
            {
                case 1: inventario.ExibirInventario(); Aguardar(); break;
                case 2: AdicionarItemInventario(); break;
                case 3: RemoverItemInventario(); break;
                case 4: BuscarItemInventario(); break;
                case 5: EquiparItem(); break;
                case 6: DesequiparItem(); break;
                case 7: OrdenarInventario(); break;
                case 8: CriarItensExemplo(); break;
                case 0: break;
                default: Console.WriteLine("❌ Opção inválida!"); Aguardar(); break;
            }
        }
    }
    
    private void AdicionarItemInventario()
    {
        Console.WriteLine("\n➕ ADICIONAR ITEM:");
        Console.WriteLine("Tipo de item:");
        Console.WriteLine("1. 🗡️  Arma");
        Console.WriteLine("2. 🛡️  Equipamento");
        Console.WriteLine("3. 🧪 Consumível");
        Console.WriteLine("4. 🎯 Munição");

        int tipo = LerInteiro("Opção: ");
        string nome = LerTexto("Nome: ");
        double peso = double.Parse(LerTexto("Peso (kg): "));
        decimal valor = decimal.Parse(LerTexto("Valor: "));
        string raridade = LerTexto("Raridade (Comum, Raro, Épico, Lendário): ");

        IItemInventario item = CriarItem(tipo, nome, peso, valor, raridade);

        if (item != null && gerenciador.PersonagemAtual.Inventario.AdicionarItem(item))
            Console.WriteLine("✅ Item adicionado com sucesso!");

        Aguardar();
    }

    private string LerTexto(string pergunta)
    {
        Console.Write(pergunta);
        return Console.ReadLine();
    }

    private int LerInteiro(string pergunta)
    {
        return int.Parse(LerTexto(pergunta));
    }

    private IItemInventario CriarItem(int tipo, string nome, double peso, decimal valor, string raridade)
    {
        switch (tipo)
        {
            case 1: return CriarArma(nome, peso, valor, raridade);
            case 2: return CriarEquipamento(nome, peso, valor, raridade);
            case 3: return CriarConsumivel(nome, peso, valor, raridade);
            case 4: return CriarMunicao(nome, peso, valor, raridade);
            default: return null;
        }
    }

    private ItemArma CriarArma(string nome, double peso, decimal valor, string raridade)
    {
        string tipoDano = LerTexto("Tipo de dano: ");
        int bonusAtaque = LerInteiro("Bônus de ataque: ");
        int alcance = LerInteiro("Alcance: ");
        return new ItemArma(nome, peso, valor, tipoDano, bonusAtaque, alcance, raridade);
    }

    private ItemEquipamento CriarEquipamento(string nome, double peso, decimal valor, string raridade)
    {
        string slot = LerTexto("Slot (Cabeça, Torso, Mãos, Pés): ");
        int bonusDefesa = LerInteiro("Bônus de defesa: ");
        return new ItemEquipamento(nome, peso, valor, slot, bonusDefesa, 0, raridade);
    }

    private ItemConsumivel CriarConsumivel(string nome, double peso, decimal valor, string raridade)
    {
        string efeito = LerTexto("Efeito: ");
        int cargas = LerInteiro("Cargas: ");
        return new ItemConsumivel(nome, peso, valor, efeito, cargas, raridade);
    }

    private ItemMunicao CriarMunicao(string nome, double peso, decimal valor, string raridade)
    {
        string tipoMunicao = LerTexto("Tipo: ");
        int quantidade = LerInteiro("Quantidade: ");
        return new ItemMunicao(nome, peso, valor, quantidade, tipoMunicao, raridade);
    }
    
    private void RemoverItemInventario()
    {
        Console.Write("\nNome do item a remover: ");
        string nome = Console.ReadLine();
        
        if (gerenciador.PersonagemAtual.Inventario.RemoverItem(nome))
            Console.WriteLine("✅ Item removido com sucesso!");
        
        Aguardar();
    }
    
    private void BuscarItemInventario()
    {
        Console.WriteLine("\n🔍 BUSCAR ITEM:");
        Console.WriteLine("1. Por nome");
        Console.WriteLine("2. Por tipo");
        Console.WriteLine("3. Por raridade");
        Console.Write("Opção: ");
        
        int opcao = int.Parse(Console.ReadLine());
        List<IItemInventario> resultados = new List<IItemInventario>();
        
        switch (opcao)
        {
            case 1:
                Console.Write("Nome: ");
                string nome = Console.ReadLine();
                var item = gerenciador.PersonagemAtual.Inventario.BuscarItem(nome);
                if (item != null) resultados.Add(item);
                break;
                
            case 2:
                Console.Write("Tipo: ");
                string tipo = Console.ReadLine();
                resultados = gerenciador.PersonagemAtual.Inventario.BuscarPorTipo(tipo);
                break;
                
            case 3:
                Console.Write("Raridade: ");
                string raridade = Console.ReadLine();
                resultados = gerenciador.PersonagemAtual.Inventario.BuscarPorRaridade(raridade);
                break;
        }
        
        Console.WriteLine($"\n🔎 Resultados: {resultados.Count} item(s) encontrado(s)");
        foreach (var i in resultados)
            i.ExibirDetalhes();
        
        Aguardar();
    }
    
    private void EquiparItem()
    {
        var equipaveis = gerenciador.PersonagemAtual.Inventario
            .GetItens()
            .Where(i => i is ItemEquipamento)
            .Cast<ItemEquipamento>()
            .ToList();
        
        if (equipaveis.Count == 0)
        {
            Console.WriteLine("\n❌ Nenhum item equipável no inventário.");
            Aguardar();
            return;
        }
        
        Console.WriteLine("\n🛡️  ITENS EQUIPÁVEIS:");
        for (int i = 0; i < equipaveis.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {equipaveis[i].Nome} ({equipaveis[i].Slot})");
        }
        
        Console.Write("\n➡️  Escolha o item para equipar: ");
        int escolha = int.Parse(Console.ReadLine()) - 1;
        
        if (escolha >= 0 && escolha < equipaveis.Count)
        {
            gerenciador.PersonagemAtual.EquiparItem(equipaveis[escolha]);
        }
        else
        {
            Console.WriteLine("❌ Escolha inválida!");
        }
        
        Aguardar();
    }
    
    private void DesequiparItem()
    {
        Console.WriteLine("\n📦 DESEQUIPAR ITEM:");
        var slotsComItens = gerenciador.PersonagemAtual.Equipamentos
            .Where(kv => kv.Value != null)
            .ToList();
        
        if (slotsComItens.Count == 0)
        {
            Console.WriteLine("❌ Nenhum item equipado.");
            Aguardar();
            return;
        }
        
        for (int i = 0; i < slotsComItens.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {slotsComItens[i].Key}: {slotsComItens[i].Value.Nome}");
        }
        
        Console.Write("\n➡️  Escolha o slot para desequipar: ");
        int escolha = int.Parse(Console.ReadLine()) - 1;
        
        if (escolha >= 0 && escolha < slotsComItens.Count)
        {
            gerenciador.PersonagemAtual.DesequiparItem(slotsComItens[escolha].Key);
        }
        else
        {
            Console.WriteLine("❌ Escolha inválida!");
        }
        
        Aguardar();
    }
    
    private void OrdenarInventario()
    {
        Console.WriteLine("\n📊 ORDENAR INVENTÁRIO:");
        Console.WriteLine("1. Por nome (A-Z)");
        Console.WriteLine("2. Por peso (leve-pesado)");
        Console.WriteLine("3. Por valor (alto-baixo)");
        Console.Write("Opção: ");
        
        int opcao = int.Parse(Console.ReadLine());
        
        switch (opcao)
        {
            case 1: gerenciador.PersonagemAtual.Inventario.OrdenarPorNome(); break;
            case 2: gerenciador.PersonagemAtual.Inventario.OrdenarPorPeso(); break;
            case 3: gerenciador.PersonagemAtual.Inventario.OrdenarPorValor(); break;
        }
        
        Console.WriteLine("✅ Inventário ordenado!");
        Aguardar();
    }
    
    private void CriarItensExemplo()
    {
        Console.WriteLine("\n🎁 CRIANDO ITENS DE EXEMPLO...");
        
        var inventario = gerenciador.PersonagemAtual.Inventario;
        
        // Armas
        inventario.AdicionarItem(new ItemArma("Espada de Aço", 3.5, 120, "Corte", 8));
        inventario.AdicionarItem(new ItemArma("Arco Longo", 2.0, 80, "Perfuração", 6, 10));
        
        // Equipamentos
        inventario.AdicionarItem(new ItemEquipamento("Elmo de Ferro", 2.5, 60, "Cabeça", 4));
        inventario.AdicionarItem(new ItemEquipamento("Botas de Couro", 1.5, 30, "Pés", 2));
        
        // Consumíveis
        inventario.AdicionarItem(new ItemConsumivel("Poção de Cura Maior", 0.5, 50, "Restaura 50 HP", 2, "Raro"));
        inventario.AdicionarItem(new ItemConsumivel("Elixir de Força", 0.3, 75, "Aumenta Força em +5 por 1 hora", 1, "Épico"));
        
        // Munição
        inventario.AdicionarItem(new ItemMunicao("Flechas", 0.1, 2, 20, "Flecha", "Comum"));
        
        // Item lendário
        var espadaLendaria = new ItemArma("Lâmina do Dragão", 4.0, 1000, "Fogo", 25, 1, "Lendário");
        espadaLendaria.AdicionarRequisito("força");
        espadaLendaria.AdicionarRequisito("destreza");
        inventario.AdicionarItem(espadaLendaria);
        
        Console.WriteLine("✅ Itens de exemplo adicionados ao inventário!");
        Aguardar();
    }
    
    private void Aguardar()
    {
        Console.WriteLine("\n⏎ Pressione ENTER para continuar...");
        Console.ReadLine();
    }
}

// ======================================================
// MAIN
// ======================================================
public class Program
{
    public static void Main()
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Menu menu = new Menu();
            menu.Iniciar();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERRO CRÍTICO: {ex.Message}");
            Console.WriteLine($"Detalhes: {ex.StackTrace}");
            Console.ReadLine();
        }
    }
}