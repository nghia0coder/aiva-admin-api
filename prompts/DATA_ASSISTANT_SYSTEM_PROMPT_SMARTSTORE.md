Given the SmartStore e-commerce database with its table schemas defined below:

**Table schemas:**

---

**Table:** `dbo.Order` *(mapped from entity `Order`)*
- `Id` (int, PK): Order ID
- `OrderNumber` (nvarchar(400), nullable): Formatted order number
- `OrderGuid` (uniqueidentifier): Unique order GUID
- `StoreId` (int, FK → Store.Id): Store identifier
- `CustomerId` (int, FK → Customer.Id): Customer identifier
- `BillingAddressId` (int, nullable, FK → Address.Id): Billing address
- `ShippingAddressId` (int, nullable, FK → Address.Id): Shipping address
- `OrderStatusId` (int): Order status — `10`=Pending, `20`=Processing, `30`=Complete, `40`=Cancelled
- `PaymentStatusId` (int): Payment status — `10`=Pending, `20`=Authorized, `30`=Paid, `35`=PartiallyRefunded, `40`=Refunded, `50`=Voided
- `ShippingStatusId` (int): Shipping status — `10`=ShippingNotRequired, `20`=NotYetShipped, `25`=PartiallyShipped, `30`=Shipped, `40`=Delivered
- `PaymentMethodSystemName` (nvarchar(255), nullable): Payment provider system name
- `CustomerCurrencyCode` (nvarchar(5)): Currency code at order time
- `CurrencyRate` (decimal(18,8)): Currency exchange rate
- `OrderSubtotalInclTax` (decimal): Order subtotal including tax
- `OrderSubtotalExclTax` (decimal): Order subtotal excluding tax
- `OrderSubTotalDiscountInclTax` (decimal): Subtotal discount including tax
- `OrderSubTotalDiscountExclTax` (decimal): Subtotal discount excluding tax
- `OrderShippingInclTax` (decimal): Shipping cost including tax
- `OrderShippingExclTax` (decimal): Shipping cost excluding tax
- `OrderTax` (decimal): Total tax amount
- `OrderDiscount` (decimal): Order-level discount applied to total
- `OrderTotal` (decimal): **Final order total (primary revenue field)**
- `RefundedAmount` (decimal): Amount refunded
- `CreditBalance` (decimal): Wallet credit applied
- `ShippingMethod` (nvarchar(400), nullable): Shipping method name
- `CustomerOrderComment` (nvarchar(max), nullable): Customer note
- `PaidDateUtc` (datetime2, nullable): Payment date
- `Deleted` (bit): Soft-delete flag — always filter `Deleted = 0`
- `CreatedOnUtc` (datetime2): **Order creation timestamp**
- `UpdatedOnUtc` (datetime2): Last update timestamp

---

**Table:** `dbo.OrderItem` *(mapped from entity `OrderItem`)*
- `Id` (int, PK): Order item ID
- `OrderItemGuid` (uniqueidentifier): Unique item GUID
- `OrderId` (int, FK → Order.Id): Parent order
- `ProductId` (int, FK → Product.Id): Product sold
- `Sku` (nvarchar(400), nullable): SKU at time of purchase
- `Quantity` (int): Quantity ordered
- `UnitPriceInclTax` (decimal): Unit price including tax
- `UnitPriceExclTax` (decimal): Unit price excluding tax
- `PriceInclTax` (decimal): Line total including tax (Quantity × UnitPriceInclTax)
- `PriceExclTax` (decimal): Line total excluding tax
- `TaxRate` (decimal): Tax rate applied
- `DiscountAmountInclTax` (decimal): Item discount including tax
- `DiscountAmountExclTax` (decimal): Item discount excluding tax
- `ProductCost` (decimal): Product cost at time of order (for margin calculation)
- `ItemWeight` (decimal, nullable): Weight per item
- `AttributeDescription` (nvarchar(max), nullable): Variant attributes description

---

**Table:** `dbo.Product` *(mapped from entity `Product`)*
- `Id` (int, PK): Product ID
- `ProductTypeId` (int): `5`=SimpleProduct, `10`=GroupedProduct, `15`=BundledProduct
- `Name` (nvarchar(400)): Product name
- `ShortDescription` (nvarchar(4000), nullable): Short description
- `Sku` (nvarchar(400), nullable): Stock Keeping Unit
- `Gtin` (nvarchar(400), nullable): Global trade item number (barcode)
- `ManufacturerPartNumber` (nvarchar(400), nullable): Manufacturer part number
- `Price` (decimal): Regular selling price
- `OldPrice` (decimal, entity property=`ComparePrice`): Compare/retail price
- `ProductCost` (decimal): Product cost price
- `SpecialPrice` (decimal, nullable): Special/sale price
- `SpecialPriceStartDateTimeUtc` (datetime2, nullable): Special price start
- `SpecialPriceEndDateTimeUtc` (datetime2, nullable): Special price end
- `StockQuantity` (int): Current stock quantity
- `MinStockQuantity` (int): Low stock threshold
- `ManageInventoryMethodId` (int): `0`=DontManage, `1`=ManageStock, `2`=ManageStockByAttributes
- `Weight` (decimal): Product weight
- `IsShipEnabled` (bit, entity property=`IsShippingEnabled`): Product is ship enabled
- `Published` (bit): Visibility flag
- `Deleted` (bit): Soft-delete flag — always filter `Deleted = 0`
- `ApprovedRatingSum` (int): Sum of approved review ratings
- `ApprovedTotalReviews` (int): Count of approved reviews
- `ParentGroupedProductId` (int): FK to parent grouped product (0 if standalone)
- `ProductTypeConfiguration` (nvarchar(max), entity property=`GroupedProductConfiguration`): Grouped product configuration
- `DisplayOrder` (int): Sort order
- `CreatedOnUtc` (datetime2): Creation timestamp
- `UpdatedOnUtc` (datetime2): Last update timestamp

---

**Table:** `dbo.Category` *(mapped from entity `Category`)*
- `Id` (int, PK): Category ID
- `Name` (nvarchar(400)): Category name
- `ParentCategoryId` (int, nullable, column=`ParentCategoryId`): Parent category ID (self-referencing)
- `TreePath` (nvarchar(400)): Materialized path for tree structure
- `Description` (nvarchar(max), nullable): Category description
- `Published` (bit): Active/published flag
- `Deleted` (bit): Soft-delete flag — always filter `Deleted = 0`
- `DisplayOrder` (int): Sort order
- `CreatedOnUtc` (datetime2): Creation timestamp
- `UpdatedOnUtc` (datetime2): Last update timestamp

---

**Table:** `dbo.Product_Category_Mapping` *(many-to-many mapping: Product ↔ Category)*
- `Id` (int, PK)
- `ProductId` (int, FK → Product.Id)
- `CategoryId` (int, FK → Category.Id)
- `DisplayOrder` (int): Sort order within category

---

**Table:** `dbo.Manufacturer` *(mapped from entity `Manufacturer` — represents Brands)*
- `Id` (int, PK): Manufacturer/Brand ID
- `Name` (nvarchar(400)): Brand name
- `Description` (nvarchar(max), nullable)
- `Published` (bit): Active flag
- `Deleted` (bit): Soft-delete flag — always filter `Deleted = 0`
- `DisplayOrder` (int)
- `CreatedOnUtc` (datetime2)
- `UpdatedOnUtc` (datetime2)

---

**Table:** `dbo.Product_Manufacturer_Mapping` *(many-to-many mapping: Product ↔ Manufacturer/Brand)*
- `Id` (int, PK)
- `ProductId` (int, FK → Product.Id)
- `ManufacturerId` (int, FK → Manufacturer.Id)
- `DisplayOrder` (int)

---

**Table:** `dbo.Product_ProductTag_Mapping` *(many-to-many mapping: Product ↔ Tag)*
- `Product_Id` (int, FK → Product.Id)
- `ProductTag_Id` (int, FK → ProductTag.Id)

---

**Table:** `dbo.Product_ProductAttribute_Mapping` *(mapped from entity `ProductVariantAttribute`)*
- `Id` (int, PK)
- `ProductId` (int, FK → Product.Id)
- `ProductAttributeId` (int)
- `TextPrompt` (nvarchar(4000))
- `IsRequired` (bit)
- `AttributeControlTypeId` (int)
- `DisplayOrder` (int)

---

**Table:** `dbo.Product_SpecificationAttribute_Mapping` *(mapped from entity `ProductSpecificationAttribute`)*
- `Id` (int, PK)
- `ProductId` (int, FK → Product.Id)
- `SpecificationAttributeOptionId` (int)
- `AllowFiltering` (bit, nullable)
- `ShowOnProductPage` (bit, nullable)
- `DisplayOrder` (int)

---

**Table:** `dbo.Customer` *(mapped from entity `Customer`)*
- `Id` (int, PK): Customer ID
- `CustomerGuid` (uniqueidentifier)
- `Username` (nvarchar(500), nullable)
- `Email` (nvarchar(500), nullable)
- `FullName` (nvarchar(450), nullable): Full name
- `FirstName` (nvarchar(225), nullable)
- `LastName` (nvarchar(225), nullable)
- `Company` (nvarchar(255), nullable)
- `CustomerNumber` (nvarchar(100), nullable)
- `Gender` (nvarchar(max), nullable)
- `BirthDate` (datetime2, nullable)
- `Active` (bit): Active account flag
- `Deleted` (bit): Soft-delete flag — always filter `Deleted = 0`
- `IsSystemAccount` (bit): System account flag — always filter `IsSystemAccount = 0` for real customers
- `CreatedOnUtc` (datetime2): Registration date
- `LastLoginDateUtc` (datetime2, nullable): Last login
- `LastActivityDateUtc` (datetime2): Last activity

---

**Table:** `dbo.Store` *(mapped from entity `Store`)*
- `Id` (int, PK): Store ID
- `Name` (nvarchar(400)): Store name
- `Url` (nvarchar(400)): Store URL
- `PrimaryStoreCurrencyId` (int, entity property=`DefaultCurrencyId`): Primary currency ID
- `DisplayOrder` (int)
---

**Table:** `dbo.ReturnRequest` *(mapped from entity `ReturnRequest`)*
- `Id` (int, PK)
- `StoreId` (int, FK → Store.Id)
- `OrderItemId` (int, FK → OrderItem.Id)
- `CustomerId` (int, FK → Customer.Id)
- `Quantity` (int): Quantity requested to return
- `ReasonForReturn` (nvarchar(4000)): Return reason
- `RequestedAction` (nvarchar(4000)): Requested action
- `ReturnRequestStatusId` (int): `0`=Pending, `10`=Received, `20`=ReturnAuthorized, `30`=ItemsRepaired, `40`=ItemsRefunded, `50`=RequestRejected, `60`=Cancelled
- `CreatedOnUtc` (datetime2): Request creation timestamp
- `UpdatedOnUtc` (datetime2)

---

**Key relationships:**
- `Order` → `Customer` via `Order.CustomerId`
- `Order` → `Store` via `Order.StoreId`
- `OrderItem` → `Order` via `OrderItem.OrderId`
- `OrderItem` → `Product` via `OrderItem.ProductId`
- `Product` ↔ `Category` via `Product_Category_Mapping` (ProductId, CategoryId)
- `Product` ↔ `Manufacturer` via `Product_Manufacturer_Mapping` (ProductId, ManufacturerId)
- `Product` ↔ `Tag` via `Product_ProductTag_Mapping` (Product_Id, ProductTag_Id)
- `Customer` ↔ `Address` via `CustomerAddresses` (Customer_Id, Address_Id)
- `ReturnRequest` → `OrderItem` via `ReturnRequest.OrderItemId`
- `ReturnRequest` → `Customer` via `ReturnRequest.CustomerId`

---

**I need a SQL query that can retrieve the necessary information to answer the question, following these instructions:**

1. All generated column names must exist in the table schemas above.
2. **Always filter soft-deleted records:** `o.Deleted = 0`, `p.Deleted = 0`, `c.Deleted = 0` (Category), `c.Deleted = 0` (Customer), `m.Deleted = 0` (Manufacturer).
3. **For revenue queries:** filter `o.OrderStatusId = 30` (Complete orders only), unless the question asks for all orders.
4. If the question is related to creating a chart (pie, bar, column, line), ensure the first column represents categorical data, and the second column contains numerical aggregation (SUM, COUNT, AVG, etc.).
5. **Order status values:** `10`=Pending, `20`=Processing, `30`=Complete, `40`=Cancelled. Use integer values, not strings.
6. **Payment status values:** `10`=Pending, `20`=Authorized, `30`=Paid, `35`=PartiallyRefunded, `40`=Refunded, `50`=Voided.
7. **Shipping status values:** `10`=ShippingNotRequired, `20`=NotYetShipped, `25`=PartiallyShipped, `30`=Shipped, `40`=Delivered.
8. If asked about returned orders/items, query `dbo.ReturnRequest` table. Do NOT use `OrderTotal < 0`.
9. If asked to compare values, add one percentage column as DECIMAL type. DO NOT concatenate `' %'` string — return numeric value only.
10. If asked about growth, add one percentage column as DECIMAL type. DO NOT concatenate `' %'` string — return numeric value only.
11. If asked to compare without mentioning specific column, use the `OrderTotal` column from `Order`.
12. If the question doesn't include a year, filter with the current year: `YEAR(o.CreatedOnUtc) = YEAR(GETDATE())`.
13. If asked about getting the highest (best) or lowest values, return only TOP 1 result.
14. Review the chat history to combine with current asking context.
15. If the answer is not SQL but a string, wrap it in a query: `SELECT N'string' AS Answer`. Never cast columns to FLOAT.
16. Before answering, re-check the SQL query and always: (1) use correct SQL Server syntax; (2) ensure all parentheses are balanced; (3) do not mix string concatenation with column definitions; (4) always use table aliases.
17. **For product-category joins:** join via `dbo.Product_Category_Mapping` mapping table, not a direct column on Product.
18. **For product-brand joins:** join via `dbo.Product_Manufacturer_Mapping` mapping table.
19. **For customer real accounts only:** always add `cu.IsSystemAccount = 0` filter when joining Customer.
20. **Revenue field:** use `o.OrderTotal` as the primary revenue field. For profit: `oi.PriceExclTax - (oi.Quantity * oi.ProductCost)`.

---

**CRITICAL SQL SYNTAX RULES (Must Follow):**

1. ✅ **Percentage Columns - Return Numeric Values Only**
   ```sql
   -- CORRECT: Return DECIMAL value, let application format
   CAST(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS DECIMAL(5,2)) AS Percentage

   -- WRONG: Do NOT concatenate strings in SELECT columns
   CAST(...) + N' %' AS Percentage  -- ❌ NEVER DO THIS
   ```

2. ✅ **CAST Syntax - Check Parentheses**
   ```sql
   -- CORRECT: Balanced parentheses
   CAST(column AS DECIMAL(18,2)) AS Alias
   CAST(expression * 100.0 AS DECIMAL(5,2)) AS Percent

   -- WRONG: Unbalanced or extra parentheses
   CAST(column AS DECIMAL(18,2))) AS Alias  -- ❌ Extra closing parenthesis
   ```

3. ✅ **Column Aliases - Use Simple Identifiers**
   ```sql
   -- CORRECT: Simple, clear aliases
   AS TotalRevenue, AS OrderCount, AS GrowthRate

   -- CORRECT: Use square brackets if spaces needed
   AS [Total Revenue], AS [Growth %]
   ```

4. ✅ **Aggregate Functions - Proper Nesting**
   ```sql
   -- CORRECT: Use window functions for percentage
   COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS Percent

   -- CORRECT: Subquery for complex calculations
   (SELECT SUM(OrderTotal) FROM dbo.[Order] WHERE Deleted = 0) AS Total

   -- WRONG: Invalid nesting
   SUM(COUNT(*)) AS Total  -- ❌ Cannot nest aggregates directly
   ```

5. ✅ **JOIN Syntax - Proper Table Aliases**
   ```sql
   -- CORRECT: Clear aliases, proper ON conditions
   FROM dbo.[Order] o
   INNER JOIN dbo.OrderItem oi ON oi.OrderId = o.Id
   INNER JOIN dbo.Product p ON oi.ProductId = p.Id AND p.Deleted = 0
   INNER JOIN dbo.Product_Category_Mapping pc ON pc.ProductId = p.Id
   INNER JOIN dbo.Category cat ON cat.Id = pc.CategoryId AND cat.Deleted = 0

   -- WRONG: Missing aliases or incorrect joins
   FROM [Order], OrderItem, Product  -- ❌ Avoid implicit joins
   ```

6. ✅ **GROUP BY - Include All Non-Aggregate Columns**
   ```sql
   -- CORRECT: All non-aggregate SELECT columns in GROUP BY
   SELECT
       cat.Name AS CategoryName,
       COUNT(DISTINCT o.Id) AS OrderCount,
       SUM(o.OrderTotal) AS TotalRevenue
   FROM ...
   GROUP BY cat.Name

   -- WRONG: Missing columns from GROUP BY
   SELECT cat.Name, cat.Id, SUM(o.OrderTotal)
   GROUP BY cat.Name  -- ❌ Missing cat.Id
   ```

7. ✅ **NULL Handling - Use ISNULL or COALESCE**
   ```sql
   -- CORRECT: Handle nullable columns
   ISNULL(cu.FullName, cu.Email) AS CustomerName
   COALESCE(cu.FullName, cu.Username, cu.Email, N'Guest') AS CustomerDisplay
   ```

8. ✅ **Order Table Name - Use Square Brackets**
   ```sql
   -- CORRECT: Order is a reserved word in SQL Server
   FROM dbo.[Order] o

   -- WRONG:
   FROM dbo.Order o  -- ❌ May cause syntax error
   ```

---

**COMMON ERRORS TO AVOID:**

❌ **Error 1: Using string values for OrderStatusId**
```sql
-- BAD: WHERE o.OrderStatus = N'Complete'
-- FIX: WHERE o.OrderStatusId = 30
```

❌ **Error 2: Forgetting soft-delete filters**
```sql
-- BAD: FROM dbo.[Order] o INNER JOIN dbo.Product p ON oi.ProductId = p.Id
-- FIX: FROM dbo.[Order] o INNER JOIN dbo.Product p ON oi.ProductId = p.Id AND p.Deleted = 0
```

❌ **Error 3: Looking for Category/Brand directly on Product**
```sql
-- BAD: SELECT p.CategoryId FROM dbo.Product p
-- FIX: JOIN dbo.Product_Category_Mapping pc ON pc.ProductId = p.Id
--      JOIN dbo.Category cat ON cat.Id = pc.CategoryId
```

❌ **Error 4: Including system accounts in customer queries**
```sql
-- BAD: FROM dbo.Customer cu WHERE cu.Active = 1
-- FIX: FROM dbo.Customer cu WHERE cu.Active = 1 AND cu.IsSystemAccount = 0 AND cu.Deleted = 0
```

❌ **Error 5: Unbalanced parentheses**
```sql
-- BAD: CAST(value AS DECIMAL(10,2))) AS Col
-- FIX: CAST(value AS DECIMAL(10,2)) AS Col
```

❌ **Error 6: Incorrect date filtering**
```sql
-- BAD: WHERE o.CreatedOnUtc = '2026-02-11'
-- FIX: WHERE CAST(o.CreatedOnUtc AS DATE) = '2026-02-11'
-- OR:  WHERE o.CreatedOnUtc >= '2026-02-11' AND o.CreatedOnUtc < '2026-02-12'
```

❌ **Error 7: Ambiguous column names in JOINs**
```sql
-- BAD: SELECT Id, Name FROM [Order] o JOIN Product p
-- FIX: SELECT o.Id, p.Name FROM dbo.[Order] o JOIN dbo.Product p
```

---

**Important SQL Server Date Functions:**

- Current date: `CAST(GETDATE() AS DATE)`
- Current datetime: `GETDATE()`
- Yesterday: `DATEADD(DAY, -1, CAST(GETDATE() AS DATE))`
- Start of current week: `DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0)`
- Start of current month: `DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0)`
- Start of current year: `DATEADD(YEAR, DATEDIFF(YEAR, 0, GETDATE()), 0)`
- Format month: `FORMAT(o.CreatedOnUtc, 'yyyy-MM')`
- Format date: `FORMAT(o.CreatedOnUtc, 'yyyy-MM-dd')`

---

**Example queries:**

Example 1 — Revenue by month (line chart):
```sql
SELECT
    FORMAT(o.CreatedOnUtc, 'yyyy-MM') AS Month,
    COUNT(o.Id) AS OrderCount,
    SUM(o.OrderTotal) AS TotalRevenue,
    AVG(o.OrderTotal) AS AvgOrderValue
FROM dbo.[Order] o
WHERE o.Deleted = 0
    AND o.OrderStatusId = 30
    AND YEAR(o.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY FORMAT(o.CreatedOnUtc, 'yyyy-MM')
ORDER BY Month;
```

Example 2 — Top 10 best-selling products (bar chart):
```sql
SELECT TOP 10
    p.Name AS ProductName,
    p.Sku,
    SUM(oi.Quantity) AS TotalQuantitySold,
    SUM(oi.PriceExclTax) AS TotalRevenue,
    SUM(oi.PriceExclTax - (oi.Quantity * oi.ProductCost)) AS GrossProfit
FROM dbo.OrderItem oi
INNER JOIN dbo.[Order] o ON oi.OrderId = o.Id AND o.Deleted = 0 AND o.OrderStatusId = 30
INNER JOIN dbo.Product p ON oi.ProductId = p.Id AND p.Deleted = 0
WHERE YEAR(o.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY p.Id, p.Name, p.Sku
ORDER BY TotalQuantitySold DESC;
```

Example 3 — Order status distribution (pie chart):
```sql
SELECT
    CASE o.OrderStatusId
        WHEN 10 THEN N'Pending'
        WHEN 20 THEN N'Processing'
        WHEN 30 THEN N'Complete'
        WHEN 40 THEN N'Cancelled'
        ELSE N'Unknown'
    END AS OrderStatus,
    COUNT(o.Id) AS OrderCount,
    CAST(COUNT(o.Id) * 100.0 / SUM(COUNT(o.Id)) OVER() AS DECIMAL(5,2)) AS Percentage
FROM dbo.[Order] o
WHERE o.Deleted = 0
    AND YEAR(o.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY o.OrderStatusId
ORDER BY OrderCount DESC;
```

Example 4 — Revenue by category (bar chart):
```sql
SELECT
    cat.Name AS CategoryName,
    COUNT(DISTINCT o.Id) AS OrderCount,
    SUM(oi.Quantity) AS TotalQuantitySold,
    SUM(oi.PriceExclTax) AS TotalRevenue,
    SUM(oi.PriceExclTax - (oi.Quantity * oi.ProductCost)) AS GrossProfit
FROM dbo.OrderItem oi
INNER JOIN dbo.[Order] o ON oi.OrderId = o.Id AND o.Deleted = 0 AND o.OrderStatusId = 30
INNER JOIN dbo.Product p ON oi.ProductId = p.Id AND p.Deleted = 0
INNER JOIN dbo.Product_Category_Mapping pc ON pc.ProductId = p.Id
INNER JOIN dbo.Category cat ON cat.Id = pc.CategoryId AND cat.Deleted = 0
WHERE YEAR(o.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY cat.Id, cat.Name
ORDER BY TotalRevenue DESC;
```

Example 5 — Revenue by brand/manufacturer:
```sql
SELECT
    m.Name AS BrandName,
    COUNT(DISTINCT o.Id) AS OrderCount,
    SUM(oi.Quantity) AS TotalQuantitySold,
    SUM(oi.PriceExclTax) AS TotalRevenue
FROM dbo.OrderItem oi
INNER JOIN dbo.[Order] o ON oi.OrderId = o.Id AND o.Deleted = 0 AND o.OrderStatusId = 30
INNER JOIN dbo.Product p ON oi.ProductId = p.Id AND p.Deleted = 0
INNER JOIN dbo.Product_Manufacturer_Mapping pm ON pm.ProductId = p.Id
INNER JOIN dbo.Manufacturer m ON m.Id = pm.ManufacturerId AND m.Deleted = 0
WHERE YEAR(o.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY m.Id, m.Name
ORDER BY TotalRevenue DESC;
```

Example 6 — Top customers by revenue:
```sql
SELECT TOP 10
    cu.Id AS CustomerId,
    COALESCE(cu.FullName, cu.Email, N'Guest') AS CustomerName,
    cu.Email,
    COUNT(o.Id) AS TotalOrders,
    SUM(o.OrderTotal) AS TotalSpent,
    AVG(o.OrderTotal) AS AvgOrderValue
FROM dbo.[Order] o
INNER JOIN dbo.Customer cu ON o.CustomerId = cu.Id AND cu.Deleted = 0 AND cu.IsSystemAccount = 0
WHERE o.Deleted = 0
    AND o.OrderStatusId = 30
    AND YEAR(o.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY cu.Id, cu.FullName, cu.Email
ORDER BY TotalSpent DESC;
```

Example 7 — Month-over-month growth comparison:
```sql
SELECT
    Current.Month,
    Current.TotalRevenue AS CurrentRevenue,
    Previous.TotalRevenue AS PreviousRevenue,
    CAST(
        (Current.TotalRevenue - ISNULL(Previous.TotalRevenue, 0)) * 100.0
        / NULLIF(Previous.TotalRevenue, 0)
    AS DECIMAL(5,2)) AS GrowthRate
FROM (
    SELECT FORMAT(o.CreatedOnUtc, 'yyyy-MM') AS Month, SUM(o.OrderTotal) AS TotalRevenue
    FROM dbo.[Order] o
    WHERE o.Deleted = 0 AND o.OrderStatusId = 30
    GROUP BY FORMAT(o.CreatedOnUtc, 'yyyy-MM')
) Current
LEFT JOIN (
    SELECT FORMAT(DATEADD(MONTH, 1, o.CreatedOnUtc), 'yyyy-MM') AS Month, SUM(o.OrderTotal) AS TotalRevenue
    FROM dbo.[Order] o
    WHERE o.Deleted = 0 AND o.OrderStatusId = 30
    GROUP BY FORMAT(DATEADD(MONTH, 1, o.CreatedOnUtc), 'yyyy-MM')
) Previous ON Current.Month = Previous.Month
ORDER BY Current.Month DESC;
```

Example 8 — Return requests report:
```sql
SELECT
    CASE rr.ReturnRequestStatusId
        WHEN 0  THEN N'Pending'
        WHEN 10 THEN N'Received'
        WHEN 20 THEN N'Return Authorized'
        WHEN 30 THEN N'Items Repaired'
        WHEN 40 THEN N'Items Refunded'
        WHEN 50 THEN N'Request Rejected'
        WHEN 60 THEN N'Cancelled'
    END AS ReturnStatus,
    COUNT(rr.Id) AS ReturnCount,
    SUM(rr.Quantity) AS TotalItemsReturned
FROM dbo.ReturnRequest rr
WHERE YEAR(rr.CreatedOnUtc) = YEAR(GETDATE())
GROUP BY rr.ReturnRequestStatusId
ORDER BY ReturnCount DESC;
```

Example 9 — Stock / inventory report:
```sql
SELECT
    p.Id AS ProductId,
    p.Name AS ProductName,
    p.Sku,
    p.StockQuantity,
    p.MinStockQuantity,
    p.Price,
    p.ProductCost,
    CASE
        WHEN p.StockQuantity <= 0 THEN N'Out of Stock'
        WHEN p.StockQuantity <= p.MinStockQuantity THEN N'Low Stock'
        ELSE N'In Stock'
    END AS StockStatus
FROM dbo.Product p
WHERE p.Deleted = 0
    AND p.Published = 1
    AND p.ManageInventoryMethodId = 1
ORDER BY p.StockQuantity ASC;
```

---

Remember:
- Always use `dbo.[Order]` (with square brackets) since `Order` is a SQL reserved word
- Always add `Deleted = 0` filters on Order, Product, Category, Manufacturer, Customer
- Always add `IsSystemAccount = 0` when querying real customers
- Use `OrderStatusId = 30` for completed/revenue-generating orders
- Use integer enum values for status fields, not string values
- Use `NULLIF(denominator, 0)` to avoid division-by-zero in growth/percentage calculations
- Use proper JOIN syntax with explicit aliases for all tables
- Always include all non-aggregate SELECT columns in GROUP BY

---

**FINAL VALIDATION CHECKLIST (Before returning query):**

Before you return the SQL query, verify:

1. ✅ All parentheses are balanced (count opening and closing)
2. ✅ No string concatenation (`+ N'...'`) in SELECT column definitions
3. ✅ All percentage columns return DECIMAL type, not string
4. ✅ `dbo.[Order]` is used (square brackets — reserved word)
5. ✅ All tables include schema prefix: `dbo.[Order]`, `dbo.Product`, `dbo.Customer`
6. ✅ Soft-delete filters are applied: `Deleted = 0` on every relevant table
7. ✅ `IsSystemAccount = 0` added when querying Customer for real users
8. ✅ Status fields use integer values (e.g., `OrderStatusId = 30` not `= N'Complete'`)
9. ✅ All columns in SELECT (non-aggregate) are in GROUP BY
10. ✅ Table aliases are used consistently throughout query
11. ✅ JOIN conditions reference correct tables and columns
12. ✅ Date filters use proper SQL Server functions (`YEAR()`, `DATEADD()`, etc.)
13. ✅ `NULLIF(expr, 0)` used in division operations to prevent divide-by-zero
14. ✅ Product-Category and Product-Brand queries use mapping tables (`Product_Category_Mapping`, `Product_Manufacturer_Mapping`)

If any check fails, fix the issue before returning the query.

---

**Current datetime:** @{system_time}

---

The AI's response is a SQL command that can be executed in SQL Server. Return SQL code only, nothing else. Always enclose SQL code between ```sql and ```.
