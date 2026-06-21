import json
import os
import re
import glob

QUESTIONS_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "Resources", "Questions"
)

def normalize_text(text):
    if not text:
        return ""
    # Lowercase
    text = text.lower()
    
    # Remove leading question prefixes like "gk soru 12: ", "soru 5:", "gk soru 12-", "12. "
    text = re.sub(r'^(gk\s+)?soru\s*\d+\s*[\:\-]?\s*', '', text)
    text = re.sub(r'^\d+\s*[\.\-\)]\s*', '', text)
    
    # Remove all non-alphanumeric characters (Turkish characters mapped or kept)
    text = re.sub(r'[^a-z0-9çığöşü]', '', text)
    
    return text.strip()

def clean_duplicates():
    dir_path = os.path.abspath(QUESTIONS_DIR)
    if not os.path.isdir(dir_path):
        print(f"Dizin bulunamadi: {dir_path}")
        return

    # Define branch prefixes to group files
    branch_prefixes = ["kpss_ortaogretim", "kpss_onlisans", "kpss_lisans", "kpss_oabt"]
    
    total_original = 0
    total_cleaned = 0
    total_removed = 0

    for prefix in branch_prefixes:
        # Find all JSON files for this branch prefix
        files = sorted(glob.glob(os.path.join(dir_path, f"{prefix}*.json")))
        if not files:
            continue
            
        print("\n" + "-" * 50)
        print(f"Brans Grubu: {prefix.upper()} ({len(files)} dosya)")
        print("-" * 50)
        
        seen_questions = set()
        
        for filepath in files:
            filename = os.path.basename(filepath)
            
            with open(filepath, encoding="utf-8") as f:
                try:
                    data = json.load(f)
                except json.JSONDecodeError as e:
                    print(f"  JSON parse hatasi: {e}")
                    continue
            
            # Support both array and {questions: []} format
            is_wrapped = isinstance(data, dict) and "questions" in data
            questions = data["questions"] if is_wrapped else data
            
            original_count = len(questions)
            total_original += original_count
            
            cleaned_list = []
            removed_count = 0
            
            for q in questions:
                q_text = q.get("questionText", "")
                norm = normalize_text(q_text)
                
                if not norm:
                    # Keep empty question texts for the general cleaning script to handle/remove
                    cleaned_list.append(q)
                    continue
                    
                if norm in seen_questions:
                    removed_count += 1
                    total_removed += 1
                else:
                    seen_questions.add(norm)
                    cleaned_list.append(q)
            
            if removed_count > 0:
                if is_wrapped:
                    data["questions"] = cleaned_list
                    output = data
                else:
                    output = cleaned_list
                    
                with open(filepath, "w", encoding="utf-8") as f:
                    json.dump(output, f, ensure_ascii=False, indent=2)
                    
                print(f"  OK {filename}: {original_count} -> {len(cleaned_list)} soru ({removed_count} tekrar eden silindi)")
            else:
                print(f"  OK {filename}: {original_count} soru (Tekrar eden yok)")
                
            total_cleaned += len(cleaned_list)

    print("\n" + "=" * 50)
    print("Tum temizlik islemleri tamamlandi.")
    print(f"Baslangictaki Soru Sayisi: {total_original}")
    print(f"Silinen Soru Sayisi:       {total_removed}")
    print(f"Kalan Soru Sayisi:          {total_cleaned}")
    print("=" * 50)

if __name__ == "__main__":
    clean_duplicates()
