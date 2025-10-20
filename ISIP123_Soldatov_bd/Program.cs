using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISIP123_Soldatov_bd;

namespace AutoServiceGame
{
    public class ServiceManager
    {
        // Свойства
        public Inventory Inventory { get; set; }
        public Balance Balance { get; set; }
        public List<Purchase> PendingPurchases { get; set; }
        public int CarsProcessed { get; set; } // Счетчик обработанных машин для задержки доставки

        // Методы
        public void StartGame() { }
        public void ProcessClient(ClientOrder order) { }
        public void AcceptOrder(ClientOrder order) { }
        public void RejectOrder(ClientOrder order) { }
        public void BuyParts(string partName, int quantity) { }
        public void HandleDelivery() { } // Проверяет и доставляет покупки спустя 2 машины
        public void RepairCar(ClientOrder order) { }
        public void ApplyWrongPartPenalty(ClientOrder order) { }
    }

    // Класс для баланса
    public class Balance
    {
        // Свойства
        public decimal CurrentAmount { get; set; }

        // Методы
        public void UpdateBalance(decimal amount) { } // Добавляет или вычитает сумму
    }

    // Класс для запчастей
    public class Part
    {
        // Свойства
        public string Name { get; set; }
        public decimal Cost { get; set; } // Цена закупки
        public decimal RepairPrice { get; set; } // Цена ремонта (деталь + работа)
    }

    // Класс для склада
    public class Inventory
    {
        // Свойства
        public Dictionary<Part, int> PartsStock { get; set; } // Запчасти и их количество

        // Методы
        public bool CheckPartAvailability(Part part) { return false; }
        public void AddPart(Part part, int quantity) { }
        public void RemovePart(Part part) { } // Удаляет одну единицу
        public Part GetRandomAvailablePart() { return null; } // Для неправильной замены
    }

    // Класс для заказа клиента
    public class ClientOrder
    {
        // Свойства
        public Client Client { get; set; }
        public Part BrokenPart { get; set; }
        public decimal RepairCost { get; set; }
        public string Status { get; set; } // Accepted, Rejected, Completed, Failed

        // Методы
        public void GenerateRandomOrder() { } // Генерирует случайную поломку
    }

    // Класс для клиента
    public class Client
    {
        // Свойства
        public string Name { get; set; }
    }

    // Класс для закупок
    public class Purchase
    {
        // Свойства
        public Part Part { get; set; }
        public int Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public int CarsUntilDelivery { get; set; } // Изначально 2, уменьшается с каждой машиной

        // Методы
        public void ProcessDelivery(Inventory inventory) { }
    }

    // Класс для штрафов
    public class Penalty
    {
        // Свойства
        public string Type { get; set; } // Rejection или WrongPart
        public decimal Amount { get; set; }

        // Методы
        public void Apply(Balance balance) { }
    }
    internal class Program
    {
        static void Main(string[] args)
        {

        }
    }
}
