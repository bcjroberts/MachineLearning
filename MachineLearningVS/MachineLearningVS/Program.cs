using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math;
using Accord.Statistics.Filters;
using System;
using System.Data;

namespace C4._5Test {
	class Program {
		static void Main(string[] args) {
			MyTest myTest = new MyTest();
			myTest.runTest();

		}
	}

	class MyTest {
		public MyTest() {

		}

		public void runTest() {

			DataTable data = new DataTable("Mitchell's Tennis Example");

			data.Columns.Add("Day");
			data.Columns.Add("Outlook");
			data.Columns.Add("Temperature");
			data.Columns.Add("Humidity");
			data.Columns.Add("Wind");
			data.Columns.Add("PlayTennis");

			data.Rows.Add("D1", "Sunny", "Hot", "High", "Weak", "No");
			data.Rows.Add("D2", "Sunny", "Hot", "High", "Strong", "No");
			data.Rows.Add("D3", "Overcast", "Hot", "High", "Weak", "Yes");
			data.Rows.Add("D4", "Rain", "Mild", "High", "Weak", "Yes");
			data.Rows.Add("D5", "Rain", "Cool", "Normal", "Weak", "Yes");
			data.Rows.Add("D6", "Rain", "Cool", "Normal", "Strong", "No");
			data.Rows.Add("D7", "Overcast", "Cool", "Normal", "Strong", "Yes");
			data.Rows.Add("D8", "Sunny", "Mild", "High", "Weak", "No");
			data.Rows.Add("D9", "Sunny", "Cool", "Normal", "Weak", "Yes");
			data.Rows.Add("D10", "Rain", "Mild", "Normal", "Weak", "Yes");
			data.Rows.Add("D11", "Sunny", "Mild", "Normal", "Strong", "Yes");
			data.Rows.Add("D12", "Overcast", "Mild", "High", "Strong", "Yes");
			data.Rows.Add("D13", "Overcast", "Hot", "Normal", "Weak", "Yes");
			data.Rows.Add("D14", "Rain", "Mild", "High", "Strong", "No");

			Codification codebook = new Codification(data);

			DecisionVariable[] attributes =
			{
				new DecisionVariable("Outlook", 3), // 3 possible values (Sunny, overcast, rain)
   				new DecisionVariable("Temperature", 3), // 3 possible values (Hot, mild, cool)  
   				new DecisionVariable("Humidity",    2), // 2 possible values (High, normal)    
   				new DecisionVariable("Wind",        2)  // 2 possible values (Weak, strong) 
 			};

			int classCount = 2; // 2 possible output values for playing tennis: yes or no
			DecisionTree tree = new DecisionTree(attributes, classCount);

			// Create a new instance of the ID3 algorithm
			C45Learning c45learnig = new C45Learning(tree);

			// Translate our training data into integer symbols using our codebook:
			DataTable symbols = codebook.Apply(data);
			int[][] inputs = symbols.ToIntArray("Outlook", "Temperature", "Humidity", "Wind");
			int[] outputs = symbols.ToIntArray("PlayTennis").GetColumn(0);

			// Learn the training instances!
			c45learnig.Learn(inputs, outputs);

			int[] query = codebook.Transform(new[,]
			{
				{ "Outlook",     "Rain"  },
				{ "Temperature", "Mild"    },
				{ "Humidity",    "High"   },
				{ "Wind",        "Weak" }
			});

			int output = tree.Decide(query);

			string answer = codebook.Revert("PlayTennis", output); // answer will be "No".
			Console.WriteLine(tree.ToRules().NumberOfClasses);
			Console.ReadKey();
		}
	}
}
