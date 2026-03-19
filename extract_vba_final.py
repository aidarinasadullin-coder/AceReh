# VBA extraction script
import os
import sys

file_path = r'C:\Айдар\IA\ace\план-исправлений\gidravlica.xls'
output_path = r'C:\Айдар\IA\ace\vba_extracted.txt'

print(f"Checking file: {file_path}")
print(f"File exists: {os.path.exists(file_path)}")

if os.path.exists(file_path):
    print(f"File size: {os.path.getsize(file_path)} bytes")
    
    # Try to read VBA using oletools
    try:
        from oletools.olevba import VBA_Parser
        print("Parsing VBA...")
        vbaparser = VBA_Parser(file_path)
        
        with open(output_path, 'w', encoding='utf-8') as out:
            if vbaparser.detect_vba_macros():
                out.write("VBA macros found!\n\n")
                for (filename, stream_path, vba_filename, vba_code) in vbaparser.extract_macros():
                    out.write(f"=== Module: {vba_filename} ===\n")
                    out.write(vba_code)
                    out.write("\n\n")
            else:
                out.write("No VBA macros found.\n")
                
        vbaparser.close()
        print(f"VBA code saved to: {output_path}")
        
    except ImportError as e:
        print(f"oletools not installed: {e}")
        print("Trying to install...")
        import subprocess
        subprocess.check_call([sys.executable, "-m", "pip", "install", "oletools"])
        print("Please run script again")
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
else:
    print("File not found!")
