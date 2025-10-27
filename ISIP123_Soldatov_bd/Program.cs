using ISIP123_Soldatov_bd;
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    static void Main(string[] args)
    {
        ServiceManager manager = new ServiceManager();
        Console.OutputEncoding = System.Text.Encoding.UTF8; 
        while (true)
        {
            Console.Clear(); 
            DisplayHeader(manager);

            Console.WriteLine("╔═════════════════════════════╗");
            Console.WriteLine("║        ГЛАВНОЕ МЕНЮ         ║");
            Console.WriteLine("╠═════════════════════════════╣");
            Console.WriteLine("║ 1. Следующий клиент         ║");
            Console.WriteLine("║ 2. Закупка запчастей        ║");
            Console.WriteLine("║ 3. Показать склад           ║");
            Console.WriteLine("║ 4. Выход                    ║");
            Console.WriteLine("╚═════════════════════════════╝");
            Console.Write("Выберите действие: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    HandleNewCustomer(manager);
                    break;
                case "2":
                    HandlePartPurchase(manager);
                    break;
                case "3":
                    DisplayInventoryScreen(manager);
                    break;
                case "4":
                    Console.WriteLine("Спасибо за игру! До свидания.");
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Неверный ввод. Пожалуйста, выберите пункт меню.");
                    Console.ResetColor();
                    break;
            }

            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }

    private static void DisplayHeader(ServiceManager manager)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("============================== АВТОСЕРВИС ==============================");
        Console.WriteLine($"ВАШ БАЛАНС: {manager.GetCurrentBalance():C}"); 
        Console.WriteLine("========================================================================\n");
        Console.ResetColor();
    }

    private static void HandleNewCustomer(ServiceManager manager)
    {
        Console.Clear();
        DisplayHeader(manager);
        var possibleProblems = manager.GetAvailablePartsForPurchase();
        if (!possibleProblems.Any())
        {
            Console.WriteLine("Нет доступных деталей для генерации проблем. Сначала закупите что-нибудь.");
            return;
        }
        Random random = new Random();
        var brokenPart = possibleProblems[random.Next(possibleProblems.Count)];
        decimal repairCost = brokenPart.CostPerUnit * 2.5m; 

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"К вам приехал новый клиент!");
        Console.WriteLine($"Проблема: Сломан(а) '{brokenPart.PartName}' (ID детали: {brokenPart.PartID})");
        Console.WriteLine($"Стоимость ремонта для клиента: {repairCost:C}");
        Console.ResetColor();
        Console.WriteLine("------------------------------------------------------------------------");

        DisplayInventory(manager); 

        Console.Write("\nПринять клиента? (y/n): ");
        string decision = Console.ReadLine();

        var order = new CustomerOrders("Клиент", brokenPart.PartID, repairCost);

        if (decision.Equals("n", StringComparison.OrdinalIgnoreCase))
        {
            var (success, message) = manager.ProcessCustomer(order, "Отказать");
            Console.WriteLine(message);
            return;
        }

        if (decision.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.Write("Введите ID детали, которую будете использовать для ремонта: ");
            if (int.TryParse(Console.ReadLine(), out int partIdToUse))
            {
                var (success, message) = manager.ProcessCustomer(order, "Принять", partIdToUse);

                Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Неверный ID. Ремонт отменен.");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Неизвестное действие. Клиент уехал.");
            Console.ResetColor();
        }
    }

    private static void HandlePartPurchase(ServiceManager manager)
    {
        Console.Clear();
        DisplayHeader(manager);

        Console.WriteLine("--- МЕНЮ ЗАКУПКИ ---");
        var availableParts = manager.GetAvailablePartsForPurchase();
        Console.WriteLine("ID\tНазвание\t\tЦена закупки");
        Console.WriteLine("----------------------------------------------------");
        foreach (var part in availableParts)
        {
            Console.WriteLine($"{part.PartID}\t{part.PartName,-20}\t{part.CostPerUnit:C}");
        }
        Console.WriteLine("----------------------------------------------------\n");

        Console.Write("Введите ID детали для покупки: ");
        if (!int.TryParse(Console.ReadLine(), out int partId))
        {
            Console.WriteLine("Неверный ID.");
            return;
        }

        var partToBuy = availableParts.FirstOrDefault(p => p.PartID == partId);
        if (partToBuy == null)
        {
            Console.WriteLine("Деталь с таким ID не найдена.");
            return;
        }

        Console.Write($"Введите количество '{partToBuy.PartName}' для покупки: ");
        if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
        {
            Console.WriteLine("Неверное количество.");
            return;
        }

        var (success, message) = manager.BuyParts(partId, quantity, partToBuy.CostPerUnit);
        Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void DisplayInventoryScreen(ServiceManager manager)
    {
        Console.Clear();
        DisplayHeader(manager);
        DisplayInventory(manager);
    }
    private static void DisplayInventory(ServiceManager manager)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n--- ТЕКУЩЕЕ СОСТОЯНИЕ СКЛАДА ---");
        var inventory = manager.GetInventory();
        if (inventory.Any())
        {
            Console.WriteLine("ID\tНазвание\t\tКоличество");
            Console.WriteLine("--------------------------------------------");
            foreach (var part in inventory)
            {
                Console.WriteLine($"{part.PartID}\t{part.PartName,-20}\t{part.Quantity} шт.");
            }
        }
        else
        {
            Console.WriteLine("Склад пуст!");
        }
        Console.WriteLine("--------------------------------------------");
        Console.ResetColor();
    }
}