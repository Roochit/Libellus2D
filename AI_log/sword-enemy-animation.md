# คู่มือระบบ Animation ศัตรูถือดาบ (Enemy_Sword Animation System)

**วันที่บันทึก:** 24 ตุลาคม 2026  
**โปรเจกต์:** Libellus2D  
**ไฟล์สคริปต์ควบคุม:** [`Assets/Script/Enemy/Enemy_Sword/EnemySwordAnimationCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/Enemy_Sword/EnemySwordAnimationCS.cs)  
**ไฟล์สคริปต์หลัก:** [`Assets/Script/Enemy/Enemy_Sword/EnemyCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/Enemy_Sword/EnemyCS.cs)  
**ไฟล์ Animator Controller:** `Assets/img/Enemy_Sword.controller`  

---

## 1. ภาพรวมของระบบ (Overview)

ศัตรูถือดาบ (`Enemy_Sword`) ควบคุมแอนิเมชันผ่านระบบ **Animator Parameters และ Transitions ใน Unity 100%**  
โดยมีท่าแอนิเมชันทั้งหมด **5 ท่าหลัก**:

| ลำดับ | ชื่อ State ใน Animator | ท่าทาง | การวนลูป (Loop Time) |
| :---: | :--- | :--- | :---: |
| 1 | **`Enemy_Sword_idel_WalkSideAndDowe`** | ท่ายืนนิ่ง, เดินลง หรือเดินไปข้างๆ | ✅ **วนลูป (Loop = 1)** |
| 2 | **`Enemy_Sword_WalkUp`** | ท่าเดินขึ้นด้านบน (หันหลัง) | ✅ **วนลูป (Loop = 1)** |
| 3 | **`Enemy_Sword_AttackSide`** | ท่าฟันดาบไปแนวนอน (ซ้าย/ขวา) | ❌ **ไม่วนลูป (Loop = 0)** |
| 4 | **`Enemy_Sword_AttackDowe`** | ท่าฟันดาบลงด้านล่าง | ❌ **ไม่วนลูป (Loop = 0)** |
| 5 | **`Enemy_Sword_AttackUp`** | ท่าฟันดาบขึ้นด้านบน | ❌ **ไม่วนลูป (Loop = 0)** |

> **💡 หมายเหตุเรื่องท่าโดนโจมตี:** ศัตรูไม่มีคลิปท่าโดนตี (Hit) โดยระบบของ [`EnemyCS.cs`](file:///Users/mac_air_m4/Documents/Unity_Project/Libellus2D/Assets/Script/Enemy/Enemy_Sword/EnemyCS.cs) จะแสดงผลการโดนดาเมจผ่าน **การกระพริบสีแดง (Red Flash)** และ **แรงกระเด็นถอยหลัง (Knockback)** ให้อัตโนมัติ (สามารถลบ State `hit` ออกจาก Animator ได้เลยครับ)

---

## 2. ขั้นตอนการติดตั้งเข้ากับตัวศัตรู (Setup Component)

1. ในหน้าต่าง **Hierarchy** คลิกเลือกตัวศัตรูถือดาบ (เช่น `B01Enemy01` หรือใน Prefab)
2. ในหน้าต่าง **Inspector**:
   - มี Component **`Animator`** ที่ใส่ `Assets/img/Enemy_Sword.controller`
   - กดปุ่ม **Add Component** ➡️ ใส่สคริปต์ **`EnemySwordAnimationCS`**

---

## 3. ขั้นตอนการตั้งค่าใน Animator (Step-by-Step)

เปิดหน้าต่าง **Animator** (ดับเบิ้ลคลิกไฟล์ `Assets/img/Enemy_Sword.controller`):

### ขั้นตอนที่ 1: สร้าง Parameters (แถบ Parameters ด้านซ้าย)
กดเครื่องหมาย **`+`** ด้านบนซ้ายของหน้าต่าง Animator แล้วสร้าง **5 ค่า** ดังนี้:

| ชื่อ Parameter | ชนิด (Type) | หน้าที่ |
| :--- | :---: | :--- |
| **`isMoving`** | **Bool** | `true` เมื่อศัตรูกำลังเคลื่อนที่ |
| **`isWalkingUp`** | **Bool** | `true` เมื่อศัตรูกำลังเดินขึ้นด้านบน |
| **`AttackSide`** | **Trigger** | สั่งเล่นท่าฟันแนวนอน (ซ้าย/ขวา) |
| **`AttackDown`** | **Trigger** | สั่งเล่นท่าฟันลงล่าง |
| **`AttackUp`** | **Trigger** | สั่งเล่นท่าฟันขึ้นบน |

---

### ขั้นตอนที่ 2: ตั้งค่าเส้น Transitions (การเปลี่ยนท่า)

#### 1. การเดินปกติ 🔁 เดินขึ้น (`Enemy_Sword_idel_WalkSideAndDowe` ⇄ `Enemy_Sword_WalkUp`)
- **เส้นไป `Enemy_Sword_WalkUp`:**
  - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
  - **Transition Duration:** `0.1`
  - **Conditions:** กด `+` เลือก `isWalkingUp` เป็น **`true`**
- **เส้นกลับ `Enemy_Sword_idel_WalkSideAndDowe`:**
  - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
  - **Transition Duration:** `0.1`
  - **Conditions:** กด `+` เลือก `isWalkingUp` เป็น **`false`**

---

#### 2. จาก `Any State` ➡️ ท่าฟันโจมตีต่างๆ
ลากเส้นลูกศรจากกล่องสีเขียว **`Any State`** ไปยังท่าโจมตีทั้ง 3 ท่า:

- **เส้น Any State ➡️ `Enemy_Sword_AttackSide`:**
  - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
  - **Transition Duration:** `0`
  - **Can Transition To Self:** ❌ **เอาออก (Uncheck)**
  - **Conditions:** กด `+` เลือก **`AttackSide`**

- **เส้น Any State ➡️ `Enemy_Sword_AttackDowe`:**
  - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
  - **Transition Duration:** `0`
  - **Can Transition To Self:** ❌ **เอาออก (Uncheck)**
  - **Conditions:** กด `+` เลือก **`AttackDown`**

- **เส้น Any State ➡️ `Enemy_Sword_AttackUp`:**
  - **Has Exit Time:** ❌ **เอาออก (Uncheck)**
  - **Transition Duration:** `0`
  - **Can Transition To Self:** ❌ **เอาออก (Uncheck)**
  - **Conditions:** กด `+` เลือก **`AttackUp`**

---

#### 3. จากท่าโจมตี ➡️ วิ่งกลับสู่ท่าหลัก (`Enemy_Sword_idel_WalkSideAndDowe`)
ลากเส้นลูกศรขากลับจากทั้ง 3 ท่าโจมตี กลับมาที่ **`Enemy_Sword_idel_WalkSideAndDowe`**:

- **คลิกที่เส้นลูกศรขากลับของแต่ละท่า:**
  - **Has Exit Time:** ✅ **ติ๊กถูก (Check)**
  - **Exit Time:** `1` (รอเล่นคลิปฟันให้จบ 1 รอบเต็ม)
  - **Transition Duration:** `0`
  - **Conditions:** **ไม่ต้องใส่ (List is Empty)**

---

## 4. สรุปผลการทำงาน

เมื่อตั้งค่าตามนี้แล้ว:
1. เมื่อศัตรูเดินตามผู้เล่น สคริปต์จะส่งค่า `isMoving` และ `isWalkingUp` ให้โดยอัตโนมัติ
2. เมื่อเข้าใกล้ระยะดาบ สคริปต์จะคำนวณตำแหน่งผู้เล่นแล้วยิง Trigger `AttackUp`, `AttackDown`, หรือ `AttackSide` ทันที
3. เมื่อฟันเสร็จ Animation จะตัดกลับมาที่ท่าเดิน/ยืนตามปกติอย่างราบรื่น
