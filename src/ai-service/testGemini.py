import os
import base64
import io
from openai import OpenAI
from PIL import Image

client = OpenAI(
    base_url="https://openrouter.ai/api/v1"
)

image_path = "/home/anas-oklah/Downloads/Screenshot 2026-07-08 at 14-57-13 SoftwareEngineering_CH1.pdf.png"

with Image.open(image_path) as img:
    if img.mode in ("RGBA", "P"):
        img = img.convert("RGB")
    img.thumbnail((1024, 1024))
    buffer = io.BytesIO()
    img.save(buffer, format="JPEG", quality=80)
    base64_image = base64.b64encode(buffer.getvalue()).decode("utf-8")

print("🔄 يتم الآن إرسال الطلب إلى الموزع المجاني الآلي...")

# ⚠️ التعديل الجوهري: استخدام المعرّف الموحد للنماذج المجانية
response = client.chat.completions.create(
    model="google/gemma-4-31b-it:free", 
    messages=[
        {
            "role": "user",
            "content": [
                {"type": "text", "text": "Extract all the text and information from this image."},
                {"type": "image_url", "image_url": {"url": f"data:image/jpeg;base64,{base64_image}"}}
            ]
        }
    ]
)

# طباعة النتيجة دون خوف من الـ 404 أو الـ AttributeError
print("✅ الاستجابة المستلمة:")
print(response.choices[0].message.content)
