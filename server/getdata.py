# save_server.py
from flask import Flask, request
from PIL import Image

app = Flask(__name__)

# 元画像上の固定クロップ領域
CROP_BOX   = (506, 108, 506+235, 108+508)  # (左, 上, 右, 下)
TARGET_SZ  = (1280, 720)

@app.route('/upload', methods=['POST'])
def upload():
    try:
        file    = request.files['file']
        img     = Image.open(file.stream)

        # 1. 固定領域をクロップ
        cropped = img.crop(CROP_BOX)

        # 2. リサイズ
        resized = cropped.resize(TARGET_SZ, Image.LANCZOS)

        # 3. 保存
        out_path = f"./received_{file.filename}"
        resized.save(out_path)

        print("File received, cropped (fixed), resized, and saved:", out_path)
        return 'OK', 200
    except Exception as e:
        print("Error:", e)
        return 'NG', 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001)
