<?php

$outputDir = "../output";
$filePath = "../input/transactions.csv";


function emptyf($outputDir) {
// Write 0-record report and clean error log per Section 3.5
    $report = "Valid transactions: 0\n" .
              "Invalid transactions: 0\n" .
              "Total revenue: 0.00\n" .
              "Highest-value transaction: N/A\n";

    file_put_contents($outputDir . "/report.txt", $report);
    file_put_contents($outputDir . "/errors.txt", "No invalid records\n");
}

$correctHeader = ["transaction_id", "product_name", "category", "quantity", "unit_price"] ;

if (!file_exists($filePath)) {
    echo "File does not exist\n";
    exit(1);
} elseif (filesize($filePath) === 0) {
    echo "File empty\n";
    emptyf($outputDir);
    exit(0);
} 


if (($handle = fopen($filePath, "r")) !== FALSE) {
    $header = fgetcsv($handle);
    if ($header !== $correctHeader) {
        echo "Invalid header\n";
        exit(1);
    }

    $seen = [];
    $validRecords = [];
    $invalidRecords = [];
    $totalRevenue = 0.0;
    $categoryTotals = [];
    $highestTx = null;

    $lineNumber = 1; 

    while (($row = fgetcsv($handle)) !== FALSE) {
        $lineNumber++;

        if (count($row) !== 5) {
            $invalidRecords[] = "row {$lineNumber} did not have 5 columns";
            continue;
        }

        $trow = array_map('trim', $row);

        if (!ctype_digit($trow[0]) || (int)$trow[0] <= 0) {
            $invalidRecords[] = "row {$lineNumber} has invalid transaction_id";
            continue;
        } 
        $tid = (int)$trow[0];

        if (isset($seen[$tid])) {
            $invalidRecords[] = "row {$lineNumber} had a duplicate transaction id";
            continue;
        }
        
        if ($trow[1] ==="") {
            $invalidRecords[] = "row {$lineNumber} has empty product_name";
            continue;
        }

        if ($trow[2] === "") {
            $invalidRecords[] = "row {$lineNumber} did not have a category";
            continue;
        }

        if (!ctype_digit($trow[3]) || (int)$trow[3] <= 0) {
            $invalidRecords[] = "row {$lineNumber} has invalid quantity";
            continue;
        }
        $qty = (int)$trow[3];
        
        $priceStr = $trow[4];

        if (!is_numeric($priceStr) || (float)$priceStr < 0.00) {
            $invalidRecords[] = "row {$lineNumber} has invalid unit_price";
            continue;
        }

        if (str_contains($priceStr, ".")) {
            $parts = explode(".", $priceStr);
            if (count($parts) !== 2 || strlen($parts[1]) > 2) {
                $invalidRecords[] = "row {$lineNumber} has invalid unit_price";
                continue;
            }
        }

        $price = (float)$priceStr;


        $seen[$tid] = true;
        $validRecords[] = $trow;
        
        $txValue = $qty * $price;
        $totalRevenue += $txValue;

        $cat = $trow[2];
        $categoryTotals[$cat] = ($categoryTotals[$cat] ?? 0.0) + $txValue;
    }
}

?>