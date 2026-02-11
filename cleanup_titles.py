import os
import re

directory = r'c:\Users\Wilmher\source\repos\Proyecto-Laboratorios-Univalle\Pages'

# Pattern to match the specific ViewData["Title"] assignment
# It handles cases like:
# @{ ViewData["Title"] = "Title"; }
# or inside a larger block
pattern = re.compile(r'ViewData\["Title"\]\s*=\s*".*?";?\s*', re.IGNORECASE)

for root, dirs, files in os.walk(directory):
    for file in files:
        if file.endswith('.cshtml'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            new_content = pattern.sub('', content)
            
            # Also clean up empty @{ } blocks left behind
            new_content = re.sub(r'@{[\s\n]*?}', '', new_content)
            
            if new_content != content:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Cleaned: {path}")
