using CarRepairGame.Data;
using CarRepairGame.Models;
using CarRepairGame.Services;
using ISIP123_Soldatov_bd;
using System;
using System.Linq;

namespace CarRepairGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Используем using, чтобы гарантировать освобождение ресурсов (подключения к БД)
            using (var context = new AppDbContext())
            {
                // Убедимся, что БД создана
                context.Database.EnsureCreated();

                // Заполним БД начальными данными, если она пуста
                SeedDatabase(context);

                // Создаем и запускаем игру
                try
                {
                    var gameManager = new GameManager(context);
                    gameManager.RunGame();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nКритическая ошибка!");
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                    Console.WriteLine("Нажмите Enter для выхода...");
                    Console.ReadLine();
                }
            }
        }

        /// <summary>
        /// Заполняет базу данных начальными значениями, если она пуста.
        /// </summary>
        private static void SeedDatabase(AppDbContext context)
        {
            // Проверяем, есть ли уже игрок
            if (!context.Players.Any())
            {
                Console.WriteLine("Создаем нового игрока...");
                // В начале у вас уже есть какая-то сумма
                context.Players.Add(new Player("Шеф", 1000m));
                context.SaveChanges();
            }

            // Проверяем, есть ли детали
            if (!context.Parts.Any())
            {
                Console.WriteLine("Заполняем склад начальными деталями...");
                // ...и несколько деталей на складе.
                context.Parts.AddRange(new Part("Свеча зажигания", 10m, 60m, 5));
                context.SaveChanges();
            }
        }
    }
}