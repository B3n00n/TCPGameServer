using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Domain.Models
{
    public class AccountState
    {
        public int AccountId { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public byte Direction { get; set; }
        public byte MovementType { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
