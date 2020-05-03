using System;
using System.Collections.Generic;
using System.Linq;
using covidSim.Utils;

namespace covidSim.Services
{
    public class Game
    {
        public List<Person> People;
        public CityMap Map;
        private DateTime _lastUpdate;

        private static Game _gameInstance;
        private static Random _random = new Random();
        
        public const int PeopleCount = 320;

        private const double InfectionRadrius = 7.0;
        private const double ChanceOfInfection = 0.5;
        public const int FieldWidth = 1000;
        public const int FieldHeight = 500;
        public const int MaxPeopleInHouse = 10;

        private Game()
        {
            Map = new CityMap();
            People = CreatePopulation();
            _lastUpdate = DateTime.Now;
        }

        public static Game Instance => _gameInstance ?? (_gameInstance = new Game());

        public static void Restart()
        {
            _gameInstance = new Game();
        }

        private List<Person> CreatePopulation()
        {
            return Enumerable
                .Repeat(0, PeopleCount)
                .Select((_, index) => new Person(index, FindHome(), Map, index <= PeopleCount * 0.03))
                .ToList();
        }

        private int FindHome()
        {
            while (true)
            {
                var homeId = _random.Next(CityMap.HouseAmount);
                var home = Map.Houses[homeId];
                
                if (home.ResidentCount < MaxPeopleInHouse && !home.IsMarket)
                {
                    home.ResidentCount++;
                    return homeId;
                }
            }
        }

        public Game GetNextState()
        {
            var diff = (DateTime.Now - _lastUpdate).TotalMilliseconds;
            if (diff >= 1000)
            {
                CalcNextStep();
                InfectPeople();
            }

            return this;
        }

        private void CalcNextStep()
        {
            _lastUpdate = DateTime.Now;
            var droppedOutPeople = new List<Person>();
            foreach (var person in People)
            {
                person.CalcNextStep();
                if (person.OutOfTheGame)
                    droppedOutPeople.Add(person);
            }
            droppedOutPeople.ForEach(p => People.Remove(p));
        }

        private void InfectPeople()
        {
            var walkingPeople = People.Where(p => p.State == PersonState.Walking).ToList();
            var infectedPeople = walkingPeople.Where(p => p.Health == PersonHealth.Sick).ToList();
            var healthyPeople = walkingPeople.Where(p => p.Health == PersonHealth.Healthy);
            var pairs = 
                from healthy in healthyPeople
                from infected in infectedPeople
                select (healthy, infected);
            foreach (var (healthy, infected) in pairs)
            {
                if (healthy.Position.GetDistanceTo(infected.Position) <= InfectionRadrius && 
                    _random.NextDouble() >= ChanceOfInfection)
                    healthy.ChangeHealth(PersonHealth.Sick);
            }
        }
    }
}