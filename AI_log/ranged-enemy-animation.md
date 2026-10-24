# คู่มือระบบ Animation ตัวร้ายยิงธนู (Ranged Enemy Animation System)

**วันที่บันทึก:** 24 ตุลาคม 2026 (อัปเดตระบบ Animator Parameters 100%)  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์:** [`Assets/Script/Enemy/RengerEnemy/RangedEnemyAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/RengerEnemy/RangedEnemyAnimationCS.cs)  
**ไฟล์ Controller:** `Assets/img/Enemy_Bullet.controller`  
**ไฟล์ Animation Clips:**  
- `Assets/img/Enemy_Bullet_idel.anim` (ท่ายืน)  
- `Assets/img/Enemy_Bullet_Attack.anim` (ท่ายิงโจมตี)  

---

## 1. ภาพรวมของระบบ (Overview)

ศัตรูยิงไกล (Ranged Enemy / มือธนู) ถูกควบคุมผ่านระบบ **Animator Parameters และ Transitions ใน Unity 100%**:
1. **ท่ายืน (Idle):** เล่นวนลูปขณะยืนนิ่งหรือตรวจจับผู้เล่น (`Enemy_Bullet_idel`)
2. **ท่ายิงโจมตี (Attack):** เล่นเมื่อถึงจังหวะยิงกระสุนธนูผ่าน Trigger **`Attack`** (`Enemy_Bullet_Attack`)

---

## 2. ตารางแมปปิ้ง Sprite Sheet (`Enemy_Bullet.PNG`)

| ชื่อ Animation Clip | ลำดับ Sprites ที่ใช้ | คำอธิบายท่าทาง | การวนลูป (Loop Time) |
| :--- | :--- | :--- | :---: |
| **`Enemy_Bullet_idel`** | `Enemy_Bullet_0` ถึง `Enemy_Bullet_4` (5 เฟรม) | ตัวละครสวมฮู้ดยืนหายใจ/ขยับตัวนิ่งๆ | ✅ **วนลูป (Loop = 1)** |
| **`Enemy_Bullet_Attack`** | `Enemy_Bullet_5` ถึง `Enemy_Bullet_9` (5 เฟรม) | ดึงคันธนู ง้างสาย ยิงลูกศร | ❌ **ไม่วนลูป (Loop = 0)** |

---

## 3. สคริปต์ควบคุม: [`RangedEnemyAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/RengerEnemy/RangedEnemyAnimationCS.cs)

### การทำงานสำคัญ:
- เชื่อมต่อกับ Event `OnAttackTriggered` ของ [`EnemyCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/Enemy_Sword/EnemyCS.cs) อัตโนมัติ
- เมื่อศัตรูถึงระยะยิงและคูลดาวน์พร้อม สคริปต์จะสั่ง Trigger:
  ```csharp
  animator.SetTrigger("Attack");
  ```
- Animator จะเปลี่ยนเข้าสู่คลิป `Enemy_Bullet_Attack` และสลับกลับมาท่ายืนเมื่อเล่นจบโดยอัตโนมัติ

---

## 4. ขั้นตอนการตั้งค่าใน Unity Editor (Step-by-Step)

### ขั้นตอนที่ 1: ติดตั้ง Component เข้ากับตัวศัตรู
1. ในหน้าต่าง **Hierarchy** คลิกเลือกตัวร้ายยิงธนู (เช่น `B01RangedEnemy02` หรือใน Prefab `RangedEnemy`)
2. ในหน้าต่าง **Inspector**:
   - เพิ่ม Component **`Animator`** (หากยังไม่มี)
   - ในช่อง **Controller** ของ Animator ให้ลากไฟล์ `Assets/img/Enemy_Bullet.controller` มาใส่
   - กดปุ่ม **Add Component** แล้วใส่สคริปต์ **`RangedEnemyAnimationCS`**

---

### ขั้นตอนที่ 2: ตั้งค่าในหน้าต่าง Animator

1. เปิดหน้าต่าง **Animator** (ดับเบิ้ลคลิกไฟล์ `Assets/img/Enemy_Bullet.controller`)
2. ในแท็บ **Parameters** (ซ้ายมือ):
   - กดเครื่องหมาย **`+`** ➡️ เลือก **`Trigger`** ➡️ ตั้งชื่อว่า: **`Attack`**
3. ตั้งค่าเส้น Transitions:
   - **เส้นวิ่งเข้า (`Enemy_Bullet_idel` ➡️ `Enemy_Bullet_Attack`):**
     - คลิกที่เส้นลูกศร
     - ใน Inspector ช่อง **Conditions** กด `+` เลือก **`Attack`**
     - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
     - **Transition Duration:** **`0`**
   - **เส้นวิ่งกลับ (`Enemy_Bullet_Attack` ➡️ `Enemy_Bullet_idel`):**
     - คลิกที่เส้นลูกศรขากลับ
     - **Conditions:** ไม่ต้องใส่ (None)
     - **Has Exit Time:** ✅ **ติ๊กถูก (Check)** (Exit Time = 1 เพื่อเล่นท่ายิงให้จบก่อน)
     - **Transition Duration:** **`0`**
