import re

file_path = r'c:\Users\Estagio\Documents\GitHub\ASNET-PROJETO\materio-bootstrap-html-aspnet-core-admin-template-v3.0.0\AspnetCoreStarter\Pages\Admin\Stocks.cshtml'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace <i class="ri-qr-code-line"></i> inside btn-icon with ??
content = re.sub(r'(<button[^>]*class="[^"]*btn-icon[^"]*"[^>]*>\s*)<i class="ri-qr-code-line"></i>(\s*</button>)', r'\1??\2', content)

# Replace <i class="ri-edit-line"></i> inside btn-icon with ??
content = re.sub(r'(<button[^>]*class="[^"]*btn-icon[^"]*"[^>]*>\s*)<i class="ri-edit-line"></i>(\s*</button>)', r'\1??\2', content)

# Replace <i class="ri-delete-bin-line"></i> inside btn-icon with ???
content = re.sub(r'(<button[^>]*class="[^"]*btn-icon[^"]*"[^>]*>\s*)<i class="ri-delete-bin-line"></i>(\s*</button>)', r'\1???\2', content)

# Replace <i class="ri-links-line"></i> inside btn-icon with ??
content = re.sub(r'(<button[^>]*class="[^"]*btn-icon[^"]*"[^>]*>\s*)<i class="ri-links-line"></i>(\s*</button>)', r'\1??\2', content)

# Replace <i class="ri-arrow-go-back-line"></i> inside btn-icon with ??
content = re.sub(r'(<button[^>]*class="[^"]*btn-icon[^"]*"[^>]*>\s*)<i class="ri-arrow-go-back-line"></i>(\s*</button>)', r'\1??\2', content)

# Add lh-1 and font-size to btn-icon buttons that have emojis
content = re.sub(r'(<button[^>]*class="[^"]*btn-icon\b(?:(?!\blh-1\b).)*?)(")(\s*>[^<]*(?:??|??|???|??|??)[^<]*</button>)', r'\1 lh-1" style="font-size: 1.1rem;"\3', content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Replaced all font icons with emojis in Admin/Stocks.cshtml")
