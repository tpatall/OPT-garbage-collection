namespace opt_grote_opdracht {
	public class InputData {
		private static InputData instance;
		public DistMatEntry[,] DistMat { get; }

		private InputData() {
			DistMat = FileReader.ReadDistMat();
		}

		public static InputData GetInstance() {
			if (instance != null) return instance;
			instance = new InputData();
			return instance;
		}
	}
}