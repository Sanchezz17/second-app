using System;
using System.Linq;
using covidSim.Models;
using covidSim.Utils;

namespace covidSim.Services
{
    public class Person
    {
        private static readonly Vec[] Directions = {
            new Vec(-1, -1),
            new Vec(-1, 1),
            new Vec(1, -1),
            new Vec(1, 1),
        };
        
        private const int MaxDistancePerTurn = 30;
        private const int InitialStepsToRecovery = 35;
        private const int InitialStepsToRot = 10;
        private const int InitialStepsToLeaveMarket = 10;
        private const double ProbabilityOfDying = 0.000003;
        private static Random random = new Random();
        public PersonHealth Health = PersonHealth.Healthy;
        private readonly CityMap map;
        private House nearestMarket;

        internal PersonState State { get; private set; } = PersonState.AtHome;
        
        public Person(int id, int homeId, CityMap map, bool isSick)
        {
            Id = id;
            HomeId = homeId;
            IsSick = isSick;
            IsBored = false;
            timeAtHome = 0;
            this.map = map;
            if (isSick) 
                ChangeHealth(PersonHealth.Sick);

            var homeCoords = map.Houses[homeId].Coordinates.LeftTopCorner;
            var x = homeCoords.X + random.Next(HouseCoordinates.Width);
            var y = homeCoords.Y + random.Next(HouseCoordinates.Height);
            Position = new Vec(x, y);

            nearestMarket = FindNearestMarket(map);
        }

        private House FindNearestMarket(CityMap map)
        {
            House nearestMarket = null;
            var minDistanceToMarket = double.MaxValue;
            foreach (var market in map.Markets)
            {
                var currentDistanceToMarket = Position.GetDistanceTo(market.Coordinates.LeftTopCorner);
                if (currentDistanceToMarket < minDistanceToMarket)
                {
                    minDistanceToMarket = currentDistanceToMarket;
                    nearestMarket = market;
                }
            }

            return nearestMarket;
        }

        public int Id;
        public int HomeId;
        public Vec Position;
        public bool IsSick;
        public bool IsBored;
        private int timeAtHome;
        public int StepsToRecovery;
        public int StepsToRot;
        private int timeAtMarket;

        public bool OutOfTheGame => Health == PersonHealth.Dead && StepsToRot == 0;

        public void CalcNextStep()
        {
            if (CalcIsAtHome())
                timeAtHome += 1;
            else
            {
                timeAtHome = 0;
            }
            IsBored = timeAtHome >= 5;
       
            switch (Health)
            {
                case PersonHealth.Dead:
                    StepsToRot--;
                    return;
                case PersonHealth.Sick:
                {
                    StepsToRecovery--;
                    if (StepsToRecovery == 0)
                        Health = PersonHealth.Healthy;
                    else if (TryToDie())
                        return;
                    break;
                }
            }
            Move();
        }

        private void Move()
        {
            if (random.NextDouble() >= 0.6 && State == PersonState.Walking)
                State = PersonState.GoingMarket;

            if (IsCoordInHouse(Position, nearestMarket) && State != PersonState.AtMarket && State != PersonState.GoingHome)
            {
                State = PersonState.AtMarket;
                timeAtMarket = InitialStepsToLeaveMarket;
            }

            switch (State)
            {
                case PersonState.AtHome:                    
                    CalcNextStepForPersonAtHome();
                    break;
                case PersonState.Walking:                  
                    CalcNextPositionForWalkingPerson();
                    break;
                case PersonState.GoingHome:                   
                    CalcNextPositionForGoingHomePerson();
                    break;
                case PersonState.GoingMarket:
                    CalcNextPositionForGoingMarketPerson();
                    break;
                case PersonState.AtMarket:
                    CalcNextStepForPersonAtMarket();
                    break;
            }

            if (PersonHealth == PersonHealth.Sick)
            {
                if (random.NextDouble() <= ProbToDie)
                {
                    PersonHealth = PersonHealth.Dying;
                }
                sickStepsCount++;
                if (sickStepsCount >= StepsToRecovery)
                    PersonHealth = PersonHealth.Healthy;
            }
            if (state == PersonState.AtHome)
            {
                if (IsHaveInfectedNeighbords())
                {
                    if (random.NextDouble() <= 0.5)
                        IsSick = true;
                }
                HomeStayingDuration++;
            }
            else if (state == PersonState.Walking)
            {
                HomeStayingDuration = 0;
                IsBored = false;
            }

            if (HomeStayingDuration > 4)
                IsBored = true;
        }

        private bool IsHaveInfectedNeighbords()
        {
            var sickNeighbords = Game.Instance.People.Where(p => p.HomeId == HomeId && p.IsSick);
            return sickNeighbords.Any(sn => sn.state == PersonState.AtHome);
        }

        private void CalcNextStepForPersonAtHome()
        {
            var goingWalk = random.NextDouble() < 0.005;
            if (!goingWalk)
                CalcNextPositionForStayingHomePerson();
            else
            {
                State = PersonState.Walking;
                CalcNextPositionForWalkingPerson();
            }

        }

        private void CalcNextPositionForStayingHomePerson()
        {
            var nextPosition = GenerateNextRandomPosition();

            if (isCoordInField(nextPosition) && IsCoordsInHouse(nextPosition))
                Position = nextPosition;
        }

        private bool IsCoordsInHouse(Vec vec)
        {
            var houseCoordinates = map.Houses[HomeId].Coordinates.LeftTopCorner;

            return
                vec.X >= houseCoordinates.X && vec.X <= HouseCoordinates.Width+ houseCoordinates.X &&
                vec.Y >= houseCoordinates.Y && vec.Y <= HouseCoordinates.Height+houseCoordinates.Y;
        }

        private Vec GenerateNextRandomPosition()
        {
            var xLength = random.Next(MaxDistancePerTurn);
            var yLength = MaxDistancePerTurn - xLength;
            var direction = ChooseRandomDirection();
            var delta = new Vec(xLength * direction.X, yLength * direction.Y);
            var nextPosition = new Vec(Position.X + delta.X, Position.Y + delta.Y);

            return nextPosition;
        }

        private void CalcNextPositionForGoingMarketPerson()
        {
            var xLength = random.Next(MaxDistancePerTurn);
            var yLength = MaxDistancePerTurn - xLength;
            var randomDirection = ChooseRandomDirection();
            var nextPosition = Position.Add(new Vec(xLength * randomDirection.X, yLength * randomDirection.Y));;
            var minDistance = double.MaxValue;
            foreach (var direction in Directions)
            {
                var delta = new Vec(xLength * direction.X, yLength * direction.Y);
                var nextPositionByCurrentDirection = Position.Add(delta);
                var marketCoordinate =
                    nearestMarket.Coordinates.LeftTopCorner.Add(
                        new Vec(HouseCoordinates.Width, HouseCoordinates.Height));
                var currentDistance = nextPositionByCurrentDirection.GetDistanceTo(marketCoordinate);
                if (currentDistance < minDistance && !IsCoordInAnyHouse(nextPositionByCurrentDirection))
                {
                    nextPosition = nextPositionByCurrentDirection;
                    minDistance = currentDistance;
                }
            }
            
            if (isCoordInField(nextPosition) && !IsCoordInAnyHouse(nextPosition))
            {
                Position = nextPosition;
            }
            else
            {
                CalcNextPositionForWalkingPerson();
            }
        }
        
        private void CalcNextPositionForWalkingPerson()
        {
            var xLength = random.Next(MaxDistancePerTurn);
            var yLength = MaxDistancePerTurn - xLength;
            var direction = ChooseRandomDirection();
            var delta = new Vec(xLength * direction.X, yLength * direction.Y);
            var nextPosition = new Vec(Position.X + delta.X, Position.Y + delta.Y);

            if (isCoordInField(nextPosition) && !IsCoordInAnyHouse(nextPosition))
            {
                Position = nextPosition;
            }
            else
            {
                CalcNextPositionForWalkingPerson();
            }
        }

        private bool CalcIsAtHome()
        {
            var game = Game.Instance;
            var homeCoordLeft = game.Map.Houses[HomeId].Coordinates.LeftTopCorner;
            var homeWidth = HouseCoordinates.Width;
            var homeHeight = HouseCoordinates.Height;
            if (Position.X < homeCoordLeft.X || Position.X >= homeCoordLeft.X + homeWidth)
                return false;
            if (Position.Y < homeCoordLeft.Y || Position.Y >= homeCoordLeft.Y + homeHeight)
                return false;
            return true;
        }

        private void CalcNextPositionForGoingHomePerson()
        {
            var game = Game.Instance;
            var homeCoord = game.Map.Houses[HomeId].Coordinates.LeftTopCorner;
            var homeCenter = new Vec(homeCoord.X + HouseCoordinates.Width / 2,
                homeCoord.Y + HouseCoordinates.Height / 2);

            var xDiff = homeCenter.X - Position.X;
            var yDiff = homeCenter.Y - Position.Y;
            var xDistance = Math.Abs(xDiff);
            var yDistance = Math.Abs(yDiff);

            var distance = xDistance + yDistance;
            if (distance <= MaxDistancePerTurn)
            {
                Position = homeCenter;
                State = PersonState.AtHome;
                return;
            }

            var direction = new Vec(Math.Sign(xDiff), Math.Sign(yDiff));

            var xLength = Math.Min(xDistance, MaxDistancePerTurn);
            var newX = Position.X + xLength * direction.X;
            var yLength = MaxDistancePerTurn - xLength;
            var newY = Position.Y + yLength * direction.Y;
            Position = new Vec(newX, newY);
        }

        public void GoHome()
        {
            if (State != PersonState.Walking) return;

            State = PersonState.GoingHome;
            CalcNextPositionForGoingHomePerson();
        }

        private Vec ChooseRandomDirection()
        {
            var index = random.Next(Directions.Length);
            return Directions[index];
        }

        /*private Vec ChooseNearestToMarketNextPosition()
        {
            Vec nearestToMarketDirection = null;
            var newMinDistanceToMarket = double.MaxValue;
            foreach (var direction in Directions)
            {
                var currentDistanceToMarket = nextPosition.GetDistanceTo(nearestMarket.Coordinates.LeftTopCorner);
                if (currentDistanceToMarket < newMinDistanceToMarket
                    && !IsCoordInAnyHouse(nextPosition))
                {
                    nearestToMarketDirection = direction;
                    newMinDistanceToMarket = currentDistanceToMarket;
                }
            }
            return nearestToMarketDirection ?? ChooseRandomDirection();
        }*/

        private bool isCoordInField(Vec vec)
        {
            var belowZero = vec.X < 0 || vec.Y < 0;
            var beyondField = vec.X > Game.FieldWidth || vec.Y > Game.FieldHeight;

            return !(belowZero || beyondField);
        }

        private bool IsCoordInAnyHouse(Vec vec) => map.Houses
            .Where(h => !h.IsMarket)
            .Any(h => IsCoordInHouse(vec, h));

        private static bool IsCoordInHouse(Vec vec, House house)
        {
            return vec.X > house.Coordinates.LeftTopCorner.X &&
                   vec.X < house.Coordinates.LeftTopCorner.X + HouseCoordinates.Width &&
                   vec.Y > house.Coordinates.LeftTopCorner.Y &&
                   vec.Y < house.Coordinates.LeftTopCorner.Y + HouseCoordinates.Height;
        }
    }
}