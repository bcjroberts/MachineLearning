using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.MachineLearning.DecisionTrees.Rules;
using Accord.Math;
using Accord.Statistics.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace C4._5Test {
	class Program {
		static void Main(string[] args) {
			MyTest myTest = new MyTest();
			myTest.runTest();

		}
	}

	class DataHolder {
		public int dataAmount = 0;
		public double readTime = 0;
		public double codeTime = 0;
		public double treeCreationTime = 0;
		public double learningCreationTime = 0;
		public double treeLearningTime = 0;
		public double ruleFetchTime = 0;
		public DataHolder (int ndataAmount) {
			dataAmount = ndataAmount;
		}

		public override string ToString () {
			string result = "Amount of Data: " + dataAmount + "\n";
			result += "Time to read data: " + readTime + "\n";
			result += "Time to Codify the data: " + codeTime + "\n";
			result += "Time to Create Decision Tree: " + treeCreationTime + "\n";
			result += "Time to Create C4.5: " + learningCreationTime + "\n";
			result += "Time to Learn the Tree: " + treeLearningTime + "\n";
			result += "Time to Fetch Rules: " + ruleFetchTime + "\n";
			result += "Total Time: " + getTotalTime();
			return result;
		}

		public double getTotalTime () {
			return readTime + codeTime + treeCreationTime + learningCreationTime + treeLearningTime + ruleFetchTime;
		}
	}

	class MyTest {
		public MyTest() {

		}

		public void runTest() {

			int[] amountOfDataToRead = { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000, 11000,
				12000, 13000, 14000, 15000, 16000, 17000, 18000, 19000, 20000 };
			int dataIndex = 0;

			List<DataHolder> dataHolders = new List<DataHolder>();
			DateTime previousTime;

			while (dataIndex < amountOfDataToRead.Length) {
				previousTime = DateTime.Now;
				DataHolder dt = new DataHolder(amountOfDataToRead[dataIndex]);
				dataHolders.Add(dt);

				int amountOfDataRead = -1;
				StreamReader reader = File.OpenText("../../../../ncdb_2015.csv");
				string line;
				bool first = true;
				DataTable data = new DataTable("2015 Collision Data");

				while ((line = reader.ReadLine()) != null && amountOfDataRead < amountOfDataToRead[dataIndex]) {
					string[] items = line.Split(',');
					if (first) { // These are the headers
						data.Columns.Add("C_CASE");
						for (int i = 0; i < items.Length; i++) {
							if (!items[i].Equals("P_ISEV") && !items[i].Equals("C_CASE") && !items[i].Equals("C_YEAR"))
								data.Columns.Add(items[i]);
						}
						data.Columns.Add("P_ISEV");
						first = false;
					} else {
						data.Rows.Add(items[22], items[1], items[2], items[3], items[4], items[5], items[6], items[7], items[8]
							, items[9], items[10], items[11], items[12], items[13], items[14], items[15], items[16], items[17],
							items[18], items[20], items[21], items[19]);
					}
					amountOfDataRead++;
				}
				reader.Close();

				dt.readTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;
				Console.WriteLine("Finished reading " + amountOfDataToRead[dataIndex] + " data, codifying...");
				
				Codification codebook = new Codification(data);

				dt.codeTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;

				Console.WriteLine("Creating decision tree...");
				DecisionVariable[] attributes =
				{
					new DecisionVariable("C_MNTH", 14),
					new DecisionVariable("C_WDAY", 9),
					new DecisionVariable("C_HOUR", 25),
					new DecisionVariable("C_SEV", 4),
					new DecisionVariable("C_VEHS", 101),
					new DecisionVariable("C_CONF", 44),
					new DecisionVariable("C_RCFG", 15),
					new DecisionVariable("C_WTHR", 10),
					new DecisionVariable("C_RSUR", 12),
					new DecisionVariable("C_RALN", 9),
					new DecisionVariable("C_TRAF", 21),
					new DecisionVariable("V_ID", 100),
					new DecisionVariable("V_TYPE", 27),
					new DecisionVariable("V_YEAR", 118),
					new DecisionVariable("P_ID", 101),
					new DecisionVariable("P_SEX", 5),
					new DecisionVariable("P_AGE", 103),
					new DecisionVariable("P_PSN", 35),
					new DecisionVariable("P_SAFE", 11),
					new DecisionVariable("P_USER", 6)
				};
				int classCount = 6; // 2 possible output values for playing tennis: yes or no
				DecisionTree tree = new DecisionTree(attributes, classCount);

				dt.treeCreationTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;

				Console.WriteLine("Creating learning tree...");
				// Create a new instance of the ID3 algorithm
				C45Learning c45learnig = new C45Learning(tree);

				dt.learningCreationTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;

				// Translate our training data into integer symbols using our codebook:
				DataTable symbols = codebook.Apply(data);
				int[][] inputs = symbols.ToIntArray("C_MNTH", "C_WDAY", "C_HOUR", "C_SEV", "C_VEHS", "C_CONF", "C_RCFG", "C_WTHR", "C_RSUR", "C_RALN", "C_TRAF", "V_ID", "V_TYPE", "V_YEAR", "P_ID", "P_SEX", "P_AGE", "P_PSN", "P_SAFE", "P_USER");
				int[] outputs = symbols.ToIntArray("P_ISEV").GetColumn(0);

				Console.WriteLine("Learning decision tree...");
				// Learn the training instances!
				c45learnig.Learn(inputs, outputs);

				dt.treeLearningTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;

				Console.WriteLine("Getting rules from tree...");
				DecisionSet ds = tree.ToRules();

				dt.ruleFetchTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				string rules = dt.ToString() + "\n" + " Rules: " + ds.Count + "\n" + ds.ToString();

				System.IO.File.WriteAllText("../../../../rules" + amountOfDataToRead[dataIndex] + ".txt", rules);
				Console.WriteLine(amountOfDataToRead[dataIndex] + " generated " + ds.Count + " rules\n" + dt.ToString());
				dataIndex++;
			}
		}
	}
}
