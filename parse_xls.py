# Parse Excel .xls file structure to extract VBA
import struct
import io
import os

def parse_xls(filepath):
    """Парсинг структуры XLS файла"""
    with open(filepath, 'rb') as f:
        data = f.read()
    
    # XLS использует OLE Compound Document Format
    # Сигнатура: D0 CF 11 E0 A1 B1 1A E1
    
    if len(data) < 512:
        return None
    
    # Проверяем сигнатуру
    sig = data[:8]
    if sig != b'\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1':
        print(f"Неверная сигнатура: {sig.hex()}")
        return None
    
    # Ищем все читаемые строки в файле
    strings = []
    current = []
    
    for i in range(len(data)):
        b = data[i]
        if 32 <= b <= 126:
            current.append(chr(b))
        elif b == 0:
            if len(current) >= 10:
                s = ''.join(current)
                # Фильтруем по ключевым словам VBA
                if any(kw in s.lower() for kw in ['attribute vb_name', 'sub ', 'function ', 'dim ', 'if ', 'end sub', 'end function']):
                    strings.append(s)
            current = []
        else:
            current = []
    
    return '\n'.join(strings)

def extract_vba_macros(filepath):
    """Извлечение VBA макросов"""
    result = parse_xls(filepath)
    return result if result else "Не удалось извлечь VBA код"

# Main
file_path = r'C:\Айдар\IA\ace\план-исправлений\gidravlica.xls'
output_file = r'C:\Айдар\IA\ace\Work\vba_parsed.txt'

print("Извлечение VBA из XLS файла...")
vba_code = extract_vba_macros(file_path)

with open(output_file, 'w', encoding='utf-8') as f:
    f.write(vba_code)

print(f"Сохранено в: {output_file}")
print(f"Длина: {len(vba_code)} символов")
print("\nПервые 5000 символов:")
print(vba_code[:5000])
