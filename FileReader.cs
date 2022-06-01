using System.Collections.Generic;
using System.IO;

namespace opt_grote_opdracht {
	public class FileReader {
		public static DistMatEntry[,] ReadDistMat() {
			DistMatEntry[,] distMat = new DistMatEntry[1099, 1099];
			using (StreamReader reader = new StreamReader("../../input/AfstandenMatrix.txt")) {
				bool firstLine = true;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (firstLine) {
						firstLine = false;
						continue;
					}

					string[] attrs = line.Split(';');
					distMat[int.Parse(attrs[0]), int.Parse(attrs[1])] =
						new DistMatEntry(int.Parse(attrs[2]), int.Parse(attrs[3]));
				}
			}

			return distMat;
		}

		public static Dictionary<int, Order> ReadOrders() {
			Dictionary<int, Order> orders = new Dictionary<int, Order>();
			using (StreamReader reader = new StreamReader("../../input/Orderbestand.txt")) {
				bool firstLine = true;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (firstLine) {
						firstLine = false;
						continue;
					}

					Order o = new Order(line);
					orders.Add(o.Id, o);
				}
			}

			return orders;
		}
	}
}