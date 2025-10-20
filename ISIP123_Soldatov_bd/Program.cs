using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISIP123_Soldatov_bd; // Подключение к БД, предполагаем, что это пространство имен для работы с SQL

namespace AutoServiceGame
{
    // Перечисление для статусов заказа
    public enum OrderStatus
    {
        Pending,
        Accepted,
        Rejected,
        Completed,
        Failed
    }

    public class DbContext { 
    
    }

    // Класс для управления игрой и автосервисом
    public class ServiceManager
    {
        // Свойства
        public Inventory Inventory { get; private set; }
        public Balance Balance { get; private set; }
        public List<Purchase> PendingPurchases { get; private set; }
        public int CarsProcessed { get; private set; } // Счетчик обработанных машин для задержки доставки
        private Random random; // Для случайных событий
        private List<Part> availableParts; // Список доступных запчастей для закупки

        // Конструктор
        public ServiceManager()
        {
            Inventory = new Inventory();
            Balance = new Balance(10000m); // Начальный баланс 10000
            PendingPurchases = new List<Purchase>();
            CarsProcessed = 0;
            random = new Random();
            availableParts = new List<Part>
            {
                new Part("Engine", 500m, 800m), // Название, цена закупки, цена ремонта
                new Part("Transmission", 300m, 500m),
                new Part("Brake Pads", 50m, 100m),
                new Part("Battery", 100m, 200m),
                new Part("Tires", 80m, 150m),
                new Part("Radiator", 150m, 250m)
            };

            // Инициализация склада начальными запчастями
            Inventory.AddPart(availableParts[0], 5);
            Inventory.AddPart(availableParts[1], 3);
            Inventory.AddPart(availableParts[2], 10);
            Inventory.AddPart(availableParts[3], 4);
        }

        // Метод для старта игры
        public void StartGame()
        {
            Console.WriteLine("Добро пожаловать в симуляцию автосервиса!");
            Console.WriteLine($"Начальный баланс: {Balance.CurrentAmount:C}");

            while (true)
            {
                // Генерация нового клиента
                ClientOrder order = GenerateRandomOrder();
                Console.WriteLine($"\nПриехал клиент {order.Client.Name}. Сломана деталь: {order.BrokenPart.Name}. Стоимость ремонта: {order.RepairCost:C}");

                // Показ текущего склада
                DisplayInventory();

                // Меню выбора
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("1. Принять заказ");
                Console.WriteLine("2. Отказать клиенту");
                Console.WriteLine("3. Зайти в меню закупки");
                Console.WriteLine("4. Выйти из игры");

                string choice = Console.ReadLine();
                if (choice == "1")
                {
                    AcceptOrder(order);
                }
                else if (choice == "2")
                {
                    RejectOrder(order);
                }
                else if (choice == "3")
                {
                    PurchaseMenu();
                    continue; // Повторить цикл без обработки машины
                }
                else if (choice == "4")
                {
                    Console.WriteLine("Игра завершена.");
                    break;
                }
                else
                {
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    continue;
                }

                // Увеличение счетчика машин
                CarsProcessed++;
                HandleDelivery();

                // Проверка на разорение
                if (Balance.CurrentAmount < 0)
                {
                    Console.WriteLine("Вы разорились! Игра окончена.");
                    break;
                }
            }
        }

        // Генерация случайного заказа
        private ClientOrder GenerateRandomOrder()
        {
            string[] clientNames = { "Иван", "Мария", "Алексей", "Ольга", "Сергей" };
            string clientName = clientNames[random.Next(clientNames.Length)];
            Part brokenPart = availableParts[random.Next(availableParts.Count)];
            decimal repairCost = brokenPart.RepairPrice;

            return new ClientOrder(new Client(clientName), brokenPart, repairCost);
        }

        // Принять заказ
        public void AcceptOrder(ClientOrder order)
        {
            if (Inventory.CheckPartAvailability(order.BrokenPart))
            {
                RepairCar(order);
                order.Status = OrderStatus.Completed.ToString();
                Console.WriteLine("Ремонт выполнен успешно!");
            }
            else
            {
                if (Inventory.PartsStock.Any(p => p.Value > 0))
                {
                    ApplyWrongPartPenalty(order);
                    order.Status = OrderStatus.Failed.ToString();
                    Console.WriteLine("Замена неправильной деталью! Клиент недоволен.");
                }
                else
                {
                    RejectOrder(order); // Если ничего нет, автоматически отказ
                    Console.WriteLine("На складе ничего нет. Автоматический отказ.");
                }
            }
        }

        // Отказать заказу
        public void RejectOrder(ClientOrder order)
        {
            order.Status = OrderStatus.Rejected.ToString();
            decimal penaltyAmount = 100m; // Штраф за отказ
            Penalty penalty = new Penalty("Rejection", penaltyAmount);
            penalty.Apply(Balance);
            Console.WriteLine($"Штраф за отказ: {penaltyAmount:C}. Текущий баланс: {Balance.CurrentAmount:C}");
        }

        // Ремонт машины
        public void RepairCar(ClientOrder order)
        {
            Inventory.RemovePart(order.BrokenPart);
            Balance.UpdateBalance(order.RepairCost);
            Console.WriteLine($"Получено {order.RepairCost:C}. Текущий баланс: {Balance.CurrentAmount:C}");
        }

        // Штраф за неправильную деталь
        public void ApplyWrongPartPenalty(ClientOrder order)
        {
            Part wrongPart = Inventory.GetRandomAvailablePart();
            Inventory.RemovePart(wrongPart);
            decimal penaltyAmount = order.RepairCost * 2; // Ущерб в двойном размере
            Penalty penalty = new Penalty("WrongPart", penaltyAmount);
            penalty.Apply(Balance);
            Console.WriteLine($"Штраф за неправильную деталь: {penaltyAmount:C}. Текущий баланс: {Balance.CurrentAmount:C}");
        }

        // Меню закупки
        public void PurchaseMenu()
        {
            Console.WriteLine("\nМеню закупки:");
            foreach (var part in availableParts)
            {
                Console.WriteLine($"{part.Name} - Цена: {part.Cost:C}");
            }

            Console.Write("Введите название детали для покупки: ");
            string partName = Console.ReadLine().Trim();
            if (string.IsNullOrEmpty(partName))
            {
                Console.WriteLine("Название не может быть пустым.");
                return;
            }

            Part partToBuy = availableParts.FirstOrDefault(p => p.Name.Equals(partName, StringComparison.OrdinalIgnoreCase));
            if (partToBuy == null)
            {
                Console.WriteLine("Деталь не найдена.");
                return;
            }

            Console.Write("Введите количество: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
            {
                Console.WriteLine("Количество должно быть положительным целым числом.");
                return;
            }

            BuyParts(partToBuy, quantity);
        }

        // Купить детали
        public void BuyParts(Part part, int quantity)
        {
            decimal totalCost = part.Cost * quantity;
            if (totalCost > Balance.CurrentAmount)
            {
                Console.WriteLine("Недостаточно средств.");
                return;
            }

            Balance.UpdateBalance(-totalCost);
            Purchase purchase = new Purchase(part, quantity, totalCost);
            PendingPurchases.Add(purchase);
            Console.WriteLine($"Куплено {quantity} шт. {part.Name} за {totalCost:C}. Доставка через 2 машины. Текущий баланс: {Balance.CurrentAmount:C}");
        }

        // Обработка доставки
        public void HandleDelivery()
        {
            foreach (var purchase in PendingPurchases.ToList())
            {
                purchase.CarsUntilDelivery--;
                if (purchase.CarsUntilDelivery <= 0)
                {
                    purchase.ProcessDelivery(Inventory);
                    PendingPurchases.Remove(purchase);
                    Console.WriteLine($"Доставлена закупка: {purchase.Quantity} шт. {purchase.Part.Name}");
                }
            }
        }

        // Показать склад
        private void DisplayInventory()
        {
            Console.WriteLine("\nТекущий склад:");
            var inStock = Inventory.PartsStock.Where(p => p.Value > 0);
            if (!inStock.Any())
            {
                Console.WriteLine("Склад пуст.");
            }
            else
            {
                foreach (var item in inStock)
                {
                    Console.WriteLine($"{item.Key.Name}: {item.Value} шт.");
                }
            }
        }
    }

    // Класс для баланса
    public class Balance
    {
        // Свойства
        public decimal CurrentAmount { get; private set; }

        // Конструктор
        public Balance(decimal initialAmount)
        {
            if (initialAmount < 0)
                throw new ArgumentException("Начальный баланс не может быть отрицательным.");
            CurrentAmount = initialAmount;
        }

        // Метод обновления баланса
        public void UpdateBalance(decimal amount)
        {
            CurrentAmount += amount;
        }
    }

    // Класс для запчастей
    public class Part
    {
        // Свойства
        public string Name { get; private set; }
        public decimal Cost { get; private set; } // Цена закупки
        public decimal RepairPrice { get; private set; } // Цена ремонта (деталь + работа)

        // Конструктор
        public Part(string name, decimal cost, decimal repairPrice)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Название детали не может быть пустым.");
            if (cost <= 0)
                throw new ArgumentException("Цена закупки должна быть положительной.");
            if (repairPrice <= 0 || repairPrice < cost)
                throw new ArgumentException("Цена ремонта должна быть положительной и не меньше цены закупки.");

            Name = name;
            Cost = cost;
            RepairPrice = repairPrice;
        }
    }

    // Класс для склада
    public class Inventory
    {
        // Свойства
        public Dictionary<Part, int> PartsStock { get; private set; }

        // Конструктор
        public Inventory()
        {
            PartsStock = new Dictionary<Part, int>();
        }

        // Проверка наличия
        public bool CheckPartAvailability(Part part)
        {
            return PartsStock.TryGetValue(part, out int quantity) && quantity > 0;
        }

        // Добавить деталь
        public void AddPart(Part part, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Количество должно быть положительным.");

            if (PartsStock.ContainsKey(part))
                PartsStock[part] += quantity;
            else
                PartsStock[part] = quantity;
        }

        // Удалить деталь (одну)
        public void RemovePart(Part part)
        {
            if (PartsStock.TryGetValue(part, out int quantity) && quantity > 0)
            {
                PartsStock[part]--;
                if (PartsStock[part] == 0)
                    PartsStock.Remove(part);
            }
        }

        // Получить случайную доступную деталь
        public Part GetRandomAvailablePart()
        {
            var available = PartsStock.Where(p => p.Value > 0).ToList();
            if (!available.Any())
                return null;

            Random rand = new Random();
            return available[rand.Next(available.Count)].Key;
        }
    }

    // Класс для заказа клиента
    public class ClientOrder
    {
        // Свойства
        public Client Client { get; private set; }
        public Part BrokenPart { get; private set; }
        public decimal RepairCost { get; private set; }
        public string Status { get; set; }

        // Конструктор
        public ClientOrder(Client client, Part brokenPart, decimal repairCost)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            BrokenPart = brokenPart ?? throw new ArgumentNullException(nameof(brokenPart));
            if (repairCost <= 0)
                throw new ArgumentException("Стоимость ремонта должна быть положительной.");
            RepairCost = repairCost;
            Status = OrderStatus.Pending.ToString();
        }
    }

    // Класс для клиента
    public class Client
    {
        // Свойства
        public string Name { get; private set; }

        // Конструктор
        public Client(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Имя клиента не может быть пустым.");
            Name = name;
        }
    }

    // Класс для закупок
    public class Purchase
    {
        // Свойства
        public Part Part { get; private set; }
        public int Quantity { get; private set; }
        public decimal TotalCost { get; private set; }
        public int CarsUntilDelivery { get; set; }

        // Конструктор
        public Purchase(Part part, int quantity, decimal totalCost)
        {
            Part = part ?? throw new ArgumentNullException(nameof(part));
            if (quantity <= 0)
                throw new ArgumentException("Количество должно быть положительным.");
            if (totalCost <= 0)
                throw new ArgumentException("Общая стоимость должна быть положительной.");
            Quantity = quantity;
            TotalCost = totalCost;
            CarsUntilDelivery = 2; // Задержка в 2 машины
        }

        // Процесс доставки
        public void ProcessDelivery(Inventory inventory)
        {
            inventory.AddPart(Part, Quantity);
        }
    }

    // Класс для штрафов
    public class Penalty
    {
        // Свойства
        public string Type { get; private set; }
        public decimal Amount { get; private set; }

        // Конструктор
        public Penalty(string type, decimal amount)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("Тип штрафа не может быть пустым.");
            if (amount <= 0)
                throw new ArgumentException("Сумма штрафа должна быть положительной.");
            Type = type;
            Amount = amount;
        }

        // Применить штраф
        public void Apply(Balance balance)
        {
            balance.UpdateBalance(-Amount);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceManager manager = new ServiceManager();
            manager.StartGame();

            // Здесь можно добавить интеграцию с БД, например, сохранять баланс или логи в SQL
            // Пример: using (var context = new YourDbContext()) { context.Balances.Add(...); }
            // Но поскольку БД не реализована в коде, оставляем как есть.

            Console.ReadKey();
        }
    }
}