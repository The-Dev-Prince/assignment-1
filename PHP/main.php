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

        $currentTx = [
            "id"       => $tid,
            "product"  => $trow[1],
            "category" => $cat,
            "quantity" => $qty,
            "price"    => $price,
            "value"    => $txValue,
        ];

        // Track highest-value transaction (Tie-breaker: lower ID wins)
        if ($highestTx === null) {
            $highestTx = $currentTx;
        } elseif ($txValue > $highestTx["value"]) {
            $highestTx = $currentTx;
        } elseif ($txValue == $highestTx["value"] && $tid < $highestTx["id"]) {
            $highestTx = $currentTx;
        }
    } // <--- The while loop should close HERE

    fclose($handle);

    // 1. Sort: Value descending, ID ascending tie-breaker
    usort($validRecords, function ($a, $b) {
        $valA = (int)$a[3] * (float)$a[4];
        $valB = (int)$b[3] * (float)$b[4];

        if ($valA != $valB) {
            return ($valA > $valB) ? -1 : 1;
        }
        return ((int)$a[0] < (int)$b[0]) ? -1 : 1;
    });

    // 2. Interactive CLI Lookup Prompt
    if (count($validRecords) === 0) {
        echo "No valid transactions are available for lookup.\n";
    } else {
        while (true) {
            $input = trim(readline("Enter a positive transaction ID to look up: "));
            if (ctype_digit($input) && (int)$input > 0) {
                $lookupId = (int)$input;
                break;
            }
            echo "Please enter a positive integer greater than 0.\n";
        }

        $found = false;
        foreach ($validRecords as $r) {
            if ((int)$r[0] === $lookupId) {
                $val = (int)$r[3] * (float)$r[4];
                printf(
                    "Found: ID=%s, Product=%s, Category=%s, Qty=%s, Price=$%.2f, Total=$%.2f\n",
                    $r[0], $r[1], $r[2], $r[3], (float)$r[4], $val
                );
                $found = true;
                break;
            }
        }
        if (!$found) {
            echo "Transaction ID {$lookupId} does not exist among the valid records.\n";
        }
    }

    // 3. Write output/errors.txt
    $errOut = "";
    if (count($invalidRecords) === 0) {
        $errOut = "No invalid records\n";
    } else {
        foreach ($invalidRecords as $err) {
            $errOut .= "{$err}\n";
        }
    }
    file_put_contents("{$outputDir}/errors.txt", $errOut);

    // 4. Write output/report.txt
    $repOut = sprintf("Valid transactions: %d\n", count($validRecords));
    $repOut .= sprintf("Invalid transactions: %d\n", count($invalidRecords));
    $repOut .= sprintf("Total revenue: %.2f\n", $totalRevenue);

    if ($highestTx !== null) {
        $repOut .= sprintf(
            "Highest-value transaction: ID=%d, Product=%s, Category=%s, Qty=%d, Price=%.2f, Value=%.2f\n",
            $highestTx["id"], $highestTx["product"], $highestTx["category"],
            $highestTx["quantity"], $highestTx["price"], $highestTx["value"]
        );
    } else {
        $repOut .= "Highest-value transaction: N/A\n";
    }

    $repOut .= "\nRevenue by Category:\n";
    ksort($categoryTotals); // Alphabetical sort by category key
    foreach ($categoryTotals as $catName => $cRev) {
        $repOut .= sprintf("  %s: %.2f\n", $catName, $cRev);
    }

    $repOut .= "\nSorted Valid Transactions:\n";
    foreach ($validRecords as $r) {
        $val = (int)$r[3] * (float)$r[4];
        $repOut .= sprintf(
            "ID=%s, Product=%s, Category=%s, Qty=%s, Price=%.2f, Value=%.2f\n",
            $r[0], $r[1], $r[2], $r[3], (float)$r[4], $val
        );
    }
    file_put_contents("{$outputDir}/report.txt", $repOut);
}    


    
?>