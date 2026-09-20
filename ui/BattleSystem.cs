public enum BattleResult
{
    Continue,
    EnemyDefeated,
    PlayerDefeated,
    Escaped
}


public enum BattlePhase
{
    PlayerCommand,
    PlayerAction,
    EnemyAction,
    Victory,
    Defeat,
    Escaped
}


public class BattleSystem
{
    public Character Player { get; }

    public Character Enemy { get; }

    public BattleTurn CurrentTurn { get; private set; }

    public BattleCommand SelectedCommand { get; private set; }

    public BattlePhase CurrentPhase { get; private set; }

    public bool IsFinished { get; private set; }

    public bool PlayerWon { get; private set; }

    public string CommandMessage { get; private set; }


    private bool defending;


    public BattleSystem(
        Character player,
        Character enemy)
    {
        Player =
            player;

        Enemy =
            enemy;

        CurrentTurn =
            BattleTurn.Player;

        CurrentPhase =
            BattlePhase.PlayerCommand;

        SelectedCommand =
            BattleCommand.Attack;

        IsFinished =
            false;

        PlayerWon =
            false;

        CommandMessage =
            "Choose an action.";

        defending =
            false;
    }


    public void SelectNextCommand()
    {
        int nextCommand =
            (int)SelectedCommand + 1;

        if (nextCommand >
            (int)BattleCommand.Run)
        {
            nextCommand =
                (int)BattleCommand.Attack;
        }

        SelectedCommand =
            (BattleCommand)nextCommand;

        CommandMessage =
            GetCommandDescription();
    }


    public void SelectPreviousCommand()
    {
        int previousCommand =
            (int)SelectedCommand - 1;

        if (previousCommand <
            (int)BattleCommand.Attack)
        {
            previousCommand =
                (int)BattleCommand.Run;
        }

        SelectedCommand =
            (BattleCommand)previousCommand;

        CommandMessage =
            GetCommandDescription();
    }


    public BattleResult PerformPlayerTurn(
        out int playerDamage,
        out int enemyDamage)
    {
        playerDamage = 0;
        enemyDamage = 0;

        if (IsFinished)
        {
            return GetBattleResult();
        }

        CurrentPhase =
            BattlePhase.PlayerAction;

        switch (SelectedCommand)
        {
            case BattleCommand.Attack:

                playerDamage =
                    PlayerAttack();

                if (IsFinished)
                {
                    return BattleResult.EnemyDefeated;
                }

                enemyDamage =
                    EnemyAttack();

                break;


            case BattleCommand.Skill:

                CommandMessage =
                    "No skills are available yet.";

                return BattleResult.Continue;


            case BattleCommand.Item:

                CommandMessage =
                    "No items are available yet.";

                return BattleResult.Continue;

            case BattleCommand.Interact:

                CommandMessage =
                    "There is nothing to interact with in this battle.";

                return BattleResult.Continue;


            case BattleCommand.Defend:

                defending =
                    true;

                CommandMessage =
                    $"{Player.Name} braces for the attack.";

                CurrentTurn =
                    BattleTurn.Enemy;

                enemyDamage =
                    EnemyAttack();

                break;


            case BattleCommand.Run:

                IsFinished =
                    true;

                PlayerWon =
                    false;

                CurrentPhase =
                    BattlePhase.Escaped;

                CommandMessage =
                    $"{Player.Name} escaped!";

                return BattleResult.Escaped;
        }


        if (IsFinished)
        {
            return BattleResult.PlayerDefeated;
        }

        return BattleResult.Continue;
    }


    private int PlayerAttack()
    {
        CurrentPhase =
            BattlePhase.PlayerAction;

        Attack basicAttack =
            new Attack(
                "Attack",
                5,
                DamageType.Physical);

        int damage =
            Player.Stats.Strength +
            Player.AttackBonus +
            basicAttack.Power;

        Enemy.TakeDamage(
            damage);

        CommandMessage =
            $"{Player.Name} attacks for {damage} damage.";

        if (Enemy.HP <= 0)
        {
            IsFinished =
                true;

            PlayerWon =
                true;

            CurrentPhase =
                BattlePhase.Victory;

            return damage;
        }

        CurrentTurn =
            BattleTurn.Enemy;

        return damage;
    }


    private int EnemyAttack()
    {
        CurrentPhase =
            BattlePhase.EnemyAction;

        if (CurrentTurn != BattleTurn.Enemy)
        {
            return 0;
        }

        Attack basicAttack =
            new Attack(
                "Attack",
                3,
                DamageType.Physical);

        int damage =
            Enemy.Stats.Strength +
            basicAttack.Power -
            Player.DefenseBonus;

        if (defending)
        {
            damage =
                damage / 2;

            defending =
                false;
        }

        damage = Math.Max(0, damage);

        Player.TakeDamage(
            damage);

        if (Player.HP <= 0)
        {
            IsFinished =
                true;

            PlayerWon =
                false;

            CurrentPhase =
                BattlePhase.Defeat;

            return damage;
        }

        CurrentTurn =
            BattleTurn.Player;

        return damage;
    }


    private string GetCommandDescription()
    {
        switch (SelectedCommand)
        {
            case BattleCommand.Attack:
                return "Attack the enemy.";

            case BattleCommand.Skill:
                return "Use a skill.";

            case BattleCommand.Item:
                return "Use an item.";

            case BattleCommand.Interact:
                return "Interact with a battlefield object.";

            case BattleCommand.Defend:
                return "Reduce incoming damage.";

            case BattleCommand.Run:
                return "Attempt to escape.";

            default:
                return "Choose an action.";
        }
    }


    private BattleResult GetBattleResult()
    {
        if (!IsFinished)
        {
            return BattleResult.Continue;
        }

        if (PlayerWon)
        {
            return BattleResult.EnemyDefeated;
        }

        return BattleResult.PlayerDefeated;
    }
}
