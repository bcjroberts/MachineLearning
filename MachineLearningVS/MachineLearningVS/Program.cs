using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.MachineLearning.DecisionTrees.Rules;
using Accord.Math;
using Accord.Statistics.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

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
		public double treeLearningTime = 0;
		public double ruleFetchTime = 0;
		public int totalRules = 0;
		public DataHolder (int ndataAmount) {
			dataAmount = ndataAmount;
		}

		public override string ToString () {
			string result = "Amount of Data: " + dataAmount + "\n";
			result += "Time to read data: " + readTime + "\n";
			result += "Time to Codify the data: " + codeTime + "\n";
			result += "Time to Learn the Tree: " + treeLearningTime + "\n";
			result += "Time to Fetch Rules: " + ruleFetchTime + "\n";
			result += "Total Time: " + getTotalTime();
			return result;
		}

		public double getTotalTime () {
			return readTime + codeTime + treeLearningTime + ruleFetchTime;
		}
	}

	class MyTest {
		public MyTest() {}

		public void runTest() {

			List<DataHolder> dataHolders = new List<DataHolder>();
			DateTime previousTime;
			Dictionary<string, int> ruleQuatityDict = new Dictionary<string, int>();

			bool done = false;
			int amountToReadPerIncrement = 10000;
			int currentAmount = 0;
			StreamReader reader = File.OpenText("../../../../ncdb_2015.csv");
			DataTable data = new DataTable("2015 Collision Data");
			string firstLine = reader.ReadLine();
			string[] firstItems = firstLine.Split(',');

			// setup the columns only at the start
			data.Columns.Add("C_CASE");
			for (int i = 0; i < firstItems.Length; i++) {
				if (!firstItems[i].Equals("P_ISEV") && !firstItems[i].Equals("C_CASE") && !firstItems[i].Equals("C_YEAR") && !firstItems[i].Equals("C_SEV"))
					data.Columns.Add(firstItems[i]);
			}
			data.Columns.Add("P_ISEV");

			string[] stringSplitStuff = { "=:", "&&", "(", ")", "==", " " };

			while (!done) {
				currentAmount += amountToReadPerIncrement;
				Console.WriteLine("Currently on: " + currentAmount);

				previousTime = DateTime.Now;
				DataHolder dataHolder = new DataHolder(amountToReadPerIncrement);
				dataHolders.Add(dataHolder);

				int amountOfDataRead = -1;
				
				string line;
				// Clear the rows to make way for the new data
				data.Rows.Clear();

				while ((line = reader.ReadLine()) != null && amountOfDataRead < amountToReadPerIncrement) {
					string[] items = line.Split(',');
					data.Rows.Add(items[22], items[1], items[2], items[3], items[5], items[6], items[7], items[8]
						, items[9], items[10], items[11], items[12], items[13], items[14], items[15], items[16], items[17],
						items[18], items[20], items[21], items[19]);
					amountOfDataRead++;
				}
				

				// Ignore the last bit of data as it will be shorter than 10000 elements
				if (amountOfDataRead < amountToReadPerIncrement) {
					Console.WriteLine("Finished, writing output: " + currentAmount);
					done = true;
					continue;
				}

				dataHolder.readTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;
				
				Codification codebook = new Codification(data);

				dataHolder.codeTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;

				DecisionVariable[] attributes =
				{
					new DecisionVariable("C_MNTH", 14),
					new DecisionVariable("C_WDAY", 9),
					new DecisionVariable("C_HOUR", 25),
					//new DecisionVariable("C_SEV", 4),
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

				// Create a new instance of the ID3 algorithm
				C45Learning c45learnig = new C45Learning(tree);

				// Translate our training data into integer symbols using our codebook:
				DataTable symbols = codebook.Apply(data);
				int[][] inputs = symbols.ToIntArray("C_MNTH", "C_WDAY", "C_HOUR", "C_VEHS", "C_CONF", "C_RCFG", "C_WTHR", "C_RSUR", "C_RALN", "C_TRAF", "V_ID", "V_TYPE", "V_YEAR", "P_ID", "P_SEX", "P_AGE", "P_PSN", "P_SAFE", "P_USER");
				int[] outputs = symbols.ToIntArray("P_ISEV").GetColumn(0);

				// Learn the training instances!
				c45learnig.Learn(inputs, outputs);

				dataHolder.treeLearningTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				previousTime = DateTime.Now;

				DecisionSet ds = tree.ToRules();

				// Place the decicion rules into the hash
				foreach (DecisionRule dr in ds) {
					string[] splitString = dr.ToString().Split(stringSplitStuff, System.StringSplitOptions.RemoveEmptyEntries);
					int severity = int.Parse(splitString[0].Trim());

					string translatedKey = codebook.Revert("P_ISEV", severity) + " =: ";
					translatedKey += "(" + splitString[1] + " == " + codebook.Revert(splitString[1], int.Parse(splitString[2])) + ")";

					for (int i = 3; i < splitString.Length; i+=2) {
						translatedKey += " && (" + splitString[i] + " == "+ codebook.Revert(splitString[i], int.Parse(splitString[i + 1])) + ")";
					}

					if (!ruleQuatityDict.ContainsKey(translatedKey)) {
						ruleQuatityDict.Add(translatedKey, 1);
					} else {
						ruleQuatityDict[translatedKey]++;
					}
				}

				dataHolder.ruleFetchTime = DateTime.Now.Subtract(previousTime).TotalSeconds;
				dataHolder.totalRules = ds.Count;
				string rules = dataHolder.ToString() + "\n" + " Rules: " + dataHolder.totalRules + "\n" + ds.ToString();

				System.IO.File.WriteAllText("../../../../rules" + amountToReadPerIncrement + ".txt", rules);
				Console.WriteLine(amountToReadPerIncrement + " generated " + dataHolder.totalRules + " rules\n" + dataHolder.ToString());
			}

			reader.Close();

			// Print out a csv with the time and rule data
			string csvData = "Data_Amount,Read_Time,Codify_Time,Learn_Time,Fetch_Rule_Time,Total_Time,Total_Rules\n";
			foreach (DataHolder dh in dataHolders) {
				csvData +=  dh.dataAmount + ","+ dh.readTime + "," + dh.codeTime + "," + dh.treeLearningTime + "," + dh.ruleFetchTime + "," + dh.getTotalTime() + "," + dh.totalRules + "\n";
			}

			System.IO.File.WriteAllText("../../../../All2015-10000Data.csv", csvData);

			Console.WriteLine("Sorting rule dictionary...");
			var myList = ruleQuatityDict.ToList();
			myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
			StringBuilder sb = new StringBuilder();
			int total = myList.Count;
			Console.WriteLine("Writing rules...");
			for (int i = 0; i < myList.Count; i++) {
				sb.Append(myList[i].Value + ": " + myList[i].Key + "\n");
				if (i % 100 == 0) Console.WriteLine("Rules written: " + i);
			}

			System.IO.File.WriteAllText("../../../../ruleQuantityData100000.txt", sb.ToString());

		}
	}
}
