using ISIP123_Soldatov_bd;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRepairGame.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseID { get; set; }

        // Внешний ключ для Player
        public int PlayerID { get; set; }
        [ForeignKey("PlayerID")]
        public virtual Player Player { get; set; }

        // Внешний ключ для Part
        public int PartID { get; set; }
        [ForeignKey("PartID")]
        public virtual Part Part { get; set; }

        // Конструктор для EF Core
        private Purchase() { }

        // Конструктор для создания записи о ремонте
        public Purchase(int playerID, int partID)
        {
            PlayerID = playerID;
            PartID = partID;
        }
    }
}