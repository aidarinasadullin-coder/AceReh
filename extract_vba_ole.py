# OLE Compound File parser for VBA extraction
import struct
import os

def read_compound_file(filepath):
    """Чтение структуры OLE Compound File"""
    with open(filepath, 'rb') as f:
        data = f.read()
    
    # Сигнатура OLE CF: D0 CF 11 E0 A1 B1 1A E1
    if data[:8] != b'\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1':
        print("Неверная сигнатура файла")
        return None
    
    # Читаем заголовок
    sector_size = 512  # обычно 512 байт
    
    # Ищем VBA потоки - они обычно содержат "VBA" в имени
    vba_streams = []
    
    # Поиск текста VBA в бинарных данных
    # VBA макросы хранятся в сжатом виде, но можно найти фрагменты
    
    return data

def extract_readable_text(data):
    """Извлечение читаемого текста из бинарных данных"""
    text_parts = []
    current_text = []
    
    for i in range(0, len(data) - 1, 2):
        # Пробуем читать как Unicode (2 байта на символ)
        char_code = struct.unpack('<H', data[i:i+2])[0]
        if 32 <= char_code <= 126:  # printable ASCII
            current_text.append(chr(char_code))
        elif char_code in [10, 13]:  # newlines
            current_text.append('\n')
        else:
            if len(current_text) > 20:
                text = ''.join(current_text)
                # Фильтруем только потенциально полезный текст
                if any(keyword in text.lower() for keyword in ['sub', 'function', 'dim', 'if', 'then', 'end', 'umdreh', 'vert', 'kv']):
                    text_parts.append(text)
            current_text = []
    
    return '\n'.join(text_parts)

# Читаем файл
file_path = r'C:\Айдар\IA\ace\план-исправлений\gidravlica.xls'

print(f"Чтение файла: {file_path}")
data = read_compound_file(file_path)

if data:
    print(f"Размер файла: {len(data)} bytes")
    text = extract_readable_text(data)
    
    output_file = r'C:\Айдар\IA\ace\Work\vba_unicode.txt'
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(text)
    
    print(f"Извлечено {len(text)} символов")
    print(f"Сохранено в: {output_file}")
    print("\nПервые 3000 символов:")
    print(text[:3000])
