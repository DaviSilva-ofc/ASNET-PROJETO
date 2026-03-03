import os

def replace_in_file(file_path):
    replacements = {
        '#8c57ff': '#f89223',
        '140, 87, 255': '248, 146, 35',
        '#ba9aff': '#f89223',
        '#7e4ee6': '#df831f',
        '126, 78, 230': '223, 131, 31',
    }
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = f.read()
        
        original_data = data
        for old, new in replacements.items():
            data = data.replace(old, new)
            data = data.replace(old.upper(), new)
        
        if data != original_data:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(data)
            print(f"Updated: {file_path}")
        else:
            print(f"No changes for: {file_path}")
    except Exception as e:
        print(f"Error {file_path}: {e}")

if __name__ == "__main__":
    files_to_process = [
        r'wwwroot\vendor\css\core.dist.css',
        r'wwwroot\vendor\css\pages\front-page-landing.dist.css',
        r'wwwroot\vendor\css\pages\front-page.dist.css'
    ]
    for f in files_to_process:
        replace_in_file(f)
