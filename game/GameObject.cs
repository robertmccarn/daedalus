public class GameObject
{
    public string Name { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }

    public GameObject(string name, int x, int y)
    {
        Name = name;
        X = x;
        Y = y;
    }

    public void MoveTo(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class Character : GameObject
{
    public List<StatusEffect> StatusEffects { get; } = new();
    public int Level { get; private set; }
    public int HP { get; private set; }
    public int MAXHP { get; private set; }
    public Stats Stats { get; private set; }
    public int AttackBonus { get; private set; }
    public int DefenseBonus { get; private set; }

    public Character(
        string name,
        int maxHp,
        int level,
        Stats stats,
        int x,
        int y)
        : base(name, x, y)
    {
        MAXHP = maxHp;
        HP = maxHp;
        Level = level;
        Stats = stats;
        AttackBonus = 0;
        DefenseBonus = 0;
    }

    public void SetEquipmentBonuses(int attackBonus, int defenseBonus)
    {
        AttackBonus = Math.Max(0, attackBonus);
        DefenseBonus = Math.Max(0, defenseBonus);
    }

    public void TakeDamage(int damage)
    {
        HP -= damage;

        if (HP < 0)
        {
            HP = 0;
        }
    }

    public void Heal(int amount)
    {
        HP += amount;

        if (HP > MAXHP)
        {
            HP = MAXHP;
        }
    }
}

public enum StatusEffect
{
    AttackBuff = 0,
    DefenseBuff = 1,
    Haste = 2,
    Poison = 3,
    Bleed = 4,
    CoreCharge = 5
}

public class Stats
{
    public int Strength { get; private set; }
    public int Magic { get; private set; }
    public int Agility { get; private set; }
    public int Luck { get; private set; }

    public Stats(int strength, int magic, int agility, int luck)
    {
        Strength = strength;
        Magic = magic;
        Agility = agility;
        Luck = luck;
    }
}

public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Electric,
    Wind
}

public class Attack
{
    public string Name { get; private set; }
    public int Power { get; private set; }
    public DamageType Type { get; private set; }

    public Attack(string name, int power, DamageType type)
    {
        Name = name;
        Power = power;
        Type = type;
    }
}
