# Product Selection Extraction

Parse the HTML table below. Extract ONLY checked rows (checked="checked"). Output a JSON array.

[additional_data]

**Output format (strict):**
```json
[
  {
    "cartId": null,
    "productId": "18",
    "quantity": 1,
    "extraData": {
      "searchAttributes": [
        {"name": "Memory capacity", "value": "64 GB"},
        {"name": "Color", "value": "Silver"}
      ]
    }
  }
]
```

**CRITICAL: Identify table type first**

**Table Type Detection:**
1. **CART TABLE** - has "Cart ID" column header → checkbox value = cartId, extract productId from product cell
2. **PRODUCT/CATALOG TABLE** - NO "Cart ID" column → checkbox value = productId, cartId = null

**Extraction Rules:**

1. **Include ONLY rows with checkbox checked** (checked="checked").

2. **For PRODUCT/CATALOG TABLES** (no "Cart ID" column):
   - `cartId`: always set to `null` (or omit the field)
   - `productId` (string, required): Extract from first available source:
     * checkbox `value` attribute (example: `<input type="checkbox" value="18">` => `"productId": "18"`)
     * `data-product-id` attribute
     * product text pattern `(ID: N)` in product cell (example: `iPhone Plus (ID: 18)` => `"productId": "18"`)
     * product link/query parameter (`productId`, `id`, or similar)

3. **For CART TABLES** (has "Cart ID" column):
   - `cartId` (string, required): Extract from first available source:
     * checkbox `value` attribute (example: `<input type="checkbox" value="20">` => `"cartId": "20"`)
     * "Cart ID" column cell text in the same row
     * action handler argument (example: `onclick="removeCartItem(20)"` => `"cartId": "20"`)
   - `productId` (string, required): Extract from first available source:
     * `data-product-id` attribute
     * product text pattern `(ID: N)` in product cell (example: `iPhone Plus (ID: 18)` => `"productId": "18"`)
     * product link/query parameter

4. **quantity**: Extract from:
   - quantity input value in row (`<input type="number" value="2">` => `2`)
   - numeric cell value in "Quantity" column
   - default to `1` if not found

5. **extraData.searchAttributes**: Build from product options/attributes in the row:
   - For `<select>` elements with `data-attribute-name="X"`:
     * Find the selected option (`selected="selected"`)
     * Create entry: `{"name": "X", "value": "selected option value"}`
   - For attribute text like `Color: White (+$15.00)`:
     * Parse and create: `{"name": "Color", "value": "White"}`
   - If no attributes found, return empty array `[]`

6. **No checked rows** → return `[]`.

**Examples:**

Product table (no Cart ID column):
```html
<input type="checkbox" value="18" checked="checked">
<td>iPhone Plus</td>
<select name="color" data-attribute-name="Color">
  <option value="Silver" selected="selected">Silver</option>
</select>
```
Result: `{"cartId": null, "productId": "18", "quantity": 1, "extraData": {"searchAttributes": [{"name": "Color", "value": "Silver"}]}}`

Cart table (has Cart ID column):
```html
<input type="checkbox" value="20" checked="checked">
<td>20</td>
<td>iPhone Plus (ID: 18)</td>
```
Result: `{"cartId": "20", "productId": "18", "quantity": 1, "extraData": {"searchAttributes": []}}`

**Return JSON array only.** Enclose between ```json and ```.
