using System;
using System.Linq;
using covidSim.Models;

namespace covidSim.Services
{
    public class Person
    {
        private const int MaxDistancePerTurn = 30;
        private static Random random = new Random();
        private PersonState state = PersonState.AtHome;
        private int IllnessDuration;
        private int HomeStayingDuration;
        private Vec homeCoords;

        public Person(int id, int homeId, CityMap map, bool isSick)
        {
            Id = id;
            HomeId = homeId;
            IsSick = isSick;
            if (IsSick)
                IllnessDuration = 35;
                

            homeCoords = map.Houses[homeId].Coordinates.LeftTopCorner;
            var x = homeCoords.X + random.Next(HouseCoordinates.Width);
            var y = homeCoords.Y + random.Next(HouseCoordinates.Height);
            Position = new Vec(x, y);
        }

        public int Id;
        public int HomeId;
        public Vec Position;
        public bool IsSick;

        public bool IsBored;

        public void CalcNextStep()
        {
            if (IsSick)
                IllnessDuration--;
            if (IllnessDuration == 0)
                IsSick = false;

            switch (state)
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
            {
                CalcNextPositionInHome();
                return;
            }

            state = PersonState.Walking;
            CalcNextPositionForWalkingPerson();
        }

        private bool isCoordInHome(Vec vec)
        {
            var xRight = homeCoords.X + random.Next(HouseCoordinates.Width);
            var yBottom = homeCoords.Y + random.Next(HouseCoordinates.Height);

            var belowHome = vec.X < homeCoords.X || vec.Y < homeCoords.Y;
            var beyondHome = vec.X > xRight || vec.Y > yBottom;

            return !(belowHome || beyondHome);
        }

        private void CalcNextPositionInHome()
        {
            var nextPosition = GetNextPosition();

            if (isCoordInHome(nextPosition))
            {
                Position = nextPosition;
            }
            else
            {
                CalcNextPositionInHome();
            }
        }

        private void CalcNextPositionForWalkingPerson()
        {
            var nextPosition = GetNextPosition();

            if (IsCoordInField(nextPosition) && IsCoordNotInOtherHouse(nextPosition))
            {
                Position = nextPosition;
            }
            else
            {
                CalcNextPositionForWalkingPerson();
            }
        }

        private Vec GetNextPosition()
        {
            var xLength = random.Next(MaxDistancePerTurn);
            var yLength = MaxDistancePerTurn - xLength;
            var direction = ChooseDirection();
            var delta = new Vec(xLength * direction.X, yLength * direction.Y);
            var nextPosition = new Vec(Position.X + delta.X, Position.Y + delta.Y);
            return nextPosition;
        }

        private void CalcNextPositionForGoingHomePerson()
        {
            var game = Game.Instance;
            var homeCoord = game.Map.Houses[HomeId].Coordinates.LeftTopCorner;
            var homeCenter = new Vec(homeCoord.X + HouseCoordinates.Width / 2, homeCoord.Y + HouseCoordinates.Height / 2);

            var xDiff = homeCenter.X - Position.X;
            var yDiff = homeCenter.Y - Position.Y;
            var xDistance = Math.Abs(xDiff);
            var yDistance = Math.Abs(yDiff);

            var distance = xDistance + yDistance;
            if (distance <= MaxDistancePerTurn)
            {
                Position = homeCenter;
                state = PersonState.AtHome;
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
            if (state != PersonState.Walking) return;

            state = PersonState.GoingHome;
            CalcNextPositionForGoingHomePerson();
        }

        private Vec ChooseDirection()
        {
            var directions = new Vec[]
            {
                new Vec(-1, -1),
                new Vec(-1, 1),
                new Vec(1, -1),
                new Vec(1, 1),
            };
            var index = random.Next(directions.Length);
            return directions[index];
        }

        private bool IsCoordInField(Vec vec)
        {
            var belowZero = vec.X < 0 || vec.Y < 0;
            var beyondField = vec.X > Game.FieldWidth || vec.Y > Game.FieldHeight;

            return !(belowZero || beyondField);
        }

        private bool IsCoordNotInOtherHouse(Vec vec)
        {
            return Game.Instance.Map.Houses
                .Where(x => x.Id != HomeId)
                .All(x => !IsCoordInHouse(vec, x.Coordinates));
        }

        private bool IsCoordInHouse(Vec vec, HouseCoordinates houseCoordinates)
        {
            var homeCoord = houseCoordinates.LeftTopCorner;
            var homeCenter = new Vec(homeCoord.X + HouseCoordinates.Width / 2,
                homeCoord.Y + HouseCoordinates.Height / 2);
            if (homeCenter.X - HouseCoordinates.Width / 2 <= vec.X &&
                vec.X <= homeCenter.X + HouseCoordinates.Width / 2 &&
                homeCenter.Y - HouseCoordinates.Height / 2 <= vec.Y &&
                vec.Y <= homeCenter.Y + HouseCoordinates.Height / 2)
                return true;
            return false;
        }
    }
}