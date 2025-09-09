using System.ComponentModel.DataAnnotations.Schema;
using TennisSim.Models.Entities;

namespace TennisSim.Models
{
    [Table("PlayerAttributes")]
    public class PlayerAttribute
    {
        public PlayerAttribute()
        {
            AttributeType = string.Empty;
            Player = null!;
        }
        public int Id { get; set; }
        public int PlayerId { get; set; }

        public Player Player { get; set; }
        public string AttributeType { get; set; }
        public int Value { get; set; }
    }
}