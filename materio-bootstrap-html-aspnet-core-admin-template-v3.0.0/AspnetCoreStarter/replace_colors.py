import os

def replace_colors(directory):
    replacements = {
        '#8c57ff': '#f89223',
        '140, 87, 255': '248, 146, 35',
        '#ba9aff': '#f89223', # dark link
        '#7e4ee6': '#df831f', # hover
        '126, 78, 230': '223, 131, 31', # hover rgb
    }
    
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.css'):
                path = os.path.join(root, file)
                try:
                    with open(path, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    new_content = content
                    for old, new in replacements.items():
                        new_content = new_content.replace(old, new)
                        new_content = new_content.replace(old.upper(), new)
                    
                    if new_content != content:
                        print(f"Updating {path}")
                        with open(path, 'w', encoding='utf-8') as f:
                            f.write(new_content)
                except Exception as e:
                    print(f"Error processing {path}: {e}")

if __name__ == "__main__":
    replace_colors('wwwroot/vendor/css')
