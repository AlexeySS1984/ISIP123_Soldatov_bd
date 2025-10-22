using CarRepairGame.Data;
using CarRepairGame.Models;
using ISIP123_Soldatov_bd;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace CarRepairGame.Services
{
    // Внутренний класс для отслеживания заказов, не хранится в БД
    internal class PendingOrder
    {
        public int PartID { get; set; }
        public int Quantity { get; set; }
        public int CarsToWait { get; set; }
    }

    public class GameManager
    {
        private readonly AppDbContext _context;
        private Player _player;
        private List<Part> _partCatalog; // Полный список *типов* деталей
        private readonly List<PendingOrder> _pendingOrders;
        private readonly Random _random;

        // Штрафы
        private const decimal PENALTY_DECLINE = 50m;
        private const decimal PENALTY_WRONG_PART = 400m;
        private const int DELIVERY_WAIT_TIME = 2; // Машины

        public GameManager(AppDbContext context)
        {
            _context = context;
            _random = new Random();
            _pendingOrders = new List<PendingOrder>();

            // Загружаем игрока (предполагаем, что он один с ID=1)
            _player = _context.Players.SingleOrDefault(p => p.PlayerID == 1);
            if (_player == null)
            {
                throw new Exception("Игрок не найден. Убедитесь, что база данных заполнена (seeded).");
            }

            // Загружаем *весь* каталог деталей (включая их текущее кол-во на складе)
            // ToList() загружает все в память, и мы работаем с этим списком
            _partCatalog = _context.Parts.ToList();
        }

        public void RunGame()
        {
            Console.WriteLine($"Добро пожаловать в Автосервис, {_player.PlayerName}!");
            Console.WriteLine("--------------------------------------");

            while (_player.Balance > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"ВАШ БАЛАНС: {_player.Balance:C}");
                Console.ResetColor();

                // 1. Проверяем доставки
                CheckDeliveries();

                // 2. Генерируем клиента
                Part brokenPart = GenerateCustomerJob();
                Console.WriteLine($"\nНовый клиент! Сломана деталь: {brokenPart.PartName}");
                Console.WriteLine($"Клиент готов заплатить: {brokenPart.RepairPrice:C}");
                Console.WriteLine($"У вас на складе: {_partCatalog.First(p => p.PartID == brokenPart.PartID).Quantity} шт.");

                // 3. Предлагаем выбор
                HandleCustomerChoice(brokenPart);

                // Сохраняем все изменения (баланс, кол-во) в БД после каждого "хода"
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"Ошибка сохранения в БД: {ex.Message}");
                    // В реальном приложении здесь нужна более сложная обработка
                }
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nИГРА ОКОНЧЕНА! Вы банкрот.");
            Console.ResetColor();
        }

        private void CheckDeliveries()
        {
            // Идем с конца, чтобы безопасно удалять элементы
            for (int i = _pendingOrders.Count - 1; i >= 0; i--)
            {
                var order = _pendingOrders[i];
                order.CarsToWait--;

                if (order.CarsToWait <= 0)
                {
                    // Доставка прибыла!
                    Part partInStock = _partCatalog.First(p => p.PartID == order.PartID);
                    partInStock.AddStock(order.Quantity);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[ДОСТАВКА] Прибыла партия: {partInStock.PartName} ({order.Quantity} шт.)");
                    Console.ResetColor();

                    _pendingOrders.RemoveAt(i);
                }
            }
        }

        private Part GenerateCustomerJob()
        {
            // Выбираем случайную деталь из *всего* каталога
            int index = _random.Next(_partCatalog.Count);
            return _partCatalog[index];
        }

        private void HandleCustomerChoice(Part brokenPart)
        {
            while (true)
            {
                Console.WriteLine("\nВаши действия?");
                Console.WriteLine("  1. Принять заказ (Ремонт)");
                Console.WriteLine("  2. Отказать клиенту (Штраф)");
                Console.WriteLine("  3. Закупить детали (Магазин)");
                Console.Write("Ваш выбор: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        RepairCar(brokenPart);
                        return; // Выход из цикла выбора, ход завершен
                    case "2":
                        DeclineJob();
                        return; // Выход из цикла выбора, ход завершен
                    case "3":
                        ShowBuyMenu();
                        // После магазина *не выходим*, а снова показываем меню выбора для
                        // текущего клиента (т.к. покупка не тратит "ход")
                        Console.WriteLine($"\n--- Возврат к клиенту ({brokenPart.PartName}) ---");
                        Console.WriteLine($"У вас на складе: {_partCatalog.First(p => p.PartID == brokenPart.PartID).Quantity} шт.");
                        break;
                    default:
                        Console.WriteLine("Неверный ввод. Попробуйте 1, 2 или 3.");
                        break;
                }
            }
        }

        private void RepairCar(Part brokenPart)
        {
            // Находим актуальный объект детали на нашем "складе" (в списке _partCatalog)
            Part partInStock = _partCatalog.First(p => p.PartID == brokenPart.PartID);

            // Пытаемся снять 1 шт. со склада
            if (partInStock.RemoveStock(1))
            {
                // УСПЕХ
                _player.AddMoney(brokenPart.RepairPrice);

                // Логируем успешный ремонт в БД
                var purchaseLog = new Purchase(_player.PlayerID, brokenPart.PartID);
                _context.Purchases.Add(purchaseLog);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Успешный ремонт! +{brokenPart.RepairPrice:C}. Ваш баланс: {_player.Balance:C}");
                Console.ResetColor();
            }
            else
            {
                // ПРОВАЛ (попытка ремонта без детали)
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ПРОВАЛ! У вас не было детали '{brokenPart.PartName}'!");

                // Пытаемся списать штраф
                try
                {
                    _player.SpendMoney(PENALTY_WRONG_PART);
                    Console.WriteLine($"Вы заплатили штраф за ущерб: {PENALTY_WRONG_PART:C}.");

                    // Пытаемся использовать *другую* деталь
                    Part wrongPart = _partCatalog.FirstOrDefault(p => p.Quantity > 0);
                    if (wrongPart != null && wrongPart.RemoveStock(1))
                    {
                        Console.WriteLine($"К тому же, вы зря потратили деталь '{wrongPart.PartName}'.");
                    }
                    else
                    {
                        Console.WriteLine("У вас на складе вообще не было деталей для замены.");
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("У вас даже нет денег на штраф!");
                    _player.SpendMoney(_player.Balance); // Банкротим игрока
                }
                Console.ResetColor();
            }
        }

        private void DeclineJob()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Вы отказали клиенту.");
            try
            {
                _player.SpendMoney(PENALTY_DECLINE);
                Console.WriteLine($"Штраф за отказ: {PENALTY_DECLINE:C}. Ваш баланс: {_player.Balance:C}");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("У вас даже нет денег на штраф за отказ!");
                _player.SpendMoney(_player.Balance); // Банкротим игрока
            }
            Console.ResetColor();
        }

        private void ShowBuyMenu()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n--- МАГАЗИН ДЕТАЛЕЙ ---");
            Console.WriteLine($"Ваш баланс: {_player.Balance:C}");
            Console.WriteLine("ID | Название\t\t | Цена закупки | На складе");
            Console.WriteLine("-------------------------------------------------------");
            foreach (var part in _partCatalog.OrderBy(p => p.PartID))
            {
                Console.WriteLine($"{part.PartID,-2} | {part.PartName,-20} | {part.BuyPrice,-12:C} | {part.Quantity} шт.");
            }
            Console.WriteLine(" 0 | Выход из магазина");
            Console.WriteLine("-------------------------------------------------------");

            while (true)
            {
                int partID = ReadInt("Введите ID детали для покупки (или 0 для выхода): ");
                if (partID == 0)
                {
                    Console.ResetColor();
                    return; // Выход из магазина
                }

                Part partToBuy = _partCatalog.FirstOrDefault(p => p.PartID == partID);
                if (partToBuy == null)
                {
                    Console.WriteLine("Деталь с таким ID не найдена.");
                    continue;
                }

                int quantity = ReadInt($"Сколько '{partToBuy.PartName}' хотите купить? ", 1);
                decimal totalCost = partToBuy.BuyPrice * quantity;

                Console.WriteLine($"Общая стоимость: {totalCost:C}");
                if (_player.Balance < totalCost)
                {
                    Console.WriteLine("Недостаточно денег!");
                    continue;
                }

                try
                {
                    // Списываем деньги
                    _player.SpendMoney(totalCost);

                    // Добавляем в очередь ожидания
                    _pendingOrders.Add(new PendingOrder
                    {
                        PartID = partToBuy.PartID,
                        Quantity = quantity,
                        CarsToWait = DELIVERY_WAIT_TIME + 1 // +1 т.к. проверка в начале след. хода
                    });

                    Console.WriteLine($"Заказ оформлен! {partToBuy.PartName} ({quantity} шт.) прибудет через {DELIVERY_WAIT_TIME} машины.");
                    Console.WriteLine($"Новый баланс: {_player.Balance:C}");
                    _context.SaveChanges(); // Сохраняем изменение баланса немедленно
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка покупки: {ex.Message}");
                }
            }
        }

        // Вспомогательный метод для безопасного ввода числа
        private int ReadInt(string prompt, int min = 0)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= min)
                {
                    return result;
                }
                Console.WriteLine($"Неверный ввод. Введите целое число не меньше {min}.");
            }
        }
    }
}