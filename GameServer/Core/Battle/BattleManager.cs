using GameServer.Domain.Models.Battle;
using GameServer.Handlers;

namespace GameServer.Core.Battle
{
    public class BattleManager
    {
        private readonly BattlePacketHandler _battlePacketHandler;
        private readonly Dictionary<GameClient, Pokemon> _playerBattles = new();
        private readonly Dictionary<GameClient, Pokemon> _serverBattles = new();
        private readonly Random _random = new();

        public BattleManager(BattlePacketHandler battlePacketHandler)
        {
            _battlePacketHandler = battlePacketHandler;
        }

        public async Task StartBattle(GameClient client)
        {
            var playerPokemon = PokemonData.CreateCharmander();
            var serverPokemon = PokemonData.CreateBulbasaur();

            _playerBattles[client] = playerPokemon;
            _serverBattles[client] = serverPokemon;

            await _battlePacketHandler.SendBattleInitiation(client, playerPokemon, serverPokemon);
        }

        public async Task HandleMoveSelection(GameClient client, int moveIndex)
        {
            if (!_playerBattles.TryGetValue(client, out var playerPokemon) || !_serverBattles.TryGetValue(client, out var serverPokemon)) return;
            if (moveIndex >= playerPokemon.Moves.Count) return;

            var playerMove = playerPokemon.Moves[moveIndex];
            var serverMove = ChooseServerMove(serverPokemon);

            string battleMessage = "";

            bool playerFirst = ShouldPlayerMoveFirst(playerPokemon, serverPokemon, playerMove, serverMove);

            if (playerFirst)
            {
                battleMessage += await ExecuteMove(playerMove, playerPokemon, serverPokemon, true);
                if (!serverPokemon.IsFainted)
                {
                    battleMessage += await ExecuteMove(serverMove, serverPokemon, playerPokemon, false);
                }
            }
            else
            {
                battleMessage += await ExecuteMove(serverMove, serverPokemon, playerPokemon, false);
                if (!playerPokemon.IsFainted)
                {
                    battleMessage += await ExecuteMove(playerMove, playerPokemon, serverPokemon, true);
                }
            }

            await _battlePacketHandler.SendBattleUpdate(client, playerPokemon, serverPokemon, battleMessage);

            // Check if battle is over
            if (playerPokemon.IsFainted || serverPokemon.IsFainted)
            {
                await _battlePacketHandler.SendBattleEnd(client, serverPokemon.IsFainted);
                _playerBattles.Remove(client);
                _serverBattles.Remove(client);
            }
        }

        private Move ChooseServerMove(Pokemon serverPokemon)
        {
            // Just choose a random move for now...
            // Could be improved with actual AI logic, possibly difficulty levels...
            return serverPokemon.Moves[_random.Next(serverPokemon.Moves.Count)];
        }

        private bool ShouldPlayerMoveFirst(Pokemon playerPokemon, Pokemon serverPokemon, Move playerMove, Move serverMove)
        {
            if (playerMove.Priority != serverMove.Priority)
            {
                return playerMove.Priority > serverMove.Priority;
            }

            if (playerPokemon.GetModifiedStat(Stat.Speed) != serverPokemon.GetModifiedStat(Stat.Speed))
            {
                return playerPokemon.GetModifiedStat(Stat.Speed) > serverPokemon.GetModifiedStat(Stat.Speed);
            }

            return _random.Next(2) == 0; // Random if speeds are equal
        }

        private async Task<string> ExecuteMove(Move move, Pokemon attacker, Pokemon defender, bool isPlayer)
        {
            string message = $"{(isPlayer ? "Your" : "Enemy")} {attacker.Name} used {move.Name}!\n";

            if (!BattleData.RollAccuracy(move.Accuracy))
            {
                return message + "But it missed!\n";
            }

            if (move.Category == MoveCategory.Status)
            {
                if (move.InflictsStatus.HasValue && defender.Status == StatusCondition.None)
                {
                    defender.Status = move.InflictsStatus.Value;
                    message += $"{defender.Name} was inflicted with {move.InflictsStatus.Value}!\n";
                }
                return message;
            }

            // Calculate damage
            bool isCritical = BattleData.RollCriticalHit();
            float effectiveness = TypeChart.GetTypeEffectiveness(move.Type, defender);

            int damage = CalculateDamage(move, attacker, defender, isCritical, effectiveness);
            defender.CurrentHP = Math.Max(0, defender.CurrentHP - damage);

            if (isCritical) message += "A critical hit!\n";
            if (effectiveness > 1.0f) message += "It's super effective!\n";
            else if (effectiveness < 1.0f && effectiveness > 0f) message += "It's not very effective...\n";
            else if (effectiveness == 0f) message += "It doesn't affect " + defender.Name + "...\n";


            if (move.InflictsStatus.HasValue && defender.Status == StatusCondition.None)
            {
                if (BattleData.RollStatusEffect())
                {
                    defender.Status = move.InflictsStatus.Value;
                    message += $"{defender.Name} was inflicted with {move.InflictsStatus.Value}!\n";
                }
            }

            if (defender.IsFainted) message += $"{defender.Name} fainted!\n";

            return message;
        }

        private int CalculateDamage(Move move, Pokemon attacker, Pokemon defender, bool isCritical, float typeEffectiveness)
        {
            float level = 50f; // hardcodded for PoC, assuming level 50...
            float critMod = isCritical ? 1.5f : 1.0f;
            float random = (_random.Next(85, 101)) / 100f;

            float stab = (move.Type == attacker.PrimaryType || (attacker.SecondaryType.HasValue && move.Type == attacker.SecondaryType.Value)) ? 1.5f : 1.0f;

            float attack = move.Category == MoveCategory.Physical ? attacker.GetModifiedStat(Stat.Attack) : attacker.GetModifiedStat(Stat.SpecialAttack);

            float defense = move.Category == MoveCategory.Physical ? defender.GetModifiedStat(Stat.Defense) : defender.GetModifiedStat(Stat.SpecialDefense);

            float baseDamage = ((2 * level + 10) / 250f) * (attack / defense) * move.Power + 2;

            return (int)(baseDamage * critMod * random * stab * typeEffectiveness);
        }
    }
}