namespace opt_grote_opdracht {
	public class Order {
		public int Id { get; }
		public int Frequency { get; }
		public int Count { get; }
		public int Volume { get; }
		public int CompressedVolume => Volume / 5;
		public float Duration { get; }
		public int MatrixId { get; }

        public Address[] Visited { get; set; }
		public int VisitedCount { get; set; }

        public bool Fulfilled => VisitedCount == Frequency;

		public void Reset()
        {
            Visited = new Address[5];
			VisitedCount = 0;
        }

        public Order() {
			Id = 0;
			MatrixId = 287;
		}

		public Order(string entry) {
			string[] attrs = entry.Split(';');
			Id = int.Parse(attrs[0]);
			Frequency = int.Parse(attrs[2].Substring(0, 1));
			Visited = new Address[5];
			VisitedCount = 0;
			Count = int.Parse(attrs[3]);
			Volume = int.Parse(attrs[4]);
			Duration = float.Parse(attrs[5]) * 60;
			MatrixId = int.Parse(attrs[6]);
		}
	}
}