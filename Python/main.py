import csv
import os
import sys

file_path = "input/transactions.csv"

if not os.path.exists(file_path):
    print(f"Error: {file_path} not found.")
    sys.exit(1)

total_revenue = 0.0
category_totals = {}  # e.g., {"Electronics": 0.0, "Furniture": 0.0}
highest_transaction = None
def bussiness(trow):
    global total_revenue, highest_transaction, category_totals
    
    tid = int(trow[0])
    product_name = trow[1]
    category = trow[2]
    qty = int(trow[3])
    price = float(trow[4])
    
    tx_value = qty * price
    total_revenue += tx_value
    
    # Accumulate by category
    category_totals[category] = category_totals.get(category, 0.0) + tx_value
    
    # Check for highest transaction (tie-breaker: lowest transaction_id)
    current_tx = {
        "id": tid,
        "product": product_name,
        "category": category,
        "quantity": qty,
        "price": price,
        "value": tx_value
    }
    
    if highest_transaction is None:
        highest_transaction = current_tx
    elif tx_value > highest_transaction["value"]:
        highest_transaction = current_tx
    elif tx_value == highest_transaction["value"] and tid < highest_transaction["id"]:
        highest_transaction = current_tx


def validation(trow, rownum):
    # Rule 3: transaction_id positive integer
    try:
        tid = int(trow[0])
        if tid <= 0:
            invalid_records.append(f"row {rownum} did not have a valid transaction id")
            return
    except ValueError:
        invalid_records.append(f"row {rownum} transaction id must be an integer")
        return

    # Rule 4: duplicate transaction_id
    if tid in seen:
        invalid_records.append(f"row {rownum} had a duplicate transaction id")
        return

    # Rule 5: product_name not empty
    if trow[1] == "":
        invalid_records.append(f"row {rownum} did not have a product name")
        return

    # Rule 6: category not empty
    if trow[2] == "":
        invalid_records.append(f"row {rownum} did not have a category")
        return

    # Rule 7: quantity positive integer (> 0)
    try:
        qty = int(trow[3])
        if qty <= 0:
            invalid_records.append(f"row {rownum} did not have a valid quantity")
            return
    except ValueError:
        invalid_records.append(f"row {rownum} quantity must be an integer")
        return

    # Rule 8: unit_price checks
    price_str = trow[4]
    if "." in price_str:
        parts = price_str.split(".")
        if len(parts) != 2 or len(parts[1]) > 2:
            invalid_records.append(f"row {rownum} unit price must have at most two decimal places")
            return

    try:
        price = float(price_str)
        if price < 0.00:
            invalid_records.append(f"row {rownum} unit price must be greater than or equal to 0.00")
            return
    except ValueError:
        invalid_records.append(f"row {rownum} unit price must be numeric")
        return

    # If valid, register the ID and accept the record
    seen.add(tid)
    valid_records.append(trow)
    bussiness(trow)

    
with open(file_path, mode="r", encoding="utf-8") as f:
    lines = csv.reader(f)
    
    correct_header = ["transaction_id", "product_name", "category", "quantity", "unit_price"]
    
    # Grab the first row
    first_line = next(lines, None)
    
    # If the file is completely empty (0 bytes), first_line is None
    if first_line is None:
        print(f"Error: {file_path} is empty.")
        sys.exit(0)  # Section 3.5 expects clean zero-record handling
        
    if first_line != correct_header:
        print(f"Error: {file_path} has an incorrect or missing header.")
        sys.exit(1)

    seen = set()
    valid_records = []
    invalid_records = []
    for rownum, row in enumerate(lines, start=2):
        if len(row) != 5:
            print(f"Error: {file_path} has a row with an incorrect number of columns: {row}")
            invalid_records.append(f"row {rownum} did not have 5 columns")
            continue
        
        trow = [col.strip() for col in row]
        validation(trow, rownum)


def empty():
    with open("output/report.txt", "w", encoding="utf-8") as rf:
        rf.write("Valid transactions: 0\n")
        rf.write("Invalid transactions: 0\n")
        rf.write("Total revenue: 0.00\n")
        rf.write("Highest-value transaction: N/A\n")
    with open("output/errors.txt", "w", encoding="utf-8") as ef:
        ef.write("No invalid records\n")
    print("no valid records are avalible")
    sys.exit(0)


os.makedirs("output", exist_ok=True)
if len(valid_records) == 0 and len(invalid_records) == 0:
    empty()
        

sortedval = sorted(valid_records, key=lambda r:(-(int(r[3])) * float(r[4]), int(r[0])))

def request():
    print("Please give a Transaction ID to lookup")
    lookup = input("ID:")
    try: 
        lookup = int(lookup)
        if lookup <= 0:
            print("Input cannot be 0 or negative")
            request()
        else:
            found = False
            for record in valid_records:
                if int(record[0]) == lookup:
                    print(f"Transaction ID: {record[0]}")
                    print(f"Product Name: {record[1]}")
                    print(f"Category: {record[2]}")
                    print(f"Quantity: {record[3]}")
                    print(f"Unit Price: {record[4]}")
                    found = True
                    break
            if not found:
                print("Transaction ID not found.")
    except ValueError:
        print("Input cannot be 0 or negative")
        request()
if len(valid_records) == 0:
    print("No valid records to process.")
else:
    request()

with open("output/errors.txt", "w", encoding="utf-8") as ef:
    if len(invalid_records) == 0:
        ef.write("No invalid records\n")
    else:
        for err in invalid_records:
            ef.write(f"{err}\n")

with open("output/report.txt", "w", encoding="utf-8") as rf:
    rf.write(f"Valid transactions: {len(valid_records)}\n")
    rf.write(f"Invalid transactions: {len(invalid_records)}\n")
    rf.write(f"Total revenue: {total_revenue:.2f}\n")
    rf.write(f"Highest-value transaction: {highest_transaction if highest_transaction else 'N/A'}\n")
    rf.write("\nRevenue by Category:\n")
    for cat in sorted(category_totals.keys()):
        rf.write(f"{cat}: {category_totals[cat]:.2f}\n")
    rf.write("\nSorted Valid Transactions:\n")
    for record in sortedval:
        rf.write(f"{record}\n")
        