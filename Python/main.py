import csv
import os
import sys

file_path = "../input/transactions.csv"

if not os.path.exists(file_path):
    print(f"Error: {file_path} not found.")
    sys.exit(1)

with open(file_path, mode="r", encoding="utf-8") as f:
    lines = f.readlines()