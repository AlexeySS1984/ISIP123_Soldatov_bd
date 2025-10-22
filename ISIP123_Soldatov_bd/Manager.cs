using ISIP123_Soldatov_bd;
using System;
using System.Collections.Generic;
using System.Linq;

public class ServiceManager
{
    private readonly AutoServiceNoNeuroEntities _dbContext;

    private int _carsProcessedSincePurchase = 0;
    private string _playerName;
    private const decimal REFUSAL_PENALTY = 200m;
    private const decimal MISTAKE_PENALTY_MULTIPLIER = 3m; 
    private const int DELIVERY_THRESHOLD = 2; 

    public ServiceManager()
    {
        _dbContext = new AutoServiceNoNeuroEntities();
        Console.Write("Введите имя игрока: ");
        _playerName = Console.ReadLine();
        InitializeDatabaseIfEmpty(_playerName);
    }

    private void InitializeDatabaseIfEmpty(string name)
    {

        if (!_dbContext.Part.Any())
        {
            _dbContext.Part.Add(new Part("Масляный фильтр", 50m, 10m, 10));
            _dbContext.Part.Add(new Part("Тормозные колодки", 150m, 50m, 5));
            _dbContext.Part.Add(new Part("Свеча зажигания", 30m, 10m, 15));
            _dbContext.Part.Add(new Part("Аккумулятор", 500m, 100m, 2));
            _dbContext.Part.Add(new Part("Ремень ГРМ", 300m, 150m, 3));
            _dbContext.SaveChanges();
        }
        if (!_dbContext.Player.Any())
        {
            Console.WriteLine("Игроков нет. Создание Игрока...");

            _dbContext.Player.Add(new Player(name, 10000m));

            _dbContext.SaveChanges();
            Console.WriteLine("Начальные данные созданы.");
        }
        else
        {
            var existingPlayer = _dbContext.Player.FirstOrDefault();
            if (existingPlayer != null)
            {
                _playerName = existingPlayer.PlayerName;
            }
            Console.WriteLine("Здравствуйте, " + _playerName);
        }
    }

    private Player GetCurrentPlayer()
    {
        return _dbContext.Player.FirstOrDefault(p => p.PlayerName == _playerName)
            ?? _dbContext.Player.FirstOrDefault();
    }

    public decimal GetCurrentBalance()
    {
        return GetCurrentPlayer()?.Balance ?? 0m;
    }

    public List<Part> GetPart()
    {
        return _dbContext.Part.ToList();
    }

    public class ClientCar
    {
        public Part BrokenPart { get; set; }
        public decimal TotalRepairCost { get; set; }
    }

    public ClientCar GenerateNewClientCar()
    {
        var allParts = _dbContext.Part.ToList();
        if (!allParts.Any())
        {
            return null;
        }

        Random rand = new Random();
        int index = rand.Next(allParts.Count);
        Part brokenPart = allParts[index];

        decimal totalCost = brokenPart.BuyPrice + brokenPart.RepairPrice;

        return new ClientCar
        {
            BrokenPart = brokenPart,
            TotalRepairCost = totalCost
        };
    }

    public bool HandleClient(ClientCar car, bool acceptRepair)
    {
        Player player = GetCurrentPlayer();
        if (player == null) return false;

        string partName = car.BrokenPart.PartName;
        decimal totalCost = car.TotalRepairCost;
        Part partInStock = _dbContext.Part.FirstOrDefault(p => p.PartName == partName);
        bool success = false;

        Console.WriteLine($"\n--- Обработка клиента ---");

        if (acceptRepair)
        {
            if (partInStock != null && partInStock.InitialStock > 0)
            {
                partInStock.InitialStock--;
                player.UpdateBalance(totalCost); 
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Ремонт '{partName}' выполнен успешно! Получено: {totalCost:C}.");
                Console.ResetColor();
                success = true;
            }
            else
            {

                decimal penalty = totalCost * MISTAKE_PENALTY_MULTIPLIER;
                player.UpdateBalance(-penalty);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ОШИБКА! Приняли заказ на '{partName}', но детали нет.");
                Console.WriteLine($"Клиент недоволен. Возмещение ущерба: {penalty:C}.");

                var availableParts = _dbContext.Part.Where(p => p.InitialStock > 0 && p.PartName != partName).ToList();
                if (availableParts.Any())
                {
                    Random rand = new Random();
                    Part wrongPart = availableParts[rand.Next(availableParts.Count)];
                    wrongPart.InitialStock--; 
                    Console.WriteLine($"Случайно использована деталь: {wrongPart.PartName}. Количество на складе уменьшено.");
                }
                else
                {
                    Console.WriteLine("На складе нет других деталей для случайной замены.");
                }
                Console.ResetColor();
                success = false;
            }
        }
        else
        {
            player.UpdateBalance(-REFUSAL_PENALTY); 
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Отказ в обслуживании. Штраф: {REFUSAL_PENALTY:C}.");
            Console.ResetColor();
            success = false;
        }

        _dbContext.SaveChanges();

        _carsProcessedSincePurchase++;
        CheckForPartDelivery();

        Console.WriteLine($"Текущий баланс: {player.Balance:C}");
        return success;
    }

    public bool BuyPart(string partName, int quantity)
    {
        Player player = GetCurrentPlayer();
        Part partToBuy = _dbContext.Part.FirstOrDefault(p => p.PartName == partName);

        if (player == null || partToBuy == null || quantity <= 0)
        {
            Console.WriteLine("Ошибка покупки: Неверное имя детали, количество или игрок.");
            return false;
        }

        decimal cost = partToBuy.BuyPrice * quantity;

        if (player.Balance < cost)
        {
            Console.WriteLine($"Недостаточно денег. Нужно {cost:C}, у вас только {player.Balance:C}.");
            return false;
        }

        player.UpdateBalance(-cost);

        for (int i = 0; i < quantity; i++)
        {
            player.Purchase.Add(new Purchase(player.PlayerID, partToBuy.PartID));
        }

        _carsProcessedSincePurchase = 0;

        _dbContext.SaveChanges();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Куплено {quantity} шт. '{partName}' за {cost:C}. Ожидайте доставки через {DELIVERY_THRESHOLD} машины.");
        Console.ResetColor();
        Console.WriteLine($"Текущий баланс: {player.Balance:C}");
        return true;
    }

    private void CheckForPartDelivery()
    {
        if (_carsProcessedSincePurchase >= DELIVERY_THRESHOLD)
        {
            var player = GetCurrentPlayer();
            if (player == null) return;

            var pendingPurchases = _dbContext.Purchase
                                             .Where(p => p.PlayerID == player.PlayerID)
                                             .GroupBy(p => p.PartID)
                                             .Select(g => new
                                             {
                                                 PartID = g.Key,
                                                 InitialStock = g.Count()
                                             })
                                             .ToList();

            if (pendingPurchases.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"\n📦 Доставка прибыла! (Обработано {DELIVERY_THRESHOLD} машины)");

                foreach (var purchaseGroup in pendingPurchases)
                {
                    var part = _dbContext.Part.Find(purchaseGroup.PartID);
                    if (part != null)
                    {
                        part.InitialStock += purchaseGroup.InitialStock;
                        Console.WriteLine($"  + {purchaseGroup.InitialStock} шт. {part.PartName}");
                    }
                }

                var purchasesToRemove = _dbContext.Purchase.Where(p => p.PlayerID == player.PlayerID).ToList();
                _dbContext.Purchase.RemoveRange(purchasesToRemove);

                _dbContext.SaveChanges();

                _carsProcessedSincePurchase = 0;
                Console.ResetColor();
            }
        }
    }
}