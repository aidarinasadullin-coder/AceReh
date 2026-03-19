import sys
sys.stdout.reconfigure(encoding='utf-8')

# Проверим существование файла
import os
file_path = r'C:\Айдар\IA\ace\план-исправлений\gidravlica.xls'
print(f"Checking file: {file_path}")
print(f"File exists: {os.path.exists(file_path)}")

if os.path.exists(file_path):
    print(f"File size: {os.path.getsize(file_path)} bytes")
    
    # Попробуем прочитать VBA
    try:
        from oletools.olevba import VBA_Parser
        print("\nParsing VBA...")
        vbaparser = VBA_Parser(file_path)
        
        if vbaparser.detect_vba_macros():
            print("VBA macros found!")
            for (filename, stream_path, vba_filename, vba_code) in vbaparser.extract_macros():
                print(f"\n=== Module: {vba_filename} ===")
                print(vba_code)
        else:
            print("No VBA macros found.")
            
        vbaparser.close()
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
