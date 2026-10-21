# คู่มือระบบควบคุมการเล่น Animation ของ Player (Player Animation System)

**วันที่บันทึก:** 21 ตุลาคม 2026  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์หลัก:** [`Assets/Script/Player/PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs)  
**โฟลเดอร์แอนิเมชัน:** `Assets/Animation/Player/`

---

## 1. ภาพรวมของระบบ (Overview)

ระบบนี้ทำหน้าที่ควบคุมการสลับคลิปแอนิเมชัน และการหันหน้า (Flip Horizontal) ของตัวละคร Player ตามทิศทางการเคลื่อนที่และสถานะการต่อสู้ โดยออกแบบให้ทำงานร่วมกับสคริปต์เดิมในโปรเจกต์อย่างกลมกลืน (Modular Architecture):

- **ตรวจจับการเดินและทิศทาง:** อ้างอิงทิศทางจาก [`PlayerMovementCS`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs) (หรือ Input WASD โดยตรงหากไม่มีสคริปต์)
- **ระบบกลับด้านอัตโนมัติ (FlipX):** ใช้คลิป `Player_side` และ `PlayerAttack_Melee_Side` ร่วมกันทั้งซ้ายและขวา โดยสคริปต์จะสลับ `SpriteRenderer.flipX` ให้อัตโนมัติเมื่อหันไปทางซ้ายหรือขวา
- **ระบบท่าหลบ/พุ่ง (Dash):** เมื่อกด `Spacebar` พุ่งหลบ สคริปต์จะตัดเข้าท่า `Player_Dash` ทันทีตามระยะเวลา Dash (`dashDuration = 0.15s`)
- **ระบบท่าฟันโจมตี 3 ทิศทาง (Melee Attack):** เมื่อคลิกซ้ายหรือกด `J` สคริปต์จะตรวจสอบทิศทางล่าสุดจาก [`PlayerCombatCS`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs) และเล่นท่าฟันตามทิศทาง (ฟันขึ้น, ฟันลง, หรือฟันข้าง) เป็นเวลาตามระยะเวลาฟัน (`meleeDuration`) ก่อนจะสลับกลับเป็นท่ายืน/เดินตามเดิมอย่างลื่นไหล
- **รองรับ 2 โหมดการควบคุม:** สามารถเลือกใช้งานระหว่าง **Direct Play** (สั่งเล่น State ผ่านโค้ดโดยตรง ใช้ง่ายที่สุด) หรือ **Animator Parameters** (ใช้ตัวแปร `state` และ Triggers คุมผ่าน Transitions ใน Animator)

---

## 2. ตารางแมปปิ้ง Sprite Sheet (`Player.PNG`) และ Animation Clips

ทุกคลิปใน `Assets/Animation/Player/` ได้รับการบรรจุ Sprites และตั้งค่าเรียบร้อยแล้ว:

| ชื่อ Animation Clip | ลำดับ Sprites ที่ใช้ | คำอธิบายท่าทาง | การวนลูป (Loop Time) |
| :--- | :--- | :--- | :---: |
| **`Player_idel`** | `Player_0` ถึง `Player_4` (5 เฟรม) | ยืนหันหน้าตรง ถือคอนโทรลเลอร์ ขยับตา/หายใจ | ✅ วนลูป (Loop = 1) |
| **`Player_Walk_down`** | `Player_23` ถึง `Player_27` (5 เฟรม) | เดินหันหน้าตรงลงข้างล่าง | ✅ วนลูป (Loop = 1) |
| **`Player_Walk_Up`** | `Player_28` ถึง `Player_32` (5 เฟรม) | เดินหันหลังขึ้นข้างบน | ✅ วนลูป (Loop = 1) |
| **`Player_side`** | `Player_33` ถึง `Player_36` (4 เฟรม) | เดินด้านข้าง (ใช้ FlipX สำหรับซ้าย-ขวา) | ✅ วนลูป (Loop = 1) |
| **`Player_Dash`** | `Player_21` ถึง `Player_22` (2 เฟรม) | ท่าย่อตัวพุ่งสไลด์หลบ (Dash) | ❌ ไม่วนลูป (Loop = 0) |
| **`PlayerAttack_Melee_Down`** | `Player_18` ถึง `Player_20` (3 เฟรม) | ฟันอาวุธตวัดลงข้างล่าง (คลื่นฟันสีขาว) | ❌ ไม่วนลูป (Loop = 0) |
| **`PlayerAttack_Melee_Up`** | `Player_15` ถึง `Player_17` (3 เฟรม) | ฟันอาวุธตวัดขึ้นข้างบน (คลื่นฟันสีขาว) | ❌ ไม่วนลูป (Loop = 0) |
| **`PlayerAttack_Melee_Side`** | `Player_5` ถึง `Player_9` (5 เฟรม) | ฟันอาวุธตวัดด้านข้าง (คลื่นฟันสีขาว) | ❌ ไม่วนลูป (Loop = 0) |

---

## 3. สคริปต์ที่มีการสร้างและปรับแต่ง

### 1) สคริปต์หลัก: [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs)
- ควบคุมทั้งท่าเดิน ยืน หลบ และฟันโจมตี
- ล็อกแอนิเมชันช่วงสั้นๆ ระหว่างฟัน (`PerformAttackAnimationRoutine`) เพื่อให้เล่นเฟรมฟันจนจบครบสมบูรณ์ แล้วค่อยคืนการควบคุมกลับไปสู่ท่ายืน/เดิน
- ตรวจสอบ Parameter ที่มีอยู่ใน Animator Controller แบบอัตโนมัติ (ไม่เกิด Error Parameter Not Found)

### 2) ปรับแต่ง [`PlayerMovementCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs)
- เพิ่ม Event `OnDashTriggered` และ Property `DashDuration`, `MoveInput`, `LastMoveDirection`

### 3) ปรับแต่ง [`PlayerCombatCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs)
- เพิ่ม Event `OnMeleeAttackTriggered`, `OnRangedAttackTriggered` และ Property `LastFacingDirection`, `MeleeDuration`

---

## 4. วิธีการเซ็ตใน Unity Editor (Step-by-Step)

### ขั้นตอนที่ 1: ติดตั้ง Component เข้ากับตัวละคร
1. ในหน้าต่าง **Hierarchy** คลิกเลือก GameObject **`Player`**
2. ในหน้าต่าง **Inspector**:
   - ตรวจสอบ Component **`Animator`**: ลากไฟล์ `Assets/Animation/Player/Player.controller` มาใส่ในช่อง **Controller**
   - ใส่ Component **`PlayerAnimationCS`** (หากยังไม่ได้ใส่)

---

### ขั้นตอนที่ 2: เลือกแนวทางการควบคุม

#### 🟢 แนวทางที่ 1: โหมด "Direct Play" (แนะนำสูงสุด — ง่ายและเสถียรที่สุด)
ไม่ต้องลากเส้น Transitions ใดๆ ในหน้าต่าง Animator สคริปต์จะสั่งเล่น State ตามชื่อคลิปโดยตรง:
1. ที่ Component **PlayerAnimationCS** ใน Inspector ปรับ **Control Mode:** `Direct Play`
2. **กด Play เล่นเกมได้ทันที!** 
   - เดิน WASD ➡️ เล่นท่าเดิน 4 ทิศทาง พร้อมหันซ้าย-ขวา
   - กด Spacebar ➡️ เล่นท่าพุ่งหลบ `Player_Dash`
   - คลิกซ้าย หรือกด J ➡️ เล่นท่าฟันตามทิศทางที่หันหน้าอยู่ทันที

---

#### 🔵 แนวทางที่ 2: โหมด "Animator Parameters" (ระบบ Transitions ของ Unity)
หากต้องการใช้ระบบเส้น Transitions ในหน้าต่าง Animator:

##### 1) ตารางค่าตัวแปร `state` (Int):
สคริปต์จะส่งค่าตัวเลข `state` เข้าสู่ Animator ดังนี้:

| ค่า `state` (Int) | State ที่สอดคล้อง | คำอธิบาย |
| :---: | :--- | :--- |
| **`0`** | **`Player_idel`** | ยืนนิ่ง |
| **`1`** | **`Player_Walk_down`** | เดินลง |
| **`2`** | **`Player_Walk_Up`** | เดินขึ้น |
| **`3`** | **`Player_side`** | เดินข้าง |
| **`4`** | **`Player_Dash`** | พุ่งหลบ |
| **`5`** | **`PlayerAttack_Melee_Down`** | ฟันลง |
| **`6`** | **`PlayerAttack_Melee_Up`** | ฟันขึ้น |
| **`7`** | **`PlayerAttack_Melee_Side`** | ฟันข้าง |

##### 2) การตั้งค่า Transitions สำหรับท่าฟันและท่าหลบ:
- **เส้นวิ่งเข้า (จาก Any State หรือ State อื่น ➡️ ท่าฟัน/หลบ):**
  - คลิกที่เส้น Transition:
    - **Conditions:** ตั้ง `state` Equals ค่าในตารางด้านบน (เช่น `state Equals 4` สำหรับ Dash, `state Equals 7` สำหรับ Attack Side)
    - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
    - **Transition Duration:** **`0`**
    - **Can Transition to Self:** ❌ **เอาออก (Uncheck)**
- **เส้นวิ่งออก (จาก ท่าฟัน/หลบ ➡️ Exit หรือ ➡️ Player_idel):**
  - คลิกที่เส้น Transition ขากลับ:
    - **Conditions:** ไม่ต้องใส่ (None)
    - **Has Exit Time:** ✅ **ติ๊กถูก (Check)**
    - **Exit Time:** **`1`** (รอให้ท่าฟัน/หลบเล่นครบ 100%)
    - **Transition Duration:** **`0`**

---

## 5. การตรวจสอบและแก้ไขปัญหา (Troubleshooting)

- **ฟันแล้วตัวละครไม่ยอมกลับมาเดิน:** ให้เช็คว่าเส้นขากลับจากท่า Attack ได้ติ๊ก `Has Exit Time = true` หรือยัง หรือเปลี่ยนไปใช้โหมด `Direct Play` ใน `PlayerAnimationCS` ซึ่งสคริปต์จะคุมเวลาให้เองอัตโนมัติ
- **ท่าฟันวนลูปไม่ยอมหยุด:** ให้คลิกที่ไฟล์ `.anim` ของท่าฟัน (`PlayerAttack_Melee_...`) ใน Project แล้วตรวจสอบใน Inspector ว่าได้ **เอาเครื่องหมายถูกออกจาก Loop Time** แล้ว

---

## 6. การปรับขนาด Hitbox และระบบเล็งตามตำแหน่งเมาส์ (Hitbox & Aim Settings)

### 1) การตั้งค่าให้ฟันตามตำแหน่งเมาส์ (Mouse Aiming):
ใน Inspector ของ GameObject `Player` ที่ Component **`PlayerCombatCS`**:
- หมวด **Aiming Settings**:
  - **Aim Mode:** มีให้เลือก 3 แบบ:
    1. **`Mouse Position` (ค่าเริ่มต้นใหม่):** ตัวละครจะหันหน้าและฟัน/ยิงไปยังทิศทางที่เคอร์เซอร์เมาส์ชี้อยู่ทันที
    2. **`Nearest Enemy`:** หันไปหาศัตรูที่ใกล้ที่สุดอัตโนมัติ
    3. **`Movement Direction`:** หันตามปุ่ม WASD ที่กำลังกด
- สคริปต์แอนิเมชัน [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs) จะสลับท่าฟัน (ขึ้น, ลง, ซ้าย-ขวา) และกลับด้านตัวละคร (FlipX) ให้สอดคล้องกับตำแหน่งเมาส์โดยอัตโนมัติ

### 2) วิธีปรับขนาด Hitbox การฟัน (มี 2 วิธี):
- **วิธีที่ 1 (ปรับผ่าน Inspector ของ Player สะดวกที่สุด):**
  - ใน Inspector ของ **`PlayerCombatCS`** ให้ปรับค่า:
    - **`Melee Hitbox Scale Multiplier`:** ค่าเริ่มต้นคือ `(1, 1)` หากต้องการให้ Hitbox กว้างขึ้นหรือใหญ่ขึ้น ให้ปรับเป็น `(1.5, 1.5)` หรือ `(2, 2)` ตามต้องการ
    - **`Attack Offset Distance`:** ปรับระยะห่างที่ Hitbox จะปรากฏออกมาจากตัวผู้เล่น (เช่น `1.2` หรือ `1.5`)
    - *เมื่อคลิกเลือก Player ใน Scene View จะมี Gizmos วงกลมสีแดงแสดงขอบเขตระยะการฟันให้เห็นทันที*
- **วิธีที่ 2 (ปรับที่ไฟล์ Prefab ต้นฉบับ):**
  - ไปที่โฟลเดอร์ `Assets/Prefab/HitBox/MeleeHitbox.prefab`
  - ดับเบิลคลิกเปิด Prefab:
    - ปรับขนาดใน Component **`BoxCollider2D` ➡️ `Size (X, Y)`**
    - หรือปรับใน **`Transform` ➡️ `Scale (X, Y)`** ได้โดยตรง
