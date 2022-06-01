namespace opt_grote_opdracht {
	public class Address {
		public Order Order { get; }
		public Address Predecessor { get; set; }
		public Address Successor { get; set; }

		public Address(Order order, Address predecessor, Address successor) {
			Order = order;
			Predecessor = predecessor;
			Successor = successor;
		}
	}
}