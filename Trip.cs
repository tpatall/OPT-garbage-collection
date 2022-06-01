using System;
using System.Collections.Generic;
using System.Linq;

namespace opt_grote_opdracht {
	public class Trip {
		private Order dumpOrder = new Order();
		private List<Order> NotVisited;
		private Dictionary<int, Address> addresses = new Dictionary<int, Address>(1178);
		public Dictionary<int, Address> Addresses => addresses;

		public int TotalAddresses => addresses.Count;

		public Address Start => Addresses[dumpOrder.Id];
		private Address lastAddress;
		public Address LastAddress => lastAddress;

		public Action<double> updateTime;
		private double totalTime;
		public double TotalTime {
			get {
				return totalTime;
			}
			private set {
				updateTime(value - totalTime);
				totalTime = value;
			}
		}
		private int totalVolume;
		public int TotalVolume => totalVolume;

		private int day;
		public int Day => day;
		private int truck;
		public int Truck => truck;

		private Action<float> updatePenalty;
		private int penaltyMultipler = 6;

		private DistMatEntry[,] dm = InputData.GetInstance().DistMat;

		public Trip(int day, int truck, List<Order> notVisited, Action<float> updatePenalty) {
			NotVisited = notVisited;
			this.day = day;
			this.truck = truck;
			this.updatePenalty = updatePenalty;
			Address address = new Address(dumpOrder, null, null);
			addresses.Add(address.Order.Id, address);
			lastAddress = address;
		}

		public void Add(Order order, Address insertAfter, ref int tripCount) {
			if (TotalAddresses == 1) {
				TotalTime = 1800;
				tripCount++;
            }
			if (insertAfter == null) insertAfter = lastAddress;
			if (insertAfter == lastAddress) TotalTime -= dm[lastAddress.Order.MatrixId, dumpOrder.MatrixId].Time;
			Address insertBefore = insertAfter?.Successor;
			if (insertBefore != null) TotalTime -= dm[insertAfter.Order.MatrixId, insertBefore.Order.MatrixId].Time;
			
			Address address = new Address(order, insertAfter, insertBefore);
			
			order.Visited[day] = address;
			order.VisitedCount++;
			if (order.Fulfilled) {
				updatePenalty(-3 * order.Frequency * order.Duration);
				// We move an addres to the last position of notYetVisited in RandomOrder.
				if (NotVisited[NotVisited.Count - 1].Id == order.Id) {
					NotVisited.RemoveAt(NotVisited.Count - 1);
				}
            }
			addresses.Add(order.Id, address);

			if (insertAfter != null) {
				insertAfter.Successor = address;
				TotalTime += dm[insertAfter.Order.MatrixId, order.MatrixId].Time;
			}

			if (insertBefore != null) {
				insertBefore.Predecessor = address;
				TotalTime += dm[order.MatrixId, insertBefore.Order.MatrixId].Time;
			}
			
			TotalTime += order.Duration;
			int previousVolume = totalVolume;
			totalVolume += order.CompressedVolume * order.Count;
			if (totalVolume > 20000) {
				if (previousVolume <= 20000) updatePenalty(4 * (TotalVolume - 20000));
				else {
					// Remove previous penalty
					updatePenalty(-penaltyMultipler * (previousVolume - 20000));
					// Add new penalty
					updatePenalty(penaltyMultipler * (totalVolume - 20000));
                }
			}
			if (address.Successor == null) {
				TotalTime += dm[order.MatrixId, dumpOrder.MatrixId].Time;
				lastAddress = address;
			}
		}

		/**
		 * Remove an order from the route.
		 */
		public void Remove(Order order, ref int tripCount) {
			Address address = addresses[order.Id];
			RemoveNodeTime(address);
			TotalTime -= order.Duration;
			int previousVolume = totalVolume;
            totalVolume -= order.CompressedVolume * order.Count;
			if (previousVolume > 20000) {
				if (totalVolume <= 20000) updatePenalty(-4 * (previousVolume - 20000));
				else {
					// Remove previous penalty
					updatePenalty(-penaltyMultipler * (previousVolume - 20000));
					// Add new penalty
					updatePenalty(penaltyMultipler * (totalVolume - 20000));
                }
            }

			if (order.Fulfilled) {
				NotVisited.Add(order);
				updatePenalty(3 * order.Frequency * order.Duration);
			}
			order.Visited[day] = null;
			order.VisitedCount--;
			if (address.Successor != null && address.Predecessor != null) {
				// Link the previous order to the next order.
				address.Successor.Predecessor = address.Predecessor;
				TotalTime += dm[address.Predecessor.Order.MatrixId, address.Successor.Order.MatrixId].Time;
			}
			if (lastAddress == address) {
				TotalTime -= dm[address.Order.MatrixId, dumpOrder.MatrixId].Time;
				lastAddress = address.Predecessor;
				TotalTime += dm[address.Predecessor.Order.MatrixId, dumpOrder.MatrixId].Time;
			}
			address.Predecessor.Successor = address.Successor;
			addresses.Remove(order.Id);
			if (TotalAddresses == 1) {
				TotalTime = 0;
				tripCount--;
            }
		}

		public double AddTime(Order order, Address insertAfter) {
			double currentTime = totalTime;

			// If adding to a new trip
			if (TotalAddresses == 1) {
				currentTime = 1800 - 900; // 1800 for loading time at dump, and subtract 900 for the tripCount reduction
				currentTime += dm[Start.Order.MatrixId, order.MatrixId].Time;
				currentTime += dm[order.MatrixId, dumpOrder.MatrixId].Time;
			}
			// If adding to end of trip
			else if (insertAfter == lastAddress || insertAfter.Successor == null) {
				currentTime -= dm[lastAddress.Order.MatrixId, dumpOrder.MatrixId].Time;
				currentTime += dm[lastAddress.Order.MatrixId, order.MatrixId].Time;
				currentTime += dm[order.MatrixId, dumpOrder.MatrixId].Time;
			}
			else {
				currentTime -= dm[insertAfter.Order.MatrixId, insertAfter.Successor.Order.MatrixId].Time;
				currentTime += dm[insertAfter.Order.MatrixId, order.MatrixId].Time;
				currentTime += dm[order.MatrixId, insertAfter.Successor.Order.MatrixId].Time;
			}

			// Add loading time
			currentTime += order.Duration;

			// If totalTime < currentTime, then this addition is a detoriation
			return currentTime - totalTime;
        }

		public double DeleteTime(Order order) {
			double currentTime = totalTime;
			Address address = addresses[order.Id];

			// If this is the only address (besides the dump)
			if (TotalAddresses == 2) return -totalTime + 900;
			// If removing at end of trip
			else if (address == lastAddress || address.Successor == null) {
				currentTime -= dm[address.Predecessor.Order.MatrixId, order.MatrixId].Time;
				currentTime -= dm[order.MatrixId, dumpOrder.MatrixId].Time;
				currentTime += dm[address.Predecessor.Order.MatrixId, dumpOrder.MatrixId].Time;
			}
			else {
				currentTime -= dm[address.Predecessor.Order.MatrixId, order.MatrixId].Time;
				currentTime -= dm[order.MatrixId, address.Successor.Order.MatrixId].Time;
				currentTime += dm[address.Predecessor.Order.MatrixId, address.Successor.Order.MatrixId].Time;
			}

			// Remove loading time
			currentTime -= order.Duration;

			// If totalTime > currentTime, then this removal is a detoriation
			return currentTime - totalTime;
		}

		public Address GetRandomAddress(Random random) {
			if (TotalAddresses <= 1) return null;
			return addresses.ElementAt(random.Next(1, addresses.Count)).Value;
		}

		public void SwapRange(Address address1, Address address2) {

			SwapTime(address1, address2);

			Address before = address1.Predecessor, after = address2.Successor;
			Address prev = address2.Successor, current = address1, next;
			while (current.Order.Id != address2.Order.Id) {
				next = current.Successor;
				current.Successor = prev;
				prev = current;
				current = next;
			}
			current.Successor = prev;
			before.Successor = address2;

			prev = address1.Predecessor;
			current = address2;
			while (current.Order.Id != address1.Order.Id) {
				next = current.Successor;
				current.Predecessor = prev;
				prev = current;
				current = next;
			}
			current.Predecessor = prev;
			if (after != null) after.Predecessor = address1;

			SwapTime(address2, address1, 1);

			if (lastAddress == address2) lastAddress = address1;
		}

		private void SwapTime(Address from, Address to, int op = -1)
        {
			double dt = 0;
			while (from != to.Successor) {
				dt += dm[from.Predecessor.Order.MatrixId, from.Order.MatrixId].Time;
				from = from.Successor;
			}

			if (to.Successor != null) dt += dm[to.Order.MatrixId, to.Successor.Order.MatrixId].Time;
			else dt += dm[to.Order.MatrixId, dumpOrder.MatrixId].Time;
			TotalTime += op * dt;
		}

		public void TwoOpt(bool withLoop = true) {
			double oldTime;
			for (Address address1 = Start.Successor; address1 != null; address1 = address1.Successor) {
				for (Address address2 = address1.Successor; address2 != null; address2 = address2.Successor) {
					oldTime = TotalTime;
					SwapRange(address1, address2);
					if (TotalTime >= oldTime) {
						SwapRange(address2, address1);
					} else {
						if (withLoop) TwoOpt();
						return;
					}
				}
			}
		}

		/**
		 * Remove the time needed to drive to and from the given node.
		 */
		private void RemoveNodeTime(params Address[] removeOrders) {
			foreach (Address address in removeOrders) {
				if (address == null) continue;
				if (address.Successor != null) {
					TotalTime -= dm[address.Order.MatrixId, address.Successor.Order.MatrixId].Time;
				}
				if (address.Predecessor != null) {
					TotalTime -= dm[address.Predecessor.Order.MatrixId, address.Order.MatrixId].Time;
				}
			}
		}

		public Order[] ToOrderArray() {
			Order[] orderArr = new Order[TotalAddresses];
			Address address = Start.Successor;

			for (int i = 0; i < orderArr.Length - 1; i++) {
				orderArr[i] = address.Order;
				address = address.Successor;
			}

			orderArr[orderArr.Length - 1] = dumpOrder;
			
			return orderArr;
		}

		public void FromOrderArray(Order[] orderArr, ref int tripCount) {
			for (int i = 0; i < orderArr.Length - 1; i++) {
				Add(orderArr[i], null, ref tripCount);
            }
        }

		public Order RandomOrder(Random random) {
			int count = NotVisited.Count;
			if (count == 0) return null;
			Order last = NotVisited[count - 1];
			Order order;
			for (int i = 0; i < 1000; i++) {
				int index = random.Next(0, count);
				order = NotVisited[index];
				if (!IsAvailable(order)) continue;
				// Swap order with the last order
				NotVisited[index] = last;
				NotVisited[count - 1] = order;
				return order;
			}
			return null;
        }

		// Frequency options:
		// 1PWK = every day posssible
		// 2PWK = (monday && thursday) || (tuesday && friday)
		// 3PWK = monday && wednesday && friday
		// 4PWK = every combination of 4 days posssible

		/**
		 * Return bool based on availability
		 */
		public bool IsAvailable(Order order) {
			bool result = false;
			if (order.Fulfilled) return false;
			else if (order.VisitedCount == 0)
            {
				if (order.Frequency == 1 || order.Frequency == 4)
					result = true;
				else if (order.Frequency == 2 && (day == 0 || day == 1 || day == 3 || day == 4))
					result = true;
				else if (order.Frequency == 3 && (day == 0 || day == 2 || day == 4))
					result = true;
            }
			else if (order.Frequency == 2) {
				if (day == 0 && order.Visited[3] != null) result = true;
				else if (day == 1 && order.Visited[4] != null) result = true;
				else if (day == 3 && order.Visited[0] != null) result = true;
				else if (day == 4 && order.Visited[1] != null) result = true;
			}
			else if (order.Frequency == 3 && (day == 0 || day == 2 || day == 4) &&
				order.Visited[day] == null)
				result = true;
			else if (order.Frequency == 4 && order.Visited[day] == null)
				result = true;

			return result;
		}
	}
}