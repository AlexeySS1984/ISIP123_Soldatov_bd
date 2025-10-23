using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    static void Main(string[] args)
    {
        ServiceManager manager = new ServiceManager();
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Для корректного отображения кириллицы

        while (true)
        {
            Console.Clear(); // Очищаем консоль для нового экрана
            DisplayHeader(manager); // Показываем текущий баланс

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
                    //HandleNewCustomer(manager);
                    break;
                case "2":
                    //HandlePartPurchase(manager);
                    break;
                case "3":
                    //DisplayInventoryScreen(manager);
                    break;
                case "4":
                    Console.WriteLine("Спасибо за игру! До свидания.");
                    return; // Выход из приложения
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
        Console.WriteLine($"ВАШ БАЛАНС: {manager.GetCurrentBalance()}"); // Форматируем как валюту
        Console.WriteLine("========================================================================\n");
        Console.ResetColor();
    }
}