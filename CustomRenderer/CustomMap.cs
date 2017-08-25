using System.Collections.Generic;
using Xamarin.Forms.Maps;

namespace CustomRenderer
{
    public class CustomMap : Map
    {
        public List<CustomPin> CustomPins { get; set; }
        public List<Position> RouteCoordinates { get; set; }

        public CustomMap() {
            RouteCoordinates = new List<Position>();
        }
	}
}