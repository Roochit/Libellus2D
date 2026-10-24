# คู่มือระบบควบคุมการเล่น Animation ของ Player (Player Animation System)

**วันที่บันทึก:** 24 ตุลาคม 2026 (อัปเดตระบบ Animator Parameters 100%)  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์หลัก:** [`Assets/Script/Player/PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs)  
**โฟลเดอร์แอนิเมชัน:** `Assets/Animation/Player/`  

---

## 1. ภาพรวมของระบบ (Overview)

ระบบควบคุมการเล่น Animation ของ Player ทำงานผ่าน **Animator Parameters และ Transitions ใน Unity 100%**:

- **ตรวจจับการเดินและทิศทาง:** อ้างอิงทิศทางจาก [`PlayerMovementCS`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs) (หรือ Input WASD)
- **ระบบกลับด้านอัตโนมัติ (FlipX):** สลับ `SpriteRenderer.flipX` ให้อัตโนมัติเมื่อเดินหรือฟันไปทางซ้าย
- **ระบบท่าหลบ/พุ่ง (Dash):** เมื่อกด `Spacebar` จะส่งพารามิเตอร์ `isDashing = true` และยิง Trigger `Dash` เข้า Animator
- **ระบบท่าฟันโจมตี 3 ทิศทาง (Melee Attack):** ตรวจสอบทิศทางการเล็ง (ตามเมาส์/ปุ่มกด) จาก [`PlayerCombatCS`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs) และยิง Trigger `AttackUp`, `AttackDown`, หรือ `AttackSide` เข้า Animator

---

## 2. ตารางแมปปิ้ง Sprite Sheet (`Player.PNG`) และ Animation Clips

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

## 3. สคริปต์ควบคุม: [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs)

สคริปต์จะคอยส่งค่า Parameters เข้าสู่ Animator Controller อัตโนมัติ:

### 1) พารามิเตอร์การเดิน/การยืน:
- **`isMoving` (Bool):** `true` เมื่อกำลังกดเดิน, `false` เมื่อยืนนิ่ง
- **`moveX` (Float), `moveY` (Float):** ทิศทางของอินพุตการเดิน
- **`state` (Integer):** รหัสสถานะสำหรับผู้ที่ต้องการควบคุมแบบ BlendState / State Integer:
  - `0`: Idle
  - `1`: Walk Down
  - `2`: Walk Up
  - `3`: Walk Side
  - `4`: Dash
  - `5`: Attack Down
  - `6`: Attack Up
  - `7`: Attack Side

### 2) พารามิเตอร์ Trigger แอ็กชัน:
- **`Dash` (Trigger):** ทำงานเมื่อกด Spacebar พุ่งหลบ
- **`Attack` (Trigger):** ทริกเกอร์โจมตีทั่วไป
- **`AttackSide` (Trigger):** ฟันดาบแนวนอน
- **`AttackDown` (Trigger):** ฟันดาบลงล่าง
- **`AttackUp` (Trigger):** ฟันดาบขึ้นบน

---

## 4. วิธีการเซ็ตใน Unity Editor (Step-by-Step)

### ขั้นตอนที่ 1: ติดตั้ง Component เข้ากับตัวละคร
1. ในหน้าต่าง **Hierarchy** คลิกเลือก GameObject **`Player`**
2. ในหน้าต่าง **Inspector**:
   - ตรวจสอบ Component **`Animator`**: ลากไฟล์ `Assets/Animation/Player/Player.controller` มาใส่ในช่อง **Controller**
   - ใส่ Component **`PlayerAnimationCS`**

---

### ขั้นตอนที่ 2: ตั้งค่า Transitions ใน Animator

#### 1) การตั้งค่าเส้น Transition สำหรับท่าฟันและท่าหลบ:
- **เส้นวิ่งเข้า (จาก Any State ➡️ ท่าฟัน/หลบ):**
  - คลิกที่เส้นลูกศร Transition:
    - **Conditions:** เลือก Trigger ที่ตรงกับท่านั้นๆ เช่น `AttackUp`, `AttackDown`, `AttackSide`, `Dash` (หรือเลือก `state Equals ค่ารหัส`)
    - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
    - **Transition Duration:** **`0`**
    - **Can Transition to Self:** ✅ **ติ๊กถูก (Check = 1)** เพื่อให้กดฟันรัวๆ ต่อเนื่องกันแล้วแอนิเมชันเริ่มเล่นใหม่ได้ทันทีโดยไม่ตกหล่น (Trigger จะถูก Reset ทันทีที่เข้าสู่ State)

- **เส้นวิ่งกลับ (จาก ท่าฟัน/หลบ ➡️ Player_idel หรือ Exit):**
  - คลิกที่เส้น Transition ขากลับ:
    - **Has Exit Time:** ✅ **ติ๊กถูก (Check)**
    - **Exit Time:** **`1`** (รอให้เล่นคลิปฟัน/หลบจบ 1 รอบ)
    - **Transition Duration:** **`0`**
    - **Conditions:** **ไม่ต้องใส่ (None)**

---

## 5. การปรับขนาด Hitbox และระบบเล็งตามตำแหน่งเมาส์ (Hitbox & Aim Settings)

### 1) การตั้งค่าให้ฟันตามตำแหน่งเมาส์ (Mouse Aiming):
ใน Inspector ของ GameObject `Player` ที่ Component **`PlayerCombatCS`**:
- หมวด **Aiming Settings**:
  - **Aim Mode:** มีให้เลือก 3 แบบ:
    1. **`Mouse Position` (ค่าเริ่มต้น):** ตัวละครจะหันหน้าและฟัน/ยิงไปยังทิศทางที่เคอร์เซอร์เมาส์ชี้อยู่ทันที
    2. **`Nearest Enemy`:** หันไปหาศัตรูที่ใกล้ที่สุดอัตโนมัติ
    3. **`Movement Direction`:** หันตามปุ่ม WASD ที่กำลังกด
- สคริปต์แอนิเมชัน [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs) จะเลือกยิง Trigger ฟันขึ้น, ฟันลง หรือฟันข้าง และกลับด้านตัวละคร (FlipX) ให้สอดคล้องกับตำแหน่งเมาส์โดยอัตโนมัติ

### 2) วิธีปรับขนาด Hitbox การฟัน:
- ใน Inspector ของ **`PlayerCombatCS`** ให้ปรับค่า:
  - **`Melee Hitbox Scale Multiplier`:** ปรับขยายขนาด Hitbox เช่น `(1.5, 1.5)` หรือ `(2, 2)`
  - **`Attack Offset Distance`:** ปรับระยะยื่นของ Hitbox ไปข้างหน้า
