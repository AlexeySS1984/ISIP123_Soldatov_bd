using ISIP123_Soldatov_bd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ServiceManager
{
    private readonly AutoServiceNoNeuroEntities _dbContext;
    string playerName;
    Player currentplayer;
    private int _carsProcessedSincePurchase = 0;

    // Конструктор: Инициализация и проверка начальных данных в БД
    public ServiceManager()
    {
        // !!! ЗАМЕНИТЕ "AutoServiceContext" на ваше имя контекста
        _dbContext = new AutoServiceNoNeuroEntities();

        // Метод для создания начальных данных (баланс, детали), если база пуста
        InitializeDatabaseIfEmpty();
    }
    private void InitializeDatabaseIfEmpty()
    {
        // Проверяем, есть ли в таблице Balance хоть одна запись
        Console.Write("Введите ваше имя: ");
        playerName = Console.ReadLine();
        if (!_dbContext.Player.Any())
        {
            Console.WriteLine("База данных пуста. Создание начальных данных...");

            // 1. Начальный баланс

            _dbContext.Player.Add(new Player(playerName, 10000m));

            _dbContext.SaveChanges();

        }
        else
        {
            //User editUser = Core.Context.User.First(u => u.FullName.Contains("Кузьмин"));
            currentplayer = _dbContext.Player.First(p => p.PlayerName.Contains(playerName));
        }
        if (!_dbContext.Part.Any())
        {
            _dbContext.Part.Add(new Part("Масляный фильтр", 10, 50m, 200m));
            _dbContext.Part.Add(new Part("Тормозные колодки", 5, 150m, 500m));

            _dbContext.SaveChanges();

            Console.WriteLine("Начальные данные созданы.");
        }
    }
    public decimal GetCurrentBalance()
    {
        return currentplayer.Balance;
    }
      public List<Part> GetInventory()
    {
        // Получаем все записи из таблицы PartsInventory
        return _dbContext.Part.ToList();
    }

    public List<Part> GetAvailablePartsForPurchase()
    {
        return _dbContext.Part.ToList();
    }

    public (bool Success, string Message) ProcessCustomer(Part brokenpart, string action, int partIdToUseForRepair = -1)
    {
        CheckAndProcessDeliveries();
        _carsProcessedSincePurchase++;

        try
        {
            if (action.Equals("Отказать", StringComparison.OrdinalIgnoreCase))
            {
                UpdateBalance(-100m);
                _dbContext.SaveChanges(); // <-- СОХРАНЯЕМ ИЗМЕНЕНИЯ
                return (true, "Клиент отказан. Выплачен штраф 100 ден.ед.");
            }

            if (action.Equals("Принять", StringComparison.OrdinalIgnoreCase))
            {
                var partToUse = _dbContext.Part.FirstOrDefault(p => p.PartID == partIdToUseForRepair);

                if (partToUse == null || partToUse.Quantity < 1)
                {
                    ApplyErrorPenalty(partToUse, "деталь для ремонта отсутствует на складе");
                    return (false, $"ОШИБКА: Детали с ID {partIdToUseForRepair} нет на складе! Выплачен крупный штраф.");
                }

                if (partToUse.PartID == brokenpart.PartID)
                {
                    partToUse.Quantity--;
                    UpdateBalance(newOrder.RepairCost);
                    newOrder.OrderStatus = "Выполнен";
                    _dbContext.CustomerOrders.Add(newOrder);
                    _dbContext.SaveChanges(); // <-- СОХРАНЯЕМ ИЗМЕНЕНИЯ
                    return (true, $"Ремонт успешен! Получено: {newOrder.RepairCost} ден.ед.");
                }
                else
                {
                    partToUse.Quantity--;
                    ApplyErrorPenalty(newOrder, "была использована неверная деталь");
                    return (false, $"ОШИБКА: Использована неверная деталь! Выплачен крупный штраф.");
                }
            }

            return (false, "Неизвестное действие.");
        }
        catch (Exception ex)
        {
            // Здесь можно добавить логирование ошибки
            return (false, $"Критическая ошибка при обработке заказа: {ex.Message}");
        }
    }

    public (bool Success, string Message) BuyParts(int partId, int quantity, decimal costPerUnit)
    {
        try
        {
            var totalCost = quantity * costPerUnit;
            if (GetCurrentBalance() < totalCost)
            {
                return (false, "Недостаточно средств для покупки.");
            }

            UpdateBalance(-totalCost);

            var newPurchase = new PartPurchases(partId, quantity, totalCost);
            _dbContext.PartPurchases.Add(newPurchase);

            _dbContext.SaveChanges(); // <-- СОХРАНЯЕМ ИЗМЕНЕНИЯ

            _carsProcessedSincePurchase = 0;

            return (true, $"Детали куплены. Списано {totalCost} ден.ед. Доставка ожидается через 2 машины.");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка при закупке: {ex.Message}");
        }
    }

    // ===============================================
    //               ПРИВАТНЫЕ МЕТОДЫ (теперь работают с БД)
    // ===============================================

    private void UpdateBalance(decimal amount)
    {
        //var balanceRecord = currentplayer.Balance;
        //if (balanceRecord != null)
        //{
        //    balanceRecord.UpdateBalance(amount);
        //    // SaveChanges будет вызван в основном методе, который вызвал этот.
        //}
        currentplayer.Balance += amount;
    }

    private void ProcessPenalty(int orderId, string type, decimal amount)
    {
        var newPenalty = new Penalties(orderId, type, amount);
        _dbContext.Penalties.Add(newPenalty);
        UpdateBalance(-amount);
    }

    private void ApplyErrorPenalty(Part brokenpart, string reason)
    {
        decimal errorPenalty = brokenpart.RepairPrice * 1.5m;
        UpdateBalance(-errorPenalty);
        _dbContext.SaveChanges(); // <-- СОХРАНЯЕМ ИЗМЕНЕНИЯ
    }

    private void CheckAndProcessDeliveries()
    {
        if (_carsProcessedSincePurchase >= 2)
        {
            var pendingPurchases = _dbContext.PartPurchases.Where(p => p.DeliveryDate > p.PurchaseDate).ToList();

            if (pendingPurchases.Any())
            {
                foreach (var purchase in pendingPurchases)
                {
                    var part = _dbContext.PartsInventory.FirstOrDefault(p => p.PartID == purchase.PartID);
                    if (part != null)
                    {
                        part.Quantity += purchase.Quantity;
                    }
                    purchase.UpdateDeliveryDate(DateTime.Now);
                }

                _dbContext.SaveChanges(); // <-- СОХРАНЯЕМ ИЗМЕНЕНИЯ
                _carsProcessedSincePurchase = 0;
            }
        }
    }
}