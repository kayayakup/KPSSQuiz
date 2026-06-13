#!/usr/bin/env python3
"""
KPSS Soru Bankası Oluşturucu - Sadece 2013-2021 yıllarını işler,
cevap anahtarlarını doğru eşleştirir, JSON'u script klasörüne kaydeder.
Kullanım: python kpss_process.py
"""

import argparse
import io
import json
import os
import re
import sys
import time
import urllib.request
from collections import Counter
from pathlib import Path

try:
    import pdfplumber
except ImportError:
    sys.exit("❌ pdfplumber kurulu değil. Kurmak için: pip install pdfplumber")

# ------------------------------------------------------------
def is_valid_pdf(data: bytes) -> bool:
    return data.startswith(b'%PDF')

def download_pdf(url: str) -> bytes | None:
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Accept": "application/pdf,*/*",
        "Referer": "https://dokuman.osym.gov.tr/",
    }
    try:
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req, timeout=30) as resp:
            data = resp.read()
            if not is_valid_pdf(data):
                print(f"  ⚠ İndirilen dosya PDF değil (ilk 4 bayt: {data[:4]})")
                return None
            return data
    except Exception as e:
        print(f"  ⚠ İndirme hatası: {e}")
        return None

def extract_text(pdf_bytes: bytes) -> str:
    if not pdf_bytes or not is_valid_pdf(pdf_bytes):
        return ""
    try:
        with pdfplumber.open(io.BytesIO(pdf_bytes)) as pdf:
            text_parts = []
            for page in pdf.pages:
                t = page.extract_text()
                if t:
                    text_parts.append(t)
            return "\n".join(text_parts)
    except Exception as e:
        print(f"  ⚠ PDF okuma hatası: {e}")
        return ""

# ------------------------------------------------------------
SUBJECT_MAP = {
    "gygk": ("kpss_lisans", "genel_yetenek_kultur", "Genel Yetenek - Genel Kültür", 3),
    "genyet": ("kpss_lisans", "genel_yetenek", "Genel Yetenek", 3),
    "genkul": ("kpss_lisans", "genel_kultur", "Genel Kültür", 3),
    "egitimbilimleri": ("kpss_lisans", "egitim_bilimleri", "Eğitim Bilimleri", 4),
    "kamu": ("kpss_lisans", "kamu_yonetimi", "Kamu Yönetimi", 4),
    "calismaekonomisi": ("kpss_lisans", "calisma_ekonomisi", "Çalışma Ekonomisi", 4),
    "uluslararasi": ("kpss_lisans", "uluslararasi_iliskiler", "Uluslararası İlişkiler", 4),
    "hukuk": ("kpss_lisans", "hukuk", "Hukuk", 4),
    "iktisat": ("kpss_lisans", "iktisat", "İktisat", 4),
    "maliye": ("kpss_lisans", "maliye", "Maliye", 4),
    "isletme": ("kpss_lisans", "isletme", "İşletme", 4),
    "istatistik": ("kpss_lisans", "istatistik", "İstatistik", 4),
    "muhasebe": ("kpss_lisans", "muhasebe", "Muhasebe", 4),
    "oabt_biyoloji": ("kpss_oabt", "biyoloji", "ÖABT Biyoloji", 5),
    "oabt_cografya": ("kpss_oabt", "cografya", "ÖABT Coğrafya", 5),
    "oabt_din": ("kpss_oabt", "din_kulturu", "ÖABT Din Kültürü", 5),
    "oabt_fen": ("kpss_oabt", "fen_bilimleri", "ÖABT Fen Bilimleri", 5),
    "oabt_fizik": ("kpss_oabt", "fizik", "ÖABT Fizik", 5),
    "oabt_matematik": ("kpss_oabt", "matematik", "ÖABT Matematik", 5),
    "oabt_ingilizce": ("kpss_oabt", "ingilizce", "ÖABT İngilizce", 5),
    "oabt_kimya": ("kpss_oabt", "kimya", "ÖABT Kimya", 5),
    "oabt_tarih": ("kpss_oabt", "tarih", "ÖABT Tarih", 5),
    "oabt_turkce": ("kpss_oabt", "turkce", "ÖABT Türkçe", 5),
    "oabt_sosyal": ("kpss_oabt", "sosyal_bilgiler", "ÖABT Sosyal Bilgiler", 5),
    "oabt_okuloncesi": ("kpss_oabt", "okul_oncesi", "ÖABT Okul Öncesi", 5),
    "oabt_rehber": ("kpss_oabt", "rehberlik", "ÖABT Rehberlik", 5),
    "oabt_sinif": ("kpss_oabt", "sinif_ogretmenligi", "ÖABT Sınıf Öğretmenliği", 5),
    "oabt_beden": ("kpss_oabt", "beden_egitimi", "ÖABT Beden Eğitimi", 5),
    "oabt_turkdili": ("kpss_oabt", "turk_dili_edebiyati", "ÖABT Türk Dili ve Edebiyatı", 5),
    "oabt_imamhatip": ("kpss_oabt", "imam_hatip", "ÖABT İmam Hatip", 5),
    "ortaogretim": ("kpss_ortaogretim", "genel", "KPSS Ortaöğretim", 2),
    "onlisans": ("kpss_onlisans", "genel", "KPSS Önlisans", 2),
    "dhbt": ("dhbt", "din_hizmetleri", "DHBT", 4),
    "almanca": ("kpss_yabanci_dil", "almanca", "Yabancı Dil - Almanca", 4),
    "ingilizce": ("kpss_lisans", "ingilizce", "Yabancı Dil - İngilizce", 4),
}
ANSWER_LETTERS = ["A", "B", "C", "D", "E"]

def get_metadata_from_url(url: str) -> dict:
    url_lower = url.lower()
    for key, meta in sorted(SUBJECT_MAP.items(), key=lambda x: -len(x[0])):
        if key in url_lower:
            exam_type, subject, subcategory, difficulty = meta
            year_match = re.search(r'(\d{4})', url)
            year = year_match.group(1) if year_match else "0000"
            return {
                "examType": exam_type,
                "subject": subject,
                "subcategory": subcategory,
                "difficulty": difficulty,
                "year": year,
                "topic": f"{subcategory} ({year} KPSS)",
            }
    year_match = re.search(r'(\d{4})', url)
    year = year_match.group(1) if year_match else "0000"
    return {
        "examType": "kpss",
        "subject": "genel",
        "subcategory": "Genel",
        "difficulty": 3,
        "year": year,
        "topic": f"Çıkmış Soru ({year} KPSS)",
    }

def is_answer_key_url(url: str) -> bool:
    u = url.lower()
    return any(k in u for k in ["cevapanahtari", "cevap_anahtari", "anah", "answer"])

def find_answer_key_url(question_url: str, all_urls: list[str]) -> str | None:
    q_base = question_url.lower().replace(".pdf", "")
    best = None
    best_len = 0
    for url in all_urls:
        if not is_answer_key_url(url):
            continue
        u_base = url.lower().replace(".pdf", "")
        common = 0
        for a, b in zip(q_base, u_base):
            if a == b:
                common += 1
            else:
                break
        if common > best_len and common > 40:
            best_len = common
            best = url
    return best

def parse_answer_key(text: str) -> dict[int, int]:
    answers = {}
    for m in re.finditer(r'(\d+)\s*[-–\.]\s*([A-E])', text):
        q_no = int(m.group(1))
        answers[q_no] = ANSWER_LETTERS.index(m.group(2).upper())
    if not answers:
        for m in re.finditer(r'\b(\d{1,3})\s+([A-E])\b', text):
            q_no = int(m.group(1))
            if 1 <= q_no <= 200:
                answers[q_no] = ANSWER_LETTERS.index(m.group(2).upper())
    return answers

def parse_questions(text: str, meta: dict, answer_key: dict, year: str, url: str) -> list[dict]:
    """
    Soru metnini ve şıkları daha güvenli bir şekilde ayırır.
    Şık formatları: A) , A. , A-  ve küçük/büyük harf fark etmez.
    """
    questions = []
    # Soru bloklarını yakala (örn. "1. ...")
    pattern = re.compile(r'(?m)^(\d{1,3})\.\s+(.*?)(?=^\d{1,3}\.\s|\Z)', re.DOTALL | re.MULTILINE)

    for match in pattern.finditer(text):
        q_no = int(match.group(1))
        block = match.group(2).strip()

        # Şıkları bul: A) veya A. veya A- (büyük/küçük harf duyarsız)
        # Her şık için grup: (harf, şık metni)
        opt_pattern = re.compile(r'([A-Ea-e])[\)\.\-]\s*(.*?)(?=(?:[A-Ea-e][\)\.\-]\s*|\Z))', re.DOTALL)
        matches = list(opt_pattern.finditer(block))

        if len(matches) != 5:
            # Tam 5 şık yoksa bu soruyu atla (muhtemelen bozuk format)
            continue

        # İlk şıktan önceki kısım soru metnidir
        first_opt_start = matches[0].start()
        q_text = block[:first_opt_start].strip()
        # Soru metnini temizle (başındaki gereksiz boşluklar, satır sonları)
        q_text = re.sub(r'\s+', ' ', q_text).strip()

        options = []
        for m in matches:
            opt_text = m.group(2).strip()
            # Şık metnindeki gereksiz satır sonlarını boşluğa çevir
            opt_text = re.sub(r'\s+', ' ', opt_text)
            options.append(opt_text)

        # Cevap anahtarından doğru indeksi al (yoksa 0 - A)
        correct_idx = answer_key.get(q_no, 0)
        correct_letter = ANSWER_LETTERS[correct_idx] if correct_idx < len(ANSWER_LETTERS) else "A"
        correct_text = options[correct_idx] if correct_idx < len(options) else ""

        explanation = (
            f"Bu soru {year} KPSS {meta['subcategory']} sınavından alınmıştır. "
            f"Doğru cevap {correct_letter} şıkkıdır"
            + (f": {correct_text[:100]}" if correct_text else "")
            + "."
        )

        q_id = int(f"{year}{q_no:04d}")

        questions.append({
            "id": q_id,
            "examType": meta["examType"],
            "subject": meta["subject"],
            "subcategory": meta["subcategory"],
            "topic": meta["topic"],
            "difficulty": meta["difficulty"],
            "questionText": q_text,
            "answers": options,
            "correctAnswerIndex": correct_idx,
            "explanation": explanation,
            "source": {"year": int(year), "questionNumber": q_no, "url": url},
        })

    return questions

def process_url(url: str, all_urls: list[str], local_pdf_dir: str) -> list[dict]:
    print(f"→ {url}")
    filename = url.split("/")[-1]
    local_path = os.path.join(local_pdf_dir, filename)

    # Soru PDF'ini yükle
    pdf_bytes = None
    if os.path.exists(local_path):
        with open(local_path, "rb") as f:
            pdf_bytes = f.read()
        if not is_valid_pdf(pdf_bytes):
            print(f"  ⚠ Yerel dosya geçersiz PDF, siliniyor: {local_path}")
            pdf_bytes = None
    else:
        pdf_bytes = download_pdf(url)
        time.sleep(0.3)

    if not pdf_bytes:
        return []

    # Cevap anahtarını bul
    answer_key = {}
    ak_url = find_answer_key_url(url, all_urls)
    if ak_url:
        ak_filename = ak_url.split("/")[-1]
        ak_local = os.path.join(local_pdf_dir, ak_filename)
        ak_bytes = None
        if os.path.exists(ak_local):
            with open(ak_local, "rb") as f:
                ak_bytes = f.read()
            if not is_valid_pdf(ak_bytes):
                ak_bytes = None
        else:
            ak_bytes = download_pdf(ak_url)
            time.sleep(0.3)
        if ak_bytes:
            ak_text = extract_text(ak_bytes)
            if ak_text:
                answer_key = parse_answer_key(ak_text)
                print(f"  ✓ Cevap anahtarı: {len(answer_key)} cevap")
            else:
                print("  ⚠ Cevap anahtarı metni boş")
        else:
            print("  ⚠ Cevap anahtarı indirilemedi/geçersiz")
    else:
        print("  ⚠ Cevap anahtarı eşleşmedi")

    meta = get_metadata_from_url(url)
    text = extract_text(pdf_bytes)
    if not text:
        print("  ⚠ Metin çıkarılamadı")
        return []
    questions = parse_questions(text, meta, answer_key, meta["year"], url)
    print(f"  ✓ {len(questions)} soru çıkarıldı")
    return questions

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--links", default="Assets\pdfs\çıkmış_sorular_pdf_link.txt")
    parser.add_argument("--out", default=None)
    parser.add_argument("--pdf-dir", default="Assets/pdfs")
    parser.add_argument("--limit", type=int, default=0)
    args = parser.parse_args()

    script_dir = Path(__file__).parent.absolute()
    if args.out is None:
        out_path = script_dir / "kpss_sorular.json"
    else:
        out_path = Path(args.out)
        if not out_path.is_absolute():
            out_path = script_dir / out_path

    if not os.path.exists(args.links):
        sys.exit(f"Hata: '{args.links}' dosyası bulunamadı.")

    with open(args.links, encoding="utf-8") as f:
        all_urls = [line.strip() for line in f if line.strip().startswith("http")]

    # Yıl filtresi: sadece 2013-2021 arası soru PDF'lerini al
    question_urls = []
    for url in all_urls:
        if is_answer_key_url(url):
            continue
        y = re.search(r'(\d{4})', url)
        if y:
            year = int(y.group(1))
            if 2013 <= year <= 2021:
                question_urls.append(url)

    print(f"📄 Toplam {len(all_urls)} URL, yıl filtresi sonrası {len(question_urls)} soru PDF'i (2013-2021)")
    print(f"   Cevap anahtarı sayısı: {len([u for u in all_urls if is_answer_key_url(u)])}")
    if args.limit:
        question_urls = question_urls[:args.limit]
        print(f"🔍 Limit: ilk {args.limit} PDF")

    Path(args.pdf_dir).mkdir(parents=True, exist_ok=True)

    all_questions = []
    seen_ids = set()
    for idx, url in enumerate(question_urls, 1):
        print(f"\n[{idx}/{len(question_urls)}]")
        questions = process_url(url, all_urls, args.pdf_dir)
        for q in questions:
            while q["id"] in seen_ids:
                q["id"] = q["id"] * 10 + 1
            seen_ids.add(q["id"])
            all_questions.append(q)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(all_questions, f, ensure_ascii=False, indent=2)

    print(f"\n✅ {len(all_questions)} soru → {out_path}")
    for exam, count in Counter(q["examType"] for q in all_questions).items():
        print(f"   • {exam}: {count} soru")

if __name__ == "__main__":
    main()