using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarRepairGame.Models
{
    public class Part
    {
        public int PartID { get; set; }

        public string PartName { get; private set; }

        public decimal BuyPrice { get; private set; } // Цена, по которой МЫ покупаем

        public decimal RepairPrice { get; private set; } // Цена, которую ПЛАТИТ клиент (включая работу)

        public int Quantity { get; private set; } // Количество на складе

        // Навигационное свойство
        public virtual ICollection<Purchase> Purchases { get; set; }

        // Конструктор для EF Core
        private Part()
        {
            Purchases = new List<Purchase>();
        }

        // Конструктор для создания новой детали
        public Part(string partName, decimal buyPrice, decimal repairPrice, int initialQuantity)
        {
            if (string.IsNullOrWhiteSpace(partName))
                throw new ArgumentException("Название детали не может быть пустым.", nameof(partName));
            if (buyPrice <= 0)
                throw new ArgumentException("Цена покупки должна быть положительной.", nameof(buyPrice));
            if (repairPrice <= 0)
                throw new ArgumentException("Цена ремонта должна быть положительной.", nameof(repairPrice));
            if (repairPrice <= buyPrice)
                throw new ArgumentException("Цена ремонта должна быть выше цены покупки.", nameof(repairPrice));
            if (initialQuantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.", nameof(initialQuantity));

            PartName = partName;
            BuyPrice = buyPrice;
            RepairPrice = repairPrice;
            Quantity = initialQuantity;
            Purchases = new List<Purchase>();
        }

        /// <summary>
        /// Добавить детали на склад
        /// </summary>
        public void AddStock(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Количество для добавления должно быть положительным.", nameof(amount));

            Quantity += amount;
        }

        /// <summary>
        /// Использовать деталь со склада. Возвращает true при успехе.
        /// </summary>
        public bool RemoveStock(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Количество для удаления должно быть положительным.", nameof(amount));
            if (Quantity < amount)
                return false; // Недостаточно на складе

            Quantity -= amount;
            return true;
        }
    }
}