Given the database with its table schemas defined below:

**Table schemas:**

**Table:** `dbo.Categories`
- `Id` (int, PK): Category ID
- `Name` (nvarchar(100)): Category name
- `ParentId` (int, nullable): Parent category ID (self-referencing)
- `Description` (nvarchar(500)): Description
- `Active` (bit): Active status
- `CreatedOnUtc` (datetime2): Creation timestamp

**Table:** `dbo.Products`
- `Id` (int, PK): Product ID
- `ProductCode` (nvarchar(50)): Unique product code
- `ProductName` (nvarchar(200)): Product name
- `Category` (nvarchar(100)): Category name
- `Brand` (nvarchar(100)): Brand name
- `Price` (decimal(18,2)): Selling price
- `CostPrice` (decimal(18,2)): Cost price
- `StockQuantity` (int): Inventory quantity
- `Unit` (nvarchar(50)): Unit of measurement
- `Description` (nvarchar(max)): Description
- `ImageUrl` (nvarchar(500)): Image URL
- `Size` (nvarchar(50)): Product size
- `Color` (nvarchar(50)): Product color
- `Active` (bit): Active status (1=active, 0=inactive)
- `CreatedOnUtc` (datetime2): Creation timestamp

**Table:** `dbo.Orders`
- `Id` (int, PK): Order ID
- `OrderCode` (nvarchar(50)): Unique order code
- `CustomerName` (nvarchar(200)): Customer name
- `CustomerPhone` (nvarchar(20)): Customer phone
- `CustomerEmail` (nvarchar(200)): Customer email
- `TotalAmount` (decimal(18,2)): Total order amount
- `Status` (nvarchar(50)): Order status
- `Channel` (nvarchar(50)): Sales channel
- `OrderDate` (datetime2): Order date
- `CompletedDate` (datetime2, nullable): Completion date
- `Note` (nvarchar(max)): Notes

**Table:** `dbo.OrderDetails`
- `Id` (int, PK): Order detail ID
- `OrderId` (int, FK): Foreign key to Orders
- `ProductId` (int, FK): Foreign key to Products
- `ProductName` (nvarchar(200)): Product name (snapshot)
- `Quantity` (int): Order quantity
- `UnitPrice` (decimal(18,2)): Unit price at order time
- `TotalPrice` (decimal(18,2)): Total line price (Quantity × UnitPrice)
- `Size` (nvarchar(50)): Size
- `Color` (nvarchar(50)): Color

---

**I need a SQL query that can retrieve the necessary information to answer the question, following these instructions:**

1. All generated column names must exist in the table schemas above.
2. If the question is related to creating a chart (pie, bar, column, line), ensure the first column represents categorical data, and the second column contains numerical aggregation (SUM, COUNT, AVG, etc.).
3. If asked about returned orders, filter with `TotalAmount < 0` (negative values).
4. If asked to compare values, add one percentage column as DECIMAL type. DO NOT concatenate ' %' string - return numeric value only.
5. If asked about growth, add one percentage column as DECIMAL type. DO NOT concatenate ' %' string - return numeric value only.
6. If asked to compare without mentioning specific column, use the amount column.
7. If the question doesn't include a year, filter with the current year: `YEAR(OrderDate) = YEAR(GETDATE())`.
8. If asked about getting the highest (best) or lowest values, return only TOP 1 result.
9. Review the chat history to combine with current asking context.
10. If the answer is not SQL but a string, wrap it in a query: `SELECT N'string' AS Answer`. Never cast columns to FLOAT.
11. Before answering, re-check the SQL query and always: (1) use NVARCHAR format (N'...') when filtering string values; (2) use correct SQL Server syntax; (3) ensure all parentheses are balanced; (4) do not mix string concatenation with column definitions.

---

**Complete data of some columns:**

- Column `Status` in Orders table includes: `N'Pending'`, `N'Confirmed'`, `N'Shipping'`, `N'Completed'`, `N'Cancelled'`
- Column `Channel` in Orders table includes: `N'Online'`, `N'Offline'`, `N'Mobile App'`, `N'Phone'`
- Column `Active` in Products table: `1` (active), `0` (inactive)

---

**CRITICAL SQL SYNTAX RULES (Must Follow):**

1. ✅ **Percentage Columns - Return Numeric Values Only**
   ```sql
   -- CORRECT: Return DECIMAL value, let application format
   CAST(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS DECIMAL(5,2)) AS TyLePhanTram
   
   -- WRONG: Do NOT concatenate strings in SELECT columns
   CAST(...) + N' %' AS TyLePhanTram  -- ❌ NEVER DO THIS
   CAST(...) AS NVARCHAR(10)) + N' %' AS TyLePhanTram  -- ❌ SYNTAX ERROR
   ```

2. ✅ **CAST Syntax - Check Parentheses**
   ```sql
   -- CORRECT: Balanced parentheses
   CAST(column AS DECIMAL(18,2)) AS Alias
   CAST(expression * 100.0 AS DECIMAL(5,2)) AS Percent
   
   -- WRONG: Unbalanced or extra parentheses
   CAST(column AS DECIMAL(18,2))) AS Alias  -- ❌ Extra closing parenthesis
   CAST(column AS NVARCHAR(10)) AS Alias  -- ❌ Double closing parenthesis
   ```

3. ✅ **Column Aliases - Use Simple Identifiers**
   ```sql
   -- CORRECT: Simple, clear aliases
   AS TongDoanhThu, AS TyLePhanTram, AS DoanhThu
   
   -- CORRECT: Use square brackets if spaces needed
   AS [Tong doanh thu], AS [Ty le %]
   
   -- WRONG: No special syntax mixing
   AS NVARCHAR(10)) AS Alias  -- ❌ Invalid syntax
   ```

4. ✅ **String Operations - Separate from Column Definitions**
   ```sql
   -- CORRECT: Return raw value, format in application
   SELECT 
       ProductName,
       Price AS GiaBan,
       StockQuantity AS TonKho
   
   -- WRONG: String concatenation in SELECT for data columns
   SELECT 
       ProductName + N' (Còn hàng)' AS TenSanPham,  -- ❌ Avoid for data values
       CAST(Price AS NVARCHAR) + N' VND' AS Gia    -- ❌ Return numeric only
   ```

5. ✅ **Aggregate Functions - Proper Nesting**
   ```sql
   -- CORRECT: Use window functions for percentage
   COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS Percent
   
   -- CORRECT: Subquery for complex calculations
   (SELECT SUM(TotalAmount) FROM Orders) AS Total
   
   -- WRONG: Invalid nesting
   SUM(COUNT(*)) AS Total  -- ❌ Cannot nest aggregates directly
   ```

6. ✅ **JOIN Syntax - Proper Table Aliases**
   ```sql
   -- CORRECT: Clear aliases, proper ON conditions
   FROM dbo.OrderDetails od
   INNER JOIN dbo.Products p ON od.ProductId = p.Id
   INNER JOIN dbo.Orders o ON od.OrderId = o.Id
   
   -- WRONG: Missing aliases or incorrect joins
   FROM OrderDetails, Products, Orders  -- ❌ Avoid implicit joins
   INNER JOIN Products ON ProductId = Id  -- ❌ Ambiguous columns
   ```

7. ✅ **GROUP BY - Include All Non-Aggregate Columns**
   ```sql
   -- CORRECT: All non-aggregate SELECT columns in GROUP BY
   SELECT 
       p.Brand, p.Category,
       SUM(od.TotalPrice) AS Revenue
   FROM ...
   GROUP BY p.Brand, p.Category
   
   -- WRONG: Missing columns from GROUP BY
   SELECT p.Brand, p.Category, SUM(od.TotalPrice)
   GROUP BY p.Brand  -- ❌ Missing p.Category
   ```

8. ✅ **NULL Handling - Use ISNULL or COALESCE**
   ```sql
   -- CORRECT: Handle nullable columns
   ISNULL(CompletedDate, OrderDate) AS EffectiveDate
   COALESCE(p.Brand, N'Không có') AS ThuongHieu
   
   -- CORRECT: NULL checks in WHERE
   WHERE CompletedDate IS NOT NULL
   WHERE Brand IS NULL OR Brand = N''
   ```

---

**COMMON ERRORS TO AVOID:**

❌ **Error 1: String concatenation in column definition**
```sql
-- BAD: CAST(...) + N' %' AS NVARCHAR(10)) AS Column
-- FIX: CAST(...) AS DECIMAL(5,2)) AS Column
```

❌ **Error 2: Unbalanced parentheses**
```sql
-- BAD: CAST(value AS TYPE(10)))
-- FIX: CAST(value AS TYPE(10))
```

❌ **Error 3: Missing N prefix for Unicode strings**
```sql
-- BAD: WHERE Status = 'Completed'
-- FIX: WHERE Status = N'Completed'
```

❌ **Error 4: Ambiguous column names in JOINs**
```sql
-- BAD: SELECT Id, Name FROM Orders o JOIN Products p
-- FIX: SELECT o.Id, p.Name FROM Orders o JOIN Products p
```

❌ **Error 5: Using aggregate without GROUP BY**
```sql
-- BAD: SELECT Category, SUM(Price) FROM Products
-- FIX: SELECT Category, SUM(Price) FROM Products GROUP BY Category
```

❌ **Error 6: Incorrect date filtering**
```sql
-- BAD: WHERE OrderDate = '2026-02-11'
-- FIX: WHERE CAST(OrderDate AS DATE) = '2026-02-11'
-- OR: WHERE OrderDate >= '2026-02-11' AND OrderDate < '2026-02-12'
```

---

**Important SQL Server Date Functions:**

- Current date: `CAST(GETDATE() AS DATE)`
- Current datetime: `GETDATE()`
- Yesterday: `DATEADD(DAY, -1, CAST(GETDATE() AS DATE))`
- Start of current week: `DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0)`
- Start of current month: `DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0)`
- Start of current year: `DATEADD(YEAR, DATEDIFF(YEAR, 0, GETDATE()), 0)`
- Format month: `FORMAT(OrderDate, 'yyyy-MM')`
- Format date: `FORMAT(OrderDate, 'yyyy-MM-dd')`

---

**Example queries:**

Example 1 - Revenue by month (for line chart):
```sql
SELECT 
    FORMAT(o.OrderDate, 'yyyy-MM') AS Thang,
    COUNT(o.Id) AS SoDonHang,
    SUM(o.TotalAmount) AS TongDoanhThu,
    AVG(o.TotalAmount) AS TrungBinhDon
FROM dbo.Orders o
WHERE o.Status = N'Completed'
    AND YEAR(o.OrderDate) = YEAR(GETDATE())
GROUP BY FORMAT(o.OrderDate, 'yyyy-MM')
ORDER BY Thang;
```

Example 2 - Top products (for bar chart):
```sql
SELECT TOP 10
    p.ProductName AS TenSanPham,
    SUM(od.Quantity) AS SoLuongBan,
    SUM(od.TotalPrice) AS DoanhThu
FROM dbo.OrderDetails od
INNER JOIN dbo.Products p ON od.ProductId = p.Id
INNER JOIN dbo.Orders o ON od.OrderId = o.Id
WHERE o.Status = N'Completed'
    AND YEAR(o.OrderDate) = YEAR(GETDATE())
GROUP BY p.ProductName
ORDER BY SoLuongBan DESC;
```

Example 3 - Order status distribution (for pie chart):
```sql
SELECT 
    o.Status AS TrangThai,
    COUNT(o.Id) AS SoDonHang,
    CAST(COUNT(o.Id) * 100.0 / SUM(COUNT(o.Id)) OVER() AS DECIMAL(5,2)) AS TyLePhanTram
FROM dbo.Orders o
WHERE YEAR(o.OrderDate) = YEAR(GETDATE())
GROUP BY o.Status
ORDER BY SoDonHang DESC;
```

Example 4 - Revenue by category with JOIN:
```sql
SELECT 
    p.Category AS DanhMuc,
    COUNT(DISTINCT od.OrderId) AS SoDonHang,
    SUM(od.Quantity) AS SoLuongBan,
    SUM(od.TotalPrice) AS DoanhThu,
    SUM(od.TotalPrice) - SUM(od.Quantity * p.CostPrice) AS LoiNhuan
FROM dbo.OrderDetails od
INNER JOIN dbo.Products p ON od.ProductId = p.Id
INNER JOIN dbo.Orders o ON od.OrderId = o.Id
WHERE o.Status = N'Completed'
    AND YEAR(o.OrderDate) = YEAR(GETDATE())
GROUP BY p.Category
ORDER BY DoanhThu DESC;
```

Example 5 - Growth comparison with percentage:
```sql
SELECT 
    ThangHienTai.Thang,
    ThangHienTai.DoanhThu AS DoanhThuHienTai,
    ThangTruoc.DoanhThu AS DoanhThuThangTruoc,
    CAST((ThangHienTai.DoanhThu - ThangTruoc.DoanhThu) * 100.0 / ThangTruoc.DoanhThu AS DECIMAL(5,2)) AS TangTruong
FROM (
    SELECT FORMAT(OrderDate, 'yyyy-MM') AS Thang, SUM(TotalAmount) AS DoanhThu
    FROM dbo.Orders
    WHERE Status = N'Completed'
    GROUP BY FORMAT(OrderDate, 'yyyy-MM')
) ThangHienTai
LEFT JOIN (
    SELECT FORMAT(DATEADD(MONTH, 1, OrderDate), 'yyyy-MM') AS Thang, SUM(TotalAmount) AS DoanhThu
    FROM dbo.Orders
    WHERE Status = N'Completed'
    GROUP BY FORMAT(DATEADD(MONTH, 1, OrderDate), 'yyyy-MM')
) ThangTruoc ON ThangHienTai.Thang = ThangTruoc.Thang
ORDER BY ThangHienTai.Thang DESC;
```

Remember:
- Always wrap column names with spaces in square brackets: `[Ten san pham]`
- Use N prefix for Vietnamese strings: `N'Hoàn thành'`
- Use proper JOIN syntax when querying multiple tables
- Always filter by order status when calculating revenue
- Use appropriate GROUP BY with aggregate functions

---

**FINAL VALIDATION CHECKLIST (Before returning query):**

Before you return the SQL query, verify:

1. ✅ All parentheses are balanced (count opening and closing)
2. ✅ No string concatenation (+ N'...') in SELECT column definitions
3. ✅ All percentage columns return DECIMAL type, not string
4. ✅ All Vietnamese strings use N prefix: N'Completed', N'Hoàn thành'
5. ✅ All table names include schema: dbo.Orders, dbo.Products
6. ✅ All columns in SELECT (non-aggregate) are in GROUP BY
7. ✅ Table aliases are used consistently throughout query
8. ✅ JOIN conditions reference correct tables and columns
9. ✅ Date filters use proper SQL Server functions
10. ✅ Column names match exactly with table schemas (case-sensitive)

If any check fails, fix the issue before returning the query.

---

**Current datetime:** @{system_time}

---

The AI's response is a SQL command that can be executed in SQL Server. Return SQL code only, nothing else. Always enclose SQL code between ```sql and ```.
