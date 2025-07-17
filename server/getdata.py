# save_server.py
from flask import Flask, request, jsonify
from PIL import Image
import torch
import yolov5  # pip install yolov5
import os
import openai
from dotenv import load_dotenv  # 追加
import json
import cv2
import numpy as np

load_dotenv()  # .envファイルを読み込む

app = Flask(__name__)

# 元画像上の固定クロップ領域
CROP_BOX   = (506, 108, 506+235, 108+508)  # (左, 上, 右, 下)
TARGET_SZ  = (1280, 720)

# YOLOv5モデルのロード（初回のみ）
model = yolov5.load('yolov5s.pt')  # yolov5s.ptは自動DLされます

# MiDaSモデルのロード（初回のみ）
midas = torch.hub.load("intel-isl/MiDaS", "DPT_Large")
midas.eval()
midas_transforms = torch.hub.load("intel-isl/MiDaS", "transforms").dpt_transform

OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")  # .envから取得
openai.api_key = OPENAI_API_KEY

def send_image_to_gpt(image_path, label):
    import base64
    with open(image_path, "rb") as f:
        image_bytes = f.read()
    img_b64 = base64.b64encode(image_bytes).decode()
    prompt = f"これは{label}です。詳しく説明してください。出力は必ず次の形式で返してください：{{\"{label}\": \"説明文\"}}"
    response = openai.chat.completions.create(
        model="gpt-4o",
        messages=[
            {"role": "user", "content": [
                {"type": "text", "text": prompt},
                {"type": "image_url", "image_url": {"url": f"data:image/png;base64,{img_b64}"}}
            ]}
        ],
        max_tokens=512,
    )
    return response.choices[0].message.content

def estimate_depth(pil_img):
    img = np.array(pil_img.convert("RGB"))
    input_batch = midas_transforms(img).unsqueeze(0)
    with torch.no_grad():
        prediction = midas(input_batch)
        prediction = torch.nn.functional.interpolate(
            prediction.unsqueeze(1),
            size=img.shape[:2],
            mode="bicubic",
            align_corners=False,
        ).squeeze()
    depth_map = prediction.cpu().numpy()
    return depth_map

def send_objects_to_gpt(objects):
    # objects: [{'label': 'person', 'bbox': [xmin, ymin, xmax, ymax]}, ...]
    prompt = "以下の物体について、ラベルとバウンディングボックス座標をもとに、それぞれを詳しく説明してください。\n"
    for obj in objects:
        prompt += f"ラベル: {obj['label']}, 座標: {obj['bbox']}\n"
    prompt += "出力は必ず次の形式で返してください：{\"ラベル1\": \"説明文1\", \"ラベル2\": \"説明文2\", ...}"

    response = openai.chat.completions.create(
        model="gpt-4o",
        messages=[{"role": "user", "content": prompt}],
        max_tokens=1024,
    )
    return response.choices[0].message.content

@app.route('/upload', methods=['POST'])
def upload():
    try:
        print("受信intrinsics:", request.form['intrinsics'])  # デバッグ用
        intrinsics = json.loads(request.form['intrinsics'])
        fx = intrinsics['fx']
        fy = intrinsics['fy']
        cx = intrinsics['cx']
        cy = intrinsics['cy']
        file    = request.files['file']
        img     = Image.open(file.stream)
        img.save("debug_received.png")

        # ここで画像とカメラ行列を使って処理

        # 1. 固定領域をクロップ
        cropped = img.crop(CROP_BOX)
        # 2. リサイズ
        resized = cropped.resize(TARGET_SZ, Image.LANCZOS)
        out_path = f"./received_{file.filename}"
        resized.save(out_path)

        # --- 深度マップ推論 ---
        depth_map = estimate_depth(resized)

        # --- YOLO推論 ---
        results = model(out_path)
        df = results.pandas().xyxy[0]
        print(df.columns)
        print(df.head())
        obj_desc = {}

        if df.empty:
            print("YOLO検出結果なし")
            return jsonify({"error": "no objects detected"}), 200

        # 物体情報をまとめてリスト化
        objects = []
        for idx, row in df.iterrows():
            label = row.get('name', '')
            # カラム名の違いに対応
            xmin = int(row.get('xmin', row.get('x1')))
            ymin = int(row.get('ymin', row.get('y1')))
            xmax = int(row.get('xmax', row.get('x2')))
            ymax = int(row.get('ymax', row.get('y2')))
            objects.append({
                "label": label,
                "bbox": [xmin, ymin, xmax, ymax]
            })

        # GPTにまとめて問い合わせ
        desc_json = send_objects_to_gpt(objects)
        descriptions = json.loads(desc_json)

        # 3D位置計算とレスポンス生成
        for idx, row in df.iterrows():
            label = row['name']
            xmin, ymin, xmax, ymax = map(int, [row['xmin'], row['ymin'], row['xmax'], row['ymax']])
            cx_img = int((xmin + xmax) / 2)
            cy_img = int((ymin + ymax) / 2)
            z = float(depth_map[cy_img, cx_img])
            Xc = (cx_img - cx) / fx * z
            Yc = (cy_img - cy) / fy * z
            Zc = z
            obj_desc[f"{label}_{idx}"] = {
                "description": descriptions.get(label, ""),
                "world_position": [Xc, Yc, Zc]
            }

        print(obj_desc)
        return jsonify(obj_desc), 200
    except Exception as e:
        print("Error:", e)
        return 'NG', 500

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5001, debug=True)
