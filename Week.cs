using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace opt_grote_opdracht {
	public class Week {
		private Route[,] Routes { get; }
		private Random random;
		private List<Order> notVisited;
		private double penalty = 640760.39999999;
		private double time;
		private int tripCount;
		public double Score => time + penalty - tripCount * 900;
		public double ScoreWithoutOvertime => Routes.Cast<Route>().Sum(route => route.TotalTimeWithoutOvertime) + penalty;

		public double neighbourScore;
		private int penaltyMultiplier = 6;

		public Week(Random random) {
			Routes = new Route[5, 2];
			notVisited = new List<Order>(FileReader.ReadOrders().Values);
			this.random = random;

			foreach (Order order in notVisited) {
				if (order.VisitedCount > 0) order.Reset();
			}
		}

		public Week(Random random, string week) {
			Routes = new Route[5, 2];
			this.random = random;
			Dictionary<int, Order> orders = FileReader.ReadOrders();
            List<List<Order>>[,] best = new List<List<Order>>[5, 2];
            for (int day = 0; day < 5; day++) {
                for (int truck = 0; truck < 2; truck++) {
                    best[day, truck] = new List<List<Order>>();
                }
            }

            using (StringReader reader = new StringReader(week)) {
                string line;
                int trip = 0;
                int previousTruck = 0;
                int previousDay = 0;
                int previousOrder = 0;
                while ((line = reader.ReadLine()) != null) {
                    string[] attrs = line.Split(';');
                    int truck = int.Parse(attrs[0]);
                    int day = int.Parse(attrs[1]);
                    int order = int.Parse(attrs[3]);

                    if (previousTruck != truck || previousDay != day) trip = 0;
                    if (previousOrder == 0) best[day - 1, truck - 1].Add(new List<Order>());
                    if (order == 0) best[day - 1, truck - 1][trip++].Add(new Order());
                    else best[day - 1, truck - 1][trip].Add(orders[order]);

                    previousTruck = truck;
                    previousDay = day;
                    previousOrder = order;
                }
            }

            Order[,][][] end = new Order[5, 2][][];
            for (int day = 0; day < end.GetLength(0); day++) {
                for (int truck = 0; truck < end.GetLength(1); truck++) {
                    // Always keep one trip per route to prevent NullReferenceException in GetRandomTrip
                    if (best[day, truck].Count == 0) {
						end[day, truck] = new Order[1][];
						end[day, truck][0] = new Order[0];
						continue;
                    }

                    end[day, truck] = new Order[best[day, truck].Count][];
                    for (int trip = 0; trip < best[day, truck].Count; trip++) {
                        end[day, truck][trip] = best[day, truck][trip].ToArray();
                    }
                }
            }
            FillFromArray(end, orders);
		}

		// Pre-Processing Functions (or initilization)

		public void GenerateStart() {
			Action<float> updatePenalty = p => penalty += p;
			for (int day = 0; day < 5; day++) {
				for (int truck = 0; truck < 2; truck++) {
					Route route = new Route(day, delta => time += delta);
					route.AddTrip(new Trip(day, truck, notVisited, updatePenalty));
					Routes[day, truck] = route;
				}
			}
		}

		private void FillFromArray(Order[,][][] bestArray, Dictionary<int, Order> orders) {
			// penalty = CalculateTotalPenalty(orders.Values);
			notVisited = new List<Order>(orders.Values);
			// Because the Orders are not obtained from Trip.RandomOrder(), Orders are not placed at the end
			// of notVisited. This means Orders are not removed from the NotVisited List.
			// To solve this, we re-build the NotVisited List here.
			Dictionary<int, Order> newNotVisited = new Dictionary<int, Order>(orders);
			
			Action<float> updatePenalty = p => penalty += p;
			for (int day = 0; day < 5; day++) {
				for (int truck = 0; truck < 2; truck++) {
					Route route = new Route(day, delta => time += delta);
					for (int k = 0; k < bestArray[day, truck].Count(); k++) {
						Trip trip = new Trip(day, truck, notVisited, updatePenalty);
						route.AddTrip(trip);
						trip.FromOrderArray(bestArray[day, truck][k], ref tripCount);
						foreach (Order order in bestArray[day, truck][k]) {
							newNotVisited.Remove(order.Id);
						}
					}

					Routes[day, truck] = route;
				}
			}

			notVisited.Clear();
			notVisited.AddRange(newNotVisited.Values);
		}

		// Simulated Annealing Functions

		private Route GetRandomRoute() {
			return Routes[random.Next(0, 5), random.Next(0, 2)];
		}

		private Trip GetRandomTrip(Route route) {
			return route.Trips[random.Next(0, route.Trips.Count)];
		}

        private Tuple<Route, Trip>[] GetOtherTrips(Route route, Trip trip, Order order) {
            Tuple<Route, Trip>[] trips = new Tuple<Route, Trip>[5];
            trips[route.Day] = new Tuple<Route, Trip>(route, trip);

            if (order.Frequency == 2) {
                if (trips[0] != null) {
                    Route r = Routes[3, random.Next(0, 2)];
                    trips[r.Day] = new Tuple<Route, Trip>(r, GetRandomTrip(r));
                }
                else if (trips[1] != null) {
                    Route r = Routes[4, random.Next(0, 2)];
                    trips[r.Day] = new Tuple<Route, Trip>(r, GetRandomTrip(r));
                }
                else if (trips[3] != null) {
                    Route r = Routes[0, random.Next(0, 2)];
                    trips[r.Day] = new Tuple<Route, Trip>(r, GetRandomTrip(r));
                }
                else if (trips[4] != null) {
                    Route r = Routes[1, random.Next(0, 2)];
                    trips[r.Day] = new Tuple<Route, Trip>(r, GetRandomTrip(r));
                }
            }
            else if (order.Frequency == 3) {
                if (trips[0] != null) {
                    Route r1 = Routes[2, random.Next(0, 2)];
                    trips[r1.Day] = new Tuple<Route, Trip>(r1, GetRandomTrip(r1));
                    Route r2 = Routes[4, random.Next(0, 2)];
                    trips[r2.Day] = new Tuple<Route, Trip>(r2, GetRandomTrip(r2));
                }
                else if (trips[2] != null) {
                    Route r1 = Routes[0, random.Next(0, 2)];
                    trips[r1.Day] = new Tuple<Route, Trip>(r1, GetRandomTrip(r1));
                    Route r2 = Routes[4, random.Next(0, 2)];
                    trips[r2.Day] = new Tuple<Route, Trip>(r2, GetRandomTrip(r2));
                }
                else if (trips[4] != null) {
                    Route r1 = Routes[0, random.Next(0, 2)];
                    trips[r1.Day] = new Tuple<Route, Trip>(r1, GetRandomTrip(r1));
                    Route r2 = Routes[2, random.Next(0, 2)];
                    trips[r2.Day] = new Tuple<Route, Trip>(r2, GetRandomTrip(r2));
                }
            }
            else if (order.Frequency == 4) {
                int tripsCount = 1;
                for (int i = 0; i < 100 && tripsCount < 4; i++) {
					Route newRoute = GetRandomRoute();
                    if (trips[newRoute.Day] == null) {
                        trips[newRoute.Day] = new Tuple<Route, Trip>(newRoute, GetRandomTrip(newRoute));
                        tripsCount++;
                    }
                }
				if (tripsCount < 4) return new Tuple<Route, Trip>[0];
            }

            return trips;
        }

        private Tuple<Trip, Address> FullTrip(Route route, Trip trip, Order newOrder) {
			bool foundOtherTrip = false;
			Address after = null;
			foreach (Trip t in route.Trips) {
				if (t.TotalVolume + newOrder.CompressedVolume * newOrder.Count > 20000) continue;
				trip = t;
				after = trip.GetRandomAddress(random);
				foundOtherTrip = true;
				break;
			}

			if (!foundOtherTrip) {
				if (route.Trips.Count > 4) return null;
				trip = new Trip(trip.Day, trip.Truck, notVisited, p => penalty += p);
				route.AddTrip(trip);
				after = trip.Start;
			}

			return new Tuple<Trip, Address>(trip, after);
		}

		private void EditRouteTime(Route route, Trip trip, Order order, Address predecessor = null, bool add = false) {
			double routeWithoutOvertime = route.TotalTimeWithoutOvertime;
			routeWithoutOvertime += add ? trip.AddTime(order, predecessor) : trip.DeleteTime(order);
			double routeTime = routeWithoutOvertime <= 43200
				? routeWithoutOvertime
				: 43200 + 4 * (routeWithoutOvertime - 43200);

			neighbourScore -= route.TotalTime;
			neighbourScore += routeTime;
		}

		public Action AddOrder(Route route, Trip trip, Order order, Address predecessor) {
			if (order.Frequency == 1) {
				if (trip.TotalVolume + order.CompressedVolume * order.Count > 20000) {
					if (random.NextDouble() <= 0.5) {
						Tuple<Trip, Address> change = FullTrip(route, trip, order);
						if (change == null) return AddRandomOrder(0);
						trip = change.Item1;
						predecessor = change.Item2;
                    }
					// Allow capacity to be overridden
					else {
						neighbourScore += penaltyMultiplier * (trip.TotalVolume + order.CompressedVolume * order.Count - 20000);
                    }
				}

				EditRouteTime(route, trip, order, predecessor, true);
				// Subtract penalty
				neighbourScore -= 3 * order.Frequency * order.Duration;

				return () => { 
					trip.Add(order, predecessor, ref tripCount); 
				};
			}
			else {
				Tuple<Route, Trip>[] trips = GetOtherTrips(route, trip, order);
				Address[] addresses = new Address[5];

				// First check all trips for availability
				for (int i = 0; i < trips.Length; i++) {
					if (trips[i] == null) continue;

					Route r = trips[i].Item1;
					Trip t = trips[i].Item2;
					predecessor = t.GetRandomAddress(random);
					if (t.TotalVolume + order.CompressedVolume * order.Count > 20000) {
						if (random.NextDouble() <= 0.5) {
							Tuple<Trip, Address> change = FullTrip(r, t, order);
							if (change == null) return AddRandomOrder(0);
							t = change.Item1;
							trips[i] = new Tuple<Route, Trip>(r, t);
							predecessor = change.Item2;
                        }
						else {
							neighbourScore += penaltyMultiplier * (t.TotalVolume + order.CompressedVolume * order.Count - 20000);
                        }
					}
					addresses[r.Day] = predecessor;
				}
				// Then calculate new time
				for (int i = 0; i < trips.Length; i++) {
					if (trips[i] == null) continue;
					EditRouteTime(trips[i].Item1, trips[i].Item2, order, addresses[trips[i].Item1.Day], true);
				}
				neighbourScore -= 3 * order.Frequency * order.Duration;

				return () => {
					for (int i = 0; i < trips.Length; i++) {
						if (trips[i] == null) continue;
						trips[i].Item2.Add(order, addresses[trips[i].Item1.Day], ref tripCount);
					}
				};
			}
		}

		public Action AddRandomOrder(int maxDepth = 200) {
			if (maxDepth == 0) return () => { };

			Route route = GetRandomRoute();
			Trip trip = GetRandomTrip(route);
			Order newOrder = trip.RandomOrder(random);
			Address after = trip.GetRandomAddress(random);

			if (newOrder == null) return AddRandomOrder(maxDepth - 1);
			return AddOrder(route, trip, newOrder, after);
		}

		// Removes every instance of given order
		public Action DeleteOrder(Route route, Trip trip, Order order) {
			if (order.Frequency == 1) {
				EditRouteTime(route, trip, order);
				if (trip.TotalVolume > 20000 && trip.TotalVolume - order.CompressedVolume * order.Count < 20000)
					neighbourScore -= penaltyMultiplier * (trip.TotalVolume - 20000);
				// Add penalty
				neighbourScore += 3 * order.Frequency * order.Duration;

				return () => {
					trip.Remove(order, ref tripCount);
				};
			}
			else {
				// Make a list of trips containing this order, so there is no need for a second loop throughout all Routes
				List<Trip> trips = new List<Trip>(5);

				foreach (Route r in Routes) {
					foreach (Trip t in r.Trips) {
						if (t.Addresses.ContainsKey(order.Id)) {
							EditRouteTime(r, t, order);
							if (t.TotalVolume > 20000 && t.TotalVolume - order.CompressedVolume * order.Count < 20000)
								neighbourScore -= penaltyMultiplier * (t.TotalVolume - 20000);
							trips.Add(t);
						}
					}
				}
				neighbourScore += 3 * order.Frequency * order.Duration;

				return () => {
					foreach (Trip t in trips) {
						t.Remove(order, ref tripCount);
                    }
				};
			}
		}

		public Action DeleteRandomOrder(int maxDepth = 200) {
			if (maxDepth == 0) return () => { };
			Route route = GetRandomRoute();
			Trip trip = GetRandomTrip(route);
			Address address = trip.GetRandomAddress(random);
			if (address == null) return DeleteRandomOrder(maxDepth - 1);

			return DeleteOrder(route, trip, address.Order);
		}

		public Action Shift(int maxDepth = 10) {
			if (maxDepth == 0) return () => { };
			Route r1 = GetRandomRoute();
			Trip t1 = GetRandomTrip(r1);
			Address address = t1.GetRandomAddress(random);
			if (address == null) return Shift(maxDepth - 1);
			Order order = address.Order;

			Route r2 = GetRandomRoute();
			Trip t2 = GetRandomTrip(r2);
			Address predecessor = t2.GetRandomAddress(random);
			// Skip uneditable trips and direct neighbours
			if ((t1 == t2 && t1.TotalAddresses <= 2) || 
				address.Predecessor == predecessor || address == predecessor || address.Successor == predecessor)  
				return Shift(maxDepth - 1);

			// When the shift takes place in a single trip, a lot of checks can be skipped
			if (t1 == t2) {
				EditRouteTime(r1, t1, order);
				EditRouteTime(r2, t2, order, predecessor, true);
				// Penalty remains unaffected

				return () => {
					t1.Remove(order, ref tripCount);
					t2.Add(order, predecessor, ref tripCount);
				};
			}
			else if (order.Frequency == 1) {
				if (t2.TotalVolume + order.CompressedVolume * order.Count > 20000) {
					if (random.NextDouble() <= 0.5) {
						Tuple<Trip, Address> change = FullTrip(r2, t2, order);
						if (change == null || t1 == change.Item1) return Shift(maxDepth - 1);
						t2 = change.Item1;
						predecessor = change.Item2;
                    }
					else {
						neighbourScore += penaltyMultiplier * (t2.TotalVolume + order.CompressedVolume * order.Count - 20000);
					}
				}
				if (t1.TotalVolume > 20000 && t1.TotalVolume - order.CompressedVolume * order.Count < 20000)
					neighbourScore -= penaltyMultiplier * (t1.TotalVolume - 20000);

				EditRouteTime(r1, t1, order);
				EditRouteTime(r2, t2, order, predecessor, true);
				// Penalty remains unaffected

				return () => {
					t1.Remove(order, ref tripCount);
					t2.Add(order, predecessor, ref tripCount);
				};
			}
            else {
                // To pass the IsAvailable check, we have to simulate removing the address in the most efficient way possible
                Address currentAddress = order.Visited[t1.Day];
                order.Visited[t1.Day] = null;

                if (!t2.IsAvailable(order)) {
                    order.Visited[t1.Day] = currentAddress;
                    return Shift(maxDepth - 1);
                }
                order.Visited[t1.Day] = currentAddress;

				if (t2.TotalVolume + order.CompressedVolume * order.Count > 20000) {
					if (random.NextDouble() <= 0.5) {
						Tuple<Trip, Address> change = FullTrip(r2, t2, order);
						if (change == null || t1 == change.Item1) return Shift(maxDepth - 1);
						t2 = change.Item1;
						predecessor = change.Item2;
					}
					else {
						neighbourScore += penaltyMultiplier * (t2.TotalVolume + order.CompressedVolume * order.Count - 20000);
					}
				}
				if (t1.TotalVolume > 20000 && t1.TotalVolume - order.CompressedVolume * order.Count < 20000)
					neighbourScore -= penaltyMultiplier * (t1.TotalVolume - 20000);

				EditRouteTime(r1, t1, order);
				EditRouteTime(r2, t2, order, predecessor, true);
                // Penalty remains unaffected

                return () => {
                    t1.Remove(order, ref tripCount);
                    t2.Add(order, predecessor, ref tripCount);
                };
            }
        }

		// As this function is called so seldom, make sure it is applied to a valid trip
		public void TripTwoOpt() {
			Trip trip = null;
			for (int i = 0; i < 100 && trip == null; i++) {
				Trip getTrip = GetRandomTrip(GetRandomRoute());
				if (getTrip.TotalAddresses > 3) trip = getTrip;
            }
			if (trip != null) trip.TwoOpt(false);
        }

		public void TwoOpt() {
			foreach (Route route in Routes) {
				foreach (Trip trip in route.Trips) {
					trip.TwoOpt();
				}
			}
		}

		public Order[,][][] ToArray() {
			Order[,][][] res = new Order[Routes.GetLength(0), Routes.GetLength(1)][][];
			for (int day = 0; day < 5; day++) {
				for (int truck = 0; truck < 2; truck++) {
					Route route = Routes[day, truck];
					res[day, truck] = new Order[route?.Trips.Count ?? 0][];
					for (int trip = 0; trip < route.Trips.Count; trip++) {
						res[day, truck][trip] = route.Trips[trip].ToOrderArray();
					}
				}
			}

			return res;
		}

		// Post-Processing functions

		public override string ToString() {
			return PrintOrders(ToArray());
		}

		public static string PrintOrders(Order[,][][] orders) {
			string res = "";
			for (int day = 0; day < orders.GetLength(0); day++) {
				for (int truck = 0; truck < orders.GetLength(1); truck++) {
					int count = 1;
					for (int trip = 0; trip < orders[day, truck].Length; trip++) {
						if (orders[day, truck][trip].Length <= 1) continue;
						foreach (Order order in orders[day, truck][trip]) {
							res += (truck + 1) + ";" + (day + 1) + ";" + count++ + ";" + order.Id + "\n";
						}
					}
				}
			}

			return res;
		}
	}
}