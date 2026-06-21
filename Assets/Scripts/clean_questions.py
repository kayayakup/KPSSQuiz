"""
clean_questions.py
Tüm KPSS soru JSON dosyalarını tarar:
  - Boş/eksik cevap içeren soruları siler
  - correctAnswerIndex boş bir cevabı gösteriyorsa siler
  - Temizlenmiş JSON'u geri yazar
  - Özet rapor basar
"""

import json
import os
import glob

QUESTIONS_DIR = os.path.join(
    os.path.dirname(__file__),
    "..", "Resources", "Questions"
)

MIN_VALID_ANSWERS = 4  # En az kaç dolu cevap olmalı


def is_broken(q):
    answers = q.get("answers", [])
    correct_idx = q.get("correctAnswerIndex", -1)
    question_text = q.get("questionText", "")

    # Soru metni boşsa bozuk
    if not question_text or not question_text.strip():
        return True, "boş questionText"

    # Cevap dizisi yoksa veya kısaysa bozuk
    if not answers or len(answers) < MIN_VALID_ANSWERS:
        return True, f"answers sayısı < {MIN_VALID_ANSWERS}"

    # Dolu cevap sayısı kontrolü
    valid_answers = [str(a).strip() for a in answers if str(a).strip()]
    if len(valid_answers) < MIN_VALID_ANSWERS:
        return True, f"dolu cevap sayısı {len(valid_answers)} < {MIN_VALID_ANSWERS}"

    # correctAnswerIndex geçerli mi?
    if correct_idx < 0 or correct_idx >= len(answers):
        return True, f"correctAnswerIndex={correct_idx} geçersiz"

    # Doğru cevap boş mu?
    if not str(answers[correct_idx]).strip():
        return True, f"doğru cevap (index {correct_idx}) boş"

    return False, ""


def process_file(filepath):
    filename = os.path.basename(filepath)
    with open(filepath, encoding="utf-8") as f:
        try:
            data = json.load(f)
        except json.JSONDecodeError as e:
            print(f"  ❌ JSON parse hatası: {e}")
            return 0, 0

    # Hem array hem {questions:[]} formatını destekle
    is_wrapped = isinstance(data, dict) and "questions" in data
    questions = data["questions"] if is_wrapped else data

    original_count = len(questions)
    cleaned = []
    removed = []

    for q in questions:
        broken, reason = is_broken(q)
        if broken:
            removed.append((q.get("id", "?"), reason))
        else:
            cleaned.append(q)

    removed_count = len(removed)

    if removed_count > 0:
        # Geri yaz
        if is_wrapped:
            data["questions"] = cleaned
            output = data
        else:
            output = cleaned

        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(output, f, ensure_ascii=False, indent=2)

        print(f"  ✅ {original_count} → {len(cleaned)} soru ({removed_count} silindi)")
        for q_id, reason in removed[:5]:
            print(f"     • ID={q_id}: {reason}")
        if removed_count > 5:
            print(f"     ... ve {removed_count - 5} daha")
    else:
        print(f"  ✅ {original_count} soru — bozuk yok")

    return original_count, removed_count


def main():
    dir_path = os.path.abspath(QUESTIONS_DIR)
    if not os.path.isdir(dir_path):
        print(f"❌ Dizin bulunamadı: {dir_path}")
        return

    files = sorted(glob.glob(os.path.join(dir_path, "*.json")))
    if not files:
        print("❌ JSON dosyası bulunamadı.")
        return

    print(f"📁 {len(files)} JSON dosyası işlenecek...\n")

    total_original = 0
    total_removed = 0

    for filepath in files:
        print(f"📄 {os.path.basename(filepath)}")
        orig, removed = process_file(filepath)
        total_original += orig
        total_removed += removed

    print(f"\n{'='*50}")
    print(f"TOPLAM: {total_original} soru işlendi, {total_removed} bozuk silindi")
    print(f"KALAN:  {total_original - total_removed} soru")
    print(f"{'='*50}")


if __name__ == "__main__":
    main()
