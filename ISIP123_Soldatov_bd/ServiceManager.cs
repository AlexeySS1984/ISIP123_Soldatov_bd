using ISIP123_Soldatov_bd;
using System;
using System.Collections.Generic;
using System.Linq;

public class ServiceManager
{
    private readonly AutoServiceGameEntities _dbContext;

    private int _carsProcessedSincePurchase = 0;

    public ServiceManager()
    {
        _dbContext = new AutoServiceGameEntities();

        InitializeDatabaseIfEmpty();
    }

    private void InitializeDatabaseIfEmpty()
    {
        if (!_dbContext.Balance.Any())
        {
            Console.WriteLine("База данных пуста. Создание начальных данных...");

            _dbContext.Balance.Add(new Balance(10000m));

            _dbContext.PartsInventory.Add(new PartsInventory("Масляный фильтр", 10, 50m));
            _dbContext.PartsInventory.Add(new PartsInventory("Тормозные колодки", 5, 150m));

            _dbContext.SaveChanges();
            Console.WriteLine("Начальные данные созданы.");
        }
    }

    // ===============================================
    //               ПУБЛИЧНЫЕ МЕТОДЫ 
    // ===============================================

    public decimal GetCurrentBalance()
    {
        return _dbContext.Balance.FirstOrDefault()?.CurrentBalance ?? 0m;
    }

    public List<PartsInventory> GetInventory()
    {
        return _dbContext.PartsInventory.ToList();
    }

    public List<PartsInventory> GetAvailablePartsForPurchase()
    {
        return _dbContext.PartsInventory.ToList();
    }

    public (bool Success, string Message) ProcessCustomer(CustomerOrders newOrder, string action, int partIdToUseForRepair = -1)
    {
        CheckAndProcessDeliveries();
        _carsProcessedSincePurchase++;

        try
        {
            if (action.Equals("Отказать", StringComparison.OrdinalIgnoreCase))
            {
                ProcessPenalty(newOrder.OrderID, "Отказ в обслуживании", 100m);
                newOrder.OrderStatus = "Отказано";
                _dbContext.CustomerOrders.Add(newOrder);
                _dbContext.SaveChanges();
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
                    _dbContext.SaveChanges(); 
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

            _dbContext.SaveChanges();

            _carsProcessedSincePurchase = 0;

            return (true, $"Детали куплены. Списано {totalCost} ден.ед. Доставка ожидается через 2 машины.");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка при закупке: {ex.Message}");
        }
    }

    // ===============================================
    //               ПРИВАТНЫЕ МЕТОДЫ 
    // ===============================================

    private void UpdateBalance(decimal amount)
    {
        var balanceRecord = _dbContext.Balance.FirstOrDefault();
        if (balanceRecord != null)
        {
            balanceRecord.UpdateBalance(amount);
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
        _dbContext.SaveChanges(); 
    }

    private void CheckAndProcessDeliveries()
    {
        if (_carsProcessedSincePurchase >= 1)
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

                _dbContext.SaveChanges(); 
                _carsProcessedSincePurchase = 0;
            }
        }
    }
}