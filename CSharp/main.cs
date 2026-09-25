using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TransactionAnalyzer
{
    class Transaction
    {
        public int Id { get; set; }
        public string Product { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            return $"['{Id}', '{Product}', '{Category}', '{Quantity}', '{Price:F2}']";
        }
    }

    class Program
    {
        private static double _totalRevenue = 0.0;
        private static readonly SortedDictionary<string, double> CategoryTotals = new SortedDictionary<string, double>();
        private static Transaction? _highestTransaction = null;

        private static readonly HashSet<int> SeenIds = new HashSet<int>();
        private static readonly List<Transaction> ValidRecords = new List<Transaction>();
        private static readonly List<string> InvalidRecords = new List<string>();

        private static string _outputDir = "output";

        static void Main(string[] args)
        {
            // Dynamically walk up directory tree until input/transactions.csv is found
            string? currentDir = Directory.GetCurrentDirectory();
            string? foundFilePath = null;

            while (currentDir != null)
            {
                string candidate = Path.Combine(currentDir, "input", "transactions.csv");
                if (File.Exists(candidate))
                {
                    foundFilePath = candidate;
                    _outputDir = Path.Combine(currentDir, "output");
                    break;
                }
                DirectoryInfo? parent = Directory.GetParent(currentDir);
                currentDir = parent?.FullName;
            }

            if (foundFilePath == null)
            {
                Console.WriteLine("Error: input/transactions.csv not found in this folder or any parent folders.");
                Environment.Exit(1);
            }

            string filePath = foundFilePath;

            // Ensure output directory exists in the same root folder
            Directory.CreateDirectory(_outputDir);

            string[] lines = File.ReadAllLines(filePath);

            // Handle 0-byte or completely empty file
            if (lines.Length == 0)
            {
                Console.WriteLine($"Error: {filePath} is empty.");
                WriteEmptyReportsAndExit();
                Environment.Exit(0);
            }

            // Check header (strip potential UTF-8 BOM)
            string[] correctHeader = { "transaction_id", "product_name", "category", "quantity", "unit_price" };
            string[] header = SplitCsvLine(lines[0]);
            if (header.Length > 0)
            {
                header[0] = header[0].TrimStart('\uFEFF');
            }

            if (!header.SequenceEqual(correctHeader))
            {
                Console.WriteLine($"Error: {filePath} has an incorrect or missing header.");
                Environment.Exit(1);
            }

            // Process data rows (1-based row numbers, header is row 1)
            for (int i = 1; i < lines.Length; i++)
            {
                int rowNum = i + 1;
                string rawLine = lines[i];

                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                string[] fields = SplitCsvLine(rawLine);

                // Rule 1: Exactly 5 columns
                if (fields.Length != 5)
                {
                    InvalidRecords.Add($"row {rowNum} did not have 5 columns");
                    continue;
                }

                // Rule 2: Trim all columns
                string[] trow = fields.Select(f => f.Trim()).ToArray();

                ValidateRow(trow, rowNum);
            }

            // Section 3.5: Handle header-only file
            if (ValidRecords.Count == 0 && InvalidRecords.Count == 0)
            {
                WriteEmptyReportsAndExit();
            }

            // Sort: Value descending, then ID ascending (tie-breaker)
            var sortedVal = ValidRecords
                .OrderByDescending(r => r.Value)
                .ThenBy(r => r.Id)
                .ToList();

            // Interactive CLI Lookup
            if (ValidRecords.Count == 0)
            {
                Console.WriteLine("No valid records to process.");
            }
            else
            {
                RequestLookup();
            }

            // Write output/errors.txt
            using (var ef = new StreamWriter(Path.Combine(_outputDir, "errors.txt")))
            {
                if (InvalidRecords.Count == 0)
                {
                    ef.WriteLine("No invalid records");
                }
                else
                {
                    foreach (var err in InvalidRecords)
                    {
                        ef.WriteLine(err);
                    }
                }
            }

            // Write output/report.txt
            using (var rf = new StreamWriter(Path.Combine(_outputDir, "report.txt")))
            {
                rf.WriteLine($"Valid transactions: {ValidRecords.Count}");
                rf.WriteLine($"Invalid transactions: {InvalidRecords.Count}");
                rf.WriteLine($"Total revenue: {_totalRevenue:F2}");

                if (_highestTransaction != null)
                {
                    rf.WriteLine($"Highest-value transaction: {{'id': {_highestTransaction.Id}, 'product': '{_highestTransaction.Product}', 'category': '{_highestTransaction.Category}', 'quantity': {_highestTransaction.Quantity}, 'price': {_highestTransaction.Price:F2}, 'value': {_highestTransaction.Value:F2}}}");
                }
                else
                {
                    rf.WriteLine("Highest-value transaction: N/A");
                }

                rf.WriteLine("\nRevenue by Category:");
                foreach (var kvp in CategoryTotals)
                {
                    rf.WriteLine($"  {kvp.Key}: {kvp.Value:F2}");
                }

                rf.WriteLine("\nSorted Valid Transactions:");
                foreach (var record in sortedVal)
                {
                    rf.WriteLine(record.ToString());
                }
            }
        }

        private static void ValidateRow(string[] trow, int rowNum)
        {
            // Rule 3: Positive integer transaction ID
            if (!int.TryParse(trow[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tid))
            {
                InvalidRecords.Add($"row {rowNum} transaction id must be an integer");
                return;
            }

            if (tid <= 0)
            {
                InvalidRecords.Add($"row {rowNum} did not have a valid transaction id");
                return;
            }

            // Rule 4: Duplicate transaction ID
            if (SeenIds.Contains(tid))
            {
                InvalidRecords.Add($"row {rowNum} had a duplicate transaction id");
                return;
            }

            // Rule 5: Product name not empty
            if (string.IsNullOrEmpty(trow[1]))
            {
                InvalidRecords.Add($"row {rowNum} did not have a product name");
                return;
            }

            // Rule 6: Category not empty
            if (string.IsNullOrEmpty(trow[2]))
            {
                InvalidRecords.Add($"row {rowNum} did not have a category");
                return;
            }

            // Rule 7: Quantity must be an integer > 0
            if (!int.TryParse(trow[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty))
            {
                InvalidRecords.Add($"row {rowNum} quantity must be an integer");
                return;
            }

            if (qty <= 0)
            {
                InvalidRecords.Add($"row {rowNum} did not have a valid quantity");
                return;
            }

            // Rule 8: Unit price validation
            string priceStr = trow[4];
            if (priceStr.Contains('.'))
            {
                string[] parts = priceStr.Split('.');
                if (parts.Length != 2 || parts[1].Length > 2)
                {
                    InvalidRecords.Add($"row {rowNum} unit price must have at most two decimal places");
                    return;
                }
            }

            if (!double.TryParse(priceStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double price))
            {
                InvalidRecords.Add($"row {rowNum} unit price must be numeric");
                return;
            }

            if (price < 0.00)
            {
                InvalidRecords.Add($"row {rowNum} unit price must be greater than or equal to 0.00");
                return;
            }

            // Passed all validation checks
            SeenIds.Add(tid);

            var tx = new Transaction
            {
                Id = tid,
                Product = trow[1],
                Category = trow[2],
                Quantity = qty,
                Price = price,
                Value = Math.Round(qty * price, 2)
            };

            ValidRecords.Add(tx);
            ProcessBusinessMetrics(tx);
        }

        private static void ProcessBusinessMetrics(Transaction tx)
        {
            _totalRevenue += tx.Value;

            if (CategoryTotals.ContainsKey(tx.Category))
            {
                CategoryTotals[tx.Category] += tx.Value;
            }
            else
            {
                CategoryTotals[tx.Category] = tx.Value;
            }

            // Tie-breaker: lowest transaction ID
            if (_highestTransaction == null)
            {
                _highestTransaction = tx;
            }
            else if (tx.Value > _highestTransaction.Value)
            {
                _highestTransaction = tx;
            }
            else if (Math.Abs(tx.Value - _highestTransaction.Value) < 0.0001 && tx.Id < _highestTransaction.Id)
            {
                _highestTransaction = tx;
            }
        }

        private static void RequestLookup()
        {
            while (true)
            {
                Console.WriteLine("Please give a Transaction ID to lookup");
                Console.Write("ID: ");
                string? input = Console.ReadLine();

                if (!int.TryParse(input, out int lookup) || lookup <= 0)
                {
                    Console.WriteLine("Input cannot be 0 or negative");
                    continue;
                }

                var match = ValidRecords.FirstOrDefault(r => r.Id == lookup);
                if (match != null)
                {
                    Console.WriteLine($"Transaction ID: {match.Id}");
                    Console.WriteLine($"Product Name: {match.Product}");
                    Console.WriteLine($"Category: {match.Category}");
                    Console.WriteLine($"Quantity: {match.Quantity}");
                    Console.WriteLine($"Unit Price: {match.Price:F2}");
                }
                else
                {
                    Console.WriteLine("Transaction ID not found.");
                }
                break;
            }
        }

        private static void WriteEmptyReportsAndExit()
        {
            using (var rf = new StreamWriter(Path.Combine(_outputDir, "report.txt")))
            {
                rf.WriteLine("Valid transactions: 0");
                rf.WriteLine("Invalid transactions: 0");
                rf.WriteLine("Total revenue: 0.00");
                rf.WriteLine("Highest-value transaction: N/A");
            }

            using (var ef = new StreamWriter(Path.Combine(_outputDir, "errors.txt")))
            {
                ef.WriteLine("No invalid records");
            }

            Console.WriteLine("no valid records are available");
            Environment.Exit(0);
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());

            return result.ToArray();
        }
    }
}