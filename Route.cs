using System;
using System.Collections.Generic;

namespace opt_grote_opdracht {
	public class Route {
		public int Day { get; }

		public List<Trip> Trips { get; } = new List<Trip>();
		private double totalTime;
		private double totalTimeWithoutOvertime;
		public double TotalTime => totalTime;
		public double TotalTimeWithoutOvertime => totalTimeWithoutOvertime;

		private Action<double> updateTime;

		public Route(int day, Action<double> updateTime) {
			Day = day;
			this.updateTime = updateTime;
		}

		public void AddTrip(Trip trip) {
			trip.updateTime = deltaTrip => {
				totalTimeWithoutOvertime += deltaTrip;
				double before = totalTime;
				totalTime = totalTimeWithoutOvertime <= 43200
					? TotalTimeWithoutOvertime
					: 43200 + 4 * (totalTimeWithoutOvertime - 43200);
				updateTime(totalTime - before);
			};
			Trips.Add(trip);
		}
	}
}