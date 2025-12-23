## 🧠 Weekly Summary (Sprint 5 – Backend)

This week, core nutrition-related backend features were implemented.

These include BMR/TDEE & BMI calculations, daily calorie and macro targets, Food & ConsumedFood entities and APIs, and a unified Nutrition Search API with flexible text-based search and pagination support.

All endpoints are designed to work consistently across web and mobile clients and align with MVP scope.

Profile (Upsert / Me)
Allows users to create or update their personal profile (height, weight, gender, activity level).
Used as the base input for all nutrition calculations.

BMR / TDEE & BMI Calculation
Calculates the user’s BMR, TDEE, BMI value, and BMI category based on profile data.
Can be used to display health metrics widgets on the profile or dashboard screen.

Daily Calorie & Macro Targets
Returns recommended daily calorie and macro targets derived from TDEE.
Used to show daily nutrition goals in dashboards or progress views.

Food List & Nutrition Search
Provides a searchable, paginated food database with name, category, and alias-based matching.
Used for food selection, autocomplete, and food search screens.

Consumed Food (User-specific)
Allows users to log consumed foods with portion (grams) and timestamp.
Acts as a raw consumption log (like a shopping cart) for daily intake calculations.

Daily Intake Aggregation Service (HLN-4 – Upcoming)
Aggregates consumed food logs to calculate total daily (and later weekly/monthly) calorie and macro intake.
This service will be implemented separately and will consume ConsumedFood data as input.

---

## 🚀 Running the Application (Local Development)

To run the project locally:

1. Create an appsettings.Development.json file under:

   GradProject.Api

2. Copy the content of appsettings.json into it and fill in your local PostgreSQL connection string and secrets.

3. Apply the latest database schema by running:

   dotnet ef database update --project GradProject.Infrastructure --startup-project GradProject.Api

This command applies all existing migrations to your local database (latest state).

---

## 🥗 Food Data & Admin Notes

Food creation, update, and deletion (CRUD) affect the global food database and are therefore admin-only operations.

Frontend and mobile developers do NOT need to use admin features for normal development, you do NOT have to use Food CRUD endpoints.

If still needed for testing:

You can manually update your user role in the database:

```
UPDATE "Users" SET "Role" = 1 WHERE "Email" = 'your-email@example.com';
```

Admin-only endpoints should be protected on the frontend/mobile side, but still you do not have to implement these admin-only pages for Sprint 5 and MVP.

---

## 🌱 Sample Food Seed Data (Optional)

Sample food seed data is provided in the README for local development convenience:

```
INSERT INTO "Foods"
("Name", "Category", "Kcal", "ProteinG", "FatG", "CarbG", "SugarG", "FiberG", "SodiumMg", "DefaultPortionG", "Source", "Aliases")
VALUES
('Apple', 'fruit', 52, 0.3, 0.2, 13.8, 10.4, 2.4, 1, 182, 'USDA', ARRAY['elma']),
('Banana', 'fruit', 89, 1.1, 0.3, 22.8, 12.2, 2.6, 1, 118, 'USDA', ARRAY['muz']),
('Pear', 'fruit', 57, 0.4, 0.1, 15.2, 9.8, 3.1, 0, 166, 'USDA', ARRAY['armut']),
('Broccoli', 'vegetable', 34, 2.8, 0.4, 6.6, 1.7, 2.6, 33, 148, 'USDA', ARRAY['brokoli']),
('Eggplant', 'vegetable', 25, 1.0, 0.2, 5.9, 2.4, 3.0, 2, 100, 'USDA', ARRAY['patlıcan']),
('Chicken Breast', 'protein', 120, 22.5, 2.6, 0.0, 0.0, 0.0, 45, 150, 'USDA', ARRAY['tavuk göğsü']),
('Beef (lean)', 'protein', 140, 21.0, 5.0, 0.0, 0.0, 0.0, 50, 150, 'USDA', ARRAY['sığır eti']),
('Salmon', 'protein', 208, 20.4, 13.4, 0.0, 0.0, 0.0, 59, 150, 'USDA', ARRAY['somon']),
('Water', 'beverage', 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, 240, 'USDA', ARRAY['su']),
('Milk (whole)', 'beverage', 61, 3.2, 3.3, 4.8, 4.8, 0.0, 43, 240, 'USDA', ARRAY['süt']),
('Coffee (black)', 'beverage', 1, 0.1, 0.0, 0.0, 0.0, 0.0, 2, 240, 'USDA', ARRAY['kahve']),
('Chocolate Bar', 'snack', 535, 7.6, 30.0, 59.0, 52.0, 3.0, 79, 40, 'USDA', ARRAY['çikolata']),
('Potato Chips', 'snack', 536, 7.1, 36.0, 57.0, 0.0, 3.6, 525, 30, 'USDA', ARRAY['patates cipsi']);
```
Happy coding and good luck with the sprint! 🚀
