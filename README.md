# 🎮 Libellus2D

[![Unity](https://img.shields.io/badge/Unity-2022.3.62f3%20LTS-black?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%202D-blue)](https://unity.com/features/universal-render-pipeline)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=c-sharp)](https://docs.microsoft.com/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-macOS%20%7C%20Windows-lightgrey)](#)
[![License](https://img.shields.io/badge/Version-Alpha-orange)](#)

**Libellus2D** คือเกมแอ็กชัน 2D ผจญภัยดันเจี้ยน (Top-down 2D Action Dungeon Crawler / Boss Rush) ที่พัฒนาด้วยเอนจิน **Unity (Universal Render Pipeline 2D)** ภายใต้ธีมการต่อสู้สุดเข้มข้นของชีวิตนักศึกษากับเหล่าอาจารย์ที่ปรึกษาและบททดสอบธีสิส

---

## 📖 สารบัญ (Table of Contents)
- [ภาพรวมของเกม (Game Overview)](#-ภาพรวมของเกม-game-overview)
- [ระบบการเล่นหลัก (Core Mechanics)](#-ระบบการเล่นหลัก-core-mechanics)
- [การควบคุมตัวละคร (Controls)](#-การควบคุมตัวละคร-controls)
- [ระบบบอสเด่น: อาจารย์รัตติกาล (Boss Spotlight)](#-ระบบบอสเด่น-อาจารย์รัตติกาล-boss-spotlight)
- [ระบบฐานข้อมูลเซฟเกม (Save & Progress System)](#-ระบบฐานข้อมูลเซฟเกม-save--progress-system)
- [โครงสร้างโฟลเดอร์โปรเจกต์ (Project Structure)](#-โครงสร้างโฟลเดอร์โปรเจกต์-project-structure)
- [การติดตั้งและเปิดโปรเจกต์ (Getting Started)](#-การติดตั้งและเปิดโปรเจกต์-getting-started)
- [บันทึกทางเทคนิคและสภาพแวดล้อม (Developer Notes)](#-บันทึกทางเทคนิคและสภาพแวดล้อม-developer-notes)

---

## 🌟 ภาพรวมของเกม (Game Overview)

ใน **Libellus2D** ผู้เล่นจะได้รับบทเป็นนักศึกษาที่ต้องฝ่าฟันห้องเรียนและดันเจี้ยนห้องปิดตาย เผชิญหน้ากับเหล่ามอนสเตอร์และบอสอาจารย์ที่มีท่าโจมตีจากอุปกรณ์การเรียนการสอนจริง เช่น ดินสอ EE, กระดานวาดรูป, คัดเตอร์ และไม้บรรทัด

### จุดเด่นของเกม:
- **Fast-paced 2D Combat:** ผสมผสานการต่อสู้ระยะประชิด (Melee) และการยิงระยะไกล (Ranged) พร้อมระบบพุ่งหลบ (Dash) ที่คล่องตัว
- **Dungeon Room Lock:** ประตูห้องจะปิดผนึกอัตโนมัติเมื่อก้าวเข้าสู่พื้นที่ต่อสู้ และจะปลดล็อกพร้อมดรอปรางวัลเมื่อเคลียร์ศัตรูหมดห้อง
- **Save State Database:** ระบบบันทึกและโหลดสถานะความคืบหน้าของด่านและบอสที่กำจัดผ่าน Local JSON Database
- **Universal Render Pipeline (URP 2D):** กราฟิกและแสงเงาแบบ 2D URP ที่สวยงามและปรับแต่งได้ลื่นไหล

---

## ⚔️ ระบบการเล่นหลัก (Core Mechanics)

### 1. ระบบเคลื่อนที่และการหลบหลีก (Movement & Dash)
- รองรับการเคลื่อนที่แบบ 8 ทิศทางพร้อมคำนวณ Physics ผ่าน `Rigidbody2D`
- ระบบ **Dash (พุ่งตัว)** มีระยะเวลาพุ่ง, ระยะคูลดาวน์ และตรวจจับขอบสิ่งกีดขวางอัตโนมัติเพื่อไม่ให้หลุดออกนอกแผนที่

### 2. ระบบการต่อสู้ (Dual Combat System)
- **การฟันระยะประชิด (Melee Slash):** สร้าง Hitbox โจมตีด้านหน้าอย่างรวดเร็ว ทำดาเมจหนัก พร้อมระบบหันหน้าหาศัตรูที่ใกล้ที่สุดอัตโนมัติ
- **การยิงกระสุนระยะไกล (Ranged Projectile):** ยิงกระสุนเวทมนตร์/พลังงานไปยังทิศทางเป้าหมายเพื่อทำดาเมจจากระยะปลอดภัย

### 3. ระบบหัวใจและพลังชีวิต (Player Health & Knockback)
- แสดงแถบพลังชีวิตเป็นรูปหัวใจ (Heart HUD)
- ระบบ **Invincibility Frames (I-Frames):** ตัวละครจะกะพริบและเป็นอมตะชั่วขณะหลังโดนโจมตี ป้องกันการโดนดาเมจซ้ำซ้อน
- ระบบ **Knockback:** มีแรงผลักกระเด็นเมื่อปะทะกับศัตรูหรือกับดัก
- หีบสมบัติ (Treasure Chests) สามารถเปิดเพื่อดรอปไอเทมและหัวใจฟื้นฟูเลือด

---

## 🕹️ การควบคุมตัวละคร (Controls)

| การกระทำ (Action) | ปุ่มบนคีย์บอร์ด / เมาส์ (Keybindings) | คำอธิบาย |
| :--- | :--- | :--- |
| **เดิน (Move)** | `W`, `A`, `S`, `D` | เคลื่อนที่ 8 ทิศทาง |
| **พุ่งหลบ (Dash)** | `Spacebar` | พุ่งตัวไปข้างหน้าอย่างรวดเร็ว |
| **ฟันระยะประชิด (Melee Attack)** | `คลิกซ้าย (LMB)` หรือปุ่ม `J` | ฟันศัตรูระยะประชิด |
| **ยิงกระสุนระยะไกล (Ranged Attack)** | `คลิกขวา (RMB)` หรือปุ่ม `K` | ยิงกระสุนระยะไกล |
| **หยุดเกม (Pause Menu)** | `Esc` | เปิดเมนูพักเกม / ตั้งค่า |

---

## 🎨 ระบบบอสเด่น: อาจารย์รัตติกาล (Boss Spotlight)

**"ก่อนจะไปซื้อเกมมาเล่น ส่งธีสิทอาจารย์ก่อนมั้ย"**  
บอสประจำด่าน **LV01** ผู้เป็นอาจารย์สาวสอนวิชาวาดภาพและที่ปรึกษาธีสิส มาพร้อมระบบ 2 เฟสการต่อสู้ (Phase 1 & Phase 2 เมื่อเลือดต่ำกว่า 50%) และท่าไม้ตาย 4 รูปแบบ:

1. ✏️ **ดินสอ EE พิฆาต 8 ทิศ:** ยิงดินสอ EE กระจายรอบทิศทาง 8 แฉกแบบระลอกคลื่น
2. 📋 **กระดานวาดรูปวนรอบตัว (Orbiting Boards):** เสกกระดานวาดรูป 4–6 แผ่น หมุนวนรอบตัวเป็นโล่ป้องกันและสร้างดาเมจเมื่อผู้เล่นเข้าใกล้
3. 🗡️ **มีดคัดเตอร์สุ่มแทงจากพื้น (Floor Cutter Traps):** ใบมีดคัดเตอร์เหลาดินสอสุ่มพุ่งขึ้นมาจากพื้นรอบตัวผู้เล่น
4. 📏 **ลำแสงไม้บรรทัดยิงทะลุจอ (Ruler Beam Cannon):** ชาร์จยิงลำแสงขนาดใหญ่ทะลุข้ามหน้าจอ พร้อมระบบ **Dynamic Camera Zoom Out** เพื่อขยายมุมมองกล้องให้เห็นความอลังการของท่าโจมตี

---

## 💾 ระบบฐานข้อมูลเซฟเกม (Save & Progress System)

ข้อมูลความคืบหน้าของเกมถูกจัดการผ่าน [`GameProgressManagerCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Progress/GameProgressManagerCS.cs) และบันทึกลงในไฟล์ [`db/game_progress.json`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/db/game_progress.json):

```json
{
  "currentScene": "LV01",
  "highestUnlockedScene": "LV01",
  "clearedStages": [],
  "defeatedBosses": [],
  "lastSaveTimestamp": "2026-10-08 19:05:58"
}
```

- **Auto-Save:** บันทึกด่านปัจจุบันอัตโนมัติเมื่อเข้าสู่ฉากการเล่น
- **Load Game:** ปุ่ม Load Game ในหน้า Lobby จะดึงด่านล่าสุดที่ผู้เล่นบันทึกไว้ขึ้นมาเล่นต่อทันที

---

## 📂 โครงสร้างโฟลเดอร์โปรเจกต์ (Project Structure)

```text
Libellus2D/
├── Assets/
│   ├── Animation/              # แอนิเมชันของตัวละคร, บอส และเอฟเฟกต์
│   ├── Font/                   # ฟอนต์ที่ใช้งานในเกม
│   ├── Prefab/                 # Prefabs ของผู้เล่น, บอส, กระสุน, ประตู, หีบสมบัติ
│   ├── Scenes/                 # ซีนของเกม (Lobby, LV01, LV02, Dev_World, Player_Dev)
│   ├── Script/                 # โค้ดภาษา C# ทั้งหมด
│   │   ├── Camera/             # ควบคุมกล้อง (CameraControllerCS)
│   │   ├── Enemy/              # ศัตรู, บอส และท่าโจมตีของบอส (RattikanBossCS)
│   │   ├── Environment/        # ประตูห้อง, หีบสมบัติ, หัวใจฮีล (RoomDoorControllerCS)
│   │   ├── HeartOffset/        # จัดการตำแหน่ง HUD หัวใจ
│   │   ├── Lobby/              # ระบบเมนูหลัก (Start, Load, Settings, Quit)
│   │   ├── Player/             # ระบบการเคลื่อนที่, การต่อสู้, เลือด (PlayerMovementCS)
│   │   ├── Progress/           # ตัวจัดการบันทึกเกม (GameProgressManagerCS)
│   │   ├── SetScene/           # จัดการเปลี่ยนฉาก
│   │   └── UI/                 # Pause Menu, Game Over Manager
│   ├── Settings/               # การตั้งค่า URP 2D Pipeline
│   └── TextMesh Pro/           # ทรัพยากรสำหรับ UI ข้อความคมชัด
├── AI_log/
│   └── ENVIRONMENT_AND_DOCKER_REPORT.md  # รายงานสรุปสภาพแวดล้อมและการวิเคราะห์ระบบ
├── db/
│   └── game_progress.json      # ไฟล์ฐานข้อมูลเซฟเกมของผู้เล่น
├── ProjectSettings/            # การตั้งค่าโปรเจกต์ Unity Engine
├── Libellus2D.sln              # Visual Studio / C# Solution
└── README.md                   # เอกสารแนะนำโปรเจกต์
```

---

## 🚀 การติดตั้งและเปิดโปรเจกต์ (Getting Started)

### ความต้องการของระบบ (Prerequisites)
- **Unity Hub** (เวอร์ชันล่าสุด)
- **Unity Editor 2022.3.62f3 (LTS)** พร้อมโมดูล Mac/Windows Build Support
- **.NET SDK** (รองรับ .NET 8 / 10 สำหรับ IntelliSense และ C# IDE Analysis)
- **Visual Studio Code** หรือ **Antigravity IDE** แนะนำส่วนขยาย:
  - *C# Dev Kit* / *C#*
  - *Unity Tools for Visual Studio Code*

### ขั้นตอนการเปิดโปรเจกต์
1. Clone หรือเปิดโฟลเดอร์โปรเจกต์:
   ```bash
   git clone https://github.com/Roochit/Libellus2D.git
   ```
2. เปิดโปรแกรม **Unity Hub** -> กดปุ่ม **Add** -> เลือกโฟลเดอร์ `Libellus2D`
3. ตรวจสอบให้แน่ใจว่าใช้ Unity Editor เวอร์ชัน **2022.3.62f3**
4. เปิดโปรเจกต์ และเริ่มต้นเล่นจากซีนหน้าเมนูหลัก:
   - ไปที่พาธ `Assets/Scenes/Lobby.unity`
   - กดปุ่ม **Play** ใน Unity Editor

---

## 🛠️ บันทึกทางเทคนิคและสภาพแวดล้อม (Developer Notes)

- **.NET SDK Configuration:**  
  โปรเจกต์นี้ได้รับการกำหนดค่าสภาพแวดล้อม C# ใน [.vscode/settings.json](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/.vscode/settings.json) ไว้อย่างถูกต้องเพื่อรองรับการตรวจจับ .NET SDK บน macOS
- **การประเมินการใช้งาน Docker:**  
  เนื่องจากโปรเจกต์นี้เป็น **Client-side Game** ที่ต้องใช้ GPU Metal Acceleration และ Unity Editor GUI ในการพัฒนาโดยตรง จึง**ไม่แนะนำให้ใช้ Docker** บนเครื่อง Local (อ่านรายงานฉบับเต็มได้ที่ [AI_log/ENVIRONMENT_AND_DOCKER_REPORT.md](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/AI_log/ENVIRONMENT_AND_DOCKER_REPORT.md))

---

**พัฒนาโดย:** Roochit & ทีมผู้พัฒนา Libellus2D  
**Repository:** [https://github.com/Roochit/Libellus2D](https://github.com/Roochit/Libellus2D)
