import json, os, glob, sys
base = r'c:/Users/yakup/OneDrive/Belgeler/GitHub/KPSSQuiz/Assets/Resources/Questions'
files = glob.glob(os.path.join(base, '*.json'))

def normalize_category(q):
    et = q.get('examType','').lower()
    sub = q.get('subcategory','') or ''
    subj = q.get('subject','') or ''
    topic = q.get('topic','') or ''
    low = sub.lower()
    # KPSS Lisans ve Ortaöğretim: only Genel Kültür ve Genel Yetenek (combined)
    if et.startswith('kpss_lisans') or et.startswith('kpss_ortaogretim'):
        return 'Genel Kültür ve Genel Yetenek'
    # KPSS Önlisans: separate categories
    if et.startswith('kpss_onlisans'):
        # If subcategory already one of the desired ones, keep it (after normalization)
        if 'genel kültür' in low:
            return 'Genel Kültür'
        if 'genel yetenek' in low:
            return 'Genel Yetenek'
        if 'alan' in low:
            return 'Alan Dersleri'
        # fallback to subject if present
        return subj if subj else sub
    # KPSS ÖABT: keep only specific field categories (topic) and remove generic terms
    if et.startswith('kpss_oabt'):
        # If subcategory contains generic terms, replace with topic
        if any(g in low for g in ['genel kültür', 'genel yetenek', 'alan']):
            return topic if topic else subj
        return sub if sub else subj
    # Default: keep existing subcategory
    return sub if sub else subj

updated_files = 0
for path in files:
    with open(path, encoding='utf-8') as f:
        data = json.load(f)
    modified = False
    for q in data.get('questions', []):
        new_cat = normalize_category(q)
        if new_cat and q.get('subcategory','') != new_cat:
            q['subcategory'] = new_cat
            modified = True
    if modified:
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print('Updated', os.path.basename(path))
        updated_files += 1
print('Total files updated:', updated_files)
