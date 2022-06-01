using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace opt_grote_opdracht {
	internal class Program {
		private static string globalBestWeek;
		private static double globalBestScore = double.MaxValue;
		private static int iteration;
		private static int threadCount;

		private enum LoopOption {
			INIT,
			IMPROVE,
			FINETUNE
		}

		public static void Main(string[] args) {
			Console.CancelKeyPress += (sender, args1) => { Print(); };

			Console.Write("Enter thread count: ");
			threadCount = int.Parse(Console.ReadLine());

			Start();
			Print();

			Console.ReadLine();
		}

		private static void Print() {
			Console.WriteLine(globalBestWeek);
			double score = new Week(new Random(), globalBestWeek).ScoreWithoutOvertime / 60;
			Console.WriteLine("Score: " + score);
		}

		private static void Start() {
			Random random = new Random();
			Week week = new Week(random);
			week.GenerateStart();

			// To load an existing solution:
			// string input = File.ReadAllText("../../input/best.txt");
			// Week week = new Week(random, input);

			globalBestScore = week.ScoreWithoutOvertime;
			globalBestWeek = week.ToString();

			Loop(LoopOption.INIT, 0, 100000); // 100.000 iterations
			Loop(LoopOption.IMPROVE, 3, 100000); // 100.000 iterations
			Loop(LoopOption.IMPROVE, 3, 300000); // 300.000 iterations
			Loop(LoopOption.IMPROVE, 2, 1000000); // 1.000.000 iterations
			Loop(LoopOption.FINETUNE, 2, 1000000); // 1.000.000 iterations
		}

		private static void Loop(LoopOption loopOption, int maxLoopIterations, int iterations) {
			int iterationsSinceLastImprovement = -1;
			while (iterationsSinceLastImprovement < maxLoopIterations) {
				Console.WriteLine("Iteration: " + iteration++);

				Tuple<double, string>[] results = new Tuple<double, string>[threadCount];
				Parallel.For(0, threadCount, index => {
					// Make sure every thread has a different seed
					Random random = new Random((index + 1) * DateTime.Now.Millisecond);
					Week week = new Week(random, globalBestWeek);

					double rand = random.NextDouble();
					int addOrders = 0, initTemp = 10;
					if (loopOption == LoopOption.INIT) {
						initTemp = 200;
					} else if (loopOption == LoopOption.IMPROVE) {
						if (rand <= 0.2) {
							addOrders = 2;
							initTemp = 10;
						} else if (rand <= 0.5) {
							addOrders = 5;
							initTemp = 20;
						} else if (rand <= 0.8) {
							addOrders = 10;
							initTemp = 40;
						} else {
							addOrders = 20;
							initTemp = 80;
						}
					} else if (loopOption == LoopOption.FINETUNE && iterationsSinceLastImprovement != 0) {
						week.DeleteRandomOrder().Invoke();
						addOrders = 5;
						initTemp = 40;
					}

					for (int i = 0; i < addOrders; i++) week.AddRandomOrder().Invoke();
					Console.WriteLine("[{0}] Starting with addOrders = {1} and initTemp = {2}", index, addOrders, initTemp);
					results[index] = SimAnn(initTemp, week, iterations, random, index);
					Console.WriteLine("[{0}] Finished with score {1}", index, results[index].Item1 / 60);
				});

				iterationsSinceLastImprovement++;
				(double bestScore, string bestWeek) = results.OrderBy(result => result.Item1).First();
				if (bestScore < globalBestScore) {
					// Found better week
					iterationsSinceLastImprovement = 0;
					globalBestScore = bestScore;
					globalBestWeek = bestWeek;
					Print();
				}
			}
		}

		private static Tuple<double, string> SimAnn(double initTemperature, Week week, int iterations, Random random, int index = -1) {
			double temperature = initTemperature;
			double alpha = 0.95;

			string bestWeek = week.ToString();
			double bestScore = week.Score;
			double bestScoreWithoutOvertime = week.ScoreWithoutOvertime;
			double currentScore, scoreWithoutOvertime;

			double rand;
			Action action;

			while (temperature > 1) {
				for (int i = 0; i < iterations; i++) {
					currentScore = week.Score;
					week.neighbourScore = currentScore;
					if (currentScore < bestScore) {
						bestScore = currentScore;
						scoreWithoutOvertime = week.ScoreWithoutOvertime;
						if (scoreWithoutOvertime < globalBestScore) {
							bestWeek = week.ToString();
							bestScoreWithoutOvertime = scoreWithoutOvertime;
							Console.WriteLine("[{0}] New best score: {1} ; {2} ; {3}",
								index, scoreWithoutOvertime / 60, temperature, i);
						}
					}

					rand = random.NextDouble();
					if (temperature >= 950) {
						if (rand <= 0.45) action = week.AddRandomOrder();
						else if (rand <= 0.8) action = week.DeleteRandomOrder();
						else action = week.Shift();
					} 
					else {
						if (rand <= 0.3) action = week.AddRandomOrder();
						else if (rand <= 0.5) action = week.DeleteRandomOrder();
						else if (rand <= 0.9996) action = week.Shift();
						else {
							week.TripTwoOpt();
							continue;
						}
					}

					// Can save having to use a Math.Pow
					if (currentScore > week.neighbourScore) {
						action();
						continue;
					}

					double p = AcceptanceProbability(currentScore, week.neighbourScore, temperature);
					if (p > random.NextDouble()) action();
				}

				temperature *= alpha;
			}

			return new Tuple<double, string>(bestScoreWithoutOvertime, bestWeek);
		}

		private static double AcceptanceProbability(double currentCost, double newCost, double T) {
			return Math.Pow(Math.E, (currentCost - newCost) / T);
		}
	}
}
