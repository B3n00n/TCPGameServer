using GameServer.Core.Network;
using GameServer.Domain.Models.Battle;

namespace GameServer.Handlers
{
    public class BattlePacketHandler : PacketHandler
    {
        public async Task SendBattleInitiation(GameClient client, Pokemon playerPokemon, Pokemon serverPokemon)
        {
            var packet = CreatePacket(30, buffer =>
            {
                buffer.WriteBits(4, 1);  // Battle start mask
                buffer.WriteBits(8, 1);  // Battle ID

                // Write player's Pokemon data
                WritePokemonData(buffer, playerPokemon);

                // Write server's Pokemon data
                WritePokemonData(buffer, serverPokemon);
            });

            await client.GetStream().WriteAsync(packet);
        }

        public async Task SendBattleUpdate(GameClient client, Pokemon playerPokemon, Pokemon serverPokemon, string battleMessage)
        {
            var packet = CreatePacket(30, buffer =>
            {
                buffer.WriteBits(4, 2);  // Battle update mask
                buffer.WriteBits(8, 1);  // Battle ID

                // Write current state of both Pokemon
                WritePokemonData(buffer, playerPokemon);
                WritePokemonData(buffer, serverPokemon);

                // Write battle message
                buffer.WriteString(battleMessage);
            });

            await client.GetStream().WriteAsync(packet);
        }

        public async Task SendBattleEnd(GameClient client, bool playerWon)
        {
            var packet = CreatePacket(30, buffer =>
            {
                buffer.WriteBits(4, 3);  // Battle end mask
                buffer.WriteBits(8, 1);  // Battle ID
                buffer.WriteBits(1, playerWon ? 1 : 0);  // Win/loss flag
            });

            await client.GetStream().WriteAsync(packet);
        }

        private void WritePokemonData(BitBuffer buffer, Pokemon pokemon)
        {
            // Write Pokemon stats
            buffer.WriteBits(16, pokemon.CurrentHP);
            buffer.WriteBits(16, pokemon.MaxHP);
            buffer.WriteBits(3, (int)pokemon.Status);  // Status condition

            // Write moves
            buffer.WriteBits(3, pokemon.Moves.Count);  // Number of moves
            foreach (var move in pokemon.Moves)
            {
                buffer.WriteString(move.Name);
                buffer.WriteBits(8, move.PP);
            }
        }

        public async Task HandleMoveSelection(GameClient client, PacketReader reader)
        {
            var moveIndex = await reader.ReadU8();
            // This will be handled by the battle manager to process the move...
        }
    }
}