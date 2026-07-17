import os
import re

scenes_dir = "Assets/Scenes"
files = [f for f in os.listdir(scenes_dir) if f.endswith(".unity")]

for f in files:
    path = os.path.join(scenes_dir, f)
    with open(path, "r", encoding="utf-8", errors="ignore") as file:
        content = file.read()
    
    go_match = re.search(r"--- !u!1 &(\d+)\nGameObject:.*?\nm_Name: camioneta_0\b", content, re.DOTALL)
    if not go_match:
        print(f"{f}: camioneta_0 not found")
        continue
    
    go_id = go_match.group(1)
    
    sr_pattern = r"--- !u!212 &\d+\nSpriteRenderer:.*?\nm_GameObject: \{fileID: " + go_id + r"\}.*?\nm_SortingOrder: (-?\d+)"
    sr_match = re.search(sr_pattern, content, re.DOTALL)
    if sr_match:
        print(f"{f}: go_id={go_id}, sorting_order={sr_match.group(1)}")
    else:
        # Sometimes fields are in different order
        sr_pattern_alt = r"--- !u!212 &\d+\nSpriteRenderer:.*?\nm_SortingOrder: (-?\d+).*?\nm_GameObject: \{fileID: " + go_id + r"\}"
        sr_match_alt = re.search(sr_pattern_alt, content, re.DOTALL)
        if sr_match_alt:
            print(f"{f}: go_id={go_id}, sorting_order={sr_match_alt.group(1)} (alt)")
        else:
            print(f"{f}: SpriteRenderer for go_id={go_id} not found")
