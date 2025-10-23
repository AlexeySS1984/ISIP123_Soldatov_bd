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
            currentplayer = Core.Context.Player.First(p => p.PlayerName.Contains(playerName));
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
}