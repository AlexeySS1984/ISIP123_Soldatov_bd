using ISIP123_Soldatov_bd;
using System;
using System.Linq;

public class Program
{
    // Главный метод, точка входа в приложение
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Для корректного отображения валюты и символов
        ServiceManager manager = new ServiceManager();

        Console.WriteLine("\n--- ДОБРО ПОЖАЛОВАТЬ В АВТОСЕРВИС! ---");

        RunGameLoop(manager);

        Console.WriteLine("\n--- ИГРА ОКОНЧЕНА. Спасибо за игру! ---");
        Console.WriteLine($"Ваш итоговый баланс: {manager.GetCurrentBalance():C}");
    }

    private static void RunGameLoop(ServiceManager manager)
    {
        // Установим лимит клиентов, чтобы игра не длилась вечно, 
        // или можно использовать бесконечный цикл (while (true)) до разорения.
        int maxClients = 10;
        int clientCount = 0;
        decimal currentBalance = manager.GetCurrentBalance();

        // Проверяем, пока не кончились клиенты ИЛИ пока игрок не разорился
        while (clientCount < maxClients && currentBalance > 0)
        {
            Console.WriteLine("\n=============================================");
            Console.WriteLine($"|              ДЕНЬ {clientCount + 1} / {maxClients}             |");
            Console.WriteLine($"| БАЛАНС: {currentBalance:C} |");
            Console.WriteLine("=============================================");

            clientCount++;

            // 1. Показываем текущий склад
            DisplayInventory(manager);

            // 2. Генерируем нового клиента
            ServiceManager.ClientCar currentCar = manager.GenerateNewClientCar();

            Console.WriteLine($"\n🚘 Прибыл новый клиент! Сломана деталь: **{currentCar.BrokenPart.PartName}**.");
            Console.WriteLine($"Стоимость ремонта (к оплате): **{currentCar.TotalRepairCost:C}**.");

            // Проверяем наличие детали на складе для принятия решения
            bool hasPart = manager.GetPart().Any(p => p.PartName == currentCar.BrokenPart.PartName && p.InitialStock > 0);

            Console.WriteLine(hasPart ? "👉 Деталь **ЕСТЬ** на складе." : "❌ Детали **НЕТ** на складе.");
            Console.WriteLine("Что делать? (1 - Чинить, 2 - Отказать, 3 - Купить детали)");

            string action = Console.ReadLine()?.Trim();
            bool repairAccepted = false;
            bool decisionMade = false;

            while (!decisionMade)
            {
                switch (action)
                {
                    case "1":
                        // Чинить
                        manager.HandleClient(currentCar, true);
                        decisionMade = true;
                        break;
                    case "2":
                        // Отказать
                        manager.HandleClient(currentCar, false);
                        decisionMade = true;
                        break;
                    case "3":
                        // Меню закупки
                        HandlePurchaseMenu(manager);
                        // После покупки возвращаемся к клиенту, чтобы принять решение по нему
                        Console.WriteLine("\nЧто делать с текущим клиентом? (1 - Чинить, 2 - Отказать)");
                        action = Console.ReadLine()?.Trim();
                        // Не ставим decisionMade = true, ждем 1 или 2
                        break;
                    default:
                        Console.WriteLine("Неверный ввод. Пожалуйста, введите 1, 2 или 3.");
                        action = Console.ReadLine()?.Trim();
                        break;
                }
            }

            currentBalance = manager.GetCurrentBalance();
        }
    }

    private static void DisplayInventory(ServiceManager manager)
    {
        Console.WriteLine("\n--- СКЛАД ЗАПЧАСТЕЙ ---");
        var parts = manager.GetPart().OrderBy(p => p.PartName);
        foreach (var part in parts)
        {
            Console.WriteLine($"[ID: {part.PartID}] {part.PartName,-20} | На складе: **{part.InitialStock}** шт.");
        }
    }

    private static void HandlePurchaseMenu(ServiceManager manager)
    {
        Console.WriteLine("\n--- МЕНЮ ЗАКУПКИ ---");
        var allParts = manager.GetPart().OrderBy(p => p.PartID).ToList();

        Console.WriteLine($"Ваш баланс: {manager.GetCurrentBalance():C}");
        Console.WriteLine("Доступные детали для заказа (Доставка через 2 машины):");

        foreach (var part in allParts)
        {
            // Цена покупки детали
            decimal costPerUnit = part.BuyPrice;
            Console.WriteLine($"[ID: {part.PartID}] {part.PartName,-20} | Цена за 1 шт.: {costPerUnit:C}");
        }

        Console.WriteLine("\nВведите ID детали, которую хотите купить, и количество (например, 1 5) или 'назад' для выхода:");
        string input = Console.ReadLine()?.Trim();

        if (input?.ToLower() == "назад")
        {
            return;
        }

        // ИСПРАВЛЕНИЕ: Используем new char[] { ' ' } для явного указания массива разделителей
        string[] parts = input?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts?.Length == 2 && int.TryParse(parts[0], out int partId) && int.TryParse(parts[1], out int quantity))
        {
            Part partToBuy = allParts.FirstOrDefault(p => p.PartID == partId);

            if (partToBuy != null)
            {
                manager.BuyPart(partToBuy.PartName, quantity);
            }
            else
            {
                Console.WriteLine("Деталь с таким ID не найдена.");
            }
        }
        else
        {
            Console.WriteLine("Неверный формат ввода.");
        }
    }
}