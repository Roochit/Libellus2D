# คู่มือระบบ Animation ตัวร้ายยิงธนู (Ranged Enemy Animation System)

**วันที่บันทึก:** 23 ตุลาคม 2026  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์:** [`Assets/Script/Enemy/RangedEnemyAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/RangedEnemyAnimationCS.cs)  
**ไฟล์ Controller:** `Assets/img/Enemy_Bullet.controller`  
**ไฟล์ Animation Clips:**  
- `Assets/img/Enemy_Bullet_idel.anim` (ท่ายืน)  
- `Assets/img/Enemy_Bullet_Attack.anim` (ท่ายิงโจมตี)  

---

## 1. ภาพรวมของระบบ (Overview)

ศัตรูยิงไกล (Ranged Enemy / มือธนู) ถูกควบคุมการเดินและยิงด้วย [`EnemyCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/EnemyCS.cs) (`enemyType = EnemyType.Ranged`)
โดยระบบแอนิเมชันจะแบ่งเป็น **2 ท่าหลัก**:
1. **ท่ายืน (Idle):** เล่นวนลูปขณะยืนนิ่งหรือตรวจจับผู้เล่น
2. **ท่ายิงโจมตี (Attack):** เล่นเมื่อถึงจังหวะยิงกระสุนธนู แล้วสลับกลับไปเป็นท่ายืนอัตโนมัติ

---

## 2. ตารางแมปปิ้ง Sprite Sheet (`Enemy_Bullet.PNG`)

ไฟล์ `Assets/img/Enemy_Bullet.PNG` ได้รับการบรรจุ Sprites และตั้งค่าให้ทั้ง 2 คลิปเรียบร้อยแล้ว:

| ชื่อ Animation Clip | ลำดับ Sprites ที่ใช้ | คำอธิบายท่าทาง | การวนลูป (Loop Time) |
| :--- | :--- | :--- | :---: |
| **`Enemy_Bullet_idel`** | `Enemy_Bullet_0` ถึง `Enemy_Bullet_4` (5 เฟรม) | ตัวละครสวมฮู้ดยืนหายใจ/ขยับตัวนิ่งๆ | ✅ **วนลูป (Loop = 1)** |
| **`Enemy_Bullet_Attack`** | `Enemy_Bullet_5` ถึง `Enemy_Bullet_9` (5 เฟรม) | ดึงคันธนู ง้างสาย ยิงลูกศร | ❌ **ไม่วนลูป (Loop = 0)** |

---

## 3. สคริปต์ควบคุม: [`RangedEnemyAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/RangedEnemyAnimationCS.cs)

### การทำงานสำคัญ:
- เชื่อมต่อกับ Event `OnAttackTriggered` ของ [`EnemyCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/EnemyCS.cs) อัตโนมัติ เมื่อศัตรูถึงระยะยิงและคูลดาวน์พร้อม สคริปต์จะตัดเข้าท่า `Enemy_Bullet_Attack` ทันที
- เมื่อเล่นท่าโจมตีจบตามเวลาที่ตั้งไว้ (`attackDuration = 0.35s`) จะสลับกลับมาเล่น `Enemy_Bullet_idel` ทันที
- รองรับ 2 โหมด:
  - **`DirectPlay` (แนะนำ — ใช้ง่ายที่สุด):** สั่งเล่นชื่อคลิปผ่านโค้ดตรงๆ ไม่ต้องเซ็ต Parameter ใดๆ ใน Animator
  - **`AnimatorTrigger`:** ใช้ Trigger `"Attack"` ในหน้าต่าง Animator ตามมาตรฐานของ Unity

---

## 4. ขั้นตอนการตั้งค่าใน Unity Editor (Step-by-Step)

### ขั้นตอนที่ 1: ติดตั้ง Component เข้ากับตัวศัตรู
1. ในหน้าต่าง **Hierarchy** คลิกเลือกตัวร้ายยิงธนู (เช่น `B01RangedEnemy02` หรือใน Prefab `RangedEnemy`)
2. ในหน้าต่าง **Inspector**:
   - เพิ่ม Component **`Animator`** (หากยังไม่มี)
   - ในช่อง **Controller** ของ Animator ให้ลากไฟล์ `Assets/img/Enemy_Bullet.controller` มาใส่
   - กดปุ่ม **Add Component** แล้วใส่สคริปต์ **`RangedEnemyAnimationCS`**

---

### ขั้นตอนที่ 2: เลือกวิธีควบคุม

#### 🟢 แบบที่ A: ใช้โหมด "Direct Play" (แนะนำสูงสุด — เล่นได้ทันที ไม่ต้องต่อสาย)
1. ใน Inspector ที่ Component **RangedEnemyAnimationCS**:
   - ปรับ **Animation Mode** เป็น **`Direct Play`**
2. ในหน้าต่าง **Animator**:
   - ไม่ต้องตั้งค่า Parameters ใดๆ สามารถลบเส้น Transition ออกหรือปล่อยไว้ก็ได้
3. **กด Play ทดสอบได้ทันที!** ศัตรูจะยืนท่า Idle และเมื่อเข้าใกล้ผู้เล่นจะง้างธนูยิงท่า Attack อัตโนมัติ

---

#### 🔵 แบบที่ B: ใช้โหมด "Animator Trigger" (วิธีดั้งเดิมของ Unity)
หากต้องการใช้ระบบเส้น Transitions ที่ต่อไว้ใน Animator:
1. ในหน้าต่าง **Animator** (แท็บ **Parameters** ซ้ายมือ):
   - กดเครื่องหมาย **`+`** ➡️ เลือก **`Trigger`** ➡️ ตั้งชื่อว่า: **`Attack`**
2. ตั้งค่าเส้น Transitions:
   - **เส้นวิ่งเข้า (`Enemy_Bullet_idel` ➡️ `Enemy_Bullet_Attack`):**
     - คลิกที่เส้นลูกศร
     - ใน Inspector ช่อง **Conditions** กด `+` เลือก **`Attack`**
     - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
     - **Transition Duration:** **`0`**
   - **เส้นวิ่งกลับ (`Enemy_Bullet_Attack` ➡️ `Enemy_Bullet_idel`):**
     - คลิกที่เส้นลูกศรขากลับ
     - **Conditions:** ไม่ต้องใส่ (None)
     - **Has Exit Time:** ✅ **ติ๊กถูก (Check)** (Exit Time = 1)
     - **Transition Duration:** **`0`**
