import subprocess
import sys

# Проверим версию Python
print("Python version:")
subprocess.run([sys.executable, "--version"], capture_output=True, text=True)

# Попробуем установить oletools
print("\nInstalling oletools...")
result = subprocess.run([sys.executable, "-m", "pip", "install", "oletools"], 
                       capture_output=True, text=True, encoding='utf-8', errors='ignore')
print("STDOUT:", result.stdout)
print("STDERR:", result.stderr)

# Проверим существование файла
import os
file_path = r'C:\Айдар\IA\ace\план-исправлений\gidravlica.xls'
print(f"\nFile exists: {os.path.exists(file_path)}")
if os.path.exists(file_path):
    print(f"File size: {os.path.getsize(file_path)} bytes")
