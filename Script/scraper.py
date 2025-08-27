from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager
import requests
import json
import time
import sqlite3

# --------- 1️⃣ Selenium ile login ve c_usertoken alma ---------
chrome_options = Options()
chrome_options.add_argument("--headless")  # Tarayıcı görünmesin
chrome_options.add_argument("--no-sandbox")
chrome_options.add_argument("--disable-dev-shm-usage")

driver = webdriver.Chrome(service=Service(ChromeDriverManager().install()), options=chrome_options)

# Siteyi aç
driver.get("https://b2b.enderyapi.com.tr/tr/giris")

# Kullanıcı adı ve şifreyi gir
driver.find_element(By.ID, "userName").send_keys("YourUsername")  # 🔑 kullanıcı adı
driver.find_element(By.ID, "password").send_keys("YourPassword")         # 🔑 şifre

# Login butonuna tıkla
driver.find_element(By.CLASS_NAME, "btn-login").click()

# Birkaç saniye bekle, cookie set edilsin
time.sleep(5)

# c_usertoken cookie'sini al
token_cookie = None
for c in driver.get_cookies():
    if c['name'] == "c_usertoken":
        token_cookie = c['value']
        break

if not token_cookie:
    print(" c_usertoken bulunamadi!")
    driver.quit()
    exit()



driver.quit()

# --------- 2️⃣ Requests ile ürünleri çekme ---------
url = "https://b2bstore.com.tr:14500/services/product/get-product-search"

headers = {
    "accept": "application/json, text/plain, */*",
    "accept-language": "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7",
    "authorization": f"Bearer {token_cookie}",
    "browser-web-url": "b2b.enderyapi.com.tr",
    "content-type": "application/json",
    "languagecode": "TR",
    "origin": "https://b2b.enderyapi.com.tr",
    "referer": "https://b2b.enderyapi.com.tr/tr/",
    "user-agent": "Mozilla/5.0",
    "x-real-for": ""
}

all_products = []
total_pages = 140  # burada 10 sayfa çekiyor, gerçek çalıştırmada 140 yapabilirsin

for page in range(1, total_pages + 1):
    payload = {
        "keyword": None,
        "languageCode": "TR",
        "Pg": str(page),
        "DiscountSale": 0,
        "MostSaled": 0,
        "MostViewed": 0,
        "NewProduct": 0,
        "ProductOfTheDay": 0,
        "ProductOfTheWeek": 0,
        "Stock": 0,
        "RunFlat": 0,
        "ConsumerPrice": False,
        "filterModel": {
            "Filter": [],
            "CategoryID": None,
            "BrandID": None,
            "SeasonID": None
        }
    }

    print(f" Fetching page {page}...")
    response = requests.post(url, headers=headers, data=json.dumps(payload))

    if response.status_code == 200:
        data = response.json()
        hits = data.get("Data", {}).get("Hits", {}).get("$values", [])
        for item in hits:
            product = {
                "ProductID": item.get("ProductID"),
                "ProductName": item.get("ProductDetails", {}).get("ProductName"),
                "ImageUrl": item.get("ProductDetails", {}).get("ImageUrl"),
                "Price": item.get("ProductPrices", {}).get("Price"),
                "PriceNormal": item.get("ProductPrices", {}).get("PriceNormal")
            }
            all_products.append(product)
    else:
        print(f" Error on page {page}: {response.status_code} {response.text}")

    time.sleep(0.5)  # sunucuyu yormamak için

# --------- 3️⃣ SQLite'e Kaydetme ---------
conn = sqlite3.connect("urunler.db", timeout=20)
cursor = conn.cursor()

# Eğer tablo yoksa oluştur
cursor.execute("""
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Barcode INTEGER NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Price REAL,
    GelisFiyati REAL,
    Stock INTEGER,
    Description TEXT,
    ImagePath TEXT
)
""")

# Barcode başlangıç değeri
barcode_start = 1_000_000

for i, product in enumerate(all_products):
    barcode = barcode_start + i
    name = product.get("ProductName", "")
    price_normal = product.get("PriceNormal") or 0
    price = product.get("Price") or 0
    image_url = product.get("ImageUrl", "")

    cursor.execute("""
    INSERT OR IGNORE INTO Products 
    (Barcode, Name, Price, GelisFiyati, Stock, Description, ImagePath)
    VALUES (?, ?, ?, ?, ?, ?, ?)
    """, (barcode, name, price_normal, price, 1, None, image_url))

conn.commit()
conn.close()

print(f" {len(all_products)} urun urunler.db -> Products tablosuna kaydedildi!")
