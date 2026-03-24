# Product Selection Extraction

Parse the HTML product table below. Extract ONLY checked rows (checked="checked"). Output a JSON array.

[additional_data]

**Output format (strict):**
```json
[
  {
    "cartId": "20",
    "productId": "17",
    "quantity": 1,
    "extraData": {
      "searchAttributes": [
        {"name": "Color", "value": "Rose"},
        {"name": "Memory capacity", "value": "64 GB"}
      ]
    }
  }
]
```

**Rules:**
1. Include ONLY rows with checkbox checked.
2. `cartId` (string) is required. Extract from first available source in this order:
   - checkbox `value` (example: `<input type="checkbox" value="20" checked="checked">` => `"cartId": "20"`)
   - Cart ID cell text in the same row (column "Cart ID")
   - action handler argument in same row (example: `onclick="removeCartItem(20)"` => `"cartId": "20"`)
3. `productId` (string) is required. Extract from first available source in this order:
   - `data-product-id`
   - product text pattern `(ID: N)` in product cell (example: `AirPods (ID: 17)` => `"productId": "17"`)
   - product link/query id (`productId`, `id`, or similar)
4. `quantity`: from quantity input/value in row; if missing use numeric cell value; default `1`.
5. `extraData.searchAttributes`: build from row attributes:
   - `<select data-attribute-name="X">` + selected option => `{"name":"X","value":"selected"}`
   - or parse attribute text like `Color: White (+$15.00)` => `{"name":"Color","value":"White"}`
   - if none, return empty array `[]`
6. No checked rows -> return `[]`.
7. Never swap `cartId` and `productId`. In cart tables, checkbox `value` is usually `cartId`, not `productId`.

**Return JSON array only.** Enclose between ```json and ```.
