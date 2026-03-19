# Extract VBA from Excel file
import struct
import os

def extract_vba_from_xls(filepath):
    """Извлечение VBA кода из файла Excel .xls"""
    
    with open(filepath, 'rb') as f:
        data = f.read()
    
    # Поиск строк VBA в бинарных данных
    vba_patterns = []
    
    # Ищем подписи VBA
    vba_signatures = [
        b'Attribute VB_Name',
        b'Sub ',
        b'Function ',
        b'Private Sub',
        b'Public Sub',
        b'Dim ',
    ]
    
    results = []
    
    # Простой поиск строк в бинарных данных
    i = 0
    current_string = b''
    readable_chars = []
    
    for byte in data:
        if 32 <= byte <= 126 or byte in [10, 13]:  # printable ASCII + newlines
            readable_chars.append(chr(byte))
        else:
            if len(readable_chars) > 50:  # если накопилось достаточно символов
                text = ''.join(readable_chars)
                # Ищем VBA-код
                if any(sig in text.encode() for sig in vba_signatures) or 'umdreh' in text.lower():
                    results.append(text)
            readable_chars = []
    
    return '\n'.join(results) if results else "VBA код не найден через простой метод"

# Читаем файл
file_path = r'C:\Айдар\IA\ace\план-исправлений\gidravlica.xls'
output_file = r'C:\Айдар\IA\ace\Work\vba_extracted.txt'

print(f"Чтение файла: {file_path}")
print(f"Размер файла: {os.path.getsize(file_path)} bytes")

# Создаем папку Work если не существует
work_dir = r'C:\Айдар\IA\ace\Work'
os.makedirs(work_dir, exist_ok=True)

# Пробуем прочитать
vba_code = extract_vba_from_xls(file_path)

# Сохраняем результат
with open(output_file, 'w', encoding='utf-8') as f:
    f.write(vba_code)

print(f"Результат сохранен в: {output_file}")
print(f"Длина извлеченного текста: {len(vba_code)} символов")

# Также выводим на экран
print("\n=== Извлеченный VBA код ===")
print(vba_code[:2000] if len(vba_code) > 2000 else vba_code)
