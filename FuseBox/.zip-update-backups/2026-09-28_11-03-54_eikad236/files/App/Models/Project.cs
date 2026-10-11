using FuseBox.App.Controllers;
using FuseBox.App.Models.BaseAbstract;
using FuseBox;
using System.Text.Json.Serialization;
using FuseBox.FuseBox;
using FuseBox.App.Models;

namespace FuseBox
{
    public class Project : BaseEntity
    {
        public FloorGrouping FloorGrouping { get; set; }
        public GlobalGrouping GlobalGrouping { get; set; }
        public InitialSettings InitialSettings { get; set; }
        public FuseBoxUnit FuseBox { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public double TotalPower { get; set; } // W


        // Обратная связь с User
        public int UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; }

        public Project()
        {
            InitialSettings = new InitialSettings();
            FuseBox = new FuseBoxUnit();
            FloorGrouping = new FloorGrouping();
            GlobalGrouping = new GlobalGrouping();
            Floors = new List<Floor>();
        }

        public Project(FuseBoxUnit fuseBox, FloorGrouping floorGrouping, GlobalGrouping globalGrouping, List<Floor> floors)     // Конструктор для тестов
        {
            FuseBox = fuseBox;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }

        public Project(InitialSettings initialSettings, FloorGrouping floorGrouping, GlobalGrouping globalGrouping, List<Floor> floors)      // Конструктор для тестов
        {
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }
        public Project(FuseBoxUnit fuseBox, InitialSettings initialSettings, FloorGrouping floorGrouping)      // Конструктор для тестов
        {
            FuseBox = fuseBox;
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
        }

        public double CalculateTotalPower() // Calculates the total power of the entire object
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Sum(equipment => equipment.Amper);
        }

        public int GetTotalNumberOfRooms() // Returns the total number of rooms in the project
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .Count();
        }

        public decimal CalculateWireCrossSection()
        {
            decimal WireSection = 0;
            // Стандартные сечения проводов (в мм²) и их предельный ток (в А) для меди
            var copperWireTable = new Dictionary<double, double>
            {
                { 1.5, 18 }, { 2.5, 25 }, { 4, 32 }, { 6, 40 }, { 10, 63 }, { 16, 80 }
            };

            // Поиск минимального сечения, подходящего под заданный ток
            foreach (var wire in copperWireTable)
            {
                if (CalculateTotalPower() <= wire.Value)
                    WireSection = (decimal)wire.Key;

                //return (decimal)wire.Key; // Возвращаем сечение, соответствующее току
            }

            return WireSection;
            // Если ток выше максимального в таблице — требуется индивидуальный расчёт
            //throw new ArgumentException("Требуется кабель большего сечения, рассчитайте вручную.");

            // Сечения медного кабеля для прокладки проводки по дому/ квартире:
            // автомат C10, сечение кабеля 1,5 мм2 для освещения
            // автомат C16, сечение кабеля 2,5 мм2 для розеток
            // автомат C32, сечение кабеля 6,0 мм2 для мощных потребителей
            // Кабель сечением 8 — 10 мм2 для соединения аппаратуры внутри щита. Обычно используется медный кабель типа ВВГнГ плоский трёхжильный монопроволочный.
        }
    }
}