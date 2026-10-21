# คู่มือระบบการฟันโจมตี, เล็งตามเมาส์ และการปรับขนาด Hitbox (Player Combat & Hitbox System)

**วันที่บันทึก:** 21 ตุลาคม 2026  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์หลัก:**  
- [`Assets/Script/Player/PlayerCombatCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs)  
- [`Assets/Script/Player/PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs)  
- [`Assets/Script/Player/PlayerMovementCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs)  
**ไฟล์ Prefab Hitbox:**  
- [`Assets/Prefab/HitBox/MeleeHitbox.prefab`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Prefab/HitBox/MeleeHitbox.prefab)  

---

## 1. ภาพรวมระบบการฟันตามตำแหน่งเมาส์ (Mouse Aiming System)

เดิมทีก่อนหน้านี้ระบบการโจมตีจะล็อกเป้าไปหาศัตรูที่ใกล้ที่สุดอัตโนมัติ (`FaceNearestEnemy`) หรืออิงตามปุ่มทิศทาง WASD ทำให้ผู้เล่นไม่สามารถควบคุมทิศทางการฟันได้อย่างอิสระ

ในเวอร์ชันนี้ได้อัปเกรดให้ **เล็งตามเคอร์เซอร์เมาส์เป็นค่าเริ่มต้น (Default)**:
- เมื่อคลิกซ้าย (ฟันประชิด) หรือคลิกขวา (ยิงไกล) ระบบจะแปลงพิกัดเมาส์บนหน้าจอเป็นตำแหน่งพิกัดในเกม (`Camera.main.ScreenToWorldPoint`)
- คำนวณเวกเตอร์ทิศทางจากตัวผู้เล่นไปยังเมาส์ `(mouseWorldPos - playerPos).normalized`
- ตัวละครจะหันหน้าและปล่อย Hitbox การฟันหรือกระสุนปืนตรงไปยังจุดที่เมาส์ชี้อยู่ 100%

### โหมดการเล็งที่มีให้เลือก (Aim Mode):
สามารถสลับโหมดได้ในหน้าต่าง Inspector ของ Component **`PlayerCombatCS`**:
1. **`Mouse Position` (ค่าเริ่มต้น — แนะนำ):** เล็งตามตำแหน่งเคอร์เซอร์เมาส์
2. **`Nearest Enemy`:** ล็อกเป้าหาศัตรูที่ใกล้ที่สุดอัตโนมัติ
3. **`Movement Direction`:** หันตามปุ่ม WASD ที่กำลังกดเดิน

---

## 2. การทำงานร่วมกับระบบแอนิเมชัน 3 ทิศทาง

สคริปต์ [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs) จะตรวจจับมุมองศาของเมาส์ แล้วเลือกเล่นท่าทางให้สอดคล้องกันโดยอัตโนมัติ:

| ตำแหน่งของเมาส์เทียบกับตัวละคร | คลิป Animation ที่เล่น | การกลับด้านตัวละคร (`flipX`) |
| :--- | :--- | :---: |
| **อยู่ด้านบน** (มุมกว้างช่วงบน) | **`PlayerAttack_Melee_Up`** | ตามเดิม |
| **อยู่ด้านล่าง** (มุมกว้างช่วงล่าง) | **`PlayerAttack_Melee_Down`** | ตามเดิม |
| **อยู่ด้านขวา** | **`PlayerAttack_Melee_Side`** | ❌ `flipX = false` (หันขวา) |
| **อยู่ด้านซ้าย** | **`PlayerAttack_Melee_Side`** | ✅ `flipX = true` (หันซ้าย) |

> [!NOTE]
> แอนิเมชันการฟันจะถูกล็อกไว้สั้นๆ ตามระยะเวลาการฟัน (`meleeDuration = 0.15s - 0.2s`) เพื่อให้เล่นเฟรมคลื่นดาบครบสมบูรณ์ก่อนจะคืนการควบคุมกลับไปสู่ท่ายืนหรือท่าเดินตามปกติ

---

## 3. วิธีการปรับขนาดและระยะ Hitbox การฟัน (Hitbox Adjustments)

ผู้พัฒนาสามารถปรับแต่งขนาดขอบเขตการฟันได้ **2 วิธี**:

### 🟢 วิธีที่ 1: ปรับผ่าน Inspector ของ Player (สะดวกและรวดเร็วที่สุด — แนะนำ)
ไม่ต้องเข้าไปแก้ไขไฟล์ Prefab สามารถจูนขนาดได้จากตัวละครโดยตรง:
1. คลิกเลือก GameObject **`Player`** ในหน้าต่าง Hierarchy
2. ในหน้าต่าง **Inspector** ที่ Component **`PlayerCombatCS`**:
   - **`Melee Hitbox Scale Multiplier`:** ตัวคูณขยายขนาด Hitbox
     - `(X: 1, Y: 1)` = ขนาดดั้งเดิมตาม Prefab
     - `(X: 1.5, Y: 1.5)` = ขยายขอบเขตการฟันใหญ่ขึ้น 50%
     - `(X: 2, Y: 2)` = ขยายขอบเขตการฟันใหญ่ขึ้น 2 เท่า (ฟันกว้างโดนง่าย)
   - **`Attack Offset Distance`:** ระยะห่างที่กล่อง Hitbox จะยื่นออกจากตัวละคร
     - ค่ามาตรฐานคือ `1.2` หากรู้สึกว่าฟันใกล้ตัวเกินไป สามารถขยับเป็น `1.4` หรือ `1.6`
3. **การดูระยะฟันใน Scene View (Gizmos Visualizer):**
   - เมื่อคลิกเลือกตัวละคร Player ในหน้าต่าง **Scene View** จะมี **เส้นสีแดงและวงกลมสีแดง (Gizmos)** แสดงจุดและรัศมีที่ Hitbox จะปรากฏขึ้นให้เห็นแบบเรียลไทม์

---

### 🔵 วิธีที่ 2: ปรับแต่งที่ไฟล์ต้นฉบับ Prefab ถาวร
หากต้องการแก้รูปทรงและ Collider ดั้งเดิม:
1. ในหน้าต่าง Project ไปที่: `Assets/Prefab/HitBox/MeleeHitbox.prefab`
2. ดับเบิลคลิกเปิดไฟล์ Prefab:
   - **ปรับขนาดกรอบชน (Collider):** ไปที่ Component **`BoxCollider2D`** แล้วปรับค่า **`Size` (X, Y)**
   - **ปรับขนาดการแสดงผลภาพสีแดง:** ไปที่ Component **`Transform`** แล้วปรับค่า **`Scale` (X, Y)**

---

## 4. ระบบท่าพุ่งหลบ (Dash / Dodge System)

- **ปุ่มกด:** กดปุ่ม **`Spacebar`**
- **คลิปแอนิเมชัน:** **`Player_Dash`** (ใช้ Sprite เฟรม `Player_21` และ `Player_22`)
- **การทำงาน:**
  - ตัวละครจะพุ่งไปตามทิศทางล่าสุดที่กำลังเคลื่อนที่ เป็นระยะทาง `dashDistance = 3`
  - มีระยะเวลาพุ่งสั้น `dashDuration = 0.15s` และคูลดาวน์ `dashCooldown = 0.5s`
  - มีระบบตรวจจับกำแพงล่วงหน้า (`RaycastHit2D`) เพื่อป้องกันไม่ให้ตัวละครสไลด์จมหรือหลุดออกนอกกำแพง
- **การหันหน้าตอนพุ่ง:** สคริปต์จะหันหน้าตัวละคร (`flipX`) ไปตามทิศทางที่พุ่งให้อัตโนมัติ

---

## 5. สรุปไฟล์ที่เกี่ยวข้องในโปรเจกต์

| ชื่อไฟล์ | ชนิดไฟล์ | หน้าที่สำคัญ |
| :--- | :--- | :--- |
| [`PlayerCombatCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs) | C# Script | ควบคุมการโจมตี, การเล็งตามเมาส์, การสร้าง Hitbox และขนาดตัวคูณ |
| [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs) | C# Script | คุม Animator, เล่นคลิปฟันตามทิศเมาส์, ท่า Dash, และ FlipX ซ้าย-ขวา |
| [`PlayerMovementCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs) | C# Script | ควบคุมการเดินและ Dash Coroutine |
| [`MeleeHitbox.prefab`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Prefab/HitBox/MeleeHitbox.prefab) | Unity Prefab | กล่อง Hitbox โจมตีประชิด (Tag: `Player_Attack`) |
| `Assets/Animation/Player/` | Animation Clips | รวบรวมคลิปท่าทางทั้งหมด: `Player_Dash`, `PlayerAttack_Melee_Up`, `PlayerAttack_Melee_Down`, `PlayerAttack_Melee_Side` |
