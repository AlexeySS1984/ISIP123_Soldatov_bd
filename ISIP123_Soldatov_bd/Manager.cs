using ISIP123_Soldatov_bd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ServiceManager
{
    private readonly AutoServiceNoNeuroEntities _dbContext;

    private int _carsProcessedSincePurchase = 0;
    string name;
    public ServiceManager()
    {
        _dbContext = new AutoServiceNoNeuroEntities();
        Console.Write("Введите имя игрока: ");
        name = Console.ReadLine();
        InitializeDatabaseIfEmpty(name);
    }

    private void InitializeDatabaseIfEmpty(string name)
    {
        if (!_dbContext.Part.Any())
        {
            _dbContext.Part.Add(new Part("Масляный фильтр", 50m, 10m, 10));
            _dbContext.Part.Add(new Part("Тормозные колодки", 150m, 50m, 5));
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
            Console.WriteLine("Здравствуйте, " + name);
        }
    }

    public decimal GetCurrentBalance()
    {
        // Берем первую (и единственную) запись из таблицы Balance
        return _dbContext.Player.FirstOrDefault()?.Balance ?? 0m;
    }

    public List<Part> GetPart()
    {
        // Получаем все записи из таблицы PartsInventory
        return _dbContext.Part.ToList();
    }

    public (bool Success, string Message) ProcessCustomer(/*CustomerOrders newOrder, */string action, int partIdToUseForRepair = -1)
    {
        CheckAndProcessDeliveries();
        _carsProcessedSincePurchase++;

        try
        {
            if (action.Equals("Отказать", StringComparison.OrdinalIgnoreCase))
            {
                //ProcessPenalty(newOrder.OrderID, "Отказ в обслуживании", 100m);
                //newOrder.OrderStatus = "Отказано";
                //_dbContext.CustomerOrders.Add(newOrder);
                Player editPlayer = Core.Context.Player.First(u => u.PlayerName.Contains("Кузьмин"));
                _dbContext.SaveChanges(); // <-- СОХРАНЯЕМ ИЗМЕНЕНИЯ
                return (true, "Клиент отказан. Выплачен штраф 100 ден.ед.");
            }

            if (action.Equals("Принять", StringComparison.OrdinalIgnoreCase))
            {
                var partToUse = _dbContext.PartsInventory.FirstOrDefault(p => p.PartID == partIdToUseForRepair);

                if (partToUse == null || partToUse.Quantity < 1)
                {
                    ApplyErrorPenalty(newOrder, "деталь для ремонта отсутствует на складе");
                    return (false, $"ОШИБКА: Детали с ID {partIdToUseForRepair} нет на складе! Выплачен крупный штраф.");
                }

                if (partToUse.PartID == newOrder.BrokenPartID)
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
        var balanceRecord = _dbContext.Balance.FirstOrDefault();
        if (balanceRecord != null)
        {
            balanceRecord.UpdateBalance(amount);
            // SaveChanges будет вызван в основном методе, который вызвал этот.
        }
    }

    private void ProcessPenalty(int orderId, string type, decimal amount)
    {
        var newPenalty = new Penalties(orderId, type, amount);
        _dbContext.Penalties.Add(newPenalty);
        UpdateBalance(-amount);
    }

    private void ApplyErrorPenalty(CustomerOrders order, string reason)
    {
        decimal errorPenalty = order.RepairCost * 1.5m;
        ProcessPenalty(order.OrderID, "Неправильный ремонт", errorPenalty);
        order.OrderStatus = "Ошибка";
        _dbContext.CustomerOrders.Add(order);
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