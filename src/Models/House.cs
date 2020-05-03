namespace covidSim.Models
{
    public class House
    {
        public House(int id, Vec cornerCoordinates, bool isMarket = false)
        {
            Id = id;
            Coordinates = new HouseCoordinates(cornerCoordinates);
            IsMarket = isMarket;
        }

        public int Id;
        public HouseCoordinates Coordinates;
        public bool IsMarket;
        public int ResidentCount = 0;
    }
}