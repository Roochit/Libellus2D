# รายงานการแก้ปัญหา .NET SDK และการประเมินการใช้งาน Docker ในโปรเจกต์ Libellus2D

**วันที่ประเมิน:** 8 ตุลาคม 2026  
**โปรเจกต์:** Libellus2D (Unity 2D Game)  
**สภาพแวดล้อม:** macOS (Apple Silicon M4) / Antigravity IDE (VS Code-based)  

---

## 1. สรุปผลการดำเนินงาน (Executive Summary)

1. **การแก้ปัญหา .NET SDK (`dotnet: command not found`):**  
   ✅ **แก้ไขสำเร็จสมบูรณ์ (100%)** — ตรวจพบว่ามีการติดตั้ง .NET 10 SDK อยู่ที่ `/usr/local/share/dotnet` เรียบร้อยแล้ว แต่เนื่องจากระบบไม่มี Symlink มาตรฐาน และ PATH ของ IDE/Subshell (`/bin/sh`) ไม่ได้รับค่า ทำให้ C# Extension และ OmniSharp หา `dotnet` ไม่เจอ  
   - ได้ดำเนินการผูก Symlink, อัปเดต Shell Config (`~/.zprofile`, `~/.zshrc`) และระบุ Path ใน `.vscode/settings.json`  
   - ทดสอบสั่ง `dotnet build Libellus2D.sln` ผ่านเรียบร้อย (**0 Error**, 26 Warnings ปกติตามสไตล์ Unity serialized fields)

2. **การประเมินการติดตั้ง Docker เป็น Sandbox/ระบบนิเวศในโปรเจกต์นี้:**  
   🛑 **ข้อสรุป: "ไม่ควรทำ" (ปล่อยไว้ตามสภาพแวดล้อม Native เดิม)**  
   - โปรเจกต์ **Libellus2D** เป็นเกม **Unity 2D แบบ Client-side Single Player** การพัฒนาจำเป็นต้องพึ่งพา **Unity Editor GUI, GPU Metal Acceleration, Real-time Play Mode และ IDE Debugger**  
   - โฟลเดอร์ `db/game_progress.json` เป็นเพียงไฟล์ Local JSON สำหรับเซฟเกม ไม่ใช่ Database Server  
   - การนำ Docker มาครอบโปรเจกต์ Unity บน macOS จะสร้างภาระให้เครื่อง (Overhead), ตัดขาดการทำงานกับ Unity Editor และทำให้การดีบัก C# ซับซ้อนขึ้นโดยไม่ได้ประโยชน์ใดๆ  

---

## 2. รายละเอียดการแก้ปัญหา .NET SDK

### สาเหตุของปัญหา
- Extension `C#` / `C# Dev Kit` ใน VS Code / Antigravity IDE รันคำสั่งตรวจสอบสภาพแวดล้อมผ่าน `/bin/sh -c "dotnet --info"`
- บน macOS ตัวติดตั้งอย่างเป็นทางการของ Microsoft จะติดตั้ง SDK ไว้ที่พาธ `/usr/local/share/dotnet`
- ตัวติดตั้งไม่ได้สร้าง Symlink ไปยัง `/usr/local/bin/dotnet` (เนื่องจากข้อจำกัดด้าน Root Permission) และเชลล์ `/bin/sh` แบบ Non-login ไม่ได้อ่าน `/etc/paths.d/dotnet` ส่งผลให้เกิดข้อผิดพลาด:
  ```text
  The .NET SDK cannot be located: Error running dotnet --info: Command failed: dotnet --info /bin/sh: dotnet: command not found.
  There were problems loading project Assembly-CSharp.csproj.
  ```

### วิธีการแก้ไข 3 ชั้น (Triple-layer Resolution)
1. **สร้าง Symlink เชื่อมโยงเข้า User Bin Directories ที่อยู่ใน PATH ของ IDE:**
   - เชื่อมต่อไปยัง `/Users/mac_air_m4/.nodejs/bin/dotnet`
   - เชื่อมต่อไปยัง `/Users/mac_air_m4/.docker/bin/dotnet`
   - เชื่อมต่อไปยัง `/Users/mac_air_m4/.unity/bin/dotnet`
2. **อัปเดต Environment Variables ถาวร:**
   - เพิ่ม `export DOTNET_ROOT="/usr/local/share/dotnet"` และ `export PATH="/usr/local/share/dotnet:$PATH"` ใน `~/.zprofile` และ `~/.zshrc`
3. **กำหนดค่าใน [.vscode/settings.json](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/.vscode/settings.json):**
   ```json
   "dotnet.defaultSolution": "Libellus2D.sln",
   "dotnet.dotnetPath": "/usr/local/share/dotnet/dotnet",
   "omnisharp.dotnetPath": "/usr/local/share/dotnet/dotnet",
   "csharp.dotnetPath": "/usr/local/share/dotnet/dotnet"
   ```

### ผลการทดสอบ
- คำสั่ง `which dotnet` คืนค่า `/Users/mac_air_m4/.nodejs/bin/dotnet`
- คำสั่ง `dotnet --info` แสดงผล .NET SDK Version `10.0.401` สถาปัตยกรรม `osx-arm64`
- ทดสอบคอมไพล์โปรเจกต์:
  ```bash
  dotnet build Libellus2D.sln
  ```
  **ผลลัพธ์:** `Build succeeded. 0 Error(s)` ไฟล์ `Assembly-CSharp.dll` ถูกสร้างสมบูรณ์

---

## 3. ทำไมจึง "ไม่ควรใช้ Docker" กับโปรเจกต์ Libellus2D?

### 3.1 ธรรมชาติของโครงสร้างโปรเจกต์ (Game Architecture)
- โปรเจกต์นี้คือ **เกม Unity 2D ฝั่ง Client (Desktop/Mobile)**
- ในไดเรกทอรี `db/game_progress.json` มีเนื้อหาดังนี้:
  ```json
  {
    "currentScene": "LV01",
    "highestUnlockedScene": "LV01",
    "clearedStages": [],
    "defeatedBosses": [],
    "lastSaveTimestamp": "2026-10-08 19:05:58"
  }
  ```
  นี่คือ **ไฟล์ JSON ธรรมดา** ที่อ่านเขียนผ่าน `System.IO.File` / `JsonUtility` ในโค้ด C# เพื่อเก็บ Save State ของผู้เล่น ไม่ใช่ระบบฐานข้อมูล Network Database (เช่น MySQL, PostgreSQL หรือ MongoDB) จึงไม่จำเป็นต้องมี Database Container

### 3.2 ข้อจำกัดทางเทคนิคของ Docker กับ Unity Editor บน macOS (Apple Silicon M4)

| หัวข้อ | การรันแบบ Native บน macOS (ปัจจุบัน) | การรันใน Docker Container |
| :--- | :--- | :--- |
| **Unity Editor GUI** | ลื่นไหลสมบูรณ์, ใช้งาน Scene View, Game View, Inspector ได้ 100% | ทำงานไม่ได้ เนื่องจาก Docker บน Mac เป็น Linux VM การส่งภาพ GUI (X11/Wayland) ข้าม VM ช้าและไม่เสถียร |
| **GPU Acceleration** | ใช้ชิป M4 Metal API เต็มประสิทธิภาพ เรนเดอร์ 60-120 FPS | Docker Linux VM ไม่รองรับ Apple Metal Passthrough ทำให้ไม่สามารถเรนเดอร์กราฟิกหรือรัน Play Mode ได้ |
| **การดีบัก C# (Debugging)** | IDE เชื่อมต่อ (Attach) เข้ากับ Process ของ Unity Editor ได้ทันที มี IntelliSense เต็มรูปแบบ | การ Attach ข้าม Docker Container ต้อง Forward พอร์ต VSTU ซับซ้อนและหลุดบ่อย |
| **การกินทรัพยากร (Resource Usage)** | ใช้ RAM/แบตเตอรี่ตามจริง Unity ปิดแล้วคืนแรมทันที | Docker Desktop ต้องรัน VM ตลอดเวลา จอง RAM 2-4 GB และกินพลังงานแบตเตอรี่ของ MacBook Air โดยเปล่าประโยชน์ |

---

## 4. กรณีใดบ้างที่ควรนำ Docker มาใช้กับ Unity ในอนาคต?

Docker จะมีประโยชน์กับงาน Unity ใน **2 กรณีเท่านั้น**:

1. **ระบบ Automated CI/CD (GameCI):**
   - รันบน GitHub Actions หรือคลาวด์เพื่อ Build เกมอัตโนมัติ (เช่น สั่งให้ Build ตัวเกมเป็น WebGL หรือ Android APK อัตโนมัติเมื่อ Push โค้ด) ซึ่งเป็นการรัน Unity แบบไม่มีหน้าต่าง (Headless/Batchmode)
2. **ระบบ Backend / Multiplayer Server:**
   - หากในอนาคตพัฒนาเกมนี้ให้มีระบบออนไลน์ เช่น มี REST API สำหรับ Leaderboard, มี WebSocket Server สำหรับ Multiplayer หรือมี Database จริงบนคลาวด์ ในส่วนที่เป็น Server แยกต่างหากนั้น ควรใช้ Docker ในการรันและทำ Sandbox

---

## 5. สรุปคำแนะนำและขั้นตอนถัดไป

1. **ไม่ต้องติดตั้ง Docker ในโปรเจกต์นี้** เพื่อรักษาประสิทธิภาพการทำงานสูงสุดของ MacBook Air M4 และความคล่องตัวในการพัฒนาเกม
2. ปัญหา `.NET SDK` ได้รับการแก้ไขแล้ว สามารถเปิด Antigravity IDE / VS Code ทำงานต่อได้ทันที
3. หากหน้าต่าง IDE ยังแสดงข้อความเตือนเก่าอยู่ ให้กด **Cmd + Shift + P** แล้วเลือก **"Developer: Reload Window"** เพื่อให้ Extension โหลดสภาพแวดล้อมใหม่ทั้งหมด
