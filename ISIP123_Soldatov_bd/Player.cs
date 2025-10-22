using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarRepairGame.Models
{
    public class Player
    {
        public int PlayerID { get; set; }

        public string PlayerName { get; private set; }

        public decimal Balance { get; private set; }

        // Навигационное свойство для истории покупок (ремонтов)
        public virtual ICollection<Purchase> Purchases { get; set; }

        // Конструктор для EF Core
        private Player()
        {
            Purchases = new List<Purchase>();
        }

        // Конструктор для создания нового игрока
        public Player(string playerName, decimal initialBalance)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Имя игрока не может быть пустым.", nameof(playerName));
            if (initialBalance < 0)
                throw new ArgumentException("Начальный баланс не может быть отрицательным.", nameof(initialBalance));

            PlayerName = playerName;
            Balance = initialBalance;
            Purchases = new List<Purchase>();
        }

        /// <summary>
        /// Потратить деньги с баланса
        /// </summary>
        public void SpendMoney(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Сумма траты должна быть положительной.", nameof(amount));
            if (amount > Balance)
                throw new InvalidOperationException("Недостаточно средств на балансе.");

            Balance -= amount;
        }

        /// <summary>
        /// Добавить деньги на баланс
        /// </summary>
        public void AddMoney(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Сумма пополнения должна быть положительной.", nameof(amount));

            Balance += amount;
        }
    }
}