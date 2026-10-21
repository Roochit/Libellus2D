# คู่มือระบบควบคุมการเล่น Animation ของ Player (Player Animation System)

**วันที่บันทึก:** 21 ตุลาคม 2026  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์หลัก:** `Assets/Script/Player/PlayerAnimationCS.cs`  

---

## 1. ภาพรวมของระบบ (Overview)

ระบบนี้ทำหน้าที่ควบคุมการสลับคลิปแอนิเมชัน และการหันหน้า (Flip Horizontal) ของตัวละคร Player ตามทิศทางการเคลื่อนที่และสถานะการต่อสู้ โดยออกแบบให้ทำงานร่วมกับสคริปต์เดิมในโปรเจกต์อย่างกลมกลืน (Modular Architecture):

- **ตรวจจับการเดินและทิศทาง:** อ้างอิงทิศทางจาก [`PlayerMovementCS`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs) (หรือ Input WASD โดยตรงหากไม่มีสคริปต์)
- **ระบบกลับด้านอัตโนมัติ (FlipX):** ใช้คลิป `Player_side` ร่วมกันทั้งการเดินซ้ายและขวา โดยสคริปต์จะสลับ `SpriteRenderer.flipX` ให้อัตโนมัติเมื่อกด A (เดินซ้าย) และ D (เดินขวา)
- **รองรับ 2 โหมดการควบคุม:** สามารถเลือกใช้งานระหว่าง **Direct Play** (สั่งเล่น State ผ่านโค้ดโดยตรง ใช้ง่ายและไม่มีปัญหา Transition) หรือ **Animator Parameters** (ใช้ตัวแปร `state` คุมผ่าน Transitions ใน Animator)
- **รองรับระบบโจมตี:** ผูก Event ร่วมกับ [`PlayerCombatCS`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs) เพื่อรองรับท่าฟัน/ยิงเมื่อมีคลิปแอนิเมชันในอนาคต

---

## 2. ตารางแมปปิ้ง Sprite Sheet (`Player.PNG`) และ Animation Clips

จากการตรวจสอบไฟล์ `Assets/img/Player.PNG` และการตั้งค่า Slice ใน `Player.PNG.meta`:

| ชื่อ Animation Clip | ลำดับ Sprites ที่ใช้ | คำอธิบายท่าทาง | สถานะในโปรเจกต์ |
| :--- | :--- | :--- | :--- |
| **`Player_idel`** | `Player_0` ถึง `Player_4` (5 เฟรม) | ยืนหันหน้าตรง ถือคอนโทรลเลอร์ ขยับตา/หายใจ | ✅ สมบูรณ์ (วนลูป) |
| **`Player_Walk_down`** | `Player_23` ถึง `Player_27` (5 เฟรม) | เดินหันหน้าตรงลงข้างล่าง | ✅ สมบูรณ์ (วนลูป) |
| **`Player_Walk_Up`** | `Player_28` ถึง `Player_32` (5 เฟรม) | เดินหันหลังขึ้นข้างบน | ✅ แก้ไขให้ตรงเฟรมแล้ว (วนลูป) |
| **`Player_side`** | `Player_33` ถึง `Player_36` (4 เฟรม) | เดินด้านข้าง (ใช้ FlipX สำหรับซ้าย-ขวา) | ✅ บรรจุเฟรม + เปิด LoopTime แล้ว |
| *Attack Melee (เสริม)* | `Player_5` ถึง `Player_14` (แถว 2-3) | ฟัน/เหวี่ยงอาวุธด้านหน้าและเฉียง | 💡 พร้อมสำหรับตัดคลิปเพิ่ม |
| *Attack Up (เสริม)* | `Player_15` ถึง `Player_17` (แถว 4) | ฟัน/เหวี่ยงอาวุธขึ้นข้างบน | 💡 พร้อมสำหรับตัดคลิปเพิ่ม |
| *Attack Down (เสริม)* | `Player_18` ถึง `Player_20` (แถว 5) | ฟัน/เหวี่ยงอาวุธลงข้างล่าง | 💡 พร้อมสำหรับตัดคลิปเพิ่ม |

---

## 3. สคริปต์ที่มีการสร้างและปรับแต่ง

### 1) สคริปต์ใหม่: [`PlayerAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerAnimationCS.cs)
- แนบไว้ที่ GameObject `Player`
- ดึง Reference ของ `Animator`, `SpriteRenderer`, `PlayerMovementCS`, `PlayerCombatCS` อัตโนมัติ
- คำนวณแกนที่มีน้ำหนักมากกว่าระหว่าง X และ Y เพื่อเลือกท่าเดินแนวนอนหรือแนวตั้งอย่างแม่นยำ
- ป้องกันการ Reset เฟรมโดยเช็ค `currentPlayingState == stateName` ก่อนสั่งเล่น

### 2) ปรับแต่ง [`PlayerMovementCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerMovementCS.cs)
- เพิ่ม Property สาธารณะให้อ่านค่าได้จากภายนอก:
  ```csharp
  public Vector3 MoveInput => moveInput;
  public Vector3 LastMoveDirection => lastMoveDirection;
  ```

### 3) ปรับแต่ง [`PlayerCombatCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Player/PlayerCombatCS.cs)
- เพิ่ม Event แจ้งเตือนเมื่อเกิดการโจมตี:
  ```csharp
  public System.Action OnMeleeAttackTriggered;
  public System.Action OnRangedAttackTriggered;
  public Vector3 LastFacingDirection => lastFacingDirection;
  ```

---

## 4. วิธีการเซ็ตใน Unity Editor (Step-by-Step)

### ขั้นตอนที่ 1: ติดตั้ง Component เข้ากับตัวละคร
1. ในหน้าต่าง **Hierarchy** คลิกเลือก GameObject **`Player`**
2. ในหน้าต่าง **Inspector**:
   - ตรวจสอบว่ามี Component **`Animator`** แล้วลาก `Assets/img/Player.controller` มาใส่ในช่อง **Controller**
   - กดปุ่ม **Add Component** แล้วพิมพ์ค้นหา **`PlayerAnimationCS`** เพื่อเพิ่มสคริปต์ลงไป

---

### ขั้นตอนที่ 2: เลือกแนวทางการควบคุม (เลือก 1 แบบ)

#### 🟢 แนวทางที่ 1: โหมด "Direct Play" (แนะนำสูงสุดสำหรับ 2D)
โหมดนี้สั่งเล่นคลิปผ่านโค้ดโดยตรง ไม่ต้องกังวลเรื่องการลากเส้น Transition หรือปัญหาเฟรมหน่วง:
1. ใน Inspector ของ **PlayerAnimationCS** ปรับ:
   - **Control Mode:** `Direct Play`
2. ในหน้าต่าง **Animator**:
   - ไม่ต้องสร้าง Parameter ใดๆ และสามารถลบเส้น Transitions ระหว่าง State ออกได้เลย หรือปล่อยไว้ก็ได้
3. **ทดสอบรันเกมได้ทันที!** ตัวละครจะสลับท่าตามการกดปุ่ม WASD ทันที

---

#### 🔵 แนวทางที่ 2: โหมด "Animator Parameters" (ระบบ Transitions ดั้งเดิมของ Unity)
หากต้องการใช้ระบบ Transitions ที่ลากเส้นไว้ใน Animator Controller:

1. **เพิ่ม Parameter ในหน้าต่าง Animator:**
   - ที่แท็บ **Parameters** (ซ้ายมือ) กดปุ่ม **`+`** ➡️ เลือก **`Int`**
   - ตั้งชื่อ Parameter ว่า: **`state`**

2. **ตั้งค่า Transition แต่ละเส้น:**
   คลิกที่ลูกศร Transition แต่ละเส้น แล้วกำหนดค่าใน Inspector ดังนี้:

   | เส้นทาง Transition | Conditions | Has Exit Time | Transition Duration |
   | :--- | :--- | :---: | :---: |
   | เส้นชี้เข้า **`Player_idel`** | `state` **Equals** `0` | ❌ **เอาออก** | **`0`** |
   | เส้นชี้เข้า **`Player_Walk_down`** | `state` **Equals** `1` | ❌ **เอาออก** | **`0`** |
   | เส้นชี้เข้า **`Player_Walk_Up`** | `state` **Equals** `2` | ❌ **เอาออก** | **`0`** |
   | เส้นชี้เข้า **`Player_side`** | `state` **Equals** `3` | ❌ **เอาออก** | **`0`** |

> [!CAUTION]
> **3 กฎเหล็กสำคัญของ Animation 2D Pixel Art ใน Unity:**
> 1. **ต้องเอาเครื่องหมายถูกออกจาก `Has Exit Time` ทุกเส้น:** หากติ๊กไว้ ตัวละครจะรอให้ท่าปัจจุบันเล่นจบก่อน จึงจะเปลี่ยนท่า ทำให้การบังคับหน่วงและกระตุก
> 2. **ต้องปรับ `Transition Duration` เป็น `0` ทุกเส้น:** ค่าเริ่มต้นของ Unity คือ `0.25` ซึ่งเป็นระบบเบลอเปลี่ยนท่าของ 3D โมเดล สำหรับ 2D Sprite จะทำให้ภาพซ้อนหรือวูบวาบ
> 3. **หากลากลูกศรออกจาก `Any State`:** ต้องปลดเครื่องหมายถูกที่ **`Can Transition to Self`** ออกเสมอ เพื่อป้องกันไม่ให้อนิเมชันรีเซ็ตกลับไปเฟรม 0 ซ้ำๆ ทุกเฟรม

---

## 5. การตรวจสอบและแก้ไขปัญหาที่พบบ่อย (Troubleshooting)

| อาการที่พบ | สาเหตุที่เป็นไปได้ | วิธีแก้ไข |
| :--- | :--- | :--- |
| ตัวละครเดินแล้วท่าไม่เปลี่ยน ค้างอยู่ที่ท่าเดิม | เปิด `Has Exit Time` ไว้ หรือค่า Transition Duration ไม่ใช่ 0 | ปิด Has Exit Time และตั้ง Duration = 0 หรือสลับไปใช้โหมด Direct Play |
| ตัวละครหันซ้ายแล้วภาพกลับหัว หรือ Scale เพี้ยน | ไปปรับ Scale แกน X ติดลบแทนการใช้ FlipX | สคริปต์นี้ใช้ `spriteRenderer.flipX` ไม่กระทบต่อ `transform.localScale` ให้คง Scale ตัวละครเป็นบวกตามเดิม |
| ท่าเดินเล่นรอบเดียวแล้วหยุดนิ่ง (ไม่วนลูป) | ใน Animation Clip ปิด `Loop Time` ไว้ | คลิกที่ไฟล์ `.anim` ใน Inspector แล้วติ๊กเครื่องหมายถูกที่ **Loop Time** |
