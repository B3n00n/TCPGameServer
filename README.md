# Game Server

A TCP based multiplayer game server written in C# that handles player authentication, real time movement, chat.

## Overview

The server manages player connections, authentication, and realtime game state synchronization. It's built with a focus on performance and can handle thousands of concurrent players. The architecture uses a custom binary protocol for efficient network communication.

## Features

- **Player Authentication & Account Management**
  - Login system with revision checking
  - Account states (position, direction, movement type)
  - Player visuals (gender, skin tone, hair, clothing)
  - Ban/mute functionality

- **Real-time Gameplay**
  - Movement synchronization between all connected players
  - Chat system with bubble text above players
  - Command system for moderation (`/kick`, `/ban`, `/mute`, etc.)
  - Player spawn/despawn handling

## Tech Stack

- **Framework**: .NET 6+ (C#)
- **Database**: SQLite with Dapper ORM
- **Networking**: Raw TCP sockets with custom binary protocol
- **Architecture**: Repository pattern for data access

## Getting Started

### Prerequisites

- .NET 6 SDK or later
- SQLite

### Database Setup

The server expects a SQLite database with the following tables:

```sql
-- Accounts table
CREATE TABLE Accounts (
    Id INTEGER PRIMARY KEY,
    Username TEXT UNIQUE NOT NULL,
    Password TEXT NOT NULL,
    Rank INTEGER DEFAULT 0,
    IsBanned INTEGER DEFAULT 0,
    IsMuted INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt DATETIME
);

-- Account states (position data)
CREATE TABLE AccountStates (
    AccountId INTEGER PRIMARY KEY,
    PositionX INTEGER DEFAULT 50,
    PositionY INTEGER DEFAULT 50,
    Direction INTEGER DEFAULT 0,
    MovementType INTEGER DEFAULT 0,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
);

-- Player appearance
CREATE TABLE AccountVisuals (
    AccountId INTEGER PRIMARY KEY,
    Gender INTEGER DEFAULT 0,
    SkinTone INTEGER DEFAULT 0,
    HairType INTEGER DEFAULT 0,
    HairColor INTEGER DEFAULT 0,
    HatId INTEGER DEFAULT 65535,
    TopId INTEGER DEFAULT 65535,
    LegsId INTEGER DEFAULT 65535,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
);
```

### Configuration

Edit `GameConfig.cs` to match your setup:

```csharp
public const int GAME_SERVER_PORT = 30811;     // Main game server port
public const int REVISION = 1;                  // Client version check
public const string CONNECTION_STRING = "Data Source=gamedb.sqlite";
```

### Running the Server

```bash
dotnet build
dotnet run
```

The server will start listening on port 30811 by default.

## Project Structure

```
GameServer/
├── Core/
│   ├── Network/         # Packet reading/writing, bit buffer
│   ├── Chat/            # Chat commands and service
│   └── Events/          # Game events (chat, battles)
├── Domain/
│   └── Models/          # Account, PlayerData, enums
├── Handlers/            # Packet handlers for different opcodes
├── Infrastructure/
│   ├── Config/          # Server configuration
│   ├── Database/        # Database context
│   └── Repositories/    # Data access layer
└── Server/
    └── Core/           # Main server and client handling
```

## Network Protocol

The server uses a custom binary protocol with the following packet structure:

```
[Opcode: 1 byte][Length: 2 bytes][Payload: variable]
```

### Key Opcodes

- `2` - Player movement
- `3` - Ping/heartbeat
- `4` - Chat message
- `6` - Game/system message
- `10` - Login request
- `14` - Initial handshake
- `29` - Player updates (spawn, visuals, movement)
- `30` - Battle initiation

## Chat Commands

Commands are rank based (0 = regular player, 6 = admin):

- `/online` - Show online player count
- `/kick <username>` - Kick a player (rank 6)
- `/ban <username>` - Ban a player (rank 6)
- `/mute <username>` - Mute a player (rank 6)
- `/broadcast <message>` - Server wide message (rank 6)

## Development Notes

### Adding New Features

1. **New Packet Types**: Add handler in `GameServer.ProcessClientPackets()`
2. **New Commands**: Create class implementing `IChatCommand`, register in `CommandHandler`
3. **New Database Fields**: Update repositories and domain models

### Performance Considerations

- Uses object pooling for player indices
- Concurrent collections for thread safe client management
- Async/await throughout for non-blocking I/O
- Bit packed data for efficient bandwidth usage

### Known Limitations

- No encryption on network traffic
- Passwords stored in plaintext
- Limited to 5,000 concurrent connections