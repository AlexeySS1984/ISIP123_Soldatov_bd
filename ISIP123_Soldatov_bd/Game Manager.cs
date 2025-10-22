using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading; // Для имитации задержки доставки

namespace ISIP123_Soldatov_bd
{
    public class CarServiceGame
    {
        private readonly AutoServiceGameEntities _context; // Ваш DbContext
        private readonly List<PartPurchases> _pendingDeliveries = new List<PartPurchases>();
        private int _carsProcessedSincePurchase = 0;
        private readonly Random _random = new Random();

        // Предполагаемые сломанные детали и базовая стоимость ремонта
        private readonly Dictionary<string, decimal> _potentialBrokenParts = new Dictionary<string, decimal>
        {
            {"Brake Pad", 100m},
            {"Oil Filter", 50m},
            {"Spark Plug", 80m},
            {"Battery", 300m},
            {"Tire", 250m}
        };

        public CarServiceGame(AutoServiceGameEntities context)
        {
            _context = context;
        }

        // --- Управление Балансом ---
        private decimal GetCurrentBalance()
        {
            return _context.Balance.OrderByDescending(b => b.LastUpdated).FirstOrDefault()?.CurrentBalance ?? 0m;
        }

        private void UpdateBalance(decimal amountChange, string reason)
        {
            var currentBalance = GetCurrentBalance();
            var newBalance = currentBalance + amountChange;

            if (newBalance < 0)
            {
                throw new InvalidOperationException($"Insufficient funds for operation: {reason}. Current: {currentBalance}, Change: {amountChange}");
            }

            var BalanceEntry = Core.Context.Balance.First(Balance => Balance..Contains);

            //_context.Balance.Add(newBalanceEntry);
            //_context.SaveChanges();

            Balance 

            //User editUser = Core.Context.User.First(u => u.FullName.Contains("Кузьмин")); // находим пользователя для изменений
            //editUser.Услуга = "Изменено"; // вносим изменения

            //Core.Context.SaveChanges(); // сохранение изменений в БД

            //User editUser = Core.Context.User.ToList().Last(u => u.FullNameu.Contains("Орлова")); // находим пользователя для изменений
            //editUser.Услуга = "Изменено"; // вносим изменения

            Core.Context.SaveChanges(); // сохранение изменений в БД
            Console.WriteLine($"\nBalance update: {amountChange:C}. New balance: {newBalance:C}");
        }

        public List<PartsInventory> GetInventory()
        {
            return _context.PartsInventory.ToList();
        }

        private PartsInventory GetPartByName(string partName)
        {
            return _context.PartsInventory.FirstOrDefault(p => p.PartName == partName);
        }

        private void ChangePartQuantity(string partName, int quantityChange, decimal costPerUnit = 0)
        {
            var part = GetPartByName(partName);

            if (part == null)
            {
                if (quantityChange > 0)
                {
                    // Добавление новой детали
                    part = new PartsInventory
                    {
                        PartName = partName,
                        Quantity = 0, // Установим 0, чтобы потом добавить, используя QuantityChange
                        CostPerUnit = costPerUnit > 0 ? costPerUnit : 1m, // Устанавливаем цену
                        // PartID будет автоматически установлен базой данных
                    };
                    _context.PartsInventory.Add(part);
                }
                else
                {
                    throw new InvalidOperationException($"Part {partName} not found to decrease quantity.");
                }
            }

            var newQuantity = part.Quantity + quantityChange;

            if (newQuantity < 0)
            {
                throw new InvalidOperationException($"Insufficient quantity of {partName}. Current: {part.Quantity}, Change: {quantityChange}");
            }

            part.Quantity = newQuantity;

            // Обновляем CostPerUnit при покупке, если передано
            if (quantityChange > 0 && costPerUnit > 0)
            {
                part.CostPerUnit = costPerUnit;
            }

            _context.SaveChanges();
        }

        // --- Покупка Деталей ---
        public void BuyParts(string partName, int quantity, decimal costPerUnit)
        {
            if (quantity <= 0)
            {
                Console.WriteLine("Purchase quantity must be positive.");
                return;
            }
            if (costPerUnit <= 0)
            {
                Console.WriteLine("Cost per unit must be positive.");
                return;
            }

            var totalCost = quantity * costPerUnit;
            if (GetCurrentBalance() < totalCost)
            {
                Console.WriteLine("Purchase failed: Insufficient funds.");
                return;
            }

            // Создаем запись о покупке
            var purchase = new PartPurchases
            {
                PartID = GetPartByName(partName)?.PartID ?? -1, // Если деталь новая, PartID будет -1, позже обновим
                Quantity = quantity,
                TotalCost = totalCost,
                PurchaseDate = DateTime.Now,
                DeliveryDate = null, // Пока не доставлено
                // PurchaseID будет автоматически установлен базой данных
            };

            // Если детали нет в PartsInventory, создадим её временно для связи
            if (purchase.PartID == -1)
            {
                var newPart = new PartsInventory { PartName = partName, Quantity = 0, CostPerUnit = costPerUnit };
                _context.PartsInventory.Add(newPart);
                _context.SaveChanges(); // Сохраняем новую деталь, чтобы получить PartID
                purchase.PartID = newPart.PartID;
            }

            _context.PartPurchases.Add(purchase);
            _context.SaveChanges();

            // Списываем деньги и добавляем в ожидание
            UpdateBalance(-totalCost, $"Part purchase: {partName} x {quantity}");
            _pendingDeliveries.Add(purchase);
            _carsProcessedSincePurchase = 0; // Сбрасываем счетчик

            Console.WriteLine($"\nSuccessfully purchased {quantity} x {partName} for {totalCost:C}. Delivery pending (2 cars).");
        }

        private void CheckDelivery()
        {
            if (!_pendingDeliveries.Any()) return;

            _carsProcessedSincePurchase++;
            Console.WriteLine($"\nCars processed since last purchase: {_carsProcessedSincePurchase}/2. Delivery imminent...");

            if (_carsProcessedSincePurchase >= 2)
            {
                Console.WriteLine("\n--- DELIVERY ARRIVED! ---");
                foreach (var purchase in _pendingDeliveries)
                {
                    var part = _context.PartsInventory.Find(purchase.PartID);
                    if (part != null)
                    {
                        ChangePartQuantity(part.PartName, purchase.Quantity, part.CostPerUnit); // Обновляем количество и цену
                        purchase.DeliveryDate = DateTime.Now;
                        _context.SaveChanges(); // Обновляем DeliveryDate в PartPurchases
                        Console.WriteLine($"Delivered {purchase.Quantity} x {part.PartName}. New quantity: {part.Quantity}");
                    }
                }
                _pendingDeliveries.Clear();
                _carsProcessedSincePurchase = 0;
            }
        }

        // --- Обслуживание Клиентов ---
        public (string brokenPart, decimal repairCost) GenerateCustomer()
        {
            var keys = _potentialBrokenParts.Keys.ToList();
            var brokenPart = keys[_random.Next(keys.Count)];
            // Ремонт = Цена детали + Оплата за работу (например, 50% от цены детали)
            var partCostBase = _potentialBrokenParts[brokenPart];
            var repairCost = partCostBase + (partCostBase * 0.5m);

            Console.WriteLine("\n--- NEW CUSTOMER ARRIVED ---");
            Console.WriteLine($"Broken part: {brokenPart}");
            Console.WriteLine($"Repair cost offered by customer: {repairCost:C}");
            return (brokenPart, repairCost);
        }

        public void ServiceCustomer(string brokenPart, decimal repairCost, bool acceptOrder)
        {
            CheckDelivery(); // Проверяем доставку перед обслуживанием

            if (!acceptOrder)
            {
                // Штраф за отказ в обслуживании
                var penaltyAmount = repairCost * 0.1m; // Например, 10% от стоимости ремонта
                UpdateBalance(-penaltyAmount, "Refusal of service penalty");
                RecordPenalty("Refusal", penaltyAmount, null);
                Console.WriteLine($"Order refused. Paid penalty: {penaltyAmount:C}");
                return;
            }

            var partInStock = GetPartByName(brokenPart);

            if (partInStock != null && partInStock.Quantity > 0)
            {
                // Успешный ремонт
                ChangePartQuantity(brokenPart, -1); // Списываем 1 деталь
                UpdateBalance(repairCost, $"Successful repair: {brokenPart}");
                RecordCustomerOrder(brokenPart, repairCost, null, true);
                Console.WriteLine($"Successful repair of {brokenPart}. Received {repairCost:C}.");
            }
            else
            {
                // Принят заказ, но детали нет на складе -> Неправильный ремонт и штраф
                var availableParts = _context.PartsInventory.Where(p => p.Quantity > 0).ToList();
                string partUsed = brokenPart;
                if (availableParts.Any())
                {
                    // Имитация замены на случайную другую деталь
                    var randomPart = availableParts[_random.Next(availableParts.Count)];
                    partUsed = randomPart.PartName;
                    ChangePartQuantity(partUsed, -1); // Списываем случайную деталь
                }
                else
                {
                    // Деталей нет вообще, имитация провального ремонта без списания
                    partUsed = "No part available (Failure)";
                }

                var penaltyAmount = repairCost * 1.5m; // Ущерб больше, чем просто отказ
                UpdateBalance(-penaltyAmount, "Wrong repair penalty");
                RecordPenalty("Wrong part used", penaltyAmount, partUsed);
                RecordCustomerOrder(brokenPart, repairCost, partUsed, false);
                Console.WriteLine($"\n!!! WRONG REPAIR !!! Broken: {brokenPart}, Used: {partUsed}. Paid huge penalty: {penaltyAmount:C}.");
            }
        }

        // --- Запись в БД ---
        private void RecordCustomerOrder(int brokenPartID, decimal repairCost, string partUsedName, bool isSuccessful)
        {
            var brokenPartId = GetPartByName(brokenPartName)?.PartID;
            var partUsedId = partUsedName != null && partUsedName != "No part available (Failure)" ? GetPartByName(partUsedName)?.PartID : null;

            var order = new CustomerOrders
            {
                CustomerName = "Client " + _random.Next(100, 999),
                BrokenPartID = brokenPartName,
                RepairCost = repairCost,
                OrderStatus = isSuccessful ? "Completed" : "Failed",
                OrderDate = DateTime.Now,
                // PartID for PartsInventory (если нужно) - в вашей схеме OrderID связана с PartsInventory (1:*) 
                // и BrokenPart - это просто строка, что упрощает.
            };

            // Можно добавить логику для связи с PartsInventory, если требуется по схеме
            // order.PartsInventory = partInStock; // Если нужно связать

            _context.CustomerOrders.Add(order);
            _context.SaveChanges();
        }

        private void RecordPenalty(string type, decimal amount, string partUsed)
        {
            var penalty = new Penalties
            {
                OrderID = _context.CustomerOrders.OrderByDescending(o => o.OrderID).FirstOrDefault()?.OrderID, // Связываем с последним заказом
                PenaltyType = type,
                PenaltyAmount = amount,
                PenaltyDate = DateTime.Now
            };

            _context.Penalties.Add(penalty);
            _context.SaveChanges();
        }

        // --- Главный Цикл Игры (для Console App) ---
        public void StartGameLoop()
        {
            // Инициализация стартового баланса, если 0
            if (GetCurrentBalance() == 0)
            {
                UpdateBalance(5000m, "Initial balance");
                // Инициализация стартовых деталей (если нет)
                if (!_context.PartsInventory.Any())
                {
                    ChangePartQuantity("Brake Pad", 5, 50m);
                    ChangePartQuantity("Oil Filter", 5, 25m);
                }
            }

            Console.WriteLine("--- Car Service Simulation Started ---");
            Console.WriteLine($"Initial Balance: {GetCurrentBalance():C}\n");

            while (GetCurrentBalance() > 0)
            {
                var (brokenPart, repairCost) = GenerateCustomer();

                Console.WriteLine("\nCurrent Inventory:");
                GetInventory().ForEach(p => Console.WriteLine($"- {p.PartName}: {p.Quantity} in stock, {p.CostPerUnit:C} cost"));

                Console.WriteLine("\nChoose action:");
                Console.WriteLine("1: Accept order (Repair)");
                Console.WriteLine("2: Refuse order (Pay penalty)");
                Console.WriteLine("3: Go to Purchase Menu");
                Console.WriteLine("4: Exit Game");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ServiceCustomer(brokenPart, repairCost, true);
                        break;
                    case "2":
                        ServiceCustomer(brokenPart, repairCost, false);
                        break;
                    case "3":
                        PurchaseMenu();
                        break;
                    case "4":
                        Console.WriteLine("Game over. Final balance: " + GetCurrentBalance().ToString("C"));
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Skipping customer.");
                        CheckDelivery(); // Проверяем доставку даже при ошибке
                        break;
                }

                Thread.Sleep(1000); // Небольшая пауза между клиентами
            }

            Console.WriteLine("\n--- GAME OVER! YOU ARE BROKE! ---");
            Console.WriteLine("Final balance: " + GetCurrentBalance().ToString("C"));
        }

        private void PurchaseMenu()
        {
            Console.WriteLine("\n--- PURCHASE MENU ---");
            var availableParts = _potentialBrokenParts.Keys.ToList();

            for (int i = 0; i < availableParts.Count; i++)
            {
                var partName = availableParts[i];
                var baseCost = _potentialBrokenParts[partName]; // Используем базовую цену как закупочную
                Console.WriteLine($"{i + 1}: {partName} - Cost: {baseCost:C}");
            }
            Console.WriteLine("0: Back to main menu");

            Console.Write("Enter part number to buy: ");
            if (int.TryParse(Console.ReadLine(), out int partIndex) && partIndex > 0 && partIndex <= availableParts.Count)
            {
                var partName = availableParts[partIndex - 1];
                var cost = _potentialBrokenParts[partName];

                Console.Write($"Enter quantity for {partName} (Cost: {cost:C}): ");
                if (int.TryParse(Console.ReadLine(), out int quantity) && quantity > 0)
                {
                    BuyParts(partName, quantity, cost);
                }
                else
                {
                    Console.WriteLine("Invalid quantity.");
                }
            }
            else if (partIndex != 0)
            {
                Console.WriteLine("Invalid selection.");
            }
        }
    }
}