import csv
import os
import sys

file_path = "input/transactions.csv"

if not os.path.exists(file_path):
    print(f"Error: {file_path} not found.")
    sys.exit(1)

def validation(trow):
    print(trow)
    
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

    valid_records = []
    invalid_records = []
    for row in lines:
        if len(row) != 5:
            print(f"Error: {file_path} has a row with an incorrect number of columns: {row}")
            continue  # Skip to the next row instead of crashing
        
        trow = [col.strip() for col in row]
        validation(trow)

    if len(valid_records) == 0 and len(invalid_records) == 0:
        print("File has a header but no data rows.")


def validation(trow):
    # Placeholder for actual validation logic
    print("trow")