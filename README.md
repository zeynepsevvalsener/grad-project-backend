## 🧠 Weekly Summary (Sprint 7 – Backend)

This week, the nutrition module was extended to support **multilingual food names via aliases**, and the local development database seed was expanded from **13 → 25 foods**.

Key outcomes:

### ✅ Multilingual Food Alias Support

* Added **FoodAliases** table and mapping logic for TR/EN-friendly food searching.
* Food search now supports **alias-based matching** (e.g., “elma” → Apple).
* Designed to be **extensible** for future languages.

### ✅ Expanded Food Seed Dataset (25 Foods)

* Seed dataset updated to include more common foods across categories (grain, dairy, fat, legume, nut).
* Values are stored in the **Foods** table (global), and TR aliases are stored in **FoodAliases**.

### ✅ New Search Function

* Enhanced /api/v1/foods/search endpoint with advanced filtering, sorting, and pagination support.
* Added sorting options (asc/desc) - (alphabetical)
* Implemented full pagination metadata and Refactored query logic for improved efficiency using SQL-based filtering

---

## 🚀 Running the Application (Local Development)

1. Create an `appsettings.Development.json` file under:

`GradProject.Api`

2. Copy the content of `appsettings.json` into it and fill in:

* Local PostgreSQL connection string
* JWT / secret values (if any)
* AI port (it is on the AI repo's README file)

3. **Drop your existing local database (recommended when schema changes):**


4. Apply the latest schema (migrations) to recreate the DB:

```bash
dotnet ef database update --project GradProject.Infrastructure --startup-project GradProject.Api
```

✅ This applies all migrations and brings your local database to the latest state.


---

## 🧩 PostgreSQL + PostGIS Setup Notes (Important)

If you **don’t have Stack Builder** (commonly seen when PostgreSQL isn’t installed as the full package) or you are not using **PostgreSQL 17**, do this:

1. **Uninstall** your current PostgreSQL.
2. Install **PostgreSQL 17 – Full Package** so **Stack Builder** is included.
3. During installation, for the cluster language step, choose **C** (helps avoid locale/cluster issues).
4. At the end of installation (or afterward), open **Stack Builder** and install:

* **PostGIS 3.6** extension

PostGIS must be installed properly for upcoming features / extensions to work correctly.

---

## 🌍 Language Switching (TR/EN Testing)

We have a **Language endpoint** that allows you to switch the account language.

Current usage:

* Changes language for **basic messages** (e.g., login/register feedback)
* Changes the **display language for foods** (via aliases)

Frontend labels/text should still be implemented using **i18n** on the client side, with the help of language column on the User entity, with language endpoint.

✅ For quick testing:

* Use Swagger
* Register/Login
* Call the Language endpoint to switch to `tr` or `en` (it is ''en'' default)
* Test Food Search results in both languages

---

## 🥗 Food Data & Admin Notes (Optional, you do not need these endpoints)

Food creation/update/deletion affects the global database and is therefore **admin-only**.

Frontend and mobile developers do **NOT** need admin features for normal development.

If needed for testing, manually update your role in DB:

```sql
UPDATE "Users" SET "Role" = 1 WHERE "Email" = 'your-email@example.com';
```

---

## 🌱 Sample Food Seed Data (25 Foods)


### Foods (25)

```sql
INSERT INTO "Foods"
("Name", "Category", "Kcal", "ProteinG", "FatG", "CarbG", "SugarG", "FiberG", "SodiumMg", "DefaultPortionG", "Source")
VALUES
('Apple', 'fruit', 52, 0.3, 0.2, 13.8, 10.4, 2.4, 1, 182, 'USDA'),
('Banana', 'fruit', 89, 1.1, 0.3, 22.8, 12.2, 2.6, 1, 118, 'USDA'),
('Pear', 'fruit', 57, 0.4, 0.1, 15.2, 9.8, 3.1, 0, 166, 'USDA'),
('Broccoli', 'vegetable', 34, 2.8, 0.4, 6.6, 1.7, 2.6, 33, 148, 'USDA'),
('Eggplant', 'vegetable', 25, 1.0, 0.2, 5.9, 2.4, 3.0, 2, 100, 'USDA'),
('Chicken Breast', 'protein', 120, 22.5, 2.6, 0.0, 0.0, 0.0, 45, 150, 'USDA'),
('Beef (lean)', 'protein', 140, 21.0, 5.0, 0.0, 0.0, 0.0, 50, 150, 'USDA'),
('Salmon', 'protein', 208, 20.4, 13.4, 0.0, 0.0, 0.0, 59, 150, 'USDA'),
('Water', 'beverage', 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, 240, 'USDA'),
('Milk (whole)', 'beverage', 61, 3.2, 3.3, 4.8, 4.8, 0.0, 43, 240, 'USDA'),
('Coffee (black)', 'beverage', 1, 0.1, 0.0, 0.0, 0.0, 0.0, 2, 240, 'USDA'),
('Chocolate Bar', 'snack', 535, 7.6, 30.0, 59.0, 52.0, 3.0, 79, 40, 'USDA'),
('Potato Chips', 'snack', 536, 7.1, 36.0, 57.0, 0.0, 3.6, 525, 30, 'USDA'),
('White Rice (Long-Grain, Cooked)', 'grain', 130, 2.69, 0.28, 28.17, 0.05, 0.4, 1, 158, 'USDA'),
('White Bread', 'grain', 266, 7.64, 3.29, 50.61, 4.31, 2.4, 681, 30, 'USDA'),
('Plain Yogurt (Whole Milk)', 'dairy', 61, 3.47, 3.25, 4.66, 4.66, 0.0, 46, 200, 'USDA'),
('Egg (Whole, Raw)', 'protein', 147, 12.58, 9.94, 0.77, 0.77, 0.0, 140, 50, 'USDA'),
('Tomato (Raw)', 'vegetable', 18, 0.88, 0.2, 3.92, 2.63, 1.2, 5, 123, 'USDA'),
('Cucumber (with Peel, Raw)', 'vegetable', 15, 0.65, 0.11, 3.63, 1.67, 0.5, 2, 100, 'USDA'),
('Olive Oil', 'fat', 884, 0.0, 100.0, 0.0, 0.0, 0.0, 2, 14, 'USDA'),
('Cheddar Cheese', 'dairy', 403, 24.9, 33.14, 1.28, 0.52, 0.0, 621, 28, 'USDA'),
('Oats (Dry)', 'grain', 389, 16.89, 6.9, 66.27, 0.99, 10.6, 2, 40, 'USDA'),
('Spaghetti (Cooked, No Salt)', 'grain', 158, 5.8, 0.93, 30.86, 0.56, 1.8, 1, 140, 'USDA'),
('Cooked Lentils (No Added Fat)', 'legume', 115, 8.97, 0.38, 20.01, 1.79, 7.9, 233, 198, 'USDA'),
('Almonds', 'nut', 578, 21.26, 50.64, 19.74, 4.8, 11.8, 1, 28, 'USDA');
```

### FoodAliases (TR)

```sql
INSERT INTO "FoodAliases"
("FoodId", "Language", "Alias", "NormalizedAlias")
VALUES
(1, 'tr', 'elma', 'elma'),
(2, 'tr', 'muz', 'muz'),
(3, 'tr', 'armut', 'armut'),
(4, 'tr', 'brokoli', 'brokoli'),
(5, 'tr', 'patlıcan', 'patlican'),
(6, 'tr', 'tavuk göğsü', 'tavuk gogsu'),
(7, 'tr', 'sığır eti', 'sigir eti'),
(8, 'tr', 'somon', 'somon'),
(9, 'tr', 'su', 'su'),
(10, 'tr', 'süt', 'sut'),
(11, 'tr', 'kahve', 'kahve'),
(12, 'tr', 'çikolata', 'cikolata'),
(13, 'tr', 'patates cipsi', 'patates cipsi'),
(14, 'tr', 'pirinç', 'pirinc'),
(15, 'tr', 'beyaz ekmek', 'beyaz ekmek'),
(16, 'tr', 'yoğurt', 'yogurt'),
(17, 'tr', 'yumurta', 'yumurta'),
(18, 'tr', 'domates', 'domates'),
(19, 'tr', 'salatalık', 'salatalik'),
(20, 'tr', 'zeytinyağı', 'zeytinyagi'),
(21, 'tr', 'çedar peyniri', 'cedar peyniri'),
(22, 'tr', 'yulaf', 'yulaf'),
(23, 'tr', 'makarna', 'makarna'),
(24, 'tr', 'mercimek', 'mercimek'),
(25, 'tr', 'badem', 'badem');
```

---

Happy coding and good luck with the sprint! 🚀
